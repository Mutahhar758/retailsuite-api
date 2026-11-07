namespace Retailer.Shared.Common;

/// <summary>
/// Provides centralized access to Pakistan Standard Time (PKT, UTC+5) for Windows.
/// </summary>
public static class PakistanTime
{
    private static readonly TimeZoneInfo _pktZone = ResolvePakistanTimeZone();

    public static TimeZoneInfo TimeZone => _pktZone;
    public static readonly TimeSpan Offset = TimeSpan.FromHours(5);

    /// <summary>
    /// Returns current PKT DateTime with DateTimeKind.Unspecified to represent pure wall-clock time.
    /// </summary>
    public static DateTime Now => DateTime.SpecifyKind(
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _pktZone),
        DateTimeKind.Unspecified);

    public static DateOnly Today => DateOnly.FromDateTime(Now);

    public static TimeOnly CurrentTime => TimeOnly.FromDateTime(Now);

    private static TimeZoneInfo ResolvePakistanTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("PKT", TimeSpan.FromHours(5), "Pakistan Standard Time", "Pakistan Standard Time");
            }
        }
    }
}
