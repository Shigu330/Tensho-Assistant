namespace TenshoAssistant.Chat;

/// <summary>X-447 B13 (0.1655): where the player is, for the chat window's profiles: in a duty, crafting, in a city, or none of them.</summary>
public enum ChatContext { None,Duty,Crafting,Town }

/// <summary>X-447 B13 (0.1655): a context's profile: its own look (a B16 code, ChatTheme; null: the window's) and the tabs it shows (their names;
/// null: every tab).</summary>
public sealed class ChatContextProfile
{
    public string? Look{get;set;}
    public List<string>? Tabs{get;set;}
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

    /// <summary>The tabs the window shows in a context, added to <paramref name="into"/>: those of the profile, in the settings' order; every tab
    /// when there is no profile or list, or when none of its names is a tab any more (a tab renamed: never an empty window).</summary>
    public static void Shown(IReadOnlyList<ChatTabSettings> tabs,ChatContextProfile? profile,List<ChatTabSettings> into)
    {
        var start=into.Count;
        if(profile?.Tabs is { Count:>0 } names)
            foreach(var tab in tabs)if(names.Contains(tab.Name,StringComparer.OrdinalIgnoreCase))into.Add(tab);
        if(into.Count==start)into.AddRange(tabs);
    }
}
