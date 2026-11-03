using System.Globalization;

namespace TenshoAssistant.Maps;

/// <summary>N-15 (0.442): one hunt mark met during a train — name, rank (1 B, 2 A, 3 S), map and its map coordinates, when first seen.</summary>
public sealed record HuntTrainStop(string Name,int Rank,uint MapId,uint TerritoryId,string Zone,float X,float Y,DateTimeOffset SeenAt,bool Visited=false);

/// <summary>
/// N-15 (0.442): the hunt train recorder. Each mark is noted once per map (same name and map = same stop), in the order met, 64 at most;
/// the list exports as « Name — Zone ( x , y ) » lines that the bookmark import (0.437) and hunt sites read back.
/// </summary>
public static class HuntTrain
{
    public const int Capacity=64;

    public static IReadOnlyList<HuntTrainStop> Add(IReadOnlyList<HuntTrainStop> stops,HuntTrainStop stop)
    {
        if(stops.Count>=Capacity || stops.Any(existing=>existing.MapId==stop.MapId && string.Equals(existing.Name,stop.Name,StringComparison.OrdinalIgnoreCase)))return stops;
        return [..stops,stop];
    }

    public static IReadOnlyList<HuntTrainStop> MarkVisited(IReadOnlyList<HuntTrainStop> stops,int index)
        =>stops.Select((stop,at)=>at==index ? stop with {Visited=true}:stop).ToArray();

    /// <summary>The first stop not visited yet, or null.</summary>
    public static int? Next(IReadOnlyList<HuntTrainStop> stops)
    {
        for(var index=0;index<stops.Count;index++)if(!stops[index].Visited)return index;
        return null;
    }

    public static string Export(IEnumerable<HuntTrainStop> stops)
        =>string.Join("\n",stops.Select(stop=>string.Create(CultureInfo.InvariantCulture,$"{stop.Name} — {stop.Zone} ( {stop.X:0.0} , {stop.Y:0.0} )")));

    /// <summary>X-13 (0.447): the chat channels the conductor's call can be sent to, and nothing else.</summary>
    public static readonly IReadOnlyList<string> AnnounceChannels=["/sh","/y","/p","/fc","/ls1","/cwl1"];

    public const int AnnounceMaximum=400;

    /// <summary>
    /// X-13 (0.447): the chat line « channel text &lt;flag&gt; » sent on the conductor's click, or null. The channel must be one of
    /// AnnounceChannels; control characters and « &lt; &gt; / » are removed from the text, so the only text command is the channel and the only
    /// placeholder is the final « &lt;flag&gt; ».
    /// </summary>
    public static string? ChatCommand(string channel,string text)
    {
        if(!AnnounceChannels.Contains(channel))return null;
        var clean=new string(text.Where(character=>!char.IsControl(character) && character is not ('<' or '>' or '/')).ToArray()).Trim();
        if(clean.Length==0)return null;
        if(clean.Length>AnnounceMaximum)clean=clean[..AnnounceMaximum].TrimEnd();
        return $"{channel} {clean} <flag>";
    }

    /// <summary>X-13 (0.447): true only for a line ChatCommand builds (checked again just before the game receives it).</summary>
    public static bool IsChatCommand(string command)
    {
        var space=command.IndexOf(' ');
        if(space<0 || !command.EndsWith(" <flag>",StringComparison.Ordinal))return false;
        return ChatCommand(command[..space],command[(space+1)..^" <flag>".Length])==command;
    }
}
