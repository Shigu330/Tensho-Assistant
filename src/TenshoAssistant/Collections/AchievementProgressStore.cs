using System.Text.Json;

namespace TenshoAssistant.Collections;

/// <summary>Progress of one achievement as the game received it: current, maximum, when Tensho saw it.</summary>
public sealed record AchievementProgress(uint Current,uint Max,DateTimeOffset SeenAt);

/// <summary>X-102 (0.987): the progress seen, per character (content id), on disk; and when a seen progress reaches its maximum.</summary>
public static class AchievementProgressStore
{
    /// <summary>A progress seen below its maximum before, and at it now. Never on the first sight (nothing to compare with).</summary>
    public static bool JustCompleted(AchievementProgress? before,AchievementProgress now)=>before is { } known && known.Current<known.Max && now.Max>0 && now.Current>=now.Max;
    public static string Write(Dictionary<ulong,Dictionary<uint,AchievementProgress>> all)=>JsonSerializer.Serialize(all);
    public static Dictionary<ulong,Dictionary<uint,AchievementProgress>>? Read(string text)
    {
        try{return JsonSerializer.Deserialize<Dictionary<ulong,Dictionary<uint,AchievementProgress>>>(text);}
        catch(JsonException){return null;}
    }
}
