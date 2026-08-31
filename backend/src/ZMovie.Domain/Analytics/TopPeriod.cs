namespace ZMovie.Domain.Analytics;

public enum TopPeriod
{
    Day,
    Week,
    Month
}

public static class PeriodCalculator
{
    public static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    public static DateTimeOffset CalculatePeriodStart(TopPeriod period, DateTimeOffset now, TimeZoneInfo? timeZone = null)
    {
        var zone = timeZone ?? VietnamTimeZone;
        var localNow = TimeZoneInfo.ConvertTime(now, zone);
        var date = DateOnly.FromDateTime(localNow.Date);

        var startDate = period switch
        {
            TopPeriod.Day => date,
            TopPeriod.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
            TopPeriod.Month => new DateOnly(date.Year, date.Month, 1),
            _ => date,
        };

        var localStart = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(localStart, zone.GetUtcOffset(localStart)).ToUniversalTime();
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "ICT", "ICT");
            }
        }
    }
}

public static class DeduplicationPolicy
{
    public static readonly TimeSpan DeduplicationWindow = TimeSpan.FromMinutes(30);

    public static DateTimeOffset DeduplicationThreshold(DateTimeOffset now) =>
        now.Subtract(DeduplicationWindow);
}
