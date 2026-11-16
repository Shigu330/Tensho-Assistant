using TenshoAssistant.Chat;

// X-467 (0.1504, D-181, DEC-053 completed): « Hide the game chat's messages »: only the panels are hidden, only those Tensho hid come back,
// everything comes back when no longer wanted and on unload; Tensho's window goes above the game's typing box; off by default; nothing is sent.
internal static class ChatGameHide1504Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static ChatPanelState[] Panels(params (int Index,bool Ready,bool Visible)[] panels)=>panels.Select(panel=>new ChatPanelState(panel.Index,panel.Ready,panel.Visible)).ToArray();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat game hide 1504: only the shown panels are hidden, and only those come back",() =>
        {
            var hide=new ChatGameHide();
            var (toHide,toShow)=hide.Step(true,Panels((0,true,true),(1,true,true),(2,true,false),(3,false,false)));
            Check(toHide.SequenceEqual([0,1]) && toShow.Count==0,"panels 0 and 1 hidden: "+string.Join(",",toHide));
            // Next frame: the game shows panel 2 on its own (a new tab): hidden too; the ones already hidden are not touched again.
            (toHide,toShow)=hide.Step(true,Panels((0,true,false),(1,true,false),(2,true,true),(3,false,false)));
            Check(toHide.SequenceEqual([2]) && hide.Hidden.SequenceEqual([0,1,2]),"panel 2 hidden too");
            // No longer wanted: the three come back, panel 3 (the game's own hidden one) is never shown.
            (toHide,toShow)=hide.Step(false,Panels((0,true,false),(1,true,false),(2,true,false),(3,true,false)));
            Check(toShow.SequenceEqual([0,1,2]) && toHide.Count==0 && hide.Hidden.Count==0,"given back: "+string.Join(",",toShow));
            (toHide,toShow)=hide.Step(false,Panels((0,true,true)));
            Check(toHide.Count==0 && toShow.Count==0,"nothing more once given back");
        }),
        ("Chat game hide 1504: a panel rebuilt by the game is not shown again; unload gives everything back",() =>
        {
            var hide=new ChatGameHide();
            hide.Step(true,Panels((0,true,true),(1,true,true)));
            var (_,toShow)=hide.Step(false,Panels((0,false,false),(1,true,false)));
            Check(toShow.SequenceEqual([1]) && hide.Hidden.Count==0,"panel 0 no longer ready: nothing to give back");
            hide.Step(true,Panels((0,true,true),(3,true,true)));
            Check(hide.GiveBack().SequenceEqual([0,3]) && hide.Hidden.Count==0 && hide.GiveBack().Count==0,"unload: every hidden panel, once");
        }),
        ("Chat game hide 1504: Tensho's window goes above the game's typing box, never on a doubtful box",() =>
        {
            var window=new ChatLogBox(1,824,706,256);
            ChatLogPanelBox[] panels=[new(1,true,new ChatLogBox(500,100,300,200)),new(0,false,new ChatLogBox(1,824,706,221))];
            // The 0.1502 reading: the typing box at y 1024, the panel down to 1045: the window stops at 1024.
            Check(ChatFollow.Box(panels,window,new ChatLogBox(24,1024,679,28))==new ChatLogBox(1,824,706,200),"panel 0, down to the typing box");
            // A typing box outside ChatLog, in the panel's upper half or under the panel: the panel's whole box.
            var whole=new ChatLogBox(1,824,706,221);
            Check(ChatFollow.Box(panels,window,new ChatLogBox(24,2000,679,28))==whole,"outside the window");
            Check(ChatFollow.Box(panels,window,new ChatLogBox(24,850,679,28))==whole,"upper half");
            Check(ChatFollow.Box(panels,window,new ChatLogBox(24,1050,679,28))==whole,"under the panel");
            Check(ChatFollow.Box(panels,window,null)==whole,"unread");
            Check(ChatFollow.Box([],window,null) is null && ChatFollow.Box([new(0,true,new ChatLogBox(0,0,10,300))],window,null) is null,"no panel, or too small: no place");
        }),
        ("Chat game hide 1504: off by default, given back out of it, the typing box untouched, nothing sent",() =>
        {
            var settings=new ChatSettings();
            Check(!settings.HideGameMessages && !settings.BothChats,"off by default");
            var hider=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/GameChatHider.Dalamud.cs"));
            Check(hider.Contains("CompetitiveGate.AddAlways(framework,OnUpdate)",StringComparison.Ordinal) && hider.Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)now=false;",StringComparison.Ordinal),"runs in a cut too, to give back");
            Check(hider.Contains("unit->IsVisible=false",StringComparison.Ordinal) && hider.Contains("unit->IsVisible=true",StringComparison.Ordinal),"panels hidden and shown");
            var chatLogLines=hider.Split('\n').Where(line=>line.Contains("IsVisible=",StringComparison.Ordinal));
            Check(chatLogLines.All(line=>line.Contains("Panel(index)",StringComparison.Ordinal)),"only the panels are written, never ChatLog");
            foreach(var forbidden in new[]{"ProcessChatBox","SendMessage","SetText","SetFocusNode","UIModule","SetPosition","SetSize"})
                Check(!hider.Contains(forbidden,StringComparison.Ordinal),$"the hider does not use {forbidden}");
            Check(hider.Contains("RunOnFrameworkThread(Release)",StringComparison.Ordinal),"given back on unload");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("!inDesktopApp && PanelsOpen && !chatWindowFailed && ChatOptions.WindowOpen && ChatOptions.HideGameMessages && !ChatOptions.BothChats && ChatWindowShown()",StringComparison.Ordinal),"wanted only with the window open, shown and working");
            var tracker=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Dashboard.cs"));
            Check(tracker.Contains("if(name==\"Chat\")chatWindowFailed=true;",StringComparison.Ordinal),"a failed chat window gives the messages back");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("new Chat.GameChatHider(framework,gameGui,log,mainWindow.ChatHidesGameMessages)",StringComparison.Ordinal) && plugin.Contains("chatHider?.Dispose()",StringComparison.Ordinal),"registered and disposed");
        }),
    ];
}
