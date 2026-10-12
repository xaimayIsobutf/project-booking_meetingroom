using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Infrastructure.Persistence;

namespace RoomReservation.Api.Controllers;

[ApiController, Authorize(Roles = "OrgAdmin,SuperAdmin"), Route("api/v1/audit-log")]
public class AuditLogController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(DateTime? from = null, DateTime? to = null, string? action = null, string? target_type = null, Guid? actor_id = null, Guid? organization_id = null)
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var currentOrg = Guid.Parse(User.FindFirstValue("organization_id")!);
        var query = from log in db.AuditLogs
                    join actor in db.Users on log.ActorUserId equals actor.Id into actors
                    from actor in actors.DefaultIfEmpty()
                    join org in db.Organizations on log.OrganizationId equals org.Id into orgs
                    from org in orgs.DefaultIfEmpty()
                    select new { log.Id, log.Action, log.TargetType, log.TargetId, log.Metadata, log.CreatedAt, organizationId = log.OrganizationId, organizationName = org == null ? "" : org.Name, actorUserId = log.ActorUserId, actorName = actor == null ? "" : actor.DisplayName, actorEmail = actor == null ? "" : actor.Email, actorRole = actor == null ? "" : actor.Role.ToString() };
        if (!isSuperAdmin) query = query.Where(x => x.organizationId == currentOrg);
        if (isSuperAdmin && organization_id.HasValue) query = query.Where(x => x.organizationId == organization_id.Value);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(target_type)) query = query.Where(x => x.TargetType == target_type);
        if (actor_id.HasValue) query = query.Where(x => x.actorUserId == actor_id.Value);
        return Ok(await query.OrderByDescending(x => x.CreatedAt).Take(500).ToListAsync());
    }
}
