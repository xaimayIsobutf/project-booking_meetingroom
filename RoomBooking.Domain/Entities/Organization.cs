namespace RoomBooking.Domain.Entities;

public class Building { public int Id { get; set; } public int CompanyId { get; set; } public Company? Company { get; set; } public string Name { get; set; } = ""; public string Code { get; set; } = ""; public string Address { get; set; } = ""; public bool IsActive { get; set; } = true; public ICollection<Floor> Floors { get; set; } = []; }
public class Floor { public int Id { get; set; } public int BuildingId { get; set; } public Building? Building { get; set; } public string Name { get; set; } = ""; public int FloorNumber { get; set; } public bool IsActive { get; set; } = true; public ICollection<MeetingRoom> Rooms { get; set; } = []; }
public class Facility { public int Id { get; set; } public string Name { get; set; } = ""; public string Icon { get; set; } = ""; public ICollection<RoomFacility> RoomFacilities { get; set; } = []; }
public class RoomFacility { public int RoomId { get; set; } public int FacilityId { get; set; } public MeetingRoom? Room { get; set; } public Facility? Facility { get; set; } }
