using TenshoAssistant.Chat;

// 0.1652 (X-676, review of 0.1644 to 0.1650, M1): with the tabs' names hidden, a click on the server bar's « ! Chat N » showed nothing.
internal static class ChatAlertClick1652Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset Start=new(2026,11,20,22,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat alert click 1652: behaviour: the first tab holding an alert not seen is found; tabs hidden, it is shown at once; tabs drawn, its tab item takes it; none: nothing moves",()=>
        {
            var store=new ChatStore();
            var settings=new ChatSettings{AlertWords="raid"};
            var say=new ChatTabSettings{Name="Say",Channels=[ChatChannel.Say]};
            var party=new ChatTabSettings{Name="Party",Channels=[ChatChannel.Party]};
            var (first,second)=(new ChatTabView(),new ChatTabView());
            void Refresh(){first.Refresh(store,say,100,null,settings);second.Refresh(store,party,100,null,settings);}
            Refresh();
            Check(ChatAlert.FirstWithAlert([first,second])==-1 && ChatAlert.FirstWithAlert([])==-1,"no alert yet");
            store.Add(Start,10,"Bob Roe",[new ChatSegment("hello")],100);
            store.Add(Start.AddSeconds(1),14,"Bob Roe",[new ChatSegment("raid at 9")],100);
            Refresh();
            var alertTab=ChatAlert.FirstWithAlert([first,second]);
            Check(alertTab==1,$"the party tab holds the alert: {alertTab}");
            Check(ChatAlert.ShownAfterClick(0,alertTab,tabsHidden:true)==1,"tabs hidden: the party tab is shown at once (before: nothing moved)");
            Check(ChatAlert.ShownAfterClick(0,alertTab,tabsHidden:false)==0,"tabs drawn: its tab item takes it (SetSelected), the tab shown is changed when drawn");
            second.MarkSeen();
            Check(ChatAlert.FirstWithAlert([first,second])==-1 && ChatAlert.ShownAfterClick(0,-1,tabsHidden:true)==0,"seen: none, the tab shown stays");
        }),
        ("Chat alert click 1652: wiring: the click finds the tab by ChatAlert.FirstWithAlert and shows it when the tabs are hidden; the tab item still selected otherwise",()=>
        {
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("alertTab=ChatAlert.FirstWithAlert(tabs.Select(ChatMessages.ViewOf));",StringComparison.Ordinal)
                && window.Contains("chatTabShown=ChatAlert.ShownAfterClick(chatTabShown,alertTab,options.HideTabs);",StringComparison.Ordinal)
                && window.Contains("index==alertTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None",StringComparison.Ordinal),"the window");
        }),
    ];
}
