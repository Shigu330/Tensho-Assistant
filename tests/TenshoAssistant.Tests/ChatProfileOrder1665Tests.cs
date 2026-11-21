using TenshoAssistant.Chat;

// 0.1665 (X-697 M2, review of 0.1664: the definition of X-687 asked « each profile keeps its order »; 0.1663 had one order for all): a drag or a
// move in a context with a profile changes that profile's order only; the settings' order and the other profiles keep theirs.
internal static class ChatProfileOrder1665Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static List<ChatTabSettings> Defaults()=>ChatSettings.DefaultTabs("General","Battle","Event","Social");
    private static string Names(IEnumerable<ChatTabSettings> tabs)=>string.Join(",",tabs.Select(tab=>tab.Name));
    private static string ShownBy(List<ChatTabSettings> tabs,ChatContextProfile? profile){var into=new List<ChatTabSettings>();ChatContexts.Shown(tabs,profile,into);return Names(into);}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat profile order 1665: a drag in a profile is its own order; the settings and another profile unchanged",()=>
        {
            var tabs=Defaults();
            var duty=new ChatContextProfile{Tabs=["General","Battle","Social"]};
            var town=new ChatContextProfile();
            Check(ChatContexts.SetOrder(duty,tabs,[tabs[3],tabs[1],tabs[0]]),"changed");
            Check(ShownBy(tabs,duty)=="Social,Battle,General","the duty's own order");
            Check(Names(tabs)=="General,Battle,Event,Social" && ShownBy(tabs,null)=="General,Battle,Event,Social","the window's order unchanged");
            Check(ShownBy(tabs,town)=="General,Battle,Event,Social","the other profile unchanged");
            Check(ChatContexts.SetOrder(town,tabs,[tabs[2],tabs[0],tabs[1],tabs[3]]) && ShownBy(tabs,town)=="Event,General,Battle,Social" && ShownBy(tabs,duty)=="Social,Battle,General","two profiles, two orders");
            Check(!ChatContexts.SetOrder(duty,tabs,[tabs[3],tabs[1],tabs[0]]),"the same order: nothing to save");
        }),
        ("Chat profile order 1665: a tab shown again or added later comes after; a hidden tab keeps its place; a conversation left out",()=>
        {
            var tabs=Defaults();
            var duty=new ChatContextProfile{Tabs=["General","Social"]};
            var conversation=new ChatTabSettings{Name="Aya",Conversation="Aya Lune"};
            Check(ChatContexts.SetOrder(duty,tabs,[conversation,tabs[3],tabs[0]]) && ShownBy(tabs,duty)=="Social,General","conversation out");
            ChatContexts.SetTab(duty,tabs,tabs[1],true);
            Check(ShownBy(tabs,duty)=="Social,General,Battle","a tab shown again: after the ordered ones");
            Check(ChatContexts.Move(duty,tabs,tabs[1],-1) && ShownBy(tabs,duty)=="Social,Battle,General","its menu moves it in the profile");
            ChatContexts.SetTab(duty,tabs,tabs[1],false);
            ChatContexts.SetTab(duty,tabs,tabs[1],true);
            Check(ShownBy(tabs,duty)=="Social,Battle,General","hidden then shown: its place kept");
            Check(!ChatContexts.Move(duty,tabs,tabs[3],-1) && !ChatContexts.Move(duty,tabs,tabs[2],1),"an end, or a tab not shown: nothing");
        }),
        ("Chat profile order 1665: the profile's order saved with the settings (kept at reload)",()=>
        {
            var settings=new ChatSettings{ContextProfiles=true};
            var tabs=settings.EnsureTabs("General","Battle","Event","Social");
            var duty=ChatContexts.Edit(settings,ChatContext.Duty);
            ChatContexts.SetOrder(duty,tabs,[tabs[3],tabs[2],tabs[1],tabs[0]]);
            var back=Newtonsoft.Json.JsonConvert.DeserializeObject<ChatSettings>(Newtonsoft.Json.JsonConvert.SerializeObject(settings))!;
            var backTabs=back.EnsureTabs("General","Battle","Event","Social");
            Check(ShownBy(backTabs,ChatContexts.ProfileOf(back,ChatContext.Duty))=="Social,Event,Battle,General" && Names(backTabs)=="General,Battle,Event,Social","read back");
        }),
        ("Chat profile order 1665: the window's drag and the tab's menu go to the profile in a context",()=>
        {
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("if(!same && ChatContexts.ProfileOf(options,chatContextNow) is { } profile){if(ChatContexts.SetOrder(profile,ChatTabs(options),shown))ChatTabsMoved(tabs);return;}"),"drag");
            Check(window.Contains("ChatContexts.Move(profile,all,tab,-1))ChatTabsMoved(tabs);") && window.Contains("ChatContexts.Move(profile,all,tab,1))ChatTabsMoved(tabs);"),"menu");
        }),
    ];
}
