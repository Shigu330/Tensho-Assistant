namespace PluginUiTest.Host;

/// <summary>
/// The clock a rewritten copy reads (Rewrite.Plugin's clock): <see cref="Fixed"/>, an instant that does not move, or the computer's clock
/// when null. The local time is that instant in the computer's time zone.
/// </summary>
public static class HostClock
{
    public static DateTimeOffset? Fixed;
    public static DateTimeOffset UtcNow=>Fixed is { } at ? at.ToUniversalTime() : DateTimeOffset.UtcNow;
    public static DateTimeOffset Now=>Fixed is { } at ? TimeZoneInfo.ConvertTime(at,TimeZoneInfo.Local) : DateTimeOffset.Now;
    public static DateTime DateTimeUtcNow=>UtcNow.UtcDateTime;
    public static DateTime DateTimeNow=>Fixed is { } at ? at.UtcDateTime.ToLocalTime() : DateTime.Now;
    public static DateTime Today=>DateTimeNow.Date;
    public static TimeProvider Provider{get;}=new FixedProvider();
    private sealed class FixedProvider:TimeProvider{public override DateTimeOffset GetUtcNow()=>UtcNow;}
}
