using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Domain.Enums;
using RoomReservation.Infrastructure.Persistence;
namespace RoomReservation.Api.Controllers;
[ApiController,Authorize(Roles="OrgAdmin,SuperAdmin"),Route("api/v1/approvals")]
public class ApprovalsController(AppDbContext db):ControllerBase
{
 private Guid Org=>Guid.Parse(User.FindFirstValue("organization_id")!); private Guid Actor=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet] public async Task<IActionResult> List(string? query=null,Guid? room_id=null){var q=from b in db.Bookings join r in db.Rooms on b.RoomId equals r.Id into rs from r in rs.DefaultIfEmpty() join u in db.Users on b.UserId equals u.Id into us from u in us.DefaultIfEmpty() where r!=null&&(b.OrganizationId==Org||r.OrganizationId==Org)&&b.Status==BookingStatus.Pending select new{b.Id,b.Title,b.StartAtUtc,b.EndAtUtc,b.Status,b.RoomId,roomName=r==null?"":r.Name,requesterName=u==null?"":u.DisplayName,requesterEmail=u==null?"":u.Email,b.UserId,b.CreatedAt};if(room_id.HasValue)q=q.Where(x=>x.RoomId==room_id.Value);if(!string.IsNullOrWhiteSpace(query))q=q.Where(x=>x.Title.Contains(query)||x.roomName.Contains(query)||x.requesterName.Contains(query)||x.requesterEmail.Contains(query));return Ok(await q.OrderBy(x=>x.StartAtUtc).ToListAsync());}
 [HttpPost("{id:guid}/approve")] public async Task<IActionResult> Approve(Guid id)=>await Change(id,BookingStatus.Confirmed,"booking.approved",null);
 [HttpPost("{id:guid}/reject")] public async Task<IActionResult> Reject(Guid id,RejectInput? input=null)=>await Change(id,BookingStatus.Cancelled,"booking.rejected",input?.Reason);
 private async Task<IActionResult> Change(Guid id,BookingStatus status,string action,string? reason){var b=await (from booking in db.Bookings join room in db.Rooms on booking.RoomId equals room.Id where booking.Id==id&&room.OrganizationId==Org&&booking.Status==BookingStatus.Pending select booking).FirstOrDefaultAsync();if(b is null)return NotFound();if(status==BookingStatus.Confirmed&&await db.Bookings.AnyAsync(x=>x.Id!=b.Id&&x.RoomId==b.RoomId&&x.Status!=BookingStatus.Cancelled&&x.Status!=BookingStatus.NoShow&&x.StartAtUtc<b.EndAtUtc&&x.EndAtUtc>b.StartAtUtc))return Conflict(new{message="Room is no longer available for this time"});b.Status=status;b.UpdatedAt=DateTime.UtcNow;db.AuditLogs.Add(new Domain.Entities.AuditLog{OrganizationId=Org,ActorUserId=Actor,Action=action,TargetType="Booking",TargetId=b.Id,Metadata=System.Text.Json.JsonSerializer.Serialize(new{reason,status=status.ToString()})});await db.SaveChangesAsync();return Ok(b);}
 public record RejectInput(string? Reason);
}
