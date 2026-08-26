namespace RoomBooking.Domain.Entities;
public class ReservationCheckLog { public int Id { get; set; } public int CompanyId { get; set; } public int ReservationId { get; set; } public int UserId { get; set; } public DateTime? CheckInTime { get; set; } public DateTime? CheckOutTime { get; set; } public bool IsAutoCancelled { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
