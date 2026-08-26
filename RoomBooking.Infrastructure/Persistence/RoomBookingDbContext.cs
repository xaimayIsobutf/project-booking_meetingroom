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
    }
}
