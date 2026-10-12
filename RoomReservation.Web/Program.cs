using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.WebUtilities;
using RoomReservation.Api.Hubs;
using RoomReservation.Api.Middleware;
using RoomReservation.Application;
using RoomReservation.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(connectionString)
    .AddInterceptors(new TenantSaveChangesInterceptor()));
builder.Services.AddScoped<ReservationEvents>();
builder.Services.AddScoped<IPasswordHasher<RoomReservation.Domain.Entities.User>, PasswordHasher<RoomReservation.Domain.Entities.User>>();
builder.Services.AddHostedService<BackgroundReservationService>();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/lib/roomreservation/keys"))
    .SetApplicationName("RoomReservation");
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required."));
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "SmartAuth";
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddPolicyScheme("SmartAuth", "Cookie or Bearer", options =>
{
    options.ForwardDefaultSelector = context => context.Request.Headers.ContainsKey("Authorization")
        ? JwtBearerDefaults.AuthenticationScheme
        : CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
})
.AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ValidateIssuer = false,
    ValidateAudience = false
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleSecureCookies = builder.Configuration.GetValue("Authentication:Google:SecureCookies", false);
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddCookie("External", options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            options.Cookie.Name = "RoomReservation.External";
        })
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = "External";
            options.CallbackPath = "/signin-google";
            options.CorrelationCookie.SecurePolicy = googleSecureCookies
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            // Always let the user choose which Google account to use.
            // This matters when the browser has several Google accounts signed in.
            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                var redirectUri = QueryHelpers.AddQueryString(context.RedirectUri, "prompt", "select_account");
                context.Response.Redirect(redirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/Account/Login?googleError=" + Uri.EscapeDataString("เข้าสู่ระบบด้วย Google ไม่สำเร็จ กรุณาลองใหม่"));
                return Task.CompletedTask;
            };
        });
}

var app = builder.Build();
app.UseForwardedHeaders();
if (builder.Configuration.GetValue("Database:AutoMigrate", false))
{
    var safeConnectionString = Regex.Replace(
        connectionString,
        @"(?i)(Password\s*=\s*)[^;]*",
        "$1***");
    Console.WriteLine($"RoomReservation.Web migrating database: {safeConnectionString}");
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await MigrationRunner.Run(db);
    Console.WriteLine("Database migrations completed.");
}

app.UseExceptionHandler("/Home/Error");
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<ScheduleHub>("/hubs/schedule");
app.MapHub<QrHub>("/hubs/qr");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallbackToFile("/room-display-app/{*path:nonfile}", "room-display-app/index.html");
app.MapGet("/version", () => Results.Ok(new
{
    name = "RoomReservation.Web",
    version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown",
    environment = app.Environment.EnvironmentName
}));
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
