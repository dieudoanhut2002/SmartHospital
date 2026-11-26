namespace SmartHospital.Domain.Common;

public static class HospitalTime
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveTimeZone();

    private static TimeZoneInfo ResolveTimeZone()
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
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "Vietnam Time", "ICT");
            }
        }
    }

    public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public static DateTimeOffset ToVietnamTime(DateTimeOffset utcTime)
        => TimeZoneInfo.ConvertTime(utcTime, VietnamTimeZone);

    public static DateOnly TodayVietnam()
        => DateOnly.FromDateTime(ToVietnamTime(DateTimeOffset.UtcNow).DateTime);
}
