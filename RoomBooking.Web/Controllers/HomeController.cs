using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Web.Models;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace RoomBooking.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly RoomBookingDbContext _db;
    private readonly ILogger<HomeController> _logger;

    private static readonly TimeZoneInfo ThailandTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Bangkok");

    public HomeController(
        ILogger<HomeController> logger,
        RoomBookingDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public IActionResult Index()
    {
        var tenantId = HttpContext.TenantId();

        if (tenantId is null)
            return RedirectToAction("Login", "Account");

        /*
         * IMPORTANT:
         *
         * User-facing date/time = Thailand time
         * Database timestamptz = UTC
         */

        // Current time in Thailand, used only for determining
        // today's local date and displaying the dashboard.
        var thailandNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            ThailandTimeZone);

        // Today's date according to Thailand.
        var todayThailand = thailandNow.Date;

        // Tomorrow's date according to Thailand.
        var tomorrowThailand = todayThailand.AddDays(1);

        // Convert Thailand calendar boundaries to UTC
        // before querying PostgreSQL timestamptz columns.
        var todayUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(
                todayThailand,
                DateTimeKind.Unspecified),
            ThailandTimeZone);

        var tomorrowUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(
                tomorrowThailand,
                DateTimeKind.Unspecified),
            ThailandTimeZone);

        // Database comparisons must use UTC.
        var nowUtc = DateTime.UtcNow;

        var rooms = _db.MeetingRooms
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                (
                    x.CompanyId == tenantId ||
                    _db.RoomAccesses.Any(a =>
                        a.RoomId == x.Id &&
                        a.CompanyId == tenantId &&
                        a.CanView &&
                        a.CanBook)
                ))
            .ToList();

        /*
         * StartAt / EndAt are timestamptz.
         * Therefore todayUtc / tomorrowUtc must be used here.
         */
        var reservations = _db.Reservations
            .Include(x => x.MeetingRoom)
            .Where(x =>
                (
                    x.CompanyId == tenantId ||
                    _db.RoomAccesses.Any(a =>
                        a.RoomId == x.MeetingRoomId &&
                        a.CompanyId == tenantId &&
                        a.CanView)
                ) &&
                x.StartAt < tomorrowUtc &&
                x.EndAt >= todayUtc)
            .OrderBy(x => x.StartAt)
            .Take(20)
            .ToList();

        /*
         * Compare database UTC values with UTC now.
         */
        var currentRoomIds = reservations
            .Where(r =>
                r.StartAt <= nowUtc &&
                r.EndAt > nowUtc &&
                r.Status != "Cancelled")
            .Select(r => r.MeetingRoomId)
            .ToHashSet();

        /*
         * Convert reservation UTC time back to Thailand time
         * before displaying it to the user.
         */
        var reservationViewModels = reservations
            .Select(x =>
            {
                var startThailand =
                    TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(
                            x.StartAt,
                            DateTimeKind.Utc),
                        ThailandTimeZone);

                var endThailand =
                    TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(
                            x.EndAt,
                            DateTimeKind.Utc),
                        ThailandTimeZone);

                var status =
                    x.Status == "Cancelled" ||
                    x.Status == "Rejected"
                        ? "ยกเลิก"
                        : x.StartAt <= nowUtc &&
                          x.EndAt > nowUtc
                            ? "กำลังใช้งาน"
                            : "จองแล้ว";

                return new ReservationViewModel
                {
                    RoomName = x.MeetingRoom!.Name,
                    Subject = x.Subject,

                    // Display Thailand local time.
                    Time =
                        $"{startThailand:HH:mm} - {endThailand:HH:mm}",

                    Status = status
                };
            })
            .ToList();

        return View(
            new DashboardViewModel
            {
                RoomCount = rooms.Count,

                TodayReservations = reservations.Count,

                AvailableRooms =
                    rooms.Count(x =>
                        !currentRoomIds.Contains(x.Id)),

                Rooms = rooms
                    .Take(6)
                    .Select(x =>
                        new RoomViewModel
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Location = x.Location,
                            Capacity = x.Capacity,
                            Equipment = x.Equipment,
                            Available =
                                !currentRoomIds.Contains(x.Id)
                        })
                    .ToList(),

                Reservations = reservationViewModels
            });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
    }
}