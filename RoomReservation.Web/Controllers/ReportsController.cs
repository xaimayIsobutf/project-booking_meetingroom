using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Domain.Enums;
using RoomReservation.Infrastructure.Persistence;
using System.Security.Claims;
using System.Text;

namespace RoomReservation.Api.Controllers;

[ApiController, Authorize(Roles = "OrgAdmin,SuperAdmin"), Route("api/v1/reports")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("utilization")]
    public async Task<IActionResult> Utilization(DateTime from, DateTime to, Guid? roomId = null)
    {
        var query = db.Bookings.Where(x => x.StartAtUtc < to && x.EndAtUtc > from && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow);
        if (roomId.HasValue) query = query.Where(x => x.RoomId == roomId.Value);
        var rows = await query.GroupBy(x => x.RoomId).Select(g => new { room_id = g.Key, booked_minutes = g.Sum(x => (x.EndAtUtc - x.StartAtUtc).TotalMinutes) }).ToListAsync();
        var names = await db.Rooms.Where(x => rows.Select(r => r.room_id).Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        return Ok(rows.OrderByDescending(x => x.booked_minutes).Select(x => new { x.room_id, room_name = names.GetValueOrDefault(x.room_id, ""), x.booked_minutes }));
    }

    [HttpGet("no-show-rate")]
    public async Task<IActionResult> NoShow(DateTime from, DateTime to, Guid? roomId = null)
    {
        var query = db.Bookings.Where(x => x.StartAtUtc >= from && x.StartAtUtc < to);
        if (roomId.HasValue) query = query.Where(x => x.RoomId == roomId.Value);
        return Ok(await query.GroupBy(x => 1).Select(g => new { total = g.Count(), no_show = g.Count(x => x.Status == BookingStatus.NoShow) }).ToListAsync());
    }

    [HttpGet("peak-hours")]
    public async Task<IActionResult> Peak(DateTime from, DateTime to) => Ok(await db.Bookings.Where(x => x.StartAtUtc >= from && x.StartAtUtc < to).GroupBy(x => x.StartAtUtc.Hour).Select(g => new { hour = g.Key, count = g.Count() }).OrderByDescending(x => x.count).ToListAsync());

    [HttpGet("status-summary")]
    public async Task<IActionResult> StatusSummary(DateTime from, DateTime to) => Ok(await db.Bookings.Where(x => x.StartAtUtc >= from && x.StartAtUtc < to).GroupBy(x => x.Status).Select(g => new { status = g.Key.ToString(), count = g.Count() }).OrderByDescending(x => x.count).ToListAsync());

    [HttpGet("top-rooms")]
    public async Task<IActionResult> TopRooms(DateTime from, DateTime to) => Ok(await db.Bookings.Where(x => x.StartAtUtc >= from && x.StartAtUtc < to && x.Status != BookingStatus.Cancelled).GroupBy(x => x.RoomId).Select(g => new { room_id = g.Key, bookings = g.Count(), booked_minutes = g.Sum(x => (x.EndAtUtc - x.StartAtUtc).TotalMinutes) }).OrderByDescending(x => x.bookings).Take(10).ToListAsync());

    [HttpGet("top-users")]
    public async Task<IActionResult> TopUsers(DateTime from, DateTime to)
    {
        var rows = await db.Bookings.Where(x => x.StartAtUtc >= from && x.StartAtUtc < to && x.Status != BookingStatus.Cancelled).GroupBy(x => x.UserId).Select(g => new { user_id = g.Key, bookings = g.Count(), booked_minutes = g.Sum(x => (x.EndAtUtc - x.StartAtUtc).TotalMinutes) }).OrderByDescending(x => x.bookings).Take(10).ToListAsync();
        var names = await db.Users.Where(x => rows.Select(r => r.user_id).Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.Email : x.DisplayName);
        return Ok(rows.Select(x => new { x.user_id, user_name = x.user_id.HasValue && names.TryGetValue(x.user_id.Value, out var name) ? name : "Guest", x.bookings, x.booked_minutes }));
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly(DateTime from, DateTime to) => Ok(await db.Bookings.Where(x => x.StartAtUtc >= from && x.StartAtUtc < to).GroupBy(x => new { x.StartAtUtc.Year, x.StartAtUtc.Month }).Select(g => new { year = g.Key.Year, month = g.Key.Month, count = g.Count() }).OrderBy(x => x.year).ThenBy(x => x.month).ToListAsync());

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsv(DateTime from, DateTime to)
    {
        from = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        to = DateTime.SpecifyKind(to.Date, DateTimeKind.Utc);
        if (to <= from) return BadRequest(new { message = "The end date must be after the start date." });
        if (!Guid.TryParse(User.FindFirstValue("organization_id"), out var organizationId)) return Unauthorized();

        var rows = await db.Bookings
            .Where(x => x.OrganizationId == organizationId && x.StartAtUtc >= from && x.StartAtUtc < to)
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync();
        var roomNames = await db.Rooms
            .Where(x => x.OrganizationId == organizationId && rows.Select(r => r.RoomId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);
        var csv = new StringBuilder("Title,Room,StartUtc,EndUtc,Status\n");
        foreach (var b in rows)
        {
            static string Q(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
            csv.AppendLine($"{Q(b.Title)},{Q(roomNames.GetValueOrDefault(b.RoomId, ""))},{b.StartAtUtc:O},{b.EndAtUtc:O},{b.Status}");
        }

        return File(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", "room-bookings.csv");
    }
}
