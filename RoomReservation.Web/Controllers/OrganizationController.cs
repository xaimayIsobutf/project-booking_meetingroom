using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Infrastructure.Persistence;
using System.Security.Claims;

namespace RoomReservation.Api.Controllers;

[ApiController, Authorize, Route("api/v1/org")]
public class OrganizationController(AppDbContext db) : ControllerBase
{
    private Guid Org => Guid.Parse(User.FindFirstValue("organization_id")!);

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await db.Organizations.FirstOrDefaultAsync(x => x.Id == Org));

    [HttpGet("available"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Available() => Ok(await db.Organizations.AsNoTracking().Where(x => x.Id != Org).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync());

    [HttpPatch, Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Update(UpdateOrg input)
    {
        var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == Org);
        if (organization is null) return NotFound();
        organization.Name = input.Name?.Trim() ?? organization.Name;
        organization.DefaultTimezone = input.DefaultTimezone ?? organization.DefaultTimezone;
        await db.SaveChangesAsync(); return Ok(organization);
    }

    [HttpGet("settings"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Settings() => Ok(await db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrganizationId == Org));

    [HttpPatch("settings"), Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> UpdateSettings(SettingsInput input)
    {
        var settings = await db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrganizationId == Org);
        if (settings is null) return NotFound();
        if (input.NoShowTimeoutMinutes.HasValue) settings.NoShowTimeoutMinutes = input.NoShowTimeoutMinutes.Value;
        if (input.QrTokenTtlSeconds.HasValue) settings.QrTokenTtlSeconds = input.QrTokenTtlSeconds.Value;
        if (input.BusinessStartTime is not null) settings.BusinessStartTime = input.BusinessStartTime;
        if (input.BusinessEndTime is not null) settings.BusinessEndTime = input.BusinessEndTime;
        if (input.AdvanceBookingDays.HasValue) settings.AdvanceBookingDays = input.AdvanceBookingDays.Value;
        if (input.RequireApproval.HasValue) settings.RequireApproval = input.RequireApproval.Value;
        if (input.AllowSelfSignup.HasValue) settings.AllowSelfSignup = input.AllowSelfSignup.Value;
        await db.SaveChangesAsync(); return Ok(settings);
    }

    public record UpdateOrg(string? Name, string? DefaultTimezone);
    public record SettingsInput(int? NoShowTimeoutMinutes, int? QrTokenTtlSeconds, string? BusinessStartTime, string? BusinessEndTime, int? AdvanceBookingDays, bool? RequireApproval, bool? AllowSelfSignup);
}
