namespace TenshoAssistant.Gathering;

/// <summary>C10 (0.1011): the stopwatch of the fishing help and of ocean fishing restarts at each cast.</summary>
public static class FishingStopwatch
{
    /// <summary>A cast newer than the stopwatch's start restarts it; an older one (before fishing began, or before a manual restart) or none does not.</summary>
    public static DateTimeOffset? FromCast(DateTimeOffset? since,DateTimeOffset? cast)=>cast is { } at && (since is null || at>since) ? at:since;
}
