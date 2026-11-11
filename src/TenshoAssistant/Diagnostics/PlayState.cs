using System.Text.Json;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.1094 (user report, 11/11 15 h: the developer's DLL was reloaded seven times during an ocean voyage and the voyage was lost): whether the
/// player is in something a reload of the plugin would spoil (a duty, an ocean voyage, competitive content), written to PlayState.json
/// beside the settings for the developer's own build scripts, which wait instead of replacing the DLL. Developer builds and developer mode
/// only; nothing is sent anywhere. While busy the file is rewritten every 30 s, so a file left by a closed game goes stale.
/// </summary>
public static class PlayState
{
    public const string FileName="PlayState.json";
    public static readonly TimeSpan Heartbeat=TimeSpan.FromSeconds(30);

    /// <summary>What a reload would spoil now, or null when nothing would.</summary>
    public static string? BusyReason(bool oceanVoyage,bool competitive,bool boundByDuty)
        =>oceanVoyage ? "ocean voyage":competitive ? "competitive content":boundByDuty ? "duty":null;

    /// <summary>Write when the state changes, and again every <see cref="Heartbeat"/> while busy.</summary>
    public static bool ShouldWrite(string? reason,bool written,string? writtenReason,DateTimeOffset now,DateTimeOffset writtenAt)
        =>!written || reason!=writtenReason || (reason is not null && now-writtenAt>=Heartbeat);

    public static string ToJson(string? reason,DateTimeOffset now)
        =>JsonSerializer.Serialize(new Dictionary<string,object>{["format"]=1,["busy"]=reason is not null,["reason"]=reason ?? "",["updated"]=now.ToUniversalTime().ToString("o")});
}
