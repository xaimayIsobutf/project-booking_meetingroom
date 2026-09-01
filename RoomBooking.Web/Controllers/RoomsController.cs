using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain.Entities;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Web.Models;
using RoomBooking.Web.Infrastructure;

namespace RoomBooking.Web.Controllers;

public class RoomsController(RoomBookingDbContext db) : Controller
{
    private int? TenantId => HttpContext.TenantId();
    public IActionResult Index() => View(db.MeetingRooms.AsNoTracking().Where(x => TenantId != null && (User.IsInRole("TenantAdmin") || x.IsActive) && (x.CompanyId == TenantId || db.RoomAccesses.Any(a => a.RoomId == x.Id && a.CompanyId == TenantId && a.CanView))).OrderBy(x => x.Name)
        .Select(x => new RoomViewModel { Id = x.Id, Name = x.Name, RoomSize = x.RoomSize, Building = x.Building, Floor = x.Floor, Location = x.Location, Capacity = x.Capacity, Equipment = x.Equipment, Available = x.IsActive, RequiresApproval = x.RequiresApproval, BookingNoticeHours = x.BookingNoticeHours, OwnerCompanyName = x.Company!.Name, IsShared = x.CompanyId != TenantId, HasSharing = db.RoomAccesses.Any(a => a.RoomId == x.Id) }).ToList());

    [Authorize(Roles = "TenantAdmin")]
    public IActionResult Create() => View(new RoomViewModel());

    [Authorize(Roles = "TenantAdmin")]
    public IActionResult Share(int id)
    {
        var room = db.MeetingRooms.AsNoTracking().FirstOrDefault(x => x.Id == id && x.CompanyId == TenantId);
        if (room is null) return NotFound();
        ViewBag.Room = room;
        ViewBag.Companies = db.Companies.Where(x => x.Id != TenantId).OrderBy(x => x.Name).ToList();
        ViewBag.Accesses = db.RoomAccesses.Include(x => x.Company).Where(x => x.RoomId == id).ToList();
        return View();
    }

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Share(int id, int companyId, bool canView = true, bool canBook = true, bool requiresApproval = true)
    {
        var room = db.MeetingRooms.FirstOrDefault(x => x.Id == id && x.CompanyId == TenantId);
        if (room is null || !db.Companies.Any(x => x.Id == companyId && x.Id != TenantId)) return NotFound();
        var access = db.RoomAccesses.FirstOrDefault(x => x.RoomId == id && x.CompanyId == companyId);
        if (access is null) db.RoomAccesses.Add(new RoomAccess { RoomId = id, CompanyId = companyId, CanView = canView, CanBook = canBook, RequiresApproval = requiresApproval });
        else { access.CanView = canView; access.CanBook = canBook; access.RequiresApproval = requiresApproval; }
        db.SaveChanges();
        return RedirectToAction(nameof(Share), new { id });
    }

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RemoveShare(int id, int companyId)
    {
        var access = db.RoomAccesses.FirstOrDefault(x => x.RoomId == id && x.CompanyId == companyId && x.Room!.CompanyId == TenantId);
        if (access is not null) { db.RoomAccesses.Remove(access); db.SaveChanges(); }
        return RedirectToAction(nameof(Share), new { id });
    }

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(RoomViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var company = db.Companies.FirstOrDefault(x => x.Id == TenantId);
        if (company is null) return BadRequest("ยังไม่มีบริษัทในระบบ");
        db.MeetingRooms.Add(new MeetingRoom { CompanyId = company.Id, Name = model.Name.Trim(), RoomSize = model.RoomSize.Trim(), Building = model.Building.Trim(), Floor = model.Floor.Trim(), Location = $"{model.Building.Trim()} · {model.Floor.Trim()}", Capacity = model.Capacity, Equipment = model.Equipment.Trim(), IsActive = model.Available, RequiresApproval = model.RequiresApproval, BookingNoticeHours = Math.Max(0, model.BookingNoticeHours) });
        db.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "TenantAdmin")]
    public IActionResult Edit(int id)
    {
        var room = db.MeetingRooms.Find(id);
        return room is null ? NotFound() : View(new RoomViewModel { Id = room.Id, Name = room.Name, RoomSize = room.RoomSize, Building = room.Building, Floor = room.Floor, Location = room.Location, Capacity = room.Capacity, Equipment = room.Equipment, Available = room.IsActive, RequiresApproval = room.RequiresApproval, BookingNoticeHours = room.BookingNoticeHours });
    }

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(RoomViewModel model)
    {
        var room = db.MeetingRooms.Find(model.Id);
        if (room is null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        room.Name = model.Name.Trim(); room.RoomSize = model.RoomSize.Trim(); room.Building = model.Building.Trim(); room.Floor = model.Floor.Trim(); room.Location = $"{room.Building} · {room.Floor}"; room.Capacity = model.Capacity; room.Equipment = model.Equipment.Trim(); room.IsActive = model.Available; room.RequiresApproval = model.RequiresApproval; room.BookingNoticeHours = Math.Max(0, model.BookingNoticeHours);
        db.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var room = db.MeetingRooms.Find(id);
        if (room is not null) { room.IsActive = false; db.SaveChanges(); }
        return RedirectToAction(nameof(Index));
    }
}
