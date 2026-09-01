using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain.Entities;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Web.Infrastructure;
namespace RoomBooking.Web.Controllers;
public class UsersController(RoomBookingDbContext db, IPasswordHasher<User> hasher) : Controller
{
    private int? TenantId => HttpContext.TenantId();
    public IActionResult Index() => View(db.Users.Include(x=>x.Company).AsNoTracking().Where(x=>TenantId != null && x.CompanyId==TenantId).OrderBy(x=>x.FullName).ToList());
    [Authorize(Roles = "TenantAdmin")]
    public IActionResult Create()=>View(new User());
[Authorize(Roles = "TenantAdmin")]
[HttpPost,ValidateAntiForgeryToken] public IActionResult Create(User model, string password){if(string.IsNullOrWhiteSpace(password) || password.Length < 4) ModelState.AddModelError("", "รหัสผ่านต้องมีอย่างน้อย 4 ตัวอักษร");if(!ModelState.IsValid)return View(model);var t=db.Companies.FirstOrDefault(x=>x.Id==TenantId);if(t==null)return BadRequest("ยังไม่มีบริษัท");model.CompanyId=t.Id;model.PasswordHash=hasher.HashPassword(model, password);db.Users.Add(model);db.SaveChanges();return RedirectToAction(nameof(Index));}
    [Authorize(Roles = "TenantAdmin")]
    public IActionResult Edit(int id){var u=db.Users.Find(id);return u==null?NotFound():View(u);}
    [Authorize(Roles = "TenantAdmin")]
    [HttpPost,ValidateAntiForgeryToken] public IActionResult Edit(User model){var u=db.Users.Find(model.Id);if(u==null)return NotFound();if(!ModelState.IsValid)return View(model);u.FullName=model.FullName;u.Email=model.Email;u.Role=model.Role;u.IsActive=model.IsActive;db.SaveChanges();return RedirectToAction(nameof(Index));}
    [Authorize(Roles = "TenantAdmin")]
    [HttpPost,ValidateAntiForgeryToken] public IActionResult Delete(int id){var u=db.Users.Find(id);if(u!=null){u.IsActive=false;db.SaveChanges();}return RedirectToAction(nameof(Index));}
}
