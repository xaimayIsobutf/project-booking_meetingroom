using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using RoomReservation.Domain.Entities;
using RoomReservation.Domain.Enums;
using RoomReservation.Infrastructure.Persistence;

namespace RoomReservation.Web.Controllers;

[ApiController, Authorize(Roles = "Employee,OrgAdmin,SuperAdmin"), Route("api/v1/checkin")]
public class CheckInController(AppDbContext db, RoomReservation.Api.Hubs.ReservationEvents events) : ControllerBase
{
    private Guid Org => Guid.Parse(User.FindFirstValue("organization_id")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ScanInput input, CancellationToken cancellationToken)
    {
        var room = await FindCheckInRoom(input.QrToken, cancellationToken);
        if (room is null) return BadRequest("QR code is invalid or the room is unavailable");
        var booking = await FindCurrentBooking(room.Id, cancellationToken);
        if (booking is null) return NotFound("No active booking for this user");
        if (input.BookingId.HasValue && input.BookingId.Value != booking.Id) return Conflict("The selected booking does not match the room or current user.");
        return Ok(new { valid = true, bookingId = booking.Id, roomId = booking.RoomId, room = new { room.Id, room.Name, room.Building, room.Floor, room.Capacity }, title = booking.Title, attendee_count = booking.AttendeeCount, start_at_utc = booking.StartAtUtc, end_at_utc = booking.EndAtUtc });
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanInput input, CancellationToken cancellationToken)
    {
        var room = await FindCheckInRoom(input.QrToken, cancellationToken);
        if (room is null) return BadRequest("QR code is invalid or the room is unavailable");
        var booking = await FindCurrentBooking(room.Id, cancellationToken);
        if (booking is null) return NotFound("No active booking for this user");
        if (input.BookingId.HasValue && input.BookingId.Value != booking.Id) return Conflict("The selected booking does not match the room or current user.");
        if (await db.CheckInEvents.AnyAsync(x => x.BookingId == booking.Id && x.CheckedInAt != null, cancellationToken)) return Conflict("This booking is already checked in.");
        booking.Status = BookingStatus.CheckedIn;
        var checkIn = new CheckInEvent { BookingId = booking.Id, CheckedInAt = DateTime.UtcNow };
        db.CheckInEvents.Add(checkIn);
        db.AuditLogs.Add(new AuditLog { OrganizationId = Org, ActorUserId = UserId, Action = "booking.checked_in", TargetType = "Booking", TargetId = booking.Id, Metadata = System.Text.Json.JsonSerializer.Serialize(new { status = booking.Status.ToString(), room_id = booking.RoomId }) });
        await db.SaveChangesAsync(cancellationToken);
        await events.CheckedIn(booking);
        return Ok(new { booking, checkIn });
    }

    [HttpPost("{bookingId:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid bookingId, CancellationToken cancellationToken)
    {
        var canManage = User.IsInRole("OrgAdmin") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        var booking = await db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId && x.OrganizationId == Org && (x.UserId == UserId || canManage), cancellationToken);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.CheckedIn) return BadRequest("Booking is not checked in");
        booking.Status = BookingStatus.Completed;
        var checkIn = await db.CheckInEvents.FirstOrDefaultAsync(x => x.BookingId == bookingId, cancellationToken);
        if (checkIn is not null) checkIn.CheckedOutAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { OrganizationId = Org, ActorUserId = UserId, Action = "booking.checked_out", TargetType = "Booking", TargetId = booking.Id, Metadata = System.Text.Json.JsonSerializer.Serialize(new { status = booking.Status.ToString(), room_id = booking.RoomId }) });
        await db.SaveChangesAsync(cancellationToken);
        await events.CheckedOut(booking);
        return Ok(booking);
    }

    [HttpGet("qr/{roomId:guid}/current")]
    public async Task<IActionResult> Current(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == roomId && x.OrganizationId == Org && x.IsActive, cancellationToken);
        if (room is null) return NotFound();
        return Ok(new { roomId, token = room.StaticCheckInCode, expiresAt = (DateTime?)null, isStatic = true });
    }

    [HttpGet("qr/{roomId:guid}/image")]
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Image(Guid roomId, [FromQuery] bool download, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roomId && x.OrganizationId == Org && x.IsActive, cancellationToken);
        if (room is null) return NotFound();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(room.StaticCheckInCode, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(16);
        var fileName = $"{SanitizeFileName(room.Name)}-check-in-qr.png";
        return download
            ? File(png, "image/png", fileName, enableRangeProcessing: true)
            : File(png, "image/png");
    }

    private Task<Room?> FindCheckInRoom(string? code, CancellationToken cancellationToken)
    {
        var normalized = code?.Trim();
        return db.Rooms.AsNoTracking().FirstOrDefaultAsync(x =>
            x.IsActive && x.StaticCheckInCode == normalized &&
            (x.OrganizationId == Org || db.RoomAccesses.Any(a => a.RoomId == x.Id && a.OrganizationId == Org && a.CanBook)), cancellationToken);
    }

    private async Task<Booking?> FindCurrentBooking(Guid roomId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var settings = await db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == Org, cancellationToken);
        var windowMinutes = Math.Max(1, settings?.NoShowTimeoutMinutes ?? 10);
        return await db.Bookings.FirstOrDefaultAsync(x =>
            x.OrganizationId == Org && x.RoomId == roomId && x.UserId == UserId &&
            x.Status == BookingStatus.Confirmed &&
            x.StartAtUtc.AddMinutes(-windowMinutes) <= now &&
            x.StartAtUtc.AddMinutes(windowMinutes) > now, cancellationToken);
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "room" : safe;
    }

    public record ScanInput(string QrToken, Guid? BookingId = null);
}
