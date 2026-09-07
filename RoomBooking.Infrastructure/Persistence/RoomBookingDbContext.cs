using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Infrastructure.Persistence;

public class RoomBookingDbContext(DbContextOptions<RoomBookingDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<MeetingRoom> MeetingRooms => Set<MeetingRoom>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationCheckLog> ReservationCheckLogs => Set<ReservationCheckLog>();
    public DbSet<RoomAccess> RoomAccesses => Set<RoomAccess>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<RoomFacility> RoomFacilities => Set<RoomFacility>();
    public DbSet<RecurringSeries> RecurringSeries => Set<RecurringSeries>();
    public DbSet<ReservationApproval> ReservationApprovals => Set<ReservationApproval>();
    public DbSet<ReservationAttendee> ReservationAttendees => Set<ReservationAttendee>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CompanyPolicy> CompanyPolicies => Set<CompanyPolicy>();
    public DbSet<CompanyHoliday> CompanyHolidays => Set<CompanyHoliday>();
    public DbSet<BusinessHour> BusinessHours => Set<BusinessHour>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Company>().ToTable("Tenants"); b.Entity<Company>().HasKey(x => x.Id); b.Entity<Company>().Property(x => x.Id).HasColumnName("TenantId"); b.Entity<Company>().Property(x => x.Name).HasColumnName("CompanyName"); b.Entity<Company>().Property(x => x.Code).HasColumnName("Subdomain"); b.Entity<Company>().HasIndex(x => x.Code).IsUnique();
        b.Entity<User>().ToTable("Users"); b.Entity<User>().HasKey(x => x.Id); b.Entity<User>().Property(x => x.Id).HasColumnName("UserId"); b.Entity<User>().Property(x => x.CompanyId).HasColumnName("TenantId");
        b.Entity<MeetingRoom>().ToTable("Rooms"); b.Entity<MeetingRoom>().HasKey(x => x.Id); b.Entity<MeetingRoom>().Property(x => x.Id).HasColumnName("RoomId"); b.Entity<MeetingRoom>().Property(x => x.CompanyId).HasColumnName("TenantId"); b.Entity<MeetingRoom>().Property(x => x.Name).HasColumnName("RoomName"); b.Entity<MeetingRoom>().Property(x => x.RoomSize).HasColumnName("RoomSize"); b.Entity<MeetingRoom>().Property(x => x.Building).HasColumnName("Building"); b.Entity<MeetingRoom>().Property(x => x.Floor).HasColumnName("Floor"); b.Entity<MeetingRoom>().Property(x => x.Equipment).HasColumnName("Amenities"); b.Entity<MeetingRoom>().Property(x => x.BookingNoticeHours).HasColumnName("BookingNoticeHours"); b.Entity<MeetingRoom>().Property(x => x.RequiresApproval).HasColumnName("RequiresApproval"); b.Entity<MeetingRoom>().Property(x => x.IsActive).HasColumnName("Status").HasConversion(v => v ? "Active" : "Closed", v => v == "Active");
        b.Entity<Reservation>().ToTable("Reservations"); b.Entity<Reservation>().HasKey(x => x.Id); b.Entity<Reservation>().Property(x => x.Id).HasColumnName("ReservationId"); b.Entity<Reservation>().Property(x => x.CompanyId).HasColumnName("TenantId"); b.Entity<Reservation>().Property(x => x.MeetingRoomId).HasColumnName("RoomId"); b.Entity<Reservation>().Property(x => x.Subject).HasColumnName("MeetingTitle"); b.Entity<Reservation>().Property(x => x.StartAt).HasColumnName("StartTime"); b.Entity<Reservation>().Property(x => x.EndAt).HasColumnName("EndTime");
        b.Entity<ReservationCheckLog>().ToTable("ReservationCheckLogs"); b.Entity<ReservationCheckLog>().HasKey(x=>x.Id); b.Entity<ReservationCheckLog>().Property(x=>x.Id).HasColumnName("CheckLogId"); b.Entity<ReservationCheckLog>().Property(x=>x.CompanyId).HasColumnName("TenantId"); b.Entity<ReservationCheckLog>().Property(x=>x.ReservationId).HasColumnName("ReservationId"); b.Entity<ReservationCheckLog>().Property(x=>x.UserId).HasColumnName("UserId");
        b.Entity<RoomAccess>().ToTable("RoomAccesses"); b.Entity<RoomAccess>().HasKey(x => x.Id); b.Entity<RoomAccess>().HasIndex(x => new { x.RoomId, x.CompanyId }).IsUnique();
        b.Entity<Company>().HasMany(x => x.Rooms).WithOne(x => x.Company).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Company>().HasMany(x => x.Users).WithOne(x => x.Company).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<MeetingRoom>().HasMany(x => x.Reservations).WithOne(x => x.MeetingRoom).HasForeignKey(x => x.MeetingRoomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<User>().HasMany(x => x.Reservations).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReservationCheckLog>().HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
        // Restrict deletes to avoid SQL Server's multiple cascade path error.
        b.Entity<RoomAccess>().HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RoomAccess>().HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Building>().ToTable("Buildings"); b.Entity<Floor>().ToTable("Floors"); b.Entity<Facility>().ToTable("Facilities"); b.Entity<RoomFacility>().ToTable("RoomFacilities"); b.Entity<RoomFacility>().HasKey(x => new { x.RoomId, x.FacilityId });
        b.Entity<Building>().HasMany(x => x.Floors).WithOne(x => x.Building).HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
        // FloorId is staged for the next database migration. Keep the current
        // production schema compatible until Rooms.FloorId is added.
        b.Entity<MeetingRoom>().Ignore(x => x.FloorId);
        b.Entity<MeetingRoom>().Ignore(x => x.Status);
        // Prevent EF convention from creating a shadow FloorId/FloorId1 column
        // before the existing Rooms table receives its migration.
        b.Entity<Floor>().Ignore(x => x.Rooms);
        // These Reservation fields are staged until the corresponding columns
        // are applied to the existing database.
        b.Entity<Reservation>().Ignore(x => x.OrganizerUserId);
        b.Entity<Reservation>().Ignore(x => x.CreatedByUserId);
        b.Entity<Reservation>().Ignore(x => x.RecurringSeriesId);
        b.Entity<Reservation>().Ignore(x => x.ApprovalDeadlineAt);
        b.Entity<Reservation>().Ignore(x => x.Note);
        b.Entity<Reservation>().Ignore(x => x.UpdatedAt);
        // RecurringSeries is staged only; do not let EF create a shadow
        // RecurringSeriesId column on the existing Reservations table.
        b.Entity<RecurringSeries>().Ignore(x => x.Reservations);
        b.Entity<RecurringSeries>().ToTable("RecurringSeries"); b.Entity<ReservationApproval>().ToTable("ReservationApprovals"); b.Entity<ReservationAttendee>().ToTable("ReservationAttendees"); b.Entity<AuditLog>().ToTable("AuditLogs"); b.Entity<CompanyPolicy>().ToTable("CompanyPolicies"); b.Entity<CompanyHoliday>().ToTable("CompanyHolidays"); b.Entity<BusinessHour>().ToTable("BusinessHours");
        b.Entity<ReservationApproval>().HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ReservationAttendee>().HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);

        // PostgreSQL: map DateTime/DateTime? columns เป็น timestamptz แทน default (timestamp without time zone)
        // เพราะโค้ดใช้ DateTime.UtcNow (Kind=Utc) ซึ่ง Npgsql ไม่ยอมเขียนลงคอลัมน์แบบไม่มี time zone
        foreach (var entityType in b.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamptz");
                }
            }
        }
    }
}
