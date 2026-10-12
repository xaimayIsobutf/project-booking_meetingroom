using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RoomReservation.Application;

public sealed record DisplaySession(Guid DeviceId, Guid OrganizationId, string DisplayToken, DateTime ExpiresAt);

public static class DisplaySessionToken
{
    public const string CookieName = "room_display_session";
    public static string Create(DisplaySession session, string secret)
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var signature = Sign(payload, secret);
        return payload + "." + signature;
    }

    public static bool TryRead(string? value, string secret, out DisplaySession session)
    {
        session = default!;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Split('.', 2);
        if (parts.Length != 2 || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Sign(parts[0], secret)), Encoding.UTF8.GetBytes(parts[1]))) return false;
        try
        {
            var padded = parts[0].Replace('-', '+').Replace('_', '/') + new string('=', (4 - parts[0].Length % 4) % 4);
            var parsed = JsonSerializer.Deserialize<DisplaySession>(Encoding.UTF8.GetString(Convert.FromBase64String(padded)));
            if (parsed is null || parsed.ExpiresAt <= DateTime.UtcNow) return false;
            session = parsed;
            return true;
        }
        catch { return false; }
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Sign(string value, string secret) => Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(value))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
