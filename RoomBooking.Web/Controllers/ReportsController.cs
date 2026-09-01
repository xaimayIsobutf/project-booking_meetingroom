using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Web.Infrastructure;
using System.Text;

namespace RoomBooking.Web.Controllers;

[Authorize]
public class ReportsController(RoomBookingDbContext db) : Controller
{
    private static readonly TimeZoneInfo ThailandTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Bangkok");

    private static DateTime ThailandToUtc(DateTime thailandTime)
    {
        var localThailandTime = DateTime.SpecifyKind(
            thailandTime,
            DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(
            localThailandTime,
            ThailandTimeZone);
    }

    private static DateTime UtcToThailand(DateTime utcTime)
    {
        var utc = DateTime.SpecifyKind(
            utcTime,
            DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(
            utc,
            ThailandTimeZone);
    }

    public IActionResult Index(
        DateTime? from,
        DateTime? to,
        string? status,
        int page = 1)
    {
        var tenant = HttpContext.TenantId();

        const int size = 20;

        var q = db.Reservations
            .Include(x => x.MeetingRoom)
            .Include(x => x.User)
            .Where(x =>
                tenant != null &&
                (
                    x.CompanyId == tenant ||
                    db.RoomAccesses.Any(a =>
                        a.RoomId == x.MeetingRoomId &&
                        a.CompanyId == tenant &&
                        a.CanView)
                ));

        // The report filter is entered as Thailand local date.
        // Convert the beginning of the selected day to UTC.
        if (from.HasValue)
        {
            var fromThailand = from.Value.Date;
            var fromUtc = ThailandToUtc(fromThailand);

            q = q.Where(x => x.StartAt >= fromUtc);
        }

        // "to" includes the whole selected Thailand day,
        // so use the beginning of the following day as an exclusive boundary.
        if (to.HasValue)
        {
            var toThailand = to.Value.Date.AddDays(1);
            var toUtc = ThailandToUtc(toThailand);

            q = q.Where(x => x.StartAt < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            status != "all")
        {
            q = q.Where(x => x.Status == status);
        }

        var all = q
            .OrderByDescending(x => x.StartAt)
            .ToList();

        page = Math.Max(1, page);

        ViewBag.Rows = all
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        ViewBag.Total = all.Count;
        ViewBag.Page = page;
        ViewBag.Pages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    all.Count / (double)size));

        ViewBag.From =
            from?.ToString("yyyy-MM-dd");

        ViewBag.To =
            to?.ToString("yyyy-MM-dd");

        ViewBag.Status =
            status ?? "all";

        ViewBag.Statuses =
            all
                .GroupBy(x => x.Status)
                .Select(x => new
                {
                    Name = x.Key,
                    Count = x.Count()
                })
                .ToList();

        ViewBag.RoomUsage =
            all
                .GroupBy(x => x.MeetingRoom?.Name ?? "-")
                .Select(x => new
                {
                    Name = x.Key,
                    Count = x.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

        ViewBag.UserUsage =
            all
                .GroupBy(x => x.User?.FullName ?? "-")
                .Select(x => new
                {
                    Name = x.Key,
                    Count = x.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

        // StartAt is stored as UTC.
        // Convert to Thailand before grouping by month.
        ViewBag.Monthly =
            all
                .GroupBy(x =>
                    UtcToThailand(x.StartAt)
                        .ToString("yyyy-MM"))
                .Select(x => new
                {
                    Name = x.Key,
                    Count = x.Count()
                })
                .OrderBy(x => x.Name)
                .ToList();

        ViewBag.SharedCount =
            all.Count(x => x.CompanyId != tenant);

        return View();
    }

    public IActionResult ExportCsv(
        DateTime? from,
        DateTime? to,
        string? status)
    {
        var tenant = HttpContext.TenantId();

        var q = db.Reservations
            .Include(x => x.MeetingRoom)
            .Include(x => x.User)
            .Where(x =>
                tenant != null &&
                (
                    x.CompanyId == tenant ||
                    db.RoomAccesses.Any(a =>
                        a.RoomId == x.MeetingRoomId &&
                        a.CompanyId == tenant &&
                        a.CanView)
                ));

        // Convert Thailand report date to UTC
        // before querying timestamptz.
        if (from.HasValue)
        {
            var fromThailand = from.Value.Date;
            var fromUtc = ThailandToUtc(fromThailand);

            q = q.Where(x => x.StartAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toThailand = to.Value.Date.AddDays(1);
            var toUtc = ThailandToUtc(toThailand);

            q = q.Where(x => x.StartAt < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            status != "all")
        {
            q = q.Where(x => x.Status == status);
        }

        var csv = new StringBuilder();

        csv.AppendLine(
            "Subject,Room,ReservedBy,Start,End,Status");

        foreach (var x in q.OrderBy(x => x.StartAt))
        {
            // Database values are UTC.
            // CSV should show Thailand local time.
            var startThailand =
                UtcToThailand(x.StartAt);

            var endThailand =
                UtcToThailand(x.EndAt);

            var subject =
                (x.Subject ?? "")
                    .Replace("\"", "\"\"");

            var room =
                (x.MeetingRoom?.Name ?? "")
                    .Replace("\"", "\"\"");

            var user =
                (x.User?.FullName ?? "")
                    .Replace("\"", "\"\"");

            csv.AppendLine(
                $"\"{subject}\"," +
                $"\"{room}\"," +
                $"\"{user}\"," +
                $"{startThailand:yyyy-MM-dd HH:mm}," +
                $"{endThailand:yyyy-MM-dd HH:mm}," +
                $"{x.Status}");
        }

        return File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            "roombooking-report.csv");
    }
}