using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoomReservation.Domain.Enums;
using RoomReservation.Infrastructure.Persistence;
using System.Security.Cryptography;

namespace RoomReservation.Application;

public sealed class BackgroundReservationService(IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;
                var settings = await db.OrganizationSettings.ToDictionaryAsync(x => x.OrganizationId, token);

                foreach (var booking in await db.Bookings
                    .Where(x => x.Status == BookingStatus.Pending && x.StartAtUtc <= now)
                    .ToListAsync(token))
                {
                    booking.Status = BookingStatus.Cancelled;
                    booking.UpdatedAt = now;
                }

                foreach (var booking in await db.Bookings
                    .Where(x => x.Status == BookingStatus.Confirmed && x.StartAtUtc <= now)
                    .ToListAsync(token))
                {
                    var timeoutMinutes = settings.TryGetValue(booking.OrganizationId, out var setting)
                        ? Math.Max(1, setting.NoShowTimeoutMinutes)
                        : 10;
                    if (booking.StartAtUtc.AddMinutes(timeoutMinutes) <= now)
                    {
                        // A confirmed booking that was not checked in within the
                        // configured window is marked as a no-show and released.
                        booking.Status = BookingStatus.NoShow;
                        booking.UpdatedAt = now;
                    }
                }

                foreach (var booking in await db.Bookings
                    .Where(x => x.Status == BookingStatus.CheckedIn && x.EndAtUtc <= now)
                    .ToListAsync(token))
                {
                    booking.Status = BookingStatus.Completed;
                    booking.UpdatedAt = now;
                    var checkIn = await db.CheckInEvents.FirstOrDefaultAsync(x => x.BookingId == booking.Id, token);
                    if (checkIn is not null)
                    {
                        checkIn.CheckedOutAt = now;
                        checkIn.AutoCheckedOut = true;
                    }
                }

                foreach (var device in await db.QrDevices.ToListAsync(token))
                {
                    if (device.TokenExpiresAt <= now)
                    {
                        device.CurrentToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
                        device.TokenExpiresAt = now.AddSeconds(
                            settings.TryGetValue(device.OrganizationId, out var setting) ? setting.QrTokenTtlSeconds : 20);
                    }
                }

                await db.SaveChangesAsync(token);
            }
            catch
            {
                // Retry on the next interval if the database is temporarily unavailable.
            }

            await Task.Delay(5000, token);
        }
    }
}
