using TenshoAssistant.Chat;

// 0.1655 (X-447 B13, D-198 a of 20/11): the chat window's look and tabs by context; the window's own look always comes back.
internal static class ChatContexts1655Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat contexts 1655: the context now: a duty first, then crafting, then a city; the profile only with the setting on",()=>
        {
            Check(ChatContexts.Now(true,true,true)==ChatContext.Duty && ChatContexts.Now(false,true,true)==ChatContext.Crafting
                && ChatContexts.Now(false,false,true)==ChatContext.Town && ChatContexts.Now(false,false,false)==ChatContext.None,"order");
            var settings=new ChatSettings();
            Check(!settings.ContextProfiles && settings.ProfileApplied==ChatContext.None && settings.OwnLook is null,"off by default");
            var duty=ChatContexts.Edit(settings,ChatContext.Duty);
            Check(ChatContexts.Edit(settings,ChatContext.Duty)==duty && ChatContexts.ProfileOf(settings,ChatContext.Duty) is null,"setting off: no profile");
            settings.ContextProfiles=true;
            Check(ChatContexts.ProfileOf(settings,ChatContext.Duty)==duty && ChatContexts.ProfileOf(settings,ChatContext.Town) is null
                && ChatContexts.ProfileOf(settings,ChatContext.None) is null,"a profile where one is chosen");
            Check(ChatContexts.TownUse==0,"the cities' use (docs/evidence/0.1655-chat-context-cities.md)");
        }),
        ("Chat contexts 1655: behaviour (R28.14, the player's look): a duty's look put on and taken off; the window's own comes back, also after a restart in the duty; a change made there kept for it",()=>
        {
            var settings=new ChatSettings{ContextProfiles=true,Opacity=40,FontScale=110};
            var own=ChatTheme.Share(settings);
            var dark=new ChatSettings{Opacity=85,FontScale=90};
            ChatContexts.Edit(settings,ChatContext.Duty).Look=ChatTheme.Share(dark);
            Check(!ChatContexts.Follow(settings,ChatContext.None) && !ChatContexts.Follow(settings,ChatContext.Town),"no look of its own: nothing changes");
            Check(ChatContexts.Follow(settings,ChatContext.Duty) && settings.Opacity==85 && settings.FontScale==90 && settings.ProfileApplied==ChatContext.Duty && settings.OwnLook==own,"in the duty");
            Check(!ChatContexts.Follow(settings,ChatContext.Duty),"still there: nothing more");
            settings.FontScale=95;   // the player changes the look in the duty
            Check(ChatContexts.Follow(settings,ChatContext.None) && settings.Opacity==40 && settings.FontScale==110 && settings.OwnLook is null && settings.ProfileApplied==ChatContext.None,"out: the window's own look");
            Check(ChatContexts.Follow(settings,ChatContext.Duty) && settings.FontScale==95,"back in the duty: the change made there kept");
            // A restart in the duty: the saved settings say the duty's look is worn and keep the window's own.
            var restarted=new ChatSettings{ContextProfiles=true,ProfileApplied=settings.ProfileApplied,OwnLook=settings.OwnLook,Profiles=settings.Profiles,Opacity=settings.Opacity,FontScale=settings.FontScale};
            Check(ChatContexts.Follow(restarted,ChatContext.None) && restarted.Opacity==40 && restarted.FontScale==110,"after a restart, out of the duty: the window's own look");
            ChatContexts.Follow(settings,ChatContext.Duty);
            settings.ContextProfiles=false;
            Check(ChatContexts.Follow(settings,ChatContext.Duty) && settings.Opacity==40 && settings.OwnLook is null,"the setting turned off: the window's own look at once");
        }),
        ("Chat contexts 1655: behaviour: from one context's look to another's the window's own stays kept; a context without a look of its own wears the window's; a broken code changes nothing",()=>
        {
            var settings=new ChatSettings{ContextProfiles=true,Opacity=40};
            ChatContexts.Edit(settings,ChatContext.Duty).Look=ChatTheme.Share(new ChatSettings{Opacity=85});
            ChatContexts.Edit(settings,ChatContext.Crafting).Look=ChatTheme.Share(new ChatSettings{Opacity=10});
            ChatContexts.Follow(settings,ChatContext.Crafting);
            ChatContexts.Follow(settings,ChatContext.Duty);
            Check(settings.Opacity==85 && settings.OwnLook is not null,"crafting, then a duty");
            ChatContexts.Follow(settings,ChatContext.Town);
            Check(settings.Opacity==40 && settings.OwnLook is null,"a city without a look of its own: the window's");
            ChatContexts.Edit(settings,ChatContext.Town).Look="TENSHO-CHATLOOK1:broken";
            Check(ChatContexts.Follow(settings,ChatContext.Town) && settings.Opacity==40,"a broken code: nothing changed");
            Check(ChatContexts.Follow(settings,ChatContext.None) && settings.Opacity==40 && settings.OwnLook is null,"and the window's own after it");
        }),
        ("Chat contexts 1655: the tabs shown: those of the profile in the settings' order, all without one or when none matches; a tab ticked off and on, never none",()=>
        {
            List<ChatTabSettings> tabs=[new(){Name="General"},new(){Name="Battle"},new(){Name="Event"},new(){Name="Social"}];
            var profile=new ChatContextProfile();
            var into=new List<ChatTabSettings>();
            ChatContexts.Shown(tabs,null,into);
            Check(into.SequenceEqual(tabs),"no profile: all");
            ChatContexts.SetTab(profile,tabs,tabs[1],false);
            ChatContexts.SetTab(profile,tabs,tabs[2],false);
            Check(profile.Tabs!.SequenceEqual(["General","Social"]) && !ChatContexts.ShowsTab(profile,tabs[1]) && ChatContexts.ShowsTab(profile,tabs[3]),string.Join(",",profile.Tabs!));
            into.Clear();
            ChatContexts.Shown(tabs,profile,into);
            Check(into.Select(tab=>tab.Name).SequenceEqual(["General","Social"]),"the profile's tabs");
            ChatContexts.SetTab(profile,tabs,tabs[0],false);
            ChatContexts.SetTab(profile,tabs,tabs[3],false);
            Check(profile.Tabs!.SequenceEqual(["Social"]),"the last one stays: "+string.Join(",",profile.Tabs!));
            foreach(var tab in tabs)ChatContexts.SetTab(profile,tabs,tab,true);
            Check(profile.Tabs is null,"every tab again: no list");
            profile.Tabs=["Renamed"];
            into.Clear();
            ChatContexts.Shown(tabs,profile,into);
            Check(into.Count==4,"a name no tab has any more: every tab (never an empty window)");
        }),
        ("Chat contexts 1655: wiring: followed before the tabs are made, the tab shown kept, the settings under the tabs, the game's states read in the plugin, the texts",()=>
        {
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("FollowChatContext(options);\n        UpdateChatConversations(options);\n        var tabs=ChatWindowTabs(options);",StringComparison.Ordinal)
                && window.Contains("ChatContexts.Shown(ChatTabs(options),ChatContexts.ProfileOf(options,chatContextNow),chatWindowTabs);",StringComparison.Ordinal)
                && window.Contains("if(options.ContextProfiles)chatSelectTab=chatShownTab;",StringComparison.Ordinal)
                && window.Contains("if(ChatContexts.Follow(options,now))TrySave(\"Chat window context\");",StringComparison.Ordinal)
                && window.Contains("DrawChatTabSettings(options);\n        DrawChatContextProfiles(options);",StringComparison.Ordinal),"the window");
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("mainWindow.InCraftingNow=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Crafting] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.PreparingToCraft];",StringComparison.Ordinal)
                && plugin.Contains("row.TerritoryIntendedUse.RowId==Chat.ChatContexts.TownUse",StringComparison.Ordinal) && plugin.Contains("catch(Exception){inTown=false;}",StringComparison.Ordinal),"the plugin");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1655-chat-context-cities.md")),"the evidence");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach(var key in new[]{"SectionContexts","ContextProfiles","ContextProfilesTip","ContextDuty","ContextCrafting","ContextTown","ContextNow","ContextLookOwn",
                    "ContextLookWindow","ContextUseLook","ContextUseLookTip","ContextNoLook","ContextNoLookTip","ContextTabTip"})
                    Check(text.Contains($"\"{key}\":",StringComparison.Ordinal),language+" "+key);
            }
        }),
    ];
}
