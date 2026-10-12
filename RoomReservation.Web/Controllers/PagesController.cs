using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Domain.Entities;
using RoomReservation.Domain.Enums;
using RoomReservation.Infrastructure.Persistence;
using RoomReservation.Web.Models;

namespace RoomReservation.Web.Controllers;

// The Razor page layer mirrors web/src/App.tsx routes. API controllers remain separate.
[Authorize]
public sealed class PagesController(AppDbContext db) : Controller
{
    private Guid Org => Guid.Parse(User.FindFirstValue("organization_id")!);
    private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<Room> Rooms => AccessibleRooms();
    private IQueryable<Room> OwnedRooms => db.Rooms.Where(x => x.OrganizationId == Org && x.IsActive).OrderBy(x => x.Name);
    private IQueryable<Room> BookableRooms => db.Rooms.Where(x => x.IsActive && (x.OrganizationId == Org || db.RoomAccesses.Any(a => a.RoomId == x.Id && a.OrganizationId == Org && a.CanView && a.CanBook))).OrderBy(x => x.Name);
    private async Task<TimeZoneInfo> OrgTimeZone() { var id = await db.Organizations.Where(x => x.Id == Org).Select(x => x.DefaultTimezone).FirstOrDefaultAsync() ?? "UTC"; try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; } catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; } }
    private static DateTime ToUtc(DateTime local, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone);

    // Compatibility links for bookmarks from the previous Razor UI.
    [HttpGet("/Reservations/Calendar")] public IActionResult LegacyCalendar() => Redirect("/schedule");
    [HttpGet("/Reservations")] public IActionResult LegacyReservations() => Redirect("/bookings");
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/Rooms")] public IActionResult LegacyRooms() => Redirect("/admin/rooms");

    [HttpGet("/")]
    [HttpGet("/dashboard")]
    public async Task<IActionResult> Dashboard() => View("Dashboard", await DashboardData());

    [HttpGet("/schedule")]
    public async Task<IActionResult> Schedule(DateTime? date = null, string view = "week")
    {
        var zone = await OrgTimeZone();
        var organizationToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
        var day = (date ?? organizationToday).Date;
        var from = view.Equals("day", StringComparison.OrdinalIgnoreCase) ? day : day.AddDays(-(int)day.DayOfWeek);
        var to = view.Equals("day", StringComparison.OrdinalIgnoreCase) ? from.AddDays(1) : from.AddDays(7);
        var fromUtc = ToUtc(from, zone);
        var toUtc = ToUtc(to, zone);
        var rooms = await Rooms.ToListAsync();
        var roomIds = rooms.Select(x => x.Id).ToArray();
        return View("Schedule", new SchedulePageModel { Date = day, View = view, From = from, TimeZoneId = zone.Id, Rooms = rooms, Bookings = await db.Bookings.AsNoTracking().Where(x => roomIds.Contains(x.RoomId) && x.StartAtUtc < toUtc && x.EndAtUtc > fromUtc).OrderBy(x => x.StartAtUtc).ToListAsync() });
    }

    [AllowAnonymous]
    [HttpGet("/room-display")]
    [HttpGet("/RoomDisplay")]
    public IActionResult RoomDisplay() => View("RoomDisplay", new RoomDisplayPageModel());

    [AllowAnonymous]
    [HttpGet("/RoomDisplay/Pair")]
    public IActionResult RoomDisplayPair(Guid? deviceId = null, Guid? organizationId = null) => View("RoomDisplay", new RoomDisplayPageModel { PairDeviceId = deviceId, PairOrganizationId = organizationId });

    [AllowAnonymous]
    [HttpGet("/RoomDisplay/Unpaired")]
    public IActionResult RoomDisplayUnpaired() => View("RoomDisplay", new RoomDisplayPageModel());

    /* The paired display no longer accepts roomId from the URL. The room comes from its session cookie. */
    private async Task<IActionResult> LegacyRoomDisplay(Guid? roomId = null)
    {
        var zone = await OrgTimeZone();
        var rooms = await Rooms.ToListAsync();
        if (rooms.Count == 0) return View("RoomDisplay", new RoomDisplayPageModel { Rooms = rooms, TimeZoneId = zone.Id });
        var room = rooms.FirstOrDefault(x => x.Id == roomId) ?? rooms[0];
        var now = DateTime.UtcNow;
        var bookings = await db.Bookings.AsNoTracking()
            .Where(x => x.OrganizationId == Org && x.RoomId == room.Id && x.EndAtUtc >= now && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow)
            .OrderBy(x => x.StartAtUtc).Take(2).ToListAsync();
        return View("RoomDisplay", new RoomDisplayPageModel { SelectedRoom = room, Rooms = rooms, CurrentBooking = bookings.FirstOrDefault(x => x.StartAtUtc <= now && x.EndAtUtc > now), NextBooking = bookings.FirstOrDefault(x => x.StartAtUtc > now), TimeZoneId = zone.Id });
    }

    [HttpGet("/bookings")]
    public async Task<IActionResult> Bookings(string? search = null, string scope = "upcoming", string status = "all", Guid? roomId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        scope = scope is "past" or "cancelled" or "pending" ? scope : "upcoming";
        status = Enum.TryParse<BookingStatus>(status, true, out var parsedStatus) ? parsedStatus.ToString() : "all";
        var zone = await OrgTimeZone();
        var now = DateTime.UtcNow;
        var q = db.Bookings.AsNoTracking().Where(x => x.OrganizationId == Org).AsQueryable();
        if (scope == "upcoming") q = q.Where(x => x.EndAtUtc >= now && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow);
        if (scope == "past") q = q.Where(x => x.EndAtUtc < now);
        if (scope == "cancelled") q = q.Where(x => x.Status == BookingStatus.Cancelled);
        if (scope == "pending") q = q.Where(x => x.Status == BookingStatus.Pending);
        if (status != "all" && Enum.TryParse<BookingStatus>(status, true, out parsedStatus)) q = q.Where(x => x.Status == parsedStatus);
        if (roomId.HasValue) q = q.Where(x => x.RoomId == roomId.Value);
        if (fromDate.HasValue) q = q.Where(x => x.StartAtUtc < ToUtc(fromDate.Value.Date.AddDays(1), zone));
        if (toDate.HasValue) q = q.Where(x => x.EndAtUtc > ToUtc(toDate.Value.Date, zone));
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Title.Contains(search) || db.Rooms.Any(r => r.Id == x.RoomId && r.Name.Contains(search)));
        var bookings = await q.OrderByDescending(x => x.StartAtUtc).Take(100).ToListAsync();
        var rooms = await db.Rooms.AsNoTracking().Where(x => bookings.Select(b => b.RoomId).Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var orgBookings = db.Bookings.AsNoTracking().Where(x => x.OrganizationId == Org);
        return View("Bookings", new BookingsPageModel { Bookings = bookings, Rooms = rooms, FilterRooms = await Rooms.ToListAsync(), Search = search, Scope = scope, Status = status, RoomId = roomId, FromDate = fromDate, ToDate = toDate, TimeZoneId = zone.Id,
            UpcomingCount = await orgBookings.CountAsync(x => x.EndAtUtc >= now && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow),
            PendingCount = await orgBookings.CountAsync(x => x.Status == BookingStatus.Pending), CheckedInCount = await orgBookings.CountAsync(x => x.Status == BookingStatus.CheckedIn), CancelledCount = await orgBookings.CountAsync(x => x.Status == BookingStatus.Cancelled) });
    }

    [HttpGet("/bookings/new")]
    public async Task<IActionResult> NewBooking(Guid? roomId = null, DateTime? startAt = null, DateTime? endAt = null) { var zone = await OrgTimeZone(); var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date; return View("NewBooking", new BookingFormModel { Rooms = await BookableRooms.ToListAsync(), RoomId = roomId ?? Guid.Empty, StartAt = startAt ?? today.AddHours(9), EndAt = endAt ?? today.AddHours(10) }); }

    [HttpPost("/bookings/new"), ValidateAntiForgeryToken]
    public async Task<IActionResult> NewBooking(BookingFormModel model)
    {
        model.Rooms = await BookableRooms.ToListAsync();
        if (model.EndAt <= model.StartAt || string.IsNullOrWhiteSpace(model.Title) || model.RoomId == Guid.Empty) { ModelState.AddModelError("", "กรุณากรอกข้อมูลการจองให้ครบ และเวลาสิ้นสุดต้องมากกว่าเวลาเริ่ม"); return View("NewBooking", model); }
        var zone = await OrgTimeZone(); var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        if (model.StartAt < nowLocal) { ModelState.AddModelError(nameof(model.StartAt), "ไม่สามารถจองย้อนหลังได้ กรุณาเลือกเวลาปัจจุบันหรือเวลาในอนาคต"); return View("NewBooking", model); }
        var s = ToUtc(model.StartAt, zone); var e = ToUtc(model.EndAt, zone);
        if (await db.Bookings.AnyAsync(x => x.RoomId == model.RoomId && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow && x.StartAtUtc < e && s < x.EndAtUtc)) { ModelState.AddModelError("", "ช่วงเวลานี้ถูกจองแล้ว"); return View("NewBooking", model); }
        var room = model.Rooms.FirstOrDefault(x => x.Id == model.RoomId);
        if (room is null) { ModelState.AddModelError(nameof(model.RoomId), "ห้องนี้ไม่มีสิทธิ์ให้จอง"); return View("NewBooking", model); }
        var sharedAccess = room.OrganizationId == Org ? null : await db.RoomAccesses.AsNoTracking().FirstOrDefaultAsync(x => x.RoomId == room.Id && x.OrganizationId == Org && x.CanBook);
        if (room.OrganizationId != Org && sharedAccess is null) return Forbid();
        db.Bookings.Add(new Booking { OrganizationId = Org, UserId = Me, RoomId = model.RoomId, BookingSource = room.OrganizationId == Org ? "Internal" : "Shared", Title = model.Title.Trim(), Notes = model.Notes, AttendeeCount = model.AttendeeCount, StartAtUtc = s, EndAtUtc = e, Status = room.RequiresApproval || sharedAccess?.RequiresApproval == true ? BookingStatus.Pending : BookingStatus.Confirmed });
        await db.SaveChangesAsync(); TempData["FlashSuccess"] = "สร้างการจองสำเร็จ"; return RedirectToAction(nameof(Bookings));
    }

    [HttpGet("/bookings/{id:guid}")]
    public async Task<IActionResult> Booking(Guid id) { var b = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org); if (b is null) return NotFound(); ViewBag.Room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == b.RoomId); ViewBag.Requester = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == b.UserId); ViewBag.AuditLogs = await db.AuditLogs.AsNoTracking().Where(x => x.OrganizationId == Org && x.TargetId == id).OrderBy(x => x.CreatedAt).ToListAsync(); ViewBag.TimeZoneId = (await OrgTimeZone()).Id; return View("Booking", b); }

    [HttpPost("/bookings/{id:guid}/cancel"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id) { var b = await db.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org && (x.UserId == Me || User.IsInRole("OrgAdmin") || User.IsInRole("SuperAdmin"))); if (b is null) return NotFound(); if (b.Status is not (BookingStatus.Pending or BookingStatus.Confirmed)) return BadRequest("Only pending or confirmed bookings can be cancelled."); b.Status = BookingStatus.Cancelled; b.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(); TempData["FlashSuccess"] = "ยกเลิกการจองสำเร็จ"; return RedirectToAction(nameof(Bookings)); }

    [HttpGet("/quick-book")] public async Task<IActionResult> QuickBook() { var zone = await OrgTimeZone(); var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone); var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1); return View("QuickBook", new BookingFormModel { Rooms = await BookableRooms.ToListAsync(), StartAt = start, EndAt = start.AddHours(1) }); }
    [HttpGet("/quick-book/available")]
    public async Task<IActionResult> QuickBookAvailable(DateTime startAt, DateTime endAt)
    {
        var zone = await OrgTimeZone();
        if (endAt <= startAt) return BadRequest(new { message = "Invalid time range" });
        var startUtc = ToUtc(startAt, zone); var endUtc = ToUtc(endAt, zone);
        var busy = await db.Bookings.AsNoTracking().Where(x => x.StartAtUtc < endUtc && startUtc < x.EndAtUtc && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow).Select(x => x.RoomId).ToListAsync();
        return Json(await BookableRooms.Where(x => !busy.Contains(x.Id)).Select(x => x.Id).ToListAsync());
    }
    [HttpGet("/check-in")]
    public async Task<IActionResult> CheckIn()
    {
        var zone = await OrgTimeZone();
        var bookings = await db.Bookings.AsNoTracking()
            .Where(x => x.OrganizationId == Org && x.UserId == Me && x.EndAtUtc >= DateTime.UtcNow.AddHours(-2) && x.StartAtUtc <= DateTime.UtcNow.AddHours(2) && x.Status != BookingStatus.Cancelled)
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync();
        ViewBag.TimeZoneId = zone.Id;
        return View("CheckIn", bookings);
    }

    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/rooms")]
    public async Task<IActionResult> AdminRooms()
    {
        var zone = await OrgTimeZone();
        var now = DateTime.UtcNow;
        var ownedRooms = await OwnedRooms.ToListAsync();
        var sharedEntries = await (from access in db.RoomAccesses.AsNoTracking()
                                   join room in db.Rooms.AsNoTracking().Where(x => x.IsActive) on access.RoomId equals room.Id
                                   join owner in db.Organizations.AsNoTracking() on access.OwnerOrganizationId equals owner.Id
                                   where access.OrganizationId == Org && access.OwnerOrganizationId != Org && access.CanView
                                   select new { Room = room, OwnerName = owner.Name }).ToListAsync();
        var rooms = ownedRooms.Concat(sharedEntries.Select(x => x.Room)).ToList();
        var roomIds = rooms.Select(x => x.Id).ToArray();
        var bookings = await db.Bookings.AsNoTracking()
            .Where(x => roomIds.Contains(x.RoomId) && x.StartAtUtc < now.AddDays(30) && x.EndAtUtc > now.AddDays(-30))
            .OrderBy(x => x.StartAtUtc).ToListAsync();
        var equipment = await db.EquipmentItems.AsNoTracking().Where(x => x.OrganizationId == Org).OrderBy(x => x.Name).ToListAsync();
        return View("AdminRooms", new RoomManagementPageModel { Rooms = rooms, OwnedRooms = ownedRooms, SharedRoomOwners = sharedEntries.ToDictionary(x => x.Room.Id, x => x.OwnerName), Bookings = bookings, Equipment = equipment, DisplayDevices = await db.RoomDisplayDevices.AsNoTracking().Where(x => x.OrganizationId == Org).OrderByDescending(x => x.CreatedAt).ToListAsync(), TimeZoneId = zone.Id });
    }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/rooms/create")] public async Task<IActionResult> CreateRoom() { ViewBag.Equipment = await db.EquipmentItems.AsNoTracking().Where(x => x.OrganizationId == Org && x.IsActive).OrderBy(x => x.Name).ToListAsync(); return View("RoomForm", new Room()); }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpPost("/admin/rooms/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(Room room) { if (!ModelState.IsValid) { ViewBag.Equipment = await db.EquipmentItems.AsNoTracking().Where(x => x.OrganizationId == Org && x.IsActive).OrderBy(x => x.Name).ToListAsync(); return View("RoomForm", room); } room.OrganizationId = Org; room.EquipmentTags = Tags(room.EquipmentTags); db.Rooms.Add(room); await db.SaveChangesAsync(); TempData["FlashSuccess"] = "เพิ่มห้องสำเร็จ"; return RedirectToAction(nameof(AdminRooms)); }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/rooms/{id:guid}/edit")] public async Task<IActionResult> EditRoom(Guid id) { var room = await db.Rooms.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org); if (room is null) return NotFound(); ViewBag.Equipment = await db.EquipmentItems.AsNoTracking().Where(x => x.OrganizationId == Org && x.IsActive).OrderBy(x => x.Name).ToListAsync(); return View("RoomForm", room); }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpPost("/admin/rooms/{id:guid}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoom(Guid id, Room input) { var r = await db.Rooms.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org); if (r is null) return NotFound(); r.Name = input.Name; r.Building = input.Building; r.Floor = input.Floor; r.Capacity = input.Capacity; r.EquipmentTags = Tags(input.EquipmentTags); r.RequiresApproval = input.RequiresApproval; r.IsActive = input.IsActive; await db.SaveChangesAsync(); TempData["FlashSuccess"] = "แก้ไขห้องสำเร็จ"; return RedirectToAction(nameof(AdminRooms)); }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpPost("/admin/rooms/{id:guid}/deactivate"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateRoom(Guid id)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (room is null) return NotFound();
        room.IsActive = false;
        await db.SaveChangesAsync();
        TempData["FlashSuccess"] = "ปิดใช้งานห้องสำเร็จ";
        return RedirectToAction(nameof(AdminRooms));
    }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpPost("/admin/rooms/{id:guid}/activate"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateRoom(Guid id)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (room is null) return NotFound();
        room.IsActive = true;
        await db.SaveChangesAsync();
        TempData["FlashSuccess"] = "เปิดใช้งานห้องสำเร็จ";
        return RedirectToAction(nameof(AdminRooms));
    }

    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpPost("/admin/rooms/equipment/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEquipment(string name)
    {
        name = name?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(name) && !await db.EquipmentItems.AnyAsync(x => x.OrganizationId == Org && x.Name == name))
            db.EquipmentItems.Add(new EquipmentItem { OrganizationId = Org, Name = name });
        await db.SaveChangesAsync();
        TempData["FlashSuccess"] = "บันทึกอุปกรณ์สำเร็จ";
        return RedirectToAction(nameof(AdminRooms));
    }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpPost("/admin/rooms/equipment/{id:guid}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEquipment(Guid id, string name)
    {
        var item = await db.EquipmentItems.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (item is null) return NotFound();
        name = name?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(name)) item.Name = name;
        await db.SaveChangesAsync();
        TempData["FlashSuccess"] = "แก้ไขอุปกรณ์สำเร็จ";
        return RedirectToAction(nameof(AdminRooms));
    }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpPost("/admin/rooms/equipment/{id:guid}/deactivate"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateEquipment(Guid id)
    {
        var item = await db.EquipmentItems.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (item is null) return NotFound();
        item.IsActive = false;
        await db.SaveChangesAsync();
        TempData["FlashSuccess"] = "ปิดใช้งานอุปกรณ์สำเร็จ";
        return RedirectToAction(nameof(AdminRooms));
    }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/rooms/{id:guid}/sharing")] public async Task<IActionResult> RoomSharing(Guid id) => await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org) is { } room ? View("RoomSharing", room) : NotFound();

    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/members")] public async Task<IActionResult> Members()
    {
        ViewBag.AvailableOrganizations = User.IsInRole("SuperAdmin")
            ? await db.Organizations.AsNoTracking().OrderBy(x => x.Name).ToListAsync()
            : [];
        return View("Members", await db.Users.AsNoTracking().Where(x => x.OrganizationId == Org).OrderBy(x => x.DisplayName).ToListAsync());
    }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/reports")]
    public async Task<IActionResult> Reports(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var zone = await OrgTimeZone();
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
        var from = (fromDate ?? today.AddDays(-29)).Date;
        var to = (toDate ?? today).Date;
        if (to < from) (from, to) = (to, from);
        var fromUtc = ToUtc(from, zone);
        var toUtc = ToUtc(to.AddDays(1), zone);
        return View("Reports", new ReportPageModel
        {
            FromDate = from,
            ToDate = to,
            TimeZoneId = zone.Id,
            Rooms = await db.Rooms.AsNoTracking().Where(x => x.OrganizationId == Org && x.IsActive).OrderBy(x => x.Name).ToListAsync(),
            Users = await db.Users.AsNoTracking().Where(x => x.OrganizationId == Org).OrderBy(x => x.DisplayName).ToListAsync(),
            Bookings = await db.Bookings.AsNoTracking().Where(x => x.OrganizationId == Org && x.StartAtUtc < toUtc && x.EndAtUtc > fromUtc).OrderByDescending(x => x.StartAtUtc).ToListAsync()
        });
    }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/approvals")] public async Task<IActionResult> Approvals() => View("Approvals", new ApprovalPageModel
    {
        Bookings = await (from booking in db.Bookings.AsNoTracking() join room in db.Rooms.AsNoTracking() on booking.RoomId equals room.Id where booking.Status == BookingStatus.Pending && (booking.OrganizationId == Org || room.OrganizationId == Org) select booking).OrderBy(x => x.StartAtUtc).ToListAsync(),
        Rooms = await db.Rooms.AsNoTracking().Where(x => x.OrganizationId == Org).OrderBy(x => x.Name).ToListAsync(),
        Users = await db.Users.AsNoTracking().Where(x => x.OrganizationId == Org).OrderBy(x => x.DisplayName).ToListAsync()
    });
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/settings")] public async Task<IActionResult> Settings()
    {
        ViewBag.Organization = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Org);
        ViewBag.CanManageOrganizations = User.IsInRole("SuperAdmin");
        if (User.IsInRole("SuperAdmin"))
            ViewBag.Organizations = await db.Organizations.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        return View("Settings", await db.OrganizationSettings.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == Org) ?? new OrganizationSettings { OrganizationId = Org });
    }
    [HttpGet("/profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Me && x.OrganizationId == Org);
        if (user is null) return NotFound();
        return View("Profile", user);
    }
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    [HttpGet("/admin/audit")] public async Task<IActionResult> Audit()
    {
        var rows = await (from log in db.AuditLogs.AsNoTracking()
                          join actor in db.Users.AsNoTracking() on log.ActorUserId equals actor.Id into actors
                          from actor in actors.DefaultIfEmpty()
                          join booking in db.Bookings.AsNoTracking() on log.TargetId equals booking.Id into bookings
                          from booking in bookings.DefaultIfEmpty()
                          join room in db.Rooms.AsNoTracking() on (booking == null ? Guid.Empty : booking.RoomId) equals room.Id into rooms
                          from room in rooms.DefaultIfEmpty()
                          where log.OrganizationId == Org
                          orderby log.CreatedAt descending
                          select new AuditLogRow { Id = log.Id, Action = log.Action, TargetType = log.TargetType, TargetId = log.TargetId, Metadata = log.Metadata, CreatedAt = log.CreatedAt, ActorName = actor == null ? "" : actor.DisplayName, ActorEmail = actor == null ? "" : actor.Email, TargetName = booking == null ? "" : booking.Title, RoomName = room == null ? "" : room.Name }).Take(500).ToListAsync();
        return View("Audit", new AuditLogPageModel { Logs = rows });
    }

    [AllowAnonymous, HttpGet("/invite/{token}")] public async Task<IActionResult> Invite(string token) => View("Invite", new InvitePageModel { Token = token, Email = (await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.InvitationToken == token))?.Email ?? "" });

    private IQueryable<Room> AccessibleRooms(bool book = false) => db.Rooms.Where(x => x.IsActive && (x.OrganizationId == Org || db.RoomAccesses.Any(a => a.RoomId == x.Id && a.OrganizationId == Org && a.CanView && (!book || a.CanBook)))).OrderBy(x => x.Name);
    private async Task<DashboardPageModel> DashboardData()
    {
        var zone = await OrgTimeZone();
        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
        var todayStart = ToUtc(todayLocal, zone);
        var todayEnd = ToUtc(todayLocal.AddDays(1), zone);
        return new DashboardPageModel
        {
            Organization = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Org),
            TimeZoneId = zone.Id,
            Rooms = await Rooms.ToListAsync(),
            Upcoming = await db.Bookings.AsNoTracking().Where(x => x.OrganizationId == Org && x.EndAtUtc >= DateTime.UtcNow && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow).OrderBy(x => x.StartAtUtc).Take(8).ToListAsync(),
            // Keep every status in today's dashboard list, including bookings whose time has already passed.
            TodayBookings = await db.Bookings.AsNoTracking().Where(x => x.OrganizationId == Org && x.StartAtUtc < todayEnd && x.EndAtUtc > todayStart).OrderBy(x => x.StartAtUtc).ToListAsync(),
            PendingCount = await db.Bookings.CountAsync(x => x.OrganizationId == Org && x.Status == BookingStatus.Pending)
        };
    }
    private static string[] Tags(string[]? tags) => tags?.SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
}
