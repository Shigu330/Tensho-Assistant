using TenshoAssistant.Chat;

// 0.1663 (X-687, the user's request of 20/11 around 22:05: « no way to sort the tabs » of the Tensho chat window): the tabs move one place by
// their menu or their settings, and by a drag in the window; the order is the settings' (saved, kept at reload), one order for the window and
// its contexts' profiles; the conversations' tabs stay after.
internal static class ChatTabOrder1663Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static List<ChatTabSettings> Defaults()=>ChatSettings.DefaultTabs("General","Battle","Event","Social");
    private static string Names(IEnumerable<ChatTabSettings> tabs)=>string.Join(",",tabs.Select(tab=>tab.Name));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat tab order 1663: one place left or right, never past an end; the tab keeps its own settings",()=>
        {
            var tabs=Defaults();
            tabs[1].KeepWords="boss";
            Check(ChatTabOrder.Move(tabs,1,-1) && Names(tabs)=="Battle,General,Event,Social" && tabs[0].KeepWords=="boss","left");
            Check(ChatTabOrder.Move(tabs,0,1) && Names(tabs)=="General,Battle,Event,Social","right again");
            Check(!ChatTabOrder.Move(tabs,0,-1) && !ChatTabOrder.Move(tabs,3,1) && !ChatTabOrder.Move(tabs,9,-1) && !ChatTabOrder.Move(tabs,1,0) && Names(tabs)=="General,Battle,Event,Social","ends and wrong places: nothing");
        }),
        ("Chat tab order 1663: after a drag, the order the window shows becomes the settings' order; a profile's tabs move among their own places",()=>
        {
            var tabs=Defaults();
            Check(ChatTabOrder.Apply(tabs,[tabs[2],tabs[0],tabs[1],tabs[3]]) && Names(tabs)=="Event,General,Battle,Social","the whole bar");
            Check(!ChatTabOrder.Apply(tabs,tabs.ToList()),"the same order: nothing to save");
            tabs=Defaults();
            // A context's profile shows General and Social only; Social dragged before General: they swap their places, the others stay.
            Check(ChatTabOrder.Apply(tabs,[tabs[3],tabs[0]]) && Names(tabs)=="Social,Battle,Event,General","a profile's tabs");
            tabs=Defaults();
            var conversation=new ChatTabSettings{Name="Aya",Conversation="Aya Lune"};
            Check(!ChatTabOrder.Apply(tabs,[conversation,tabs[0],tabs[1],tabs[2],tabs[3]]) && Names(tabs)=="General,Battle,Event,Social","a conversation dragged first: the settings' order unchanged, it goes back after them");
        }),
        ("Chat tab order 1663: the order is the settings' tabs, saved with them (kept at reload)",()=>
        {
            var settings=new ChatSettings();
            var tabs=settings.EnsureTabs("General","Battle","Event","Social");
            ChatTabOrder.Move(tabs,3,-1);
            var saved=Newtonsoft.Json.JsonConvert.SerializeObject(settings);
            var back=Newtonsoft.Json.JsonConvert.DeserializeObject<ChatSettings>(saved)!;
            Check(Names(back.EnsureTabs("General","Battle","Event","Social"))=="General,Battle,Social,Event","the moved order read back");
        }),
        ("Chat tab order 1663: the bar is reorderable with each tab's own id; the drag's order applied when it ends; moves in the tab's menu and settings",()=>
        {
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("ImGui.BeginTabBar(\"###TenshoChatTabs\"+chatTabBarGeneration,ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.Reorderable)"),"reorderable");
            Check(window.Contains("+\"###ChatTab\"+ChatTabKey(tab),") && !window.Contains("+\"###ChatTab\"+index,"),"an id of its own, not its place");
            Check(window.Contains("ChatTabsAfterDrag(tabs,options,dragged);") && window.Contains("if(!same && ChatTabOrder.Apply(ChatTabs(options),shown))ChatTabsMoved(tabs);")
                  && window.Contains("if(!same){chatTabBarGeneration++;chatTabBarFresh=true;}"),"applied after a drag, else the list's order shown");
            Check(window.Contains("ChatTabOrder.Move(all,at,-1))ChatTabsMoved(tabs);") && window.Contains("ChatTabOrder.Move(tabs,index,-1)){chatSettingsPending=true;return;}"),"the tab's menu and its settings");
            foreach(var key in new[]{"TabMoveLeft","TabMoveRight","TabMoveTip"})
                Check(Read("src/TenshoAssistant/Localization/Languages/en.json").Contains("\""+key+"\":") && Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("\""+key+"\":"),key);
        }),
    ];
}
