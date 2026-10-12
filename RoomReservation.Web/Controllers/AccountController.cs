using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RoomReservation.Infrastructure.Persistence;
using RoomReservation.Web.Models;
using RoomReservation.Domain.Entities;

namespace RoomReservation.Web.Controllers;

public class AccountController(AppDbContext db, IPasswordHasher<RoomReservation.Domain.Entities.User> passwordHasher) : Controller
{
    [HttpGet("/Account/Login")]
    [HttpGet("/login")]
    public async Task<IActionResult> Login(string? returnUrl = null)
        => View(new LoginPageModel
        {
            Organizations = await db.Organizations.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            Error = Request.Query["googleError"].FirstOrDefault(),
            GoogleConfigured = HasGoogleConfiguration()
        });

    [AllowAnonymous, HttpGet("/Account/GoogleLogin"), HttpGet("/google-login")]
    public IActionResult GoogleLogin(Guid? organizationId, string? returnUrl = null)
        => StartGoogleChallenge(organizationId, returnUrl, "login");

    [AllowAnonymous, HttpGet("/Account/GoogleRegister"), HttpGet("/google-register")]
    public IActionResult GoogleRegister(Guid? organizationId, string? returnUrl = null)
        => StartGoogleChallenge(organizationId, returnUrl, "register");

    [AllowAnonymous, HttpGet("/Account/GoogleCallback"), HttpGet("/google-callback")]
    public async Task<IActionResult> GoogleCallback(Guid? organizationId, string? returnUrl = null, string? flow = null)
    {
        var external = await HttpContext.AuthenticateAsync("External");
        if (!external.Succeeded || external.Principal is null)
            return RedirectToLoginError("ไม่สามารถอ่านข้อมูลจาก Google ได้ กรุณาลองใหม่");

        var email = external.Principal.FindFirstValue(ClaimTypes.Email)?.Trim().ToLowerInvariant();
        var subject = external.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var displayName = external.Principal.FindFirstValue(ClaimTypes.Name)
            ?? external.Principal.FindFirstValue(ClaimTypes.GivenName)
            ?? email;
        var verifiedClaim = external.Principal.FindFirst("urn:google:verified")?.Value
            ?? external.Principal.FindFirst("email_verified")?.Value;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subject) || string.Equals(verifiedClaim, "false", StringComparison.OrdinalIgnoreCase))
            return RedirectToLoginError("บัญชี Google นี้ไม่มีอีเมลที่ยืนยันแล้ว");

        var isRegistration = string.Equals(flow, "register", StringComparison.OrdinalIgnoreCase);
        var selectedOrganizationId = organizationId is { } value && value != Guid.Empty ? value : (Guid?)null;
        User? user = null;
        string? error = null;
        var previousTenant = TenantContext.OrganizationId;
        try
        {
            TenantContext.OrganizationId = selectedOrganizationId;
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("select set_config('app.login_email', {0}, true)", email);
            if (selectedOrganizationId.HasValue)
                await db.Database.ExecuteSqlRawAsync("select set_config('app.current_org_id', {0}, true)", selectedOrganizationId.Value.ToString());

            var organization = selectedOrganizationId.HasValue
                ? await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == selectedOrganizationId.Value)
                : null;
            var settings = selectedOrganizationId.HasValue
                ? await db.OrganizationSettings.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == selectedOrganizationId.Value)
                : null;

            if (selectedOrganizationId.HasValue && organization is null)
            {
                error = "ไม่พบบริษัทที่เลือก";
            }
            else
            {
                user = await db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email);
                if (user is null)
                {
                    if (!isRegistration)
                    {
                        error = "ยังไม่พบบัญชี Google นี้ กรุณาสมัครสมาชิกก่อน";
                    }
                    else if (selectedOrganizationId.HasValue && settings?.AllowSelfSignup != true)
                    {
                        error = "บริษัทนี้ยังไม่เปิดรับสมัครสมาชิกเอง กรุณาสมัครโดยไม่เลือกบริษัท หรือ ติดต่อผู้ดูแลระบบ";
                    }
                    else
                    {
                        user = new User
                        {
                            OrganizationId = selectedOrganizationId,
                            Email = email,
                            DisplayName = displayName ?? email,
                            AuthProvider = RoomReservation.Domain.Enums.AuthProvider.Google,
                            ExternalSubject = subject,
                            Role = RoomReservation.Domain.Enums.UserRole.Employee,
                            MembershipStatus = selectedOrganizationId.HasValue ? "Pending" : "Unassigned",
                            EmailVerified = true,
                            IsActive = true
                        };
                        db.Users.Add(user);
                        await db.SaveChangesAsync();
                    }
                }
                else if (!string.IsNullOrWhiteSpace(user.ExternalSubject) && user.ExternalSubject != subject)
                {
                    error = "อีเมลนี้ผูกกับบัญชี Google อื่นอยู่แล้ว";
                }
                else if (selectedOrganizationId.HasValue && user.OrganizationId.HasValue && user.OrganizationId != selectedOrganizationId)
                {
                    error = "บัญชีนี้อยู่คนละบริษัท กรุณาเข้าสู่ระบบโดยไม่เลือกบริษัท";
                }
                else
                {
                    user.ExternalSubject = subject;
                    user.AuthProvider = RoomReservation.Domain.Enums.AuthProvider.Google;
                    user.EmailVerified = true;
                    await db.SaveChangesAsync();
                }
            }
            await transaction.CommitAsync();
        }
        finally
        {
            TenantContext.OrganizationId = previousTenant;
            await HttpContext.SignOutAsync("External");
        }

        if (error is not null || user is null)
            return RedirectToLoginError(error ?? "ไม่สามารถดำเนินการด้วย Google ได้");
        if (!user.IsActive || user.MembershipStatus == "Pending")
            return RedirectToLoginError("สมัครสมาชิกสำเร็จแล้ว กรุณารอผู้ดูแลบริษัทอนุมัติบัญชี");
        await SignInWebUser(user, persistent: false);
        if (!user.OrganizationId.HasValue)
            return RedirectToAction(nameof(ChooseOrganization));
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl);
    }

    private IActionResult StartGoogleChallenge(Guid? organizationId, string? returnUrl, string flow)
    {
        if (!HasGoogleConfiguration())
            return RedirectToLoginError("Google sign-in ยังไม่ได้ตั้งค่า Client ID/Secret");
        var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl;
        var callback = Url.Action(nameof(GoogleCallback), "Account", new { organizationId, returnUrl = safeReturnUrl, flow })!;
        return Challenge(new AuthenticationProperties { RedirectUri = callback, IsPersistent = false }, GoogleDefaults.AuthenticationScheme);
    }

    [AllowAnonymous, HttpGet("/Account/Register")]
    [HttpGet("/register")]
    public IActionResult Register()
        => View("Register", new RegisterPageModel { GoogleConfigured = HasGoogleConfiguration() });

    [AllowAnonymous, HttpGet("/terms")]
    public IActionResult Terms() => View("LegalNotice", new LegalNoticePageModel { IsPrivacy = false });

    [AllowAnonymous, HttpGet("/privacy")]
    public IActionResult Privacy() => View("LegalNotice", new LegalNoticePageModel { IsPrivacy = true });

    [AllowAnonymous, HttpPost("/Account/Register"), HttpPost("/register"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterPageModel model)
    {
        model.GoogleConfigured = HasGoogleConfiguration();
        model.DisplayName = model.DisplayName?.Trim() ?? "";
        model.Email = model.Email?.Trim().ToLowerInvariant() ?? "";

        if (string.IsNullOrWhiteSpace(model.DisplayName) || string.IsNullOrWhiteSpace(model.Email))
            return RegisterError(model, "กรุณากรอกชื่อและอีเมล");
        try
        {
            if (!new System.Net.Mail.MailAddress(model.Email).Address.Equals(model.Email, StringComparison.OrdinalIgnoreCase))
                return RegisterError(model, "รูปแบบอีเมลไม่ถูกต้อง");
        }
        catch (FormatException)
        {
            return RegisterError(model, "รูปแบบอีเมลไม่ถูกต้อง");
        }
        if (!IsStrongPassword(model.Password))
            return RegisterError(model, "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร และมีทั้งตัวอักษรกับตัวเลข");
        if (model.Password != model.ConfirmPassword)
            return RegisterError(model, "รหัสผ่านไม่ตรงกัน");
        if (!model.AcceptTerms)
            return RegisterError(model, "กรุณายอมรับข้อกำหนดการใช้งานและนโยบายความเป็นส่วนตัวก่อนสมัคร");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var user = new RoomReservation.Domain.Entities.User
        {
            OrganizationId = null,
            Email = model.Email,
            DisplayName = model.DisplayName,
            Role = RoomReservation.Domain.Enums.UserRole.Employee,
            AuthProvider = RoomReservation.Domain.Enums.AuthProvider.Password,
            MembershipStatus = "Unassigned",
            EmailVerified = false,
            EmailVerificationToken = token,
            EmailVerificationExpiresAt = DateTime.UtcNow.AddHours(24),
            IsActive = true
        };
        user.PasswordHash = passwordHasher.HashPassword(user, model.Password);

        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("select set_config('app.login_email', {0}, true)", model.Email);
        if (await db.Users.AnyAsync(x => x.Email.ToLower() == model.Email))
            return RegisterError(model, "อีเมลนี้มีบัญชีอยู่แล้ว");
        db.Users.Add(user);
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch (DbUpdateException) { return RegisterError(model, "อีเมลนี้มีบัญชีอยู่แล้ว"); }

        var verificationLink = Url.Action(nameof(VerifyEmail), "Account", new { token }, Request.Scheme) ?? $"/Account/VerifyEmail?token={token}";
        return View("RegisterSuccess", new RegisterSuccessPageModel { Email = model.Email, VerificationLink = verificationLink });
    }

    [Authorize, HttpGet("/Account/ChooseOrganization"), HttpGet("/choose-organization")]
    public async Task<IActionResult> ChooseOrganization(string? error = null)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out _)) return RedirectToAction(nameof(Login));
        return View("ChooseOrganization", new ChooseOrganizationPageModel
        {
            Organizations = await db.Organizations.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            Error = error
        });
    }

    [Authorize, HttpPost("/Account/ChooseOrganization"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChooseOrganization(Guid organizationId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return RedirectToAction(nameof(Login));
        var organization = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == organizationId);
        if (organization is null) return RedirectToAction(nameof(ChooseOrganization), new { error = "ไม่พบบริษัทที่เลือก" });

        var previousTenant = TenantContext.OrganizationId;
        TenantContext.OrganizationId = organizationId;
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync("select set_config('app.current_org_id', {0}, true)", organizationId.ToString());
            await db.Database.ExecuteSqlRawAsync("select set_config('app.current_user_id', {0}, true)", userId.ToString());
            var settings = await db.OrganizationSettings.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == organizationId);
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.OrganizationId == null);
            if (user is null) return Forbid();
            if (settings?.AllowSelfSignup != true) return RedirectToAction(nameof(ChooseOrganization), new { error = "บริษัทนี้ยังไม่เปิดรับสมาชิกเอง กรุณาติดต่อผู้ดูแลระบบ" });
            user.OrganizationId = organizationId;
            user.MembershipStatus = "Pending";
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        finally { TenantContext.OrganizationId = previousTenant; }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet("/Account/VerifyEmail")]
    [HttpGet("/verify-email")]
    public async Task<IActionResult> VerifyEmail(string? token, Guid? organizationId = null)
    {
        User? user = null;
        if (!string.IsNullOrWhiteSpace(token) && organizationId.HasValue)
        {
            var previousTenant = TenantContext.OrganizationId;
            try
            {
                TenantContext.OrganizationId = organizationId.Value;
                await using var transaction = await db.Database.BeginTransactionAsync();
                await db.Database.ExecuteSqlRawAsync("select set_config('app.current_org_id', {0}, true)", organizationId.Value.ToString());
                user = await db.Users.FirstOrDefaultAsync(x => x.OrganizationId == organizationId.Value && x.EmailVerificationToken == token && x.EmailVerificationExpiresAt > DateTime.UtcNow);
                if (user is not null)
                {
                    user.EmailVerified = true;
                    user.EmailVerificationToken = null;
                    user.EmailVerificationExpiresAt = null;
                    await db.SaveChangesAsync();
                }
                await transaction.CommitAsync();
            }
            finally
            {
                TenantContext.OrganizationId = previousTenant;
            }
        }
        else if (!string.IsNullOrWhiteSpace(token))
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("select set_config('app.email_verification_token', {0}, true)", token);
            user = await db.Users.FirstOrDefaultAsync(x => x.EmailVerificationToken == token && x.EmailVerificationExpiresAt > DateTime.UtcNow);
            if (user is not null) { user.EmailVerified = true; user.EmailVerificationToken = null; user.EmailVerificationExpiresAt = null; await db.SaveChangesAsync(); }
            await transaction.CommitAsync();
        }
        return View("RegisterVerification", user is not null);
    }

    [HttpPost("/Account/Login"), ValidateAntiForgeryToken]
    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginPageModel model, string? returnUrl = null)
    {
        model.Organizations = await db.Organizations.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        var normalizedEmail = model.Email?.Trim();
        User? user = null;
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var previousTenant = TenantContext.OrganizationId;
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                if (model.OrganizationId.HasValue && model.OrganizationId.Value != Guid.Empty)
                {
                    TenantContext.OrganizationId = model.OrganizationId.Value;
                    await db.Database.ExecuteSqlRawAsync("select set_config('app.current_org_id', {0}, true)", model.OrganizationId.Value.ToString());
                    user = await db.Users.FirstOrDefaultAsync(x => x.OrganizationId == model.OrganizationId.Value && x.Email.ToLower() == normalizedEmail.ToLower());
                }
                else
                {
                    await db.Database.ExecuteSqlRawAsync("select set_config('app.login_email', {0}, true)", normalizedEmail.ToLowerInvariant());
                    var candidates = await db.Users.Where(x => x.Email.ToLower() == normalizedEmail.ToLower() && x.IsActive).Take(2).ToListAsync();
                    user = candidates.Count == 1 ? candidates[0] : candidates.FirstOrDefault(x => x.Role == RoomReservation.Domain.Enums.UserRole.SuperAdmin);
                    if (user is null && candidates.Count > 1) model.Error = "อีเมลนี้อยู่หลายบริษัท กรุณาเลือกบริษัทก่อนเข้าสู่ระบบ";
                }
                await transaction.CommitAsync();
            }
            finally
            {
                TenantContext.OrganizationId = previousTenant;
            }
        }
        if (user?.MembershipStatus == "Pending")
        {
            model.Error = user.EmailVerified
                ? "บัญชีของคุณยืนยันอีเมลแล้ว และกำลังรอผู้ดูแลระบบอนุมัติ"
                : "กรุณายืนยันอีเมลจากลิงก์ในหน้าสมัครสมาชิกก่อนเข้าสู่ระบบ";
            return View(model);
        }
        if (user is null || !user.IsActive || !user.EmailVerified || user.PasswordHash is null || !Verify(user, model.Password, user.PasswordHash))
        {
            model.Error = "อีเมล รหัสผ่าน หรือบริษัทไม่ถูกต้อง";
            return View(model);
        }
        if (user is null) { model.Error ??= "อีเมล รหัสผ่าน หรือบริษัทไม่ถูกต้อง"; return View(model); }
        if (user.OrganizationId.HasValue && (!model.OrganizationId.HasValue || model.OrganizationId == Guid.Empty))
        {
            model.Error = "บัญชีนี้อยู่ในบริษัทแล้ว กรุณาเลือกบริษัทก่อนเข้าสู่ระบบ";
            return View(model);
        }
        if (model.OrganizationId is null && user.OrganizationId is null && user.MembershipStatus == "Unassigned")
        {
            await SignInWebUser(user, model.RememberMe);
            return RedirectToAction(nameof(ChooseOrganization));
        }
        if (user.OrganizationId is null) { model.Error = "กรุณาเลือกบริษัทหรือเลือกบริษัทจากหน้าโปรไฟล์"; return View(model); }
        var claims = BuildClaims(user);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = model.RememberMe, ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null });
        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost("/Account/SwitchOrganization"), Authorize(Roles = "SuperAdmin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchOrganization(Guid organizationId, string? returnUrl = null)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        var organizationExists = await db.Organizations.AnyAsync(x => x.Id == organizationId);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive && x.Role == RoomReservation.Domain.Enums.UserRole.SuperAdmin);
        if (!organizationExists || user is null) return Forbid();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("organization_id", organizationId.ToString())
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { AllowRefresh = true });

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl);
    }

    [HttpPatch("/api/v1/account/profile"), Authorize]
    public async Task<IActionResult> UpdateProfile(ProfileInput input)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.DisplayName)) return BadRequest("Display name is required");
        user.DisplayName = input.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(input.NewPassword))
        {
            if (input.NewPassword.Length < 8 || !input.NewPassword.Any(char.IsLetter) || !input.NewPassword.Any(char.IsDigit)) return BadRequest("Password must be at least 8 characters and include letters and numbers");
            if (input.NewPassword != input.ConfirmPassword) return BadRequest("Passwords do not match");
            user.PasswordHash = passwordHasher.HashPassword(user, input.NewPassword);
        }
        await db.SaveChangesAsync();
        var claims = BuildClaims(user);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = true, AllowRefresh = true });
        return Ok(new { user.DisplayName, user.Email });
    }

    public sealed record ProfileInput(string? DisplayName, string? NewPassword, string? ConfirmPassword);

    [HttpGet("/Account/ForgotPassword"), HttpGet("/forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordPageModel());

    [HttpPost("/Account/ForgotPassword"), HttpPost("/forgot-password"), ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordPageModel model)
    {
        model.Submitted = true;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Denied() => Content("คุณไม่มีสิทธิ์เข้าถึงหน้านี้");

    private bool HasGoogleConfiguration()
        => !string.IsNullOrWhiteSpace(HttpContext.RequestServices.GetService<IConfiguration>()?["Authentication:Google:ClientId"])
            && !string.IsNullOrWhiteSpace(HttpContext.RequestServices.GetService<IConfiguration>()?["Authentication:Google:ClientSecret"]);

    private IActionResult RedirectToLoginError(string message)
        => Redirect($"/Account/Login?googleError={Uri.EscapeDataString(message)}");

    private async Task SignInWebUser(User user, bool persistent)
    {
        var claims = BuildClaims(user);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = persistent, AllowRefresh = true, ExpiresUtc = persistent ? DateTimeOffset.UtcNow.AddDays(30) : null });
    }

    private static Claim[] BuildClaims(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (user.OrganizationId.HasValue) claims.Add(new Claim("organization_id", user.OrganizationId.Value.ToString()));
        return claims.ToArray();
    }

    private bool Verify(RoomReservation.Domain.Entities.User user, string value, string hash)
    {
        if (hash.StartsWith("AQAAAA", StringComparison.Ordinal))
            return passwordHasher.VerifyHashedPassword(user, hash, value) != PasswordVerificationResult.Failed;
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value))) == hash;
    }

    private IActionResult RegisterError(RegisterPageModel model, string message)
    {
        model.Error = message;
        return View("Register", model);
    }

    private static bool IsStrongPassword(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.Length >= 8 && value.Any(char.IsLetter) && value.Any(char.IsDigit);
}
