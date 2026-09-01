namespace RoomBooking.Web.Infrastructure;

public interface IAppTime
{
    DateTime UtcNow { get; }
    DateTime ThailandNow { get; }

    DateTime ThailandToUtc(DateTime local);
    DateTime UtcToThailand(DateTime utc);
}