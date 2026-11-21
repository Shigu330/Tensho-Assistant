namespace TenshoAssistant.Chat;

/// <summary>X-447 B13 (0.1655): where the player is, for the chat window's profiles: in a duty, crafting, in a city, or none of them.</summary>
public enum ChatContext { None,Duty,Crafting,Town }

/// <summary>X-447 B13 (0.1655): a context's profile: its own look (a B16 code, ChatTheme; null: the window's) and the tabs it shows (their names;
/// null: every tab). X-697 M2 (0.1665): its own order of the tabs (their names; null: the settings' order).</summary>
public sealed class ChatContextProfile
{
    public string? Look{get;set;}
    public List<string>? Tabs{get;set;}
    public List<string>? Order{get;set;}
}

/// <summary>
/// X-447 B13 (0.1655, D-198 a of 20/11, the user's « Apparence et onglets »): the chat window changes by itself by context: its look and the
/// tabs it shows (each with its own filters), in a duty, while crafting, in a city; when the player leaves, the window's own look comes back.
/// Nothing else changes (no setting of a tab, no channel, nothing sent), and never anything in combat beyond what is shown (R03.1): a context
/// only changes the display. The window's own look is kept in the settings while a context's is shown (<see cref="ChatSettings.OwnLook"/>), so
/// it comes back even after a restart in the middle of a duty; a change of the look made in a context is kept for that context. Pure (tests).
/// </summary>
public static class ChatContexts
{
    /// <summary>TerritoryIntendedUse 0: the game's 19 cities, and only them (TerritoryType sheet, docs/evidence/0.1655-chat-context-cities.md).</summary>
    public const uint TownUse=0;
    public static readonly IReadOnlyList<ChatContext> Chosen=[ChatContext.Duty,ChatContext.Crafting,ChatContext.Town];

    /// <summary>The context now: a duty first (a city's duty is a duty), then crafting, then a city.</summary>
    public static ChatContext Now(bool duty,bool crafting,bool town)=>duty ? ChatContext.Duty:crafting ? ChatContext.Crafting:town ? ChatContext.Town:ChatContext.None;

    /// <summary>The profile that applies in <paramref name="context"/>: the setting on and a profile chosen; null otherwise.</summary>
    public static ChatContextProfile? ProfileOf(ChatSettings settings,ChatContext context)
        =>settings.ContextProfiles && context!=ChatContext.None && settings.Profiles?.GetValueOrDefault(context) is { } profile ? profile:null;

    /// <summary>A context's profile to change, made when it has none yet.</summary>
    public static ChatContextProfile Edit(ChatSettings settings,ChatContext context)
    {
        settings.Profiles??=[];
        if(!settings.Profiles.TryGetValue(context,out var profile))settings.Profiles[context]=profile=new();
        return profile;
    }

    /// <summary>The window's look follows the context: a context with a look of its own puts it on (the window's own kept aside the first time),
    /// leaving it keeps what was changed there for that context and puts back the next context's look or the window's own. True when the
    /// settings changed (to save). Nothing happens while the look shown is already the right one.</summary>
    public static bool Follow(ChatSettings settings,ChatContext now)
    {
        var target=ProfileOf(settings,now)?.Look is { Length:>0 } ? now:ChatContext.None;
        if(target==settings.ProfileApplied)return false;
        if(settings.ProfileApplied!=ChatContext.None && settings.Profiles?.GetValueOrDefault(settings.ProfileApplied) is { Look:not null } left)
            left.Look=ChatTheme.Share(settings);
        if(target==ChatContext.None)
        {
            if(settings.OwnLook is { } own)ChatTheme.Apply(own,settings);
            settings.OwnLook=null;
        }
        else
        {
            settings.OwnLook??=ChatTheme.Share(settings);
            ChatTheme.Apply(settings.Profiles![target].Look,settings);
        }
        settings.ProfileApplied=target;
        return true;
    }

    /// <summary>X-691 M2 (0.1668): the context whose look the window wears now (its changes of the look go to that profile), else null.</summary>
    public static ChatContext? Worn(ChatSettings settings)=>settings.ContextProfiles && settings.ProfileApplied!=ChatContext.None ? settings.ProfileApplied:null;

    /// <summary>The tab is shown in that profile's context (no profile, or no list: every tab).</summary>
    public static bool ShowsTab(ChatContextProfile? profile,ChatTabSettings tab)
        =>profile?.Tabs is not { } names || names.Contains(tab.Name,StringComparer.OrdinalIgnoreCase);

    /// <summary>A tab shown or not in a context; every tab shown again is no list (null); the last tab shown stays (a window is never empty).</summary>
    public static void SetTab(ChatContextProfile profile,IReadOnlyList<ChatTabSettings> tabs,ChatTabSettings tab,bool shown)
    {
        var names=(profile.Tabs ?? tabs.Select(each=>each.Name)).ToList();
        names.RemoveAll(name=>string.Equals(name,tab.Name,StringComparison.OrdinalIgnoreCase));
        if(shown)names.Add(tab.Name);
        names=names.Where(name=>tabs.Any(each=>string.Equals(each.Name,name,StringComparison.OrdinalIgnoreCase))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if(names.Count==0)return;
        profile.Tabs=tabs.All(each=>names.Contains(each.Name,StringComparer.OrdinalIgnoreCase)) ? null:names;
    }

    /// <summary>The tabs the window shows in a context, added to <paramref name="into"/>: those of the profile, in the profile's own order (X-697
    /// M2, 0.1665; a tab it does not name yet comes after, in the settings' order), else the settings' order; every tab when there is no profile
    /// or list, or when none of its names is a tab any more (a tab renamed: never an empty window).</summary>
    public static void Shown(IReadOnlyList<ChatTabSettings> tabs,ChatContextProfile? profile,List<ChatTabSettings> into)
    {
        var start=into.Count;
        if(profile?.Tabs is { Count:>0 } names)
            foreach(var tab in tabs)if(names.Contains(tab.Name,StringComparer.OrdinalIgnoreCase))into.Add(tab);
        if(into.Count==start)into.AddRange(tabs);
        if(profile?.Order is not { Count:>0 } order || into.Count-start<2)return;
        var sorted=into.GetRange(start,into.Count-start).OrderBy(tab=>Place(order,tab.Name)).ToList();
        into.RemoveRange(start,sorted.Count);
        into.AddRange(sorted);
    }

    private static int Place(List<string> order,string name)
    {
        var at=order.FindIndex(each=>string.Equals(each,name,StringComparison.OrdinalIgnoreCase));
        return at<0 ? int.MaxValue:at;
    }

    /// <summary>X-697 M2 (0.1665, the definition of X-687: « each profile keeps its order »): after a drag in a context, the order the window shows
    /// becomes that profile's order only (the settings' order and the other profiles unchanged); the tabs it does not show keep their places
    /// after them; conversations (not in <paramref name="tabs"/>) left out. False when nothing changes.</summary>
    public static bool SetOrder(ChatContextProfile profile,IReadOnlyList<ChatTabSettings> tabs,IReadOnlyList<ChatTabSettings> shown)
    {
        var names=shown.Where(tabs.Contains).Select(tab=>tab.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if(names.Count==0)return false;
        foreach(var name in profile.Order ?? [])
            if(!names.Contains(name,StringComparer.OrdinalIgnoreCase) && tabs.Any(tab=>string.Equals(tab.Name,name,StringComparison.OrdinalIgnoreCase)))names.Add(name);
        var before=new List<ChatTabSettings>();
        Shown(tabs,profile,before);
        var now=new List<ChatTabSettings>();
        Shown(tabs,new ChatContextProfile{Tabs=profile.Tabs,Order=names},now);
        if(before.SequenceEqual(now))return false;
        profile.Order=names;
        return true;
    }

    /// <summary>X-697 M2 (0.1665): a tab one place to the left (-1) or right (+1) in a context's order, by its menu; false at an end.</summary>
    public static bool Move(ChatContextProfile profile,IReadOnlyList<ChatTabSettings> tabs,ChatTabSettings tab,int step)
    {
        var shown=new List<ChatTabSettings>();
        Shown(tabs,profile,shown);
        return ChatTabOrder.Move(shown,shown.IndexOf(tab),step) && SetOrder(profile,tabs,shown);
    }
}
