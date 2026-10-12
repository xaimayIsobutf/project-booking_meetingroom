using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RoomReservation.Infrastructure.Persistence;

namespace RoomReservation.Api.Controllers;

[ApiController, Route("api/v1/auth")]
public class AuthController(AppDbContext db, IConfiguration config, IPasswordHasher<Domain.Entities.User> passwordHasher) : ControllerBase
{
    public record LoginRequest(string Email, string Password, Guid? OrganizationId = null);
    public record SwitchOrganizationRequest(Guid OrganizationId);
    public record RegisterOrgRequest(string OrganizationName, string AdminEmail, string AdminName, string Password);
    public record CreateOrganizationRequest(string Name, string? DefaultTimezone = null);
    public record UpdateOrganizationRequest(string Name, string? DefaultTimezone = null);
    public record InviteAcceptRequest(string Token, string Password);

    [HttpPost("register-org")]
    public async Task<IActionResult> RegisterOrg(RegisterOrgRequest input)
    {
        if (input.Password.Length < 8 || !input.Password.Any(char.IsLetter) || !input.Password.Any(char.IsDigit)) return BadRequest("Password must contain at least 8 characters, letters and numbers");
        var organization = new Domain.Entities.Organization { Name = input.OrganizationName };
        var user = new Domain.Entities.User { OrganizationId = organization.Id, Email = input.AdminEmail.Trim().ToLowerInvariant(), DisplayName = input.AdminName.Trim(), Role = Domain.Enums.UserRole.OrgAdmin, PasswordHash = Hash(input.Password), MembershipStatus = "Active", EmailVerified = true };
        db.Organizations.Add(organization); db.Users.Add(user); db.OrganizationSettings.Add(new Domain.Entities.OrganizationSettings { OrganizationId = organization.Id }); await db.SaveChangesAsync();
        return Ok(new { organization_id = organization.Id, user_id = user.Id });
    }

    [HttpPost("google")]
    public IActionResult Google() => StatusCode(501, new { error = "Google OAuth provider is not configured" });

    [HttpPost("refresh")]
    public IActionResult Refresh() => StatusCode(501, new { error = "Refresh token storage is not configured" });

    [HttpPost("invite-accept")]
    public async Task<IActionResult> InviteAccept(InviteAcceptRequest input)
    {
        if (input.Password.Length < 8 || !input.Password.Any(char.IsLetter) || !input.Password.Any(char.IsDigit)) return BadRequest("Password must contain at least 8 characters, letters and numbers");
        var user = await db.Users.SingleOrDefaultAsync(x => x.InvitationToken == input.Token && x.InvitedAt != null && x.InvitationExpiresAt > DateTime.UtcNow);
        if (user is null) return NotFound();
        user.PasswordHash = Hash(input.Password); user.InvitedAt = null; user.InvitationToken = null; user.InvitationExpiresAt = null; user.MembershipStatus = "Active"; user.EmailVerified = true; await db.SaveChangesAsync();
        return Ok(new { accepted = true, user_id = user.Id });
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> Organizations() => Ok(await db.Organizations.AsNoTracking().OrderBy(x => x.Name).Select(x => new { id = x.Id, name = x.Name, defaultTimezone = x.DefaultTimezone }).ToListAsync());

    [Authorize(Roles = "SuperAdmin"), HttpPost("organizations")]
    public async Task<IActionResult> CreateOrganization(CreateOrganizationRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest("Organization name is required");
        if (await db.Organizations.AnyAsync(x => x.Name == input.Name.Trim())) return Conflict("Organization already exists");
        var organization = new Domain.Entities.Organization { Name = input.Name.Trim(), DefaultTimezone = NormalizeTimezone(input.DefaultTimezone) };
        db.Organizations.Add(organization);
        db.OrganizationSettings.Add(new Domain.Entities.OrganizationSettings { OrganizationId = organization.Id });
        // SuperAdmin is creating a new tenant. Temporarily disable the tenant
        // interceptor so it does not rewrite the new settings row to the
        // currently selected organization (which causes a unique-key error).
        var currentTenant = TenantContext.OrganizationId;
        try
        {
            TenantContext.OrganizationId = null;
            await db.SaveChangesAsync();
        }
        finally
        {
            TenantContext.OrganizationId = currentTenant;
        }
        return Ok(new { id = organization.Id, name = organization.Name, defaultTimezone = organization.DefaultTimezone });
    }

    [Authorize(Roles = "SuperAdmin"), HttpPatch("organizations/{id:guid}")]
    public async Task<IActionResult> UpdateOrganization(Guid id, UpdateOrganizationRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest("Organization name is required");
        var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == id);
        if (organization is null) return NotFound();
        if (await db.Organizations.AnyAsync(x => x.Id != id && x.Name == input.Name.Trim())) return Conflict("Organization already exists");
        organization.Name = input.Name.Trim(); organization.DefaultTimezone = NormalizeTimezone(input.DefaultTimezone ?? organization.DefaultTimezone); await db.SaveChangesAsync();
        return Ok(new { id = organization.Id, name = organization.Name, defaultTimezone = organization.DefaultTimezone });
    }

    [Authorize(Roles = "SuperAdmin"), HttpDelete("organizations/{id:guid}")]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == id);
        if (organization is null) return NotFound();
        if (await db.Users.AnyAsync(x => x.OrganizationId == id)) return Conflict("Organization has members");
        db.Organizations.Remove(organization); await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "SuperAdmin"), HttpPost("switch-organization")]
    public async Task<IActionResult> SwitchOrganization(SwitchOrganizationRequest input)
    {
        if (!await db.Organizations.AnyAsync(x => x.Id == input.OrganizationId)) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id.ToString() == userId && x.IsActive && x.Role == Domain.Enums.UserRole.SuperAdmin);
        if (user is null) return Unauthorized();
        return Ok(CreateSession(user, input.OrganizationId));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest input)
    {
        var candidates = await db.Users.Where(x => x.Email.ToLower() == input.Email.Trim().ToLower() && x.IsActive && x.MembershipStatus == "Active" && x.EmailVerified).ToListAsync();
        var user = candidates.FirstOrDefault(x => input.OrganizationId.HasValue && x.OrganizationId == input.OrganizationId.Value) ?? candidates.FirstOrDefault(x => x.Role == Domain.Enums.UserRole.SuperAdmin);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !Verify(user, input.Password, user.PasswordHash)) return Unauthorized();
        var organizationId = input.OrganizationId ?? user.OrganizationId;
        if (!organizationId.HasValue || !await db.Organizations.AnyAsync(x => x.Id == organizationId.Value)) return Unauthorized();
        return Ok(CreateSession(user, organizationId.Value));
    }

    private object CreateSession(Domain.Entities.User user, Guid organizationId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("organization_id", organizationId.ToString()), new Claim("role", user.Role.ToString()), new Claim(ClaimTypes.Name, user.DisplayName), new Claim(ClaimTypes.Email, user.Email) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "development-only-change-this-secret-32chars"));
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new { access_token = new JwtSecurityTokenHandler().WriteToken(token), user_id = user.Id, organization_id = organizationId, display_name = user.DisplayName, email = user.Email, role = user.Role.ToString() };
    }

    bool Verify(Domain.Entities.User user, string value, string hash)
    {
        if (hash.StartsWith("AQAAAA", StringComparison.Ordinal))
            return passwordHasher.VerifyHashedPassword(user, hash, value) != PasswordVerificationResult.Failed;
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value))) == hash;
    }
    static string Hash(string value) { using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value))); }
    static string NormalizeTimezone(string? value) => value switch { "Asia/Vientiane" => "Asia/Vientiane", "Asia/Bangkok" => "Asia/Bangkok", "UTC" => "UTC", _ => "Asia/Vientiane" };
}
