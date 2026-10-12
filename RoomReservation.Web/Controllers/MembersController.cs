using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RoomReservation.Infrastructure.Persistence;
using RoomReservation.Domain.Entities;
using RoomReservation.Domain.Enums;
using System.Security.Claims;
using System.Security.Cryptography;

namespace RoomReservation.Api.Controllers;

[ApiController, Authorize(Roles = "OrgAdmin,SuperAdmin"), Route("api/v1/org/members")]
public class MembersController(AppDbContext db, IPasswordHasher<User> passwordHasher) : ControllerBase
{
    private Guid Org => Guid.Parse(User.FindFirstValue("organization_id")!);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var members = await db.Users
            .Where(x => x.OrganizationId == Org)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .Select(x => new
            {
                x.Id,
                x.OrganizationId,
                x.Email,
                x.DisplayName,
                role = x.Role.ToString(),
                x.IsActive,
                x.MembershipStatus,
                x.EmailVerified,
                x.InvitedAt,
                x.InvitationToken,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite(InviteInput input)
    {
        if (!Enum.TryParse<UserRole>(input.Role, true, out var parsedRole)) return BadRequest("Invalid role");
        if (string.IsNullOrWhiteSpace(input.Email)) return BadRequest("Email is required");
        var email = input.Email.Trim();
        var targetOrg = User.IsInRole("SuperAdmin") && input.OrganizationId.HasValue ? input.OrganizationId.Value : Org;
        if (!await db.Organizations.AnyAsync(x => x.Id == targetOrg)) return NotFound("Organization not found");
        if (await db.Users.AnyAsync(x => x.OrganizationId == targetOrg && x.Email == email && x.IsActive)) return Conflict("Email already exists");
        var user = new User { Id = Guid.NewGuid(), OrganizationId = targetOrg, Email = email.ToLowerInvariant(), DisplayName = input.DisplayName?.Trim() ?? "", Role = parsedRole, InvitedAt = DateTime.UtcNow, InvitationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), InvitationExpiresAt = DateTime.UtcNow.AddDays(7), IsActive = true, MembershipStatus = "Active", EmailVerified = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(new { user.Id, user.Email, user.InvitedAt, invite_token = user.InvitationToken, invite_url = $"/invite/{user.InvitationToken}" });
    }

    [HttpPost("{id:guid}/resend")]
    public async Task<IActionResult> Resend(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org && x.IsActive);
        if (user is null) return NotFound();
        user.InvitedAt = DateTime.UtcNow;
        user.InvitationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.InvitationExpiresAt = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();
        return Ok(new { user.Email, invite_token = user.InvitationToken, invite_url = $"/invite/{user.InvitationToken}" });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMemberInput input)
    {
        if (!Enum.TryParse<UserRole>(input.Role, true, out var parsedRole)) return BadRequest("Invalid role");
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.DisplayName)) return BadRequest("Name and email are required");
        if (input.Password.Length < 8 || !input.Password.Any(char.IsLetter) || !input.Password.Any(char.IsDigit)) return BadRequest("Password must contain at least 8 characters, letters and numbers");
        if (await db.Users.AnyAsync(x => x.OrganizationId == Org && x.Email == input.Email)) return Conflict("Email already exists");
        var targetOrg = User.IsInRole("SuperAdmin") && input.OrganizationId.HasValue ? input.OrganizationId.Value : Org;
        if (!await db.Organizations.AnyAsync(x => x.Id == targetOrg)) return NotFound("Organization not found");
        if (await db.Users.AnyAsync(x => x.OrganizationId == targetOrg && x.Email == input.Email.Trim())) return Conflict("Email already exists");
        var user = new User { OrganizationId = targetOrg, Email = input.Email.Trim(), DisplayName = input.DisplayName.Trim(), Role = parsedRole, IsActive = true };
        user.PasswordHash = passwordHasher.HashPassword(user, input.Password);
        user.MembershipStatus = "Active";
        user.EmailVerified = true;
        db.Users.Add(user);
        var currentTenant = TenantContext.OrganizationId;
        try
        {
            if (User.IsInRole("SuperAdmin") && input.OrganizationId.HasValue)
                TenantContext.OrganizationId = null;
            await db.SaveChangesAsync();
        }
        finally
        {
            TenantContext.OrganizationId = currentTenant;
        }
        return Ok(user);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (user is null) return NotFound();
        if (user.MembershipStatus != "Pending") return BadRequest("Only pending members can be approved");
        if (!user.EmailVerified) return BadRequest("Member must verify email before approval");
        user.MembershipStatus = "Active";
        user.IsActive = true;
        user.InvitedAt = null;
        user.InvitationToken = null;
        user.InvitationExpiresAt = null;
        db.AuditLogs.Add(new AuditLog { OrganizationId = Org, ActorUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "MemberApproved", TargetType = "User", TargetId = user.Id, Metadata = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { user.Id, status = user.MembershipStatus });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (user is null) return NotFound();
        if (user.MembershipStatus != "Pending") return BadRequest("Only pending members can be rejected");
        user.MembershipStatus = "Rejected";
        user.IsActive = false;
        db.AuditLogs.Add(new AuditLog { OrganizationId = Org, ActorUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "MemberRejected", TargetType = "User", TargetId = user.Id, Metadata = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { user.Id, status = user.MembershipStatus });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMemberInput input)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (user is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Role) && !Enum.TryParse<UserRole>(input.Role, true, out _)) return BadRequest("Invalid role");
        user.DisplayName = input.DisplayName ?? user.DisplayName;
        if (!string.IsNullOrWhiteSpace(input.Role)) user.Role = Enum.Parse<UserRole>(input.Role, true);
        if (input.IsActive.HasValue) user.IsActive = input.IsActive.Value;
        await db.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 8 || !input.Password.Any(char.IsLetter) || !input.Password.Any(char.IsDigit))
            return BadRequest("Password must contain at least 8 characters, letters and numbers");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (user is null) return NotFound();

        user.PasswordHash = passwordHasher.HashPassword(user, input.Password);
        user.InvitedAt = null;
        user.InvitationToken = null;
        user.InvitationExpiresAt = null;
        user.MembershipStatus = "Active";
        user.EmailVerified = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == Org);
        if (user is null) return NotFound();
        user.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    public record InviteInput(string Email, string Role, string? DisplayName = null, Guid? OrganizationId = null);
    public record CreateMemberInput(string Email, string DisplayName, string Password, string Role, Guid? OrganizationId = null);
    public record UpdateMemberInput(string? DisplayName, string? Role, bool? IsActive);
    public record ResetPasswordInput(string Password);

}
