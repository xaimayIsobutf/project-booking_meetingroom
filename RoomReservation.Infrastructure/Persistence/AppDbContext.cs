using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoomReservation.Domain.Entities;
using System.Text;
using System.Text.Json;

namespace RoomReservation.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<EquipmentItem> EquipmentItems => Set<EquipmentItem>();
    public DbSet<RoomAccess> RoomAccesses => Set<RoomAccess>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<RecurringSeries> RecurringSeries => Set<RecurringSeries>();
    public DbSet<QrDevice> QrDevices => Set<QrDevice>();
    public DbSet<RoomDisplayDevice> RoomDisplayDevices => Set<RoomDisplayDevice>();
    public DbSet<CheckInEvent> CheckInEvents => Set<CheckInEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("btree_gist");
        b.Entity<Organization>().ToTable("organizations");
        b.Entity<OrganizationSettings>().ToTable("organization_settings");
        b.Entity<User>().ToTable("users");
        b.Entity<Room>().ToTable("rooms");
        b.Entity<EquipmentItem>().ToTable("equipment_items");
        b.Entity<RoomAccess>().ToTable("room_accesses");
        b.Entity<Booking>().ToTable("bookings");
        b.Entity<RecurringSeries>().ToTable("recurring_series");
        b.Entity<QrDevice>().ToTable("qr_devices");
        b.Entity<RoomDisplayDevice>().ToTable("room_display_devices");
        b.Entity<CheckInEvent>().ToTable("check_in_events");
        b.Entity<AuditLog>().ToTable("audit_logs");
        b.Entity<User>().Property(x => x.AuthProvider).HasConversion<string>();
        b.Entity<User>().Property(x => x.Role).HasConversion<string>();
        b.Entity<Booking>().Property(x => x.Status).HasConversion<string>();
        b.Entity<Room>().Property(x => x.EquipmentTags).HasColumnType("text[]");
        b.Entity<Room>().Property(x => x.PhotoUrls).HasColumnType("text[]");
        b.Entity<Booking>().Property(x => x.BookingSource).HasMaxLength(32);
        b.Entity<Booking>().HasIndex(x => x.GuestManageTokenHash).IsUnique();
        b.Entity<AuditLog>().Property(x => x.Metadata).HasColumnType("jsonb").HasConversion(new ValueConverter<string, JsonDocument>(v => JsonDocument.Parse(string.IsNullOrWhiteSpace(v) ? "{}" : v, new JsonDocumentOptions()), v => v.RootElement.GetRawText()));
        b.Entity<RoomAccess>().HasIndex(x => new { x.RoomId, x.OrganizationId }).IsUnique();
        b.Entity<RoomDisplayDevice>().HasIndex(x => x.PairingCodeHash).IsUnique();
        foreach (var e in b.Model.GetEntityTypes()) foreach (var p in e.GetProperties()) p.SetColumnName(Snake(p.Name));
    }

    static string Snake(string s) { var x = new StringBuilder(); for (var i = 0; i < s.Length; i++) { if (i > 0 && char.IsUpper(s[i])) x.Append('_'); x.Append(char.ToLowerInvariant(s[i])); } return x.ToString(); }
}
