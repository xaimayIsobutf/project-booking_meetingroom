using System.Security.Claims;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain.Entities;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Web.Infrastructure;

namespace RoomBooking.Web.Controllers;

public class ReservationsController(RoomBookingDbContext db) : Controller
{
    private int? TenantId => HttpContext.TenantId();
    private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public IActionResult Index() => View(db.Reservations.Include(x => x.MeetingRoom).Include(x => x.User).Where(x => TenantId != null && x.CompanyId == TenantId).OrderByDescending(x => x.StartAt).ToList());
    public IActionResult Calendar(DateTime? date, int? year, int? month)
    {
        var selected = date?.Date ?? new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
        ViewBag.CalendarDate = selected;
        ViewBag.Rooms = db.MeetingRooms.Where(x => TenantId != null && x.IsActive && (x.CompanyId == TenantId || db.RoomAccesses.Any(a => a.RoomId == x.Id && a.CompanyId == TenantId && a.CanView && a.CanBook))).OrderBy(x => x.Name).ToList();
        return View(db.Reservations.Include(x => x.MeetingRoom).Include(x => x.User)
            .Where(x => TenantId != null && x.CompanyId == TenantId && x.StartAt.Date <= selected.Date && x.EndAt.Date >= selected.Date)
            .OrderBy(x => x.StartAt).ToList());
    }
    public IActionResult History() => View("Index", db.Reservations.Include(x => x.MeetingRoom).Include(x => x.User).Where(x => TenantId != null && x.CompanyId == TenantId && x.EndAt < DateTime.UtcNow).OrderByDescending(x => x.StartAt).ToList());
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Approver")]
    public IActionResult Approvals() => View(db.Reservations.Include(x => x.MeetingRoom).Include(x => x.User).Where(x => TenantId != null && x.CompanyId == TenantId && x.Status == "Pending").OrderBy(x => x.StartAt).ToList());
    [HttpGet] public IActionResult CheckIn(int id) => RedirectToAction(nameof(Index));
    [HttpPost, ValidateAntiForgeryToken] public IActionResult CheckInPost(int id) { var item = db.Reservations.FirstOrDefault(x => x.Id == id && x.CompanyId == TenantId); if (item is not null && item.Status == "Approved" && item.StartAt <= DateTime.Now && item.EndAt > DateTime.Now) { db.ReservationCheckLogs.Add(new ReservationCheckLog { CompanyId = item.CompanyId, ReservationId = item.Id, UserId = CurrentUserId, CheckInTime = DateTime.UtcNow }); item.Status = "CheckedIn"; db.SaveChanges(); } return RedirectToAction(nameof(Index)); }
    [HttpGet] public IActionResult CheckOut(int id) => RedirectToAction(nameof(Index));
    [HttpPost, ValidateAntiForgeryToken] public IActionResult CheckOutPost(int id) { var item = db.Reservations.FirstOrDefault(x => x.Id == id && x.CompanyId == TenantId); var log = db.ReservationCheckLogs.Where(x => x.ReservationId == id && x.CheckOutTime == null).OrderByDescending(x => x.Id).FirstOrDefault(); if (item is not null && log is not null) { log.CheckOutTime = DateTime.UtcNow; item.Status = "Completed"; db.SaveChanges(); } return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken] public IActionResult Approve(int id) { var item = db.Reservations.FirstOrDefault(x => x.Id == id && x.CompanyId == TenantId); if (item is not null && item.Status == "Pending") { item.Status = "Approved"; db.SaveChanges(); } return RedirectToAction(nameof(Approvals)); }
    [HttpPost, ValidateAntiForgeryToken] public IActionResult Reject(int id, string reason) { var item = db.Reservations.FirstOrDefault(x => x.Id == id && x.CompanyId == TenantId); if (item is not null && item.Status == "Pending") { item.Status = "Rejected"; db.SaveChanges(); } return RedirectToAction(nameof(Approvals)); }
    public IActionResult Create(int? roomId) { ViewBag.RoomId = roomId; ViewBag.Rooms = db.MeetingRooms.Where(x => x.IsActive && (x.CompanyId == TenantId || db.RoomAccesses.Any(a => a.RoomId == x.Id && a.CompanyId == TenantId && a.CanView && a.CanBook))).OrderBy(x => x.Name).ToList(); return View(); }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(string subject, DateTime date, string bookingSlot, int roomId, int attendeesCount = 1)
    {
        ViewBag.RoomId = roomId; ViewBag.Rooms = db.MeetingRooms.Where(x => x.IsActive && (x.CompanyId == TenantId || db.RoomAccesses.Any(a => a.RoomId == x.Id && a.CompanyId == TenantId && a.CanView && a.CanBook))).OrderBy(x => x.Name).ToList();
        var slots = new Dictionary<string, (TimeSpan Start, TimeSpan End)>
        {
            ["full-day"] = (new(8, 0, 0), new(17, 0, 0)),
            ["morning"] = (new(8, 0, 0), new(12, 0, 0)),
            ["afternoon"] = (new(13, 0, 0), new(17, 0, 0)),
            ["morning-1"] = (new(8, 0, 0), new(10, 0, 0)),
            ["morning-2"] = (new(10, 0, 0), new(12, 0, 0)),
            ["afternoon-1"] = (new(13, 0, 0), new(15, 0, 0)),
            ["afternoon-2"] = (new(15, 0, 0), new(17, 0, 0))
        };
        if (string.IsNullOrWhiteSpace(subject) || !slots.TryGetValue(bookingSlot ?? "", out var selectedSlot) || attendeesCount < 1) { ModelState.AddModelError("", "กรุณาเลือกช่วงเวลาการจอง"); return View(); }
        var start = selectedSlot.Start; var end = selectedSlot.End;
        var from = date.Date + start; var to = date.Date + end;
        var room = db.MeetingRooms.FirstOrDefault(x => x.Id == roomId && x.IsActive && (x.CompanyId == TenantId || db.RoomAccesses.Any(a => a.RoomId == x.Id && a.CompanyId == TenantId && a.CanBook)));
        var user = db.Users.FirstOrDefault(x => x.Id == CurrentUserId && x.CompanyId == TenantId && x.IsActive);
        if (room == null || user == null) return BadRequest("กรุณาเข้าสู่ระบบก่อนจองห้อง");
        if (attendeesCount > room.Capacity) { ModelState.AddModelError("", $"ห้องนี้รองรับได้สูงสุด {room.Capacity} คน"); return View(); }
        var noticeMinutes = room.BookingNoticeHours == 1 ? 60 : room.BookingNoticeHours;
        if (from < DateTime.Now.AddMinutes(noticeMinutes)) { ModelState.AddModelError("", $"ต้องจองล่วงหน้าอย่างน้อย {noticeMinutes} นาที"); return View(); }
        using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
        if (db.Reservations.Any(x => x.CompanyId == TenantId && x.MeetingRoomId == roomId && x.StartAt < to && x.EndAt > from && x.Status != "Cancelled" && x.Status != "Rejected")) { ModelState.AddModelError("", "ห้องนี้ถูกจองในช่วงเวลาดังกล่าวแล้ว"); transaction.Rollback(); return View(); }
        db.Reservations.Add(new Reservation { CompanyId = room.CompanyId, MeetingRoomId = roomId, UserId = user.Id, Subject = subject.Trim(), StartAt = from, EndAt = to, AttendeesCount = attendeesCount, Status = "Pending" }); db.SaveChanges(); transaction.Commit(); TempData["Message"] = "บันทึกการจองแล้ว รอ Admin อนุมัติ"; return RedirectToAction("Index", "Home");
    }
    [HttpPost, ValidateAntiForgeryToken] public IActionResult Cancel(int id) { var item = db.Reservations.FirstOrDefault(x => x.Id == id && x.CompanyId == TenantId && (x.UserId == CurrentUserId || User.IsInRole("TenantAdmin"))); if (item != null) { item.Status = "Cancelled"; db.SaveChanges(); } return RedirectToAction(nameof(Index)); }
}
