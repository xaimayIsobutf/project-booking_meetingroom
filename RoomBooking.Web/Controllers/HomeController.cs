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

    public HomeController(ILogger<HomeController> logger, RoomBookingDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public IActionResult Index()
    {
        var tenantId = HttpContext.TenantId();
        if (tenantId is null) return RedirectToAction("Login", "Account");
        var today = DateTime.Today; var tomorrow = today.AddDays(1); var now = DateTime.Now;
        var rooms = _db.MeetingRooms.AsNoTracking().Where(x=>x.CompanyId == tenantId && x.IsActive).ToList();
        var reservations = _db.Reservations.Include(x=>x.MeetingRoom).Where(x=>x.CompanyId == tenantId && x.StartAt < tomorrow && x.EndAt >= today).OrderBy(x=>x.StartAt).Take(20).ToList();
        var currentRoomIds = reservations.Where(r=>r.StartAt <= now && r.EndAt > now && r.Status != "Cancelled").Select(r=>r.MeetingRoomId).ToHashSet();
        return View(new DashboardViewModel { RoomCount=rooms.Count, TodayReservations=reservations.Count, AvailableRooms=rooms.Count(x=>!currentRoomIds.Contains(x.Id)), Rooms=rooms.Take(6).Select(x=>new RoomViewModel { Id=x.Id, Name=x.Name, Location=x.Location, Capacity=x.Capacity, Equipment=x.Equipment, Available=!currentRoomIds.Contains(x.Id) }).ToList(), Reservations=reservations.Select(x => new ReservationViewModel { RoomName=x.MeetingRoom!.Name, Subject=x.Subject, Time=$"{x.StartAt:HH:mm} - {x.EndAt:HH:mm}", Status=x.Status == "Cancelled" || x.Status == "Rejected" ? "ยกเลิก" : x.StartAt <= now && x.EndAt > now ? "กำลังใช้งาน" : "จองแล้ว" }).ToList() });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
