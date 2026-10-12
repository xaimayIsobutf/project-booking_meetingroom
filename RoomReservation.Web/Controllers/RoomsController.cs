using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Authorization;using Microsoft.EntityFrameworkCore;using RoomReservation.Infrastructure.Persistence;using RoomReservation.Domain.Entities;using RoomReservation.Domain.Enums;using System.Security.Claims;
namespace RoomReservation.Api.Controllers;
[ApiController,Authorize,Route("api/v1/rooms")]
public class RoomsController(AppDbContext db):ControllerBase{
 Guid Org=>Guid.Parse(User.FindFirstValue("organization_id")!);
 IQueryable<Room> Visible(bool book=false)=>db.Rooms.Where(r=>r.IsActive&&(r.OrganizationId==Org||db.RoomAccesses.Any(a=>a.RoomId==r.Id&&a.OrganizationId==Org&&a.CanView&&(!book||a.CanBook))));
 [HttpGet]public async Task<IEnumerable<Room>>List()=>await Visible().ToListAsync();
 [HttpGet("{id:guid}")]public async Task<ActionResult<Room>>Get(Guid id)=>await Visible().FirstOrDefaultAsync(r=>r.Id==id) is{ }r?r:NotFound();
 [HttpGet("{id:guid}/availability")]public async Task<IActionResult>Availability(Guid id,DateTime date){if(!await Visible().AnyAsync(r=>r.Id==id))return NotFound();var d=DateTime.SpecifyKind(date.Date,DateTimeKind.Utc);return Ok(await db.Bookings.Where(x=>x.RoomId==id&&x.StartAtUtc<d.AddDays(1)&&x.EndAtUtc>d&&x.Status!=BookingStatus.Cancelled&&x.Status!=BookingStatus.NoShow).Select(x=>new{x.StartAtUtc,x.EndAtUtc,x.Status}).ToListAsync());}
 [HttpPost("search")]public async Task<IActionResult>Search(SearchInput i){if(i.EndAt<=i.StartAt)return BadRequest();var q=Visible(true).Where(r=>r.Capacity>=i.CapacityMin);if(i.Equipment.Count>0)q=q.Where(r=>i.Equipment.All(e=>r.EquipmentTags.Contains(e)));var busy=await db.Bookings.Where(b=>b.StartAtUtc<i.EndAt&&i.StartAt<b.EndAtUtc&&b.Status!=BookingStatus.Cancelled&&b.Status!=BookingStatus.NoShow).Select(b=>b.RoomId).ToListAsync();return Ok(await q.Where(r=>!busy.Contains(r.Id)).ToListAsync());}
 [HttpPost,Authorize(Roles="OrgAdmin,SuperAdmin")]public async Task<Room>Create(Room r){r.OrganizationId=Org;db.Rooms.Add(r);await db.SaveChangesAsync();return r;}
 [HttpPatch("{id:guid}"),Authorize(Roles="OrgAdmin,SuperAdmin")]public async Task<IActionResult>Update(Guid id,Room i){var r=await db.Rooms.FirstOrDefaultAsync(x=>x.Id==id&&x.OrganizationId==Org);if(r is null)return NotFound();r.Name=i.Name;r.Capacity=i.Capacity;r.Building=i.Building;r.Floor=i.Floor;r.EquipmentTags=i.EquipmentTags;r.PhotoUrls=i.PhotoUrls;r.RequiresApproval=i.RequiresApproval;await db.SaveChangesAsync();return Ok(r);}
 [HttpDelete("{id:guid}"),Authorize(Roles="OrgAdmin,SuperAdmin")]public async Task<IActionResult>Deactivate(Guid id){var r=await db.Rooms.FirstOrDefaultAsync(x=>x.Id==id&&x.OrganizationId==Org);if(r is null)return NotFound();r.IsActive=false;await db.SaveChangesAsync();return NoContent();}
 public record SearchInput(DateTime StartAt,DateTime EndAt,int CapacityMin,List<string> Equipment);
}
