namespace RoomBooking.Domain.Entities;

public class RoomAccess
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int CompanyId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanBook { get; set; } = true;
    public bool RequiresApproval { get; set; } = true;
    public MeetingRoom? Room { get; set; }
    public Company? Company { get; set; }
}
