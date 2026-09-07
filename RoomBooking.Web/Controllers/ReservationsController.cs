using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain.Entities;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Web.Infrastructure;

namespace RoomBooking.Web.Controllers;

[Authorize]
public class ReservationsController(
    RoomBookingDbContext db,
    IAppTime appTime) : Controller
{
    private int? TenantId => HttpContext.TenantId();

    private int CurrentUserId =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id)
                ? id
                : 0;

    public IActionResult Index(int page = 1)
    {
        const int pageSize = 20;

        page = Math.Max(1, page);

        var query = db.Reservations
            .Include(x => x.MeetingRoom)
            .Include(x => x.User)
            .Where(x =>
                TenantId != null &&
                (
                    x.CompanyId == TenantId ||
                    db.RoomAccesses.Any(a =>
                        a.RoomId == x.MeetingRoomId &&
                        a.CompanyId == TenantId &&
                        a.CanView)
                ))
            .OrderByDescending(x => x.StartAt);

        var total = query.Count();

        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = total;
        ViewBag.TotalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    total / (double)pageSize));

        return View(
            query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList());
    }

    public IActionResult Calendar(
        DateTime? date,
        int? year,
        int? month,
        string? view)
    {
        /*
         * Calendar date is a Thailand local date.
         */
        var selected =
            date?.Date ??
            new DateTime(
                year ?? appTime.ThailandNow.Year,
                month ?? appTime.ThailandNow.Month,
                1);

        /*
         * Make sure this value is treated as a Thailand
         * calendar date, not server local time.
         */
        selected = DateTime.SpecifyKind(
            selected.Date,
            DateTimeKind.Unspecified);

        var isWeek = view == "week";

        ViewBag.CalendarDate = selected;
        ViewBag.CalendarView =
            isWeek
                ? "week"
                : "day";

        /*
         * Calendar uses Thailand dates.
         *
         * Sunday = 0, Monday = 1, etc.
         */
        var rangeStartThailand =
            isWeek
                ? selected.AddDays(
                    -(int)selected.DayOfWeek)
                : selected;

        var rangeEndThailand =
            isWeek
                ? rangeStartThailand.AddDays(7)
                : selected.AddDays(1);

        /*
         * Convert Thailand calendar boundaries to UTC
         * before querying timestamptz columns.
         */
        var rangeStartUtc =
            appTime.ThailandToUtc(
                rangeStartThailand);

        var rangeEndUtc =
            appTime.ThailandToUtc(
                rangeEndThailand);

        ViewBag.Rooms =
            db.MeetingRooms
                .Where(x =>
                    TenantId != null &&
                    x.IsActive &&
                    (
                        x.CompanyId == TenantId ||
                        db.RoomAccesses.Any(a =>
                            a.RoomId == x.Id &&
                            a.CompanyId == TenantId &&
                            a.CanView &&
                            a.CanBook)
                    ))
                .OrderBy(x => x.Name)
                .ToList();

        var reservations =
            db.Reservations
                .Include(x => x.MeetingRoom)
                .Include(x => x.User)
                .Where(x =>
                    TenantId != null &&
                    (
                        x.CompanyId == TenantId ||
                        db.RoomAccesses.Any(a =>
                            a.RoomId == x.MeetingRoomId &&
                            a.CompanyId == TenantId &&
                            a.CanView)
                    ) &&
                    x.StartAt < rangeEndUtc &&
                    x.EndAt >= rangeStartUtc)
                .OrderBy(x => x.StartAt)
                .ToList();

        return View(reservations);
    }

    public IActionResult History()
    {
        var nowUtc = appTime.UtcNow;

        return View(
            "Index",
            db.Reservations
                .Include(x => x.MeetingRoom)
                .Include(x => x.User)
                .Where(x =>
                    TenantId != null &&
                    (
                        x.CompanyId == TenantId ||
                        db.RoomAccesses.Any(a =>
                            a.RoomId == x.MeetingRoomId &&
                            a.CompanyId == TenantId &&
                            a.CanView)
                    ) &&
                    x.EndAt < nowUtc)
                .OrderByDescending(x => x.StartAt)
                .ToList());
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin,Approver")]
    public IActionResult Approvals()
    {
        return View(
            db.Reservations
                .Include(x => x.MeetingRoom)
                .Include(x => x.User)
                .Where(x =>
                    TenantId != null &&
                    x.CompanyId == TenantId &&
                    x.Status == "Pending")
                .OrderBy(x => x.StartAt)
                .ToList());
    }

    [HttpGet]
    public IActionResult CheckIn(int id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckInPost(int id)
    {
        var item =
            db.Reservations
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.CompanyId == TenantId);

        if (item is not null)
        {
            var nowUtc = appTime.UtcNow;

            if (
                item.Status == "Approved" &&
                item.StartAt <= nowUtc &&
                item.EndAt > nowUtc)
            {
                db.ReservationCheckLogs.Add(
                    new ReservationCheckLog
                    {
                        CompanyId = item.CompanyId,
                        ReservationId = item.Id,
                        UserId = CurrentUserId,
                        CheckInTime = nowUtc
                    });

                item.Status = "CheckedIn";

                db.SaveChanges();
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult CheckOut(int id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckOutPost(int id)
    {
        var item =
            db.Reservations
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.CompanyId == TenantId);

        var log =
            db.ReservationCheckLogs
                .Where(x =>
                    x.ReservationId == id &&
                    x.CheckOutTime == null)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

        if (item is not null && log is not null)
        {
            log.CheckOutTime = appTime.UtcNow;
            item.Status = "Completed";

            db.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Approve(int id)
    {
        var item =
            db.Reservations
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.CompanyId == TenantId);

        if (
            item is not null &&
            item.Status == "Pending")
        {
            item.Status = "Approved";

            db.SaveChanges();
        }

        return RedirectToAction(nameof(Approvals));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reject(
        int id,
        string reason)
    {
        var item =
            db.Reservations
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.CompanyId == TenantId);

        if (
            item is not null &&
            item.Status == "Pending")
        {
            item.Status = "Rejected";

            db.SaveChanges();
        }

        return RedirectToAction(nameof(Approvals));
    }

    public IActionResult Create(int? roomId)
    {
        ViewBag.RoomId = roomId;

        ViewBag.Rooms =
            db.MeetingRooms
                .Where(x =>
                    x.IsActive &&
                    (
                        x.CompanyId == TenantId ||
                        db.RoomAccesses.Any(a =>
                            a.RoomId == x.Id &&
                            a.CompanyId == TenantId &&
                            a.CanView &&
                            a.CanBook)
                    ))
                .OrderBy(x => x.Name)
                .ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(
        string subject,
        DateTime date,
        string bookingSlot,
        int roomId,
        int attendeesCount = 1,
        string? startTime = null,
        string? endTime = null)
    {
        ViewBag.RoomId = roomId;

        ViewBag.Rooms =
            db.MeetingRooms
                .Where(x =>
                    x.IsActive &&
                    (
                        x.CompanyId == TenantId ||
                        db.RoomAccesses.Any(a =>
                            a.RoomId == x.Id &&
                            a.CompanyId == TenantId &&
                            a.CanView &&
                            a.CanBook)
                    ))
                .OrderBy(x => x.Name)
                .ToList();

        var slots =
            new Dictionary<
                string,
                (TimeSpan Start, TimeSpan End)>
            {
                ["full-day"] =
                    (new TimeSpan(8, 0, 0),
                     new TimeSpan(17, 0, 0)),

                ["morning"] =
                    (new TimeSpan(8, 0, 0),
                     new TimeSpan(12, 0, 0)),

                ["afternoon"] =
                    (new TimeSpan(13, 0, 0),
                     new TimeSpan(17, 0, 0)),

                ["morning-1"] =
                    (new TimeSpan(8, 0, 0),
                     new TimeSpan(10, 0, 0)),

                ["morning-2"] =
                    (new TimeSpan(10, 0, 0),
                     new TimeSpan(12, 0, 0)),

                ["afternoon-1"] =
                    (new TimeSpan(13, 0, 0),
                     new TimeSpan(15, 0, 0)),

                ["afternoon-2"] =
                    (new TimeSpan(15, 0, 0),
                     new TimeSpan(17, 0, 0))
            };

        (TimeSpan Start, TimeSpan End) selectedSlot = default;
        if (TimeSpan.TryParse(startTime, out var customStart) &&
            TimeSpan.TryParse(endTime, out var customEnd) &&
            customEnd > customStart)
        {
            selectedSlot = (customStart, customEnd);
        }

        if (
    string.IsNullOrWhiteSpace(subject) ||
    ((!TimeSpan.TryParse(startTime, out customStart) || !TimeSpan.TryParse(endTime, out customEnd)) &&
     !slots.TryGetValue(bookingSlot ?? "", out selectedSlot)) ||
    attendeesCount < 1)
{
    ModelState.AddModelError(
        "",
        "กรุณากรอกข้อมูลให้ครบและเลือกช่วงเวลาการจอง");

    ViewBag.RoomId = roomId;

    ViewBag.Rooms =
        db.MeetingRooms
            .Where(x =>
                x.IsActive &&
                (
                    x.CompanyId == TenantId ||
                    db.RoomAccesses.Any(a =>
                        a.RoomId == x.Id &&
                        a.CompanyId == TenantId &&
                        a.CanView &&
                        a.CanBook)
                ))
            .OrderBy(x => x.Name)
            .ToList();

    return View();
}

        /*
         * The date from <input type="date">
         * represents a Thailand calendar date.
         */
        var fromThailand =
            DateTime.SpecifyKind(
                date.Date + selectedSlot.Start,
                DateTimeKind.Unspecified);

        var toThailand =
            DateTime.SpecifyKind(
                date.Date + selectedSlot.End,
                DateTimeKind.Unspecified);

        /*
         * Convert Thailand booking time to UTC
         * before saving to PostgreSQL timestamptz.
         */
        var fromUtc =
            appTime.ThailandToUtc(
                fromThailand);

        var toUtc =
            appTime.ThailandToUtc(
                toThailand);

        var room =
            db.MeetingRooms
                .FirstOrDefault(x =>
                    x.Id == roomId &&
                    x.IsActive &&
                    (
                        x.CompanyId == TenantId ||
                        db.RoomAccesses.Any(a =>
                            a.RoomId == x.Id &&
                            a.CompanyId == TenantId &&
                            a.CanBook)
                    ));

        var user =
            db.Users
                .FirstOrDefault(x =>
                    x.Id == CurrentUserId &&
                    x.CompanyId == TenantId &&
                    x.IsActive);

        if (room == null || user == null)
        {
            return BadRequest(
                "กรุณาเข้าสู่ระบบก่อนจองห้อง");
        }

        if (attendeesCount > room.Capacity)
        {
            ModelState.AddModelError(
                "",
                $"ห้องนี้รองรับได้สูงสุด {room.Capacity} คน");

            return View();
        }

        var noticeMinutes =
            room.BookingNoticeHours == 1
                ? 60
                : room.BookingNoticeHours;

        /*
         * Compare booking time and current time in UTC.
         */
        if (fromUtc <
            appTime.UtcNow.AddMinutes(noticeMinutes))
        {
            ModelState.AddModelError(
                "",
                $"ต้องจองล่วงหน้าอย่างน้อย {noticeMinutes} นาที");

            return View();
        }

        using var transaction =
            db.Database.BeginTransaction(
                IsolationLevel.Serializable);

        /*
         * Reservation overlap comparison is entirely UTC.
         */
        var hasConflict =
            db.Reservations.Any(x =>
                x.CompanyId == TenantId &&
                x.MeetingRoomId == roomId &&
                x.StartAt < toUtc &&
                x.EndAt > fromUtc &&
                x.Status != "Cancelled" &&
                x.Status != "Rejected");

        if (hasConflict)
        {
            ModelState.AddModelError(
                "",
                "ห้องนี้ถูกจองในช่วงเวลาดังกล่าวแล้ว");

            transaction.Rollback();

            return View();
        }

        db.Reservations.Add(
            new Reservation
            {
                CompanyId = room.CompanyId,
                MeetingRoomId = roomId,
                UserId = user.Id,
                Subject = subject.Trim(),

                // Store UTC in database.
                StartAt = fromUtc,
                EndAt = toUtc,

                AttendeesCount = attendeesCount,
                Status = "Pending"
            });

        db.SaveChanges();

        transaction.Commit();

        TempData["Message"] =
            "บันทึกการจองแล้ว รอ Admin อนุมัติ";

        return RedirectToAction(
            "Index",
            "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(int id)
    {
        var item =
            db.Reservations
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.CompanyId == TenantId &&
                    (
                        x.UserId == CurrentUserId ||
                        User.IsInRole("TenantAdmin")
                    ));

        if (item is not null)
        {
            item.Status = "Cancelled";

            db.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }
}
