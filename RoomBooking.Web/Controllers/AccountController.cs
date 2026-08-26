using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Domain.Entities;
using RoomBooking.Web.Infrastructure;
using Microsoft.AspNetCore.Identity;
namespace RoomBooking.Web.Controllers;
public class AccountController(RoomBookingDbContext db, IPasswordHasher<User> hasher) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl=null){ViewBag.Companies=db.Companies.AsNoTracking().OrderBy(x=>x.Name).ToList();return View();}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Login(string email,string password,int tenantId,string? returnUrl=null){ViewBag.Companies=db.Companies.AsNoTracking().OrderBy(x=>x.Name).ToList();var user=await db.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.CompanyId==tenantId&&x.Email==email&&x.IsActive);if(user is null||hasher.VerifyHashedPassword(user,user.PasswordHash,password)==PasswordVerificationResult.Failed){ModelState.AddModelError("","อีเมล รหัสผ่าน หรือบริษัทไม่ถูกต้อง");return View();}HttpContext.Session.SetInt32(DemoTenant.SessionKey,user.CompanyId);var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Name,user.FullName),new Claim(ClaimTypes.Email,user.Email),new Claim(ClaimTypes.Role,user.Role),new Claim("tenant_id",user.CompanyId.ToString())};await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));return LocalRedirect(returnUrl??"/");}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);return RedirectToAction(nameof(Login));}
    public IActionResult Denied()=>Content("คุณไม่มีสิทธิ์เข้าถึงหน้านี้");
}
