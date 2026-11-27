using TenshoAssistant.Chat;

// 0.1508 (X-533, the user's request of 16/11 around 23 h 15 to 23 h 25): with « Hide the game chat's messages », the game's tab bar stayed
// visible, the current channel's name (« Say ») was cut by Tensho's window, and the channel menu, which opens upward, went behind it. The tab
// bar is now hidden and given back with the messages; Tensho's window takes the frame of the messages and tabs down to the top of the typing
// row (typing box or channel name, the higher); it steps aside while the channel menu is open.
internal static class ChatGameTabs1508Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat game tabs 1508: behaviour: the frame stops above the channel's name and starts at tabs drawn above the panel",() =>
        {
            // The 0.1502 reading (dalamud.log of 16/11 14:58): ChatLog 1,824 706x256, panel 0 1,824 706x221, typing box at y 1024.
            var window=new ChatLogBox(1,824,706,256);
            ChatLogPanelBox[] panels=[new(0,true,new ChatLogBox(1,824,706,221))];
            var input=new ChatLogBox(24,1024,679,28);
            Check(ChatFollow.Box(panels,window,input,new ChatLogBox(30,1016,60,14))==new ChatLogBox(1,824,706,192),"the channel's name above the typing box: stops above it");
            Check(ChatFollow.Box(panels,window,input,new ChatLogBox(30,1030,60,14))==new ChatLogBox(1,824,706,200),"the name below the typing box's top: the typing box decides");
            Check(ChatFollow.Box(panels,window,input,new ChatLogBox(30,850,60,14))==new ChatLogBox(1,824,706,200),"a name in the panel's upper half is not believed");
            Check(ChatFollow.Box(panels,window,input,new ChatLogBox(30,2000,60,14))==new ChatLogBox(1,824,706,200),"a name outside ChatLog is not believed");
            Check(ChatFollow.Box(panels,window,input,null)==ChatFollow.Box(panels,window,input),"unread name: as before 0.1508");
            // Tabs drawn at the top of ChatLog, above the panel: the frame starts at ChatLog's top, covering them.
            var high=new ChatLogBox(1,800,706,280);
            Check(ChatFollow.Box(panels,high,input,null)==new ChatLogBox(1,800,706,224),"tabs above the panel: from ChatLog's top");
            Check(ChatFollow.Box(panels,new ChatLogBox(1,500,706,600),input,null)==new ChatLogBox(1,824,706,200),"a ChatLog top far above the panel is not a tab row");
        }),
        ("Chat game tabs 1508: behaviour: the tab bar is hidden, held and given back like the panels",() =>
        {
            var tabs=new ChatGameHide();
            ChatPanelState Slot(int slot,int count,bool node,bool visible)=>new(slot,ChatTabBar.Ready(slot,count,node),visible);
            var bar=new List<ChatPanelState>();
            for(var slot=0;slot<ChatTabBar.Count;slot++)bar.Add(Slot(slot,3,true,slot!=3 && slot!=4));
            var (hidden,_)=tabs.Step(ChatHideWant.Hide,bar);
            Check(hidden.SequenceEqual([0,1,2,5,6,7,8]),"the three tabs, « add », both ends and the settings button hidden: "+string.Join(",",hidden));
            Check(!ChatTabBar.Ready(3,3,true) && !ChatTabBar.Ready(0,3,false) && ChatTabBar.Ready(ChatTabBar.Settings,0,true),"a tab past the game's count, or no node, is not read");
            var (more,back)=tabs.Step(ChatHideWant.Hold,bar);
            Check(more.Count==0 && back.Count==0 && tabs.Hidden.Count==7,"held: nothing touched, kept to give back");
            var (_,shown)=tabs.Step(ChatHideWant.Release,bar);
            Check(shown.SequenceEqual([0,1,2,5,6,7,8]) && tabs.Hidden.Count==0,"given back: only what Tensho hid");
            tabs.Step(ChatHideWant.Hide,bar);
            Check(tabs.GiveBack().Count==7 && tabs.Hidden.Count==0,"unload gives everything back");
        }),
        ("Chat game tabs 1508: behaviour: Tensho's window steps aside only over the game's messages with the channel menu open",() =>
        {
            Check(ChatFollow.StepsAside(true,true),"following, menu open: steps aside");
            Check(!ChatFollow.StepsAside(true,false) && !ChatFollow.StepsAside(false,true) && !ChatFollow.StepsAside(false,false),"otherwise drawn");
        }),
        ("Chat game tabs 1508: wiring: tabs hidden with the panels' choice, the menu read, the window steps aside, the texts say it",() =>
        {
            var hider=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/GameChatHider.Dalamud.cs"));
            Check(hider.Contains("StepTabs(now);",StringComparison.Ordinal) && hider.Contains("var (toHide,toShow)=tabs.Step(now,states);",StringComparison.Ordinal),"the tabs follow the panels' choice (held, released in a competitive cut)");
            Check(hider.Contains("node->ToggleVisibility(false)",StringComparison.Ordinal) && hider.Contains("var slots=tabs.GiveBack();",StringComparison.Ordinal),"hidden, and given back on failure and unload");
            Check(hider.Contains("hide.Hidden.Count==0 && tabs.Hidden.Count==0",StringComparison.Ordinal),"unload gives the tabs back even with no panel hidden");
            Check(hider.Contains("menuOpen=now==ChatHideWant.Hide && MenuOpen();",StringComparison.Ordinal) && hider.Contains("var open=shown;",StringComparison.Ordinal) && !hider.Contains("owner || shown",StringComparison.Ordinal),"the menu read only while hidden, from its list alone (0.1509: the drop-down owner stays set with the menu closed)");
            Check(hider.Contains("ChatFollow.Box(boxes,window,typing,name)",StringComparison.Ordinal),"the channel's name read for the place");
            Check(!hider.Contains("TextInput->",StringComparison.Ordinal) && !hider.Contains("ChannelSelectDropDown->SelectItem",StringComparison.Ordinal),"the typing box and the menu are never written");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("if(ChatFollow.StepsAside(ChatHidesGameMessages(),ChatGameChannelMenu?.Invoke()==true))return false;",StringComparison.Ordinal),"the window steps aside");
            // X-153 (0.1894): wired by the chat family.
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Chat.cs"));
            Check(plugin.Contains("mainWindow.ChatGameChannelMenu=()=>chatHider?.ChannelMenuOpen==true;",StringComparison.Ordinal) && plugin.Contains("mainWindow.ChatGameChannelMenu=null;",StringComparison.Ordinal),"wired and cleared");
            var en=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(en.Contains("The game chat's tabs are hidden too",StringComparison.Ordinal) && en.Contains("this window steps aside so you can read and click it",StringComparison.Ordinal),"English tooltip");
            Check(fr.Contains("Les onglets du chat du jeu sont cachés aussi",StringComparison.Ordinal) && fr.Contains("cette fenêtre s'efface pour que vous puissiez le lire",StringComparison.Ordinal),"French tooltip");
            var decisions=File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md"));
            Check(decisions.Contains("Changed in 0.1508 (X-533",StringComparison.Ordinal) && decisions.Contains("Changée en 0.1508 (X-533",StringComparison.Ordinal),"DEC-053, both halves");
        }),
    ];
}
