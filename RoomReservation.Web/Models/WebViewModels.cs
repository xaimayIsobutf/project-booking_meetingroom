using RoomReservation.Domain.Entities;

namespace RoomReservation.Web.Models;

public sealed class LoginPageModel
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public Guid? OrganizationId { get; set; }
    public List<Organization> Organizations { get; set; } = [];
    public string? Error { get; set; }
    public bool RememberMe { get; set; }
    public bool GoogleConfigured { get; set; }
}

public sealed class RegisterPageModel
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public Guid? OrganizationId { get; set; }
    public List<Organization> Organizations { get; set; } = [];
    public string? Error { get; set; }
    public bool GoogleConfigured { get; set; }
    public bool AcceptTerms { get; set; }
}

public sealed class RegisterSuccessPageModel
{
    public string Email { get; set; } = "";
    public string VerificationLink { get; set; } = "";
}

public sealed class LegalNoticePageModel
{
    public bool IsPrivacy { get; set; }
}

public sealed class ForgotPasswordPageModel
{
    public string Email { get; set; } = "";
    public bool Submitted { get; set; }
}

public sealed class DashboardPageModel
{
    public Organization? Organization { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public List<Room> Rooms { get; set; } = [];
    public List<Booking> Upcoming { get; set; } = [];
    public List<Booking> TodayBookings { get; set; } = [];
    public int PendingCount { get; set; }
}

public sealed class CalendarPageModel
{
    public DateTime Date { get; set; }
    public bool Week { get; set; }
    public List<Room> Rooms { get; set; } = [];
    public List<Booking> Bookings { get; set; } = [];
    public List<Booking> HistoryBookings { get; set; } = [];
}

public sealed class BookingFormModel
{
    public Guid? Id { get; set; }
    public Guid RoomId { get; set; }
    public string Title { get; set; } = "";
    public DateTime StartAt { get; set; } = DateTime.Today.AddHours(9);
    public DateTime EndAt { get; set; } = DateTime.Today.AddHours(10);
    public int AttendeeCount { get; set; } = 1;
    public string Notes { get; set; } = "";
    public List<Room> Rooms { get; set; } = [];
}

public sealed class ReportPageModel
{
    public List<Booking> Bookings { get; set; } = [];
    public List<Room> Rooms { get; set; } = [];
    public List<User> Users { get; set; } = [];
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public int Total => Bookings.Count;
    public int Confirmed => Bookings.Count(x => x.Status == RoomReservation.Domain.Enums.BookingStatus.Confirmed);
    public int Pending => Bookings.Count(x => x.Status == RoomReservation.Domain.Enums.BookingStatus.Pending);
    public int Cancelled => Bookings.Count(x => x.Status == RoomReservation.Domain.Enums.BookingStatus.Cancelled);
    public int NoShow => Bookings.Count(x => x.Status == RoomReservation.Domain.Enums.BookingStatus.NoShow);
    public double BookedHours => Bookings.Where(x => x.Status != RoomReservation.Domain.Enums.BookingStatus.Cancelled && x.Status != RoomReservation.Domain.Enums.BookingStatus.NoShow).Sum(x => (x.EndAtUtc - x.StartAtUtc).TotalHours);
    public double AverageMinutes => Bookings.Count == 0 ? 0 : Bookings.Where(x => x.Status != RoomReservation.Domain.Enums.BookingStatus.Cancelled && x.Status != RoomReservation.Domain.Enums.BookingStatus.NoShow).DefaultIfEmpty().Average(x => x is null ? 0 : (x.EndAtUtc - x.StartAtUtc).TotalMinutes);
    public double UtilizationPercent => Rooms.Count == 0 || (ToDate - FromDate).TotalHours <= 0 ? 0 : Math.Min(100, BookedHours / (Rooms.Count * (ToDate - FromDate).TotalHours) * 100);
}

public sealed class ApprovalPageModel
{
    public List<Booking> Bookings { get; set; } = [];
    public List<Room> Rooms { get; set; } = [];
    public List<User> Users { get; set; } = [];
}

public sealed class AuditLogPageModel
{
    public List<AuditLogRow> Logs { get; set; } = [];
}

public sealed class AuditLogRow
{
    public Guid Id { get; set; }
    public string Action { get; set; } = "";
    public string TargetType { get; set; } = "";
    public Guid TargetId { get; set; }
    public string Metadata { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string ActorName { get; set; } = "";
    public string ActorEmail { get; set; } = "";
    public string TargetName { get; set; } = "";
    public string RoomName { get; set; } = "";
}

public sealed class InvitePageModel
{
    public string Token { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public sealed class SchedulePageModel
{
    public DateTime Date { get; set; }
    public DateTime From { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Bangkok";
    public string View { get; set; } = "week";
    public List<Room> Rooms { get; set; } = [];
    public List<Booking> Bookings { get; set; } = [];
    public List<Booking> HistoryBookings { get; set; } = [];
}

public sealed class RoomDisplayPageModel
{
    public Room SelectedRoom { get; set; } = new();
    public List<Room> Rooms { get; set; } = [];
    public Booking? CurrentBooking { get; set; }
    public Booking? NextBooking { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public Guid? PairDeviceId { get; set; }
    public Guid? PairOrganizationId { get; set; }
}

public sealed class BookingsPageModel
{
    public List<Booking> Bookings { get; set; } = [];
    public Dictionary<Guid, Room> Rooms { get; set; } = [];
    public List<Room> FilterRooms { get; set; } = [];
    public string? Search { get; set; }
    public string Scope { get; set; } = "upcoming";
    public string Status { get; set; } = "all";
    public Guid? RoomId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public int UpcomingCount { get; set; }
    public int PendingCount { get; set; }
    public int CheckedInCount { get; set; }
    public int CancelledCount { get; set; }
}

public sealed class RoomManagementPageModel
{
    public List<Room> Rooms { get; set; } = [];
    public List<Room> OwnedRooms { get; set; } = [];
    public Dictionary<Guid, string> SharedRoomOwners { get; set; } = new();
    public List<Booking> Bookings { get; set; } = [];
    public List<EquipmentItem> Equipment { get; set; } = [];
    public string TimeZoneId { get; set; } = "UTC";
    public List<RoomDisplayDevice> DisplayDevices { get; set; } = [];
}

public sealed class ChooseOrganizationPageModel
{
    public List<Organization> Organizations { get; set; } = [];
    public string? Error { get; set; }
}
