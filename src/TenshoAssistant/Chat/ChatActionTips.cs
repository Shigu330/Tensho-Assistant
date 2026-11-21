namespace TenshoAssistant.Chat;

/// <summary>X-473 (0.1720): a row of the game's Action sheet as the tooltip needs it (its description from ActionTransient), the client's language.</summary>
public readonly record struct ChatActionRow(uint Id,string Name,uint Icon,bool Player,bool PvP,string Description);

/// <summary>
/// X-473 (0.1720, the user's request of 15/11): the actions a combat line names by their coloured name (the game links none:
/// docs/evidence/0.1720-chat-action-names.md), by name. A name has a tooltip only when it is one action for sure (R06, never a false one):
/// in a line of a player (<see cref="ByPlayer"/>), the player's actions of that name (not PvP); else, or when none, every action of that name.
/// One row, or rows that all show the same icon and description; rows that differ: no tooltip. Pure (tests).
/// </summary>
public static class ChatActionTips
{
    /// <summary>The line's subject is a player (Dalamud's XivChatRelationKind: you, your party, your alliance, another player).</summary>
    public static bool ByPlayer(byte source)=>source is >= 1 and <= 4;

    public static (Dictionary<string,ChatStatusTip> Players,Dictionary<string,ChatStatusTip> All) Build(IEnumerable<ChatActionRow> rows)
    {
        var players=new Dictionary<string,ChatStatusTip>(StringComparer.Ordinal);
        var all=new Dictionary<string,ChatStatusTip>(StringComparer.Ordinal);
        foreach(var group in rows.Where(row=>row.Name.Trim().Length>0).GroupBy(row=>row.Name.Trim(),StringComparer.Ordinal))
        {
            if(One(group.Key,group.Where(row=>row.Player && !row.PvP).ToList()) is { } player)players[group.Key]=player;
            if(One(group.Key,group.ToList()) is { } any)all[group.Key]=any;
        }
        return (players,all);
    }

    private static ChatStatusTip? One(string name,List<ChatActionRow> rows)
        =>rows.Count>0 && rows.All(row=>row.Icon==rows[0].Icon && row.Description==rows[0].Description) ? new ChatStatusTip(rows[0].Icon,name,rows[0].Description):null;

    /// <summary>The tooltip of <paramref name="name"/> in a line whose subject is or is not a player (<see cref="ByPlayer"/>); null for none.</summary>
    public static ChatStatusTip? Find((Dictionary<string,ChatStatusTip> Players,Dictionary<string,ChatStatusTip> All) tips,string name,bool byPlayer)
    {
        var key=name.Trim();
        return byPlayer && tips.Players.TryGetValue(key,out var tip) ? tip:tips.All.GetValueOrDefault(key);
    }
}
