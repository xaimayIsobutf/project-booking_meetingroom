using Microsoft.EntityFrameworkCore;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Domain.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using RoomBooking.Web.Infrastructure;

static string GetDatabaseConnectionString(IConfiguration configuration)
{
    var value = configuration.GetConnectionString("DefaultConnection")?.Trim().Trim('"')
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    var uriStart = value.IndexOf("postgresql://", StringComparison.OrdinalIgnoreCase);
    if (uriStart < 0)
        uriStart = value.IndexOf("postgres://", StringComparison.OrdinalIgnoreCase);
    if (uriStart > 0)
        value = value[uriStart..].Trim().Trim('"', '\'');

    if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return value;

    var uri = new Uri(value);
    var userInfo = uri.UserInfo.Split(':', 2);
    if (userInfo.Length != 2)
        throw new InvalidOperationException("The PostgreSQL URI must include username and password.");

    var builder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.Trim('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = Uri.UnescapeDataString(userInfo[1]),
        SslMode = Npgsql.SslMode.Require,
        TrustServerCertificate = true
    };
    return builder.ConnectionString;
}

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options=>{options.LoginPath="/Account/Login";options.AccessDeniedPath="/Account/Denied";});
builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddDbContext<RoomBookingDbContext>(options => options.UseNpgsql(GetDatabaseConnectionString(builder.Configuration)));
builder.Services.AddSingleton<IAppTime, AppTime>();


builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoomBookingDbContext>();
    // ใช้ Migration เมื่อมี migration แล้ว; fallback นี้มีไว้ให้ Demo เดิมที่ยังไม่มีไฟล์ migration เปิดได้
    // หลังสร้าง InitialDemo migration แล้วให้ลบ fallback และใช้ db.Database.Migrate() เท่านั้น
    if (db.Database.GetMigrations().Any()) db.Database.Migrate();
    else db.Database.EnsureCreated();
    if (!db.Companies.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var company = new Company { Name = "Makub Center", Code = "MAKUB" };
        company.Rooms.Add(new MeetingRoom { Name = "Makub A", Location = "ชั้น 1", Capacity = 8, Equipment = "จอ TV · Whiteboard" });
        company.Rooms.Add(new MeetingRoom { Name = "Makub B", Location = "ชั้น 1", Capacity = 12, Equipment = "โปรเจกเตอร์ · Video call" });
        company.Rooms.Add(new MeetingRoom { Name = "Creative Room", Location = "ชั้น 2", Capacity = 20, Equipment = "จอ LED · ไมโครโฟน" });
        company.Users.Add(new User { Email = "admin@makub.local", FullName = "Makub Admin", Role = "TenantAdmin", PasswordHash = "pending" });
        company.Users.Add(new User { Email = "user@makub.local", FullName = "Makub User", Role = "Employee", PasswordHash = "pending" });
        var acme = new Company { Name = "Acme Corporation", Code = "ACME" };
        acme.Rooms.Add(new MeetingRoom { Name = "Blue Ocean", Building = "อาคาร A", Floor = "ชั้น 3", Location = "อาคาร A · ชั้น 3", Capacity = 12, Equipment = "Projector · Video call", RequiresApproval = true });
        acme.Users.Add(new User { Email = "admin@acme.local", FullName = "Acme Admin", Role = "TenantAdmin", PasswordHash = "pending" });
        acme.Users.Add(new User { Email = "user@acme.local", FullName = "Acme User", Role = "Employee", PasswordHash = "pending" });
        foreach (var u in company.Users.Concat(acme.Users)) u.PasswordHash = hasher.HashPassword(u, "1234");
        db.Companies.AddRange(company, acme);
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
