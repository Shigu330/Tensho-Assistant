using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>g, M-03 (0.373): chat channel groups the map link list filters on (the game's XivChatType, grouped by ChatMapLinkWatcher).</summary>
public enum ChatLinkChannel { Say, Shout, Yell, Party, Alliance, FreeCompany, Linkshell, CrossLinkshell, Tell, Novice, Other }

/// <summary>One map link seen in the chat: when, where from, who sent it, and the point (territory, map, map coordinates).</summary>
public sealed record ChatMapLink(DateTimeOffset At,ChatLinkChannel Channel,string Sender,uint TerritoryId,uint MapId,float X,float Y,string Place);

/// <summary>
/// g, M-03 (0.373): the map links seen in the chat this session, newest first, at most <see cref="Max"/>. Kept in memory only (never
/// saved: chat content and sender names stay out of the configuration), cleared at logout. The same point sent again by the same
/// sender within a minute (a train call repeated) moves to the top instead of being added twice.
/// </summary>
public sealed class ChatMapLinkHistory
{
    public const int Max=100;
    private readonly object gate=new();
    private ChatMapLink[] links=[];
    public IReadOnlyList<ChatMapLink> Links{get{lock(gate)return links;}}

    public void Add(ChatMapLink link)
    {
        if(link.MapId==0 || !float.IsFinite(link.X) || !float.IsFinite(link.Y))return;
        lock(gate)
        {
            var kept=links.Where(old=>!Same(old,link)).Take(Max-1);
            links=[link,..kept];
        }
    }
    public void Clear(){lock(gate)links=[];}

    private static bool Same(ChatMapLink old,ChatMapLink link)=>old.MapId==link.MapId && old.Sender==link.Sender
        && MathF.Abs(old.X-link.X)<0.05f && MathF.Abs(old.Y-link.Y)<0.05f && (link.At-old.At).Duration()<=TimeSpan.FromMinutes(1);

    /// <summary>Links of the channels kept by the filter (null = every channel).</summary>
    public static IReadOnlyList<ChatMapLink> Filter(IReadOnlyList<ChatMapLink> links,IReadOnlySet<ChatLinkChannel>? channels)
        =>channels is null ? links:links.Where(link=>channels.Contains(link.Channel)).ToList();
}

public static class ChatMapLinks
{
    /// <summary>
    /// Nearest aetheryte to a point of the same map (texture pixels, so the distance follows the world distance). When the game's
    /// teleport list is known, only the aetherytes in it count (attuned, teleportable: aethernet shards are never in it); when it is
    /// not (null), every aetheryte marker counts and the teleport question says the cost is unknown. Null when none is left.
    /// </summary>
    public static uint? NearestAetheryte(IEnumerable<(uint Id,float X,float Y)> aetherytes,Vector2 pixel,IReadOnlyDictionary<uint,uint>? teleportable)
    {
        uint? best=null;var bestDistance=float.MaxValue;
        foreach(var (id,x,y) in aetherytes)
        {
            if(id==0 || teleportable is not null && !teleportable.ContainsKey(id))continue;
            var distance=Vector2.DistanceSquared(new Vector2(x,y),pixel);
            if(distance<bestDistance){bestDistance=distance;best=id;}
        }
        return best;
    }
}
