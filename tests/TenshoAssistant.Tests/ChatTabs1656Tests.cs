using TenshoAssistant.Chat;

// 0.1656 (X-686, the user's report of 20/11): a chat tab removed by mistake can be undone a few seconds; the default tabs come back, one by
// one or all together after a question, without touching the history or the other settings.
internal static class ChatTabs1656Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static List<ChatTabSettings> Defaults()=>ChatSettings.DefaultTabs("General","Battle","Event","Social");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat tabs 1656: « Remove » keeps the tab 10 seconds; undone, it comes back at its place with its own settings",()=>
        {
            var tabs=Defaults();
            var battle=tabs[1];
            battle.NameColor=0x112233;
            battle.KeepWords="boss";
            var undo=new ChatTabUndo();
            Check(undo.Remove(tabs,1,1_000) && tabs.Count==3 && !tabs.Contains(battle),"removed at once");
            Check(undo.Waiting(1_000)==battle && undo.SecondsLeft(1_000)==10 && undo.SecondsLeft(9_500)==2,"waiting, with the seconds left");
            Check(undo.Undo(tabs,6_000) && tabs.Count==4 && tabs[1]==battle && battle.NameColor==0x112233 && battle.KeepWords=="boss","back at its place, its settings kept");
            Check(undo.Waiting(6_000) is null && !undo.Undo(tabs,6_000) && tabs.Count==4,"undone once only, never twice");
        }),
        ("Chat tabs 1656: the undo ends after 10 seconds; never the last tab; no room, no undo",()=>
        {
            var tabs=Defaults();
            var undo=new ChatTabUndo();
            undo.Remove(tabs,3,0);
            Check(undo.Waiting(ChatTabUndo.UndoMilliseconds) is null && !undo.Undo(tabs,ChatTabUndo.UndoMilliseconds) && tabs.Count==3,"too late: the tab stays removed");
            var one=new List<ChatTabSettings>{new(){Name="Only"}};
            Check(!undo.Remove(one,0,0) && one.Count==1 && !undo.Remove(tabs,7,0) && !undo.Remove(tabs,-1,0),"never the last tab, nor a place out of the list");
            var full=Defaults();
            undo.Remove(full,0,0);
            while(full.Count<ChatSettings.MaximumTabs)full.Add(new(){Name="T"+full.Count});
            Check(!undo.Undo(full,1) && full.Count==ChatSettings.MaximumTabs && undo.Waiting(1) is not null,"no room: not undone, still waiting");
            full.RemoveAt(full.Count-1);
            Check(undo.Undo(full,2) && full[0].Name=="General","room again: undone at its place");
            undo.Remove(full,0,3);
            undo.Forget();
            Check(undo.Waiting(4) is null,"forgotten (after « Restore the default tabs »)");
        }),
        ("Chat tabs 1656: a default tab removed is missing and comes back alone at its place; a renamed one or one with the same channels stands for it",()=>
        {
            var tabs=Defaults();
            Check(ChatSettings.MissingDefaults(tabs,"General","Battle","Event","Social").Count==0,"none missing at first");
            tabs.RemoveAt(1);
            var missing=ChatSettings.MissingDefaults(tabs,"General","Battle","Event","Social");
            Check(missing.Count==1 && missing[0].Place==1 && missing[0].Tab.Name=="Battle" && missing[0].Tab.Channels.SequenceEqual(Defaults()[1].Channels)
                  && !missing[0].Tab.ShowUnread,"Battle missing, as first made");
            Check(ChatSettings.AddDefault(tabs,missing[0].Place,missing[0].Tab) && tabs[1].Name=="Battle" && tabs.Count==4,"back at its place");
            tabs[2].Name="NPC";   // renamed, same channels
            tabs[3].Channels=[ChatChannel.Say];   // same name, other channels
            Check(ChatSettings.MissingDefaults(tabs,"General","Battle","Event","Social").Count==0,"a renamed tab or a tab of the same name stands for its default one");
            tabs.RemoveAt(0);
            tabs.Add(new(){Name="Mine",Channels=[ChatChannel.Say]});
            var general=ChatSettings.MissingDefaults(tabs,"General","Battle","Event","Social");
            Check(general.Count==1 && general[0].Place==0 && ChatSettings.AddDefault(tabs,0,general[0].Tab) && tabs[0].Name=="General","General back first");
            while(tabs.Count<ChatSettings.MaximumTabs)tabs.Add(new(){Name="T"+tabs.Count});
            Check(!ChatSettings.AddDefault(tabs,1,Defaults()[1]) && tabs.Count==ChatSettings.MaximumTabs,"no room: nothing added");
            Check(ChatSettings.AddDefault(new List<ChatTabSettings>(),9,Defaults()[1]),"a place past the end: at the end");
        }),
        ("Chat tabs 1656: behaviour (R28.14, the player's settings): « Restore the default tabs » gives the four tabs a new player gets, and changes nothing else",()=>
        {
            var settings=new ChatSettings{Opacity=33,FontScale=120,HistoryMessages=4321,HistoryDays=12,HiddenPlayers="Ann Example",Colors=new(){[ChatChannel.Say]=0xABCDEF},
                ContextProfiles=true};
            var tabs=settings.EnsureTabs("General","Battle","Event","Social");
            tabs.RemoveAt(2);
            tabs.Add(new(){Name="Mine",Channels=[ChatChannel.Say],NameColor=1});
            tabs[0].Channels.Clear();
            var reset=settings.ResetTabs("General","Battle","Event","Social");
            var fresh=new ChatSettings().EnsureTabs("General","Battle","Event","Social");
            Check(reset==settings.Tabs && reset.Count==4 && reset.Select(tab=>tab.Name).SequenceEqual(fresh.Select(tab=>tab.Name))
                  && reset.Zip(fresh).All(pair=>pair.First.Channels.SequenceEqual(pair.Second.Channels) && pair.First.ShowUnread==pair.Second.ShowUnread)
                  && settings.TabsVersion==ChatSettings.CurrentTabs,"the four default tabs, as a new player's");
            Check(settings.Opacity==33 && settings.FontScale==120 && settings.HistoryMessages==4321 && settings.HistoryDays==12
                  && settings.HiddenPlayers=="Ann Example" && settings.Colors[ChatChannel.Say]==0xABCDEF && settings.ContextProfiles,
                  "the history's bounds, the look, the hidden players, the colours and the profiles unchanged");
            Check(settings.EnsureTabs("General","Battle","Event","Social")==reset && reset.Count==4,"kept as they are afterwards (no upgrade added twice)");
        }),
        ("Chat tabs 1656: the window: « Remove » through the undo, the line to undo above the tabs, the defaults after a question; texts in both languages",()=>
        {
            var code=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var start=code.IndexOf("private void DrawChatTabSettings(",StringComparison.Ordinal);
            var body=code[start..code.IndexOf("private static void SameLineIfFits(",start,StringComparison.Ordinal)];
            Check(body.Contains("chatTabUndo.Remove(tabs,index,Environment.TickCount64)") && !body.Contains("tabs.RemoveAt(index)"),"« Remove » through the undo");
            Check(body.IndexOf("DrawChatTabUndo(tabs);",StringComparison.Ordinal) < body.IndexOf("for(var index=0;",StringComparison.Ordinal),"the undo above the tabs");
            Check(body.Contains("ImGui.OpenPopup(\"###ChatTabsResetAsk\")") && body.Contains("BeginPopupModal(localization.Get(\"ChatWindow.TabsResetTitle\")+\"###ChatTabsResetAsk\"")
                  && body.IndexOf("options.ResetTabs(",StringComparison.Ordinal) > body.IndexOf("ChatWindow.TabsResetYes",StringComparison.Ordinal),"reset only after the question");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach(var key in new[]{"TabRemoved","TabUndo","TabUndoTip","TabAddDefault","TabAddDefaultTip","TabsReset","TabsResetTip","TabsResetTitle","TabsResetText","TabsResetYes"})
                    Check(text.Contains($"\"{key}\": "),$"{language}: {key}");
            }
        }),
    ];
}
