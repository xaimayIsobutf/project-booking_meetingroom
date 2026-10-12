using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using RoomReservation.Application;
using RoomReservation.Domain.Entities;
using RoomReservation.Domain.Enums;
using RoomReservation.Infrastructure.Persistence;

namespace RoomReservation.Web.Controllers;

[ApiController, Route("api/v1/room-display")]
public sealed class RoomDisplayPairingController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private string Secret => config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
    private Guid Org => Guid.Parse(User.FindFirstValue("organization_id")!);

    [HttpPost("pair/start"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> StartPair([FromBody] StartPairRequest input, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.RoomId && x.OrganizationId == Org && x.IsActive, cancellationToken);
        if (room is null) return NotFound("Room not found or inactive");
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        var device = new RoomDisplayDevice { OrganizationId = Org, RoomId = room.Id, DisplayName = string.IsNullOrWhiteSpace(input.DisplayName) ? $"{room.Name} display" : input.DisplayName.Trim(), DisplayTokenHash = "pending", PairingCodeHash = DisplaySessionToken.Hash(code), PairingExpiresAt = DateTime.UtcNow.AddMinutes(5) };
        db.RoomDisplayDevices.Add(device);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { deviceId = device.Id, organizationId = Org, roomId = room.Id, roomName = room.Name, pairingCode = code, expiresAt = device.PairingExpiresAt, pairingUrl = $"/RoomDisplay/Pair?deviceId={device.Id}&organizationId={Org}" });
    }

    [HttpPost("pair/complete"), AllowAnonymous]
    public async Task<IActionResult> CompletePair([FromBody] CompletePairRequest input, CancellationToken cancellationToken)
    {
        if (input.DeviceId == Guid.Empty || input.OrganizationId == Guid.Empty || string.IsNullOrWhiteSpace(input.PairingCode)) return BadRequest("Device, organization and pairing code are required");
        var codeHash = DisplaySessionToken.Hash(input.PairingCode.Trim().ToUpperInvariant());
        var transaction = db.Database.CurrentTransaction;
        var ownsTransaction = transaction is null;
        if (ownsTransaction) transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("select set_config('app.current_org_id', {0}, true)", input.OrganizationId.ToString());
            var device = await db.RoomDisplayDevices.FirstOrDefaultAsync(x => x.Id == input.DeviceId && x.OrganizationId == input.OrganizationId && x.IsActive && x.RevokedAt == null, cancellationToken);
            if (device is null || device.PairingExpiresAt <= DateTime.UtcNow || device.PairingCodeHash != codeHash) return BadRequest("Pairing code is invalid or expired");
            return await FinishPairing(device, ownsTransaction, transaction, cancellationToken);
        }
        finally
        {
            if (ownsTransaction && transaction is not null) await transaction.DisposeAsync();
        }
    }

    [HttpPost("pair/complete-by-code"), AllowAnonymous]
    public async Task<IActionResult> CompletePairByCode([FromBody] PairByCodeRequest input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.PairingCode)) return BadRequest("Pairing code is required");

        var codeHash = DisplaySessionToken.Hash(input.PairingCode.Trim().ToUpperInvariant());
        var transaction = db.Database.CurrentTransaction;
        var ownsTransaction = transaction is null;
        if (ownsTransaction) transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // This is the only cross-tenant lookup. The database function returns
            // identifiers for one valid code, never display secrets or room data.
            var connection = db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "select organization_id from find_room_display_pairing(@pairing_code_hash) limit 1";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "pairing_code_hash";
            parameter.Value = codeHash;
            command.Parameters.Add(parameter);
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(cancellationToken);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is not Guid organizationId) return BadRequest("Pairing code is invalid or expired");

            await db.Database.ExecuteSqlRawAsync("select set_config('app.current_org_id', {0}, true)", organizationId.ToString());
            var device = await db.RoomDisplayDevices.FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.PairingCodeHash == codeHash &&
                     x.IsActive && x.RevokedAt == null && x.PairingExpiresAt > DateTime.UtcNow,
                cancellationToken);
            if (device is null) return BadRequest("Pairing code is invalid or expired");
            return await FinishPairing(device, ownsTransaction, transaction, cancellationToken);
        }
        finally
        {
            if (ownsTransaction && transaction is not null) await transaction.DisposeAsync();
        }
    }

    private async Task<IActionResult> FinishPairing(RoomDisplayDevice device, bool ownsTransaction, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        var displayToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        device.DisplayTokenHash = DisplaySessionToken.Hash(displayToken);
        device.PairingCodeHash = null;
        device.PairingExpiresAt = null;
        device.PairedAt = DateTime.UtcNow;
        device.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (ownsTransaction && transaction is not null) await transaction.CommitAsync(cancellationToken);

        var expires = DateTime.UtcNow.AddDays(30);
        var sessionToken = DisplaySessionToken.Create(new DisplaySession(device.Id, device.OrganizationId, displayToken, expires), Secret);
        // Browser clients cannot read or forward HttpOnly cookies from a cross-origin
        // Flutter Web request. They receive the same session in the body instead.
        if (!Request.Headers.ContainsKey("Origin"))
            Response.Cookies.Append(DisplaySessionToken.CookieName, sessionToken, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, Expires = expires });
        return Ok(new { paired = true, deviceId = device.Id, roomId = device.RoomId, displaySession = sessionToken });
    }

    [HttpGet("devices"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Devices(Guid? roomId, CancellationToken cancellationToken)
    {
        var query = db.RoomDisplayDevices.AsNoTracking().Where(x => x.OrganizationId == Org);
        if (roomId.HasValue) query = query.Where(x => x.RoomId == roomId.Value);
        var rooms = await db.Rooms.AsNoTracking().Where(x => x.OrganizationId == Org).ToDictionaryAsync(x => x.Id, cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return Ok(rows.Select(x => new { x.Id, x.RoomId, RoomName = rooms.TryGetValue(x.RoomId, out var room) ? room.Name : "", x.DisplayName, x.CreatedAt, x.PairedAt, x.LastSeenAt, x.RevokedAt, x.IsActive, online = x.IsActive && x.RevokedAt == null && x.LastSeenAt >= DateTime.UtcNow.AddMinutes(-2) }));
    }

    [HttpPost("devices/{id:guid}/revoke"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var device = await db.RoomDisplayDevices.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org, cancellationToken);
        if (device is null) return NotFound();
        device.RevokedAt = DateTime.UtcNow; device.IsActive = false; device.PairingCodeHash = null; device.PairingExpiresAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { revoked = true });
    }

    [HttpDelete("devices/{id:guid}"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        // Permanent deletion is intentionally limited to devices that have already
        // been unpaired/revoked, and remains scoped to the current organization.
        var device = await db.RoomDisplayDevices.FirstOrDefaultAsync(
            x => x.Id == id && x.OrganizationId == Org && x.RevokedAt != null,
            cancellationToken);
        if (device is null) return NotFound();

        db.RoomDisplayDevices.Remove(device);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("devices/{id:guid}/regenerate"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Regenerate(Guid id, CancellationToken cancellationToken)
    {
        var device = await db.RoomDisplayDevices.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org, cancellationToken);
        if (device is null) return NotFound();
        var roomName = await db.Rooms.Where(x => x.Id == device.RoomId && x.OrganizationId == Org).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken) ?? "";
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        device.RevokedAt = null; device.IsActive = true; device.PairedAt = null; device.DisplayTokenHash = "pending"; device.PairingCodeHash = DisplaySessionToken.Hash(code); device.PairingExpiresAt = DateTime.UtcNow.AddMinutes(5);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { deviceId = device.Id, organizationId = device.OrganizationId, roomId = device.RoomId, displayName = device.DisplayName, roomName, pairingCode = code, expiresAt = device.PairingExpiresAt, pairingUrl = $"/RoomDisplay/Pair?deviceId={device.Id}&organizationId={device.OrganizationId}" });
    }

    [HttpGet("me"), AllowAnonymous]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var device = await CurrentDevice(cancellationToken);
        if (device is null) return Unauthorized(new { paired = false });
        device.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == device.RoomId && x.OrganizationId == device.OrganizationId && x.IsActive, cancellationToken);
        if (room is null) return NotFound(new { message = "Room is inactive or unavailable" });
        var now = DateTime.UtcNow;
        var bookings = await db.Bookings.AsNoTracking().Where(x => x.OrganizationId == device.OrganizationId && x.RoomId == room.Id && x.EndAtUtc >= now && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow).OrderBy(x => x.StartAtUtc).Take(8).ToListAsync(cancellationToken);
        var qr = await CurrentQr(device.OrganizationId, room.Id, cancellationToken);
        var current = bookings.FirstOrDefault(x => x.StartAtUtc <= now && x.EndAtUtc > now);
        var next = bookings.FirstOrDefault(x => x.StartAtUtc > now);
        var upcoming = bookings.Where(x => x.StartAtUtc > now).Take(5).Select(x => new
        {
            x.Id,
            x.Title,
            x.StartAtUtc,
            x.EndAtUtc,
            Status = x.Status.ToString(),
            x.AttendeeCount
        });
        return Ok(new { paired = true, device = new { device.Id, device.DisplayName, device.RoomId, device.LastSeenAt }, room = new { room.Id, room.Name, room.Building, room.Floor, room.Capacity, room.EquipmentTags }, current, next, upcoming, qr = new { token = qr.Token, expiresAt = qr.ExpiresAt }, serverTimeUtc = now });
    }

    [HttpGet("me/qr.png"), AllowAnonymous]
    public async Task<IActionResult> QrImage(CancellationToken cancellationToken)
    {
        var device = await CurrentDevice(cancellationToken);
        if (device is null) return Unauthorized(new { paired = false });
        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == device.RoomId && x.OrganizationId == device.OrganizationId && x.IsActive, cancellationToken);
        if (room is null) return NotFound(new { message = "Room is inactive or unavailable" });
        var qr = await CurrentQr(device.OrganizationId, room.Id, cancellationToken);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(qr.Token, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(12);
        return File(png, "image/png");
    }

    [HttpPost("rooms/{roomId:guid}/checkin-code/regenerate"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> RegenerateCheckInCode(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(x => x.Id == roomId && x.OrganizationId == Org, cancellationToken);
        if (room is null) return NotFound();

        room.StaticCheckInCode = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { roomId = room.Id, token = room.StaticCheckInCode, isStatic = true });
    }

    private async Task<RoomDisplayDevice?> CurrentDevice(CancellationToken cancellationToken)
    {
        var sessionValue = Request.Cookies[DisplaySessionToken.CookieName]
            ?? Request.Headers["X-Display-Session"].FirstOrDefault();
        if (!DisplaySessionToken.TryRead(sessionValue, Secret, out var session)) return null;
        var device = await db.RoomDisplayDevices.FirstOrDefaultAsync(x => x.Id == session.DeviceId && x.OrganizationId == session.OrganizationId && x.IsActive && x.RevokedAt == null && x.PairedAt != null && x.DisplayTokenHash == DisplaySessionToken.Hash(session.DisplayToken), cancellationToken);
        return device;
    }

    private async Task<(string Token, DateTime? ExpiresAt)> CurrentQr(Guid organizationId, Guid roomId, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(x => x.Id == roomId && x.OrganizationId == organizationId && x.IsActive, cancellationToken);
        if (room is null) throw new InvalidOperationException("Room is inactive or unavailable");
        return (room.StaticCheckInCode, null);
    }

    public sealed record StartPairRequest(Guid RoomId, string? DisplayName);
    public sealed record CompletePairRequest(Guid DeviceId, Guid OrganizationId, string PairingCode);
    public sealed record PairByCodeRequest(string PairingCode);
}
