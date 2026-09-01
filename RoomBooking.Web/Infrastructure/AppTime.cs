namespace RoomBooking.Web.Infrastructure;

public class AppTime : IAppTime
{
    private static readonly TimeZoneInfo Thailand =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Bangkok");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime ThailandNow =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Thailand);

    public DateTime ThailandToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(
            local,
            DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(
            unspecified,
            Thailand);
    }

    public DateTime UtcToThailand(DateTime utc)
    {
        var utcValue = DateTime.SpecifyKind(
            utc,
            DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(
            utcValue,
            Thailand);
    }
}