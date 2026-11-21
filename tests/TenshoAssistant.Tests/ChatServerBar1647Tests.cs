using TenshoAssistant.Chat;

// 0.1647 (X-447 B7, the user's choice of 15/11, after WIM): the alerts not seen counted in the game's server bar, a message in two tabs once.
internal static class ChatServerBar1647Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset Start=new(2026,11,20,21,30,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat server bar 1647: behaviour: the alerts not seen of every tab, a message shown by two tabs counted once; seen in one tab, still counted for the other; seen in both, none",()=>
        {
            var store=new ChatStore();
            var settings=new ChatSettings{AlertWords="raid"};
            var all=new ChatTabSettings{Name="General",Channels=[ChatChannel.Say,ChatChannel.Party]};
            var party=new ChatTabSettings{Name="Party",Channels=[ChatChannel.Party]};
            var (general,group)=(new ChatTabView(),new ChatTabView());
            void Refresh(){general.Refresh(store,all,100,null,settings);group.Refresh(store,party,100,null,settings);}
            Refresh();
            store.Add(Start,14,"Bob Roe",[new ChatSegment("raid at 9")],100);
            store.Add(Start.AddSeconds(1),10,"Bob Roe",[new ChatSegment("raid? me too")],100);
            store.Add(Start.AddSeconds(2),10,"Bob Roe",[new ChatSegment("hello")],100);
            Refresh();
            var scratch=new HashSet<long>();
            Check(general.Alerts==2 && group.Alerts==1 && ChatAlert.Unseen([general,group],scratch)==2,$"two alerts, one in both tabs: {ChatAlert.Unseen([general,group],scratch)}");
            general.MarkSeen();
            Check(ChatAlert.Unseen([general,group],scratch)==1 && general.AlertIds.Count==0,"seen in General, still not seen in Party");
            group.MarkSeen();
            Check(ChatAlert.Unseen([general,group],scratch)==0 && ChatAlert.Unseen([],scratch)==0,"seen everywhere; no tab");
            Check(!new ChatSettings().ServerBarAlerts,"off by default");
        }),
        ("Chat server bar 1647: wiring: the entry shown only logged in with the window and the setting on, hidden during a cut, removed; the plugin gives the count and the click; the window counts and selects; texts",()=>
        {
            var entry=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Alarms/InfoDtrEntries.Dalamud.cs"));
            Check(entry.Contains("Show(chat,3,loggedIn && configuration.Chat.WindowOpen && configuration.Chat.ServerBarAlerts ? ChatLine():null);",StringComparison.Ordinal)
                && entry.Contains("if(CompetitiveGate.Cut)chat.Shown=false;",StringComparison.Ordinal) && entry.Contains("chat.Remove();",StringComparison.Ordinal)
                && entry.Contains("return count<=0 ? null:",StringComparison.Ordinal),"the entry");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.ServerBar.cs")).Contains("()=>mainWindow.ChatAlertsUnseen,mainWindow.ShowChatAlert),",StringComparison.Ordinal),"the plugin");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("Volatile.Write(ref chatAlertsUnseen,ChatAlert.Unseen(tabs.Select(ChatMessages.ViewOf),chatUnseen));",StringComparison.Ordinal)
                && window.Contains("index==alertTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None",StringComparison.Ordinal)
                && window.Contains("BoolSetting(\"ChatWindow.ServerBarAlerts\",\"ChatWindow.ServerBarAlertsTip\"",StringComparison.Ordinal),"the window");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach(var key in new[]{"ServerBarAlerts","ServerBarAlertsTip","Chat","ChatTip"})Check(text.Contains($"\"{key}\":",StringComparison.Ordinal),language+" "+key);
            }
        }),
    ];
}
