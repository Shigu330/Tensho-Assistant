using TenshoAssistant.Chat;

// 0.1506 (X-527, review of 0.1504, I1): during a cutscene, in GPose or with the game's interface hidden, Tensho's chat window is not shown;
// 0.1504 then gave the game's messages back and forced IsVisible=true on its panels, so they could come back over the cutscene, while the
// in-game test expects them to stay hidden. Now the hider holds (touches nothing) while the game hides its interface or its chat, and gives
// them back only when they are really no longer wanted (option off, window closed, both chats, competitive content, « Hide in combat »).
internal static class ChatGameHold1506Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static ChatPanelState[] Panels(params (int Index,bool Ready,bool Visible)[] panels)=>panels.Select(panel=>new ChatPanelState(panel.Index,panel.Ready,panel.Visible)).ToArray();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat game hold 1506: behaviour: during a cutscene the hidden messages are neither shown again nor touched, then hidden or given back",() =>
        {
            var hide=new ChatGameHide();
            hide.Step(ChatHideWant.Hide,Panels((0,true,true),(1,true,true)));
            // The cutscene: the game hides its interface; the frames Hold: nothing shown again (the 0.1504 bug), nothing more hidden.
            for(var frame=0;frame<3;frame++)
            {
                var (toHide,toShow)=hide.Step(ChatHideWant.Hold,Panels((0,true,false),(1,true,false),(2,true,true)));
                Check(toHide.Count==0 && toShow.Count==0 && hide.Hidden.SequenceEqual([0,1]),"held: nothing touched, the hidden panels kept to give back");
            }
            // After it: hidden again if the game showed one on its own, and given back when the option is unticked.
            var (again,_)=hide.Step(ChatHideWant.Hide,Panels((0,true,true),(1,true,false)));
            Check(again.SequenceEqual([0]),"after the cutscene, a panel the game showed again is hidden again");
            var (_,back)=hide.Step(ChatHideWant.Release,Panels((0,true,false),(1,true,false)));
            Check(back.SequenceEqual([0,1]) && hide.Hidden.Count==0,"untick: both given back");
            var (_,none)=hide.Step(ChatHideWant.Hold,Panels((0,true,false)));
            Check(none.Count==0,"held with nothing hidden: nothing");
        }),
        ("Chat game hold 1506: behaviour: the choice of each frame (cutscene and GPose hold; competitive and untick give back; X-689: combat no longer)",() =>
        {
            Check(ChatHideWants.Of(true,false,false)==ChatHideWant.Hide,"wanted, nothing else: hidden");
            Check(ChatHideWants.Of(true,false,true)==ChatHideWant.Hold,"interface hidden by the game (cutscene, GPose): held");
            Check(ChatHideWants.Of(true,true,true)==ChatHideWant.Release,"competitive content: given back, even with the interface hidden (DEC-038)");
            Check(ChatHideWants.Of(false,false,true)==ChatHideWant.Release && ChatHideWants.Of(false,false,false)==ChatHideWant.Release,"no longer wanted: given back");
        }),
        ("Chat game hold 1506: wiring: the window passes the game's interface state, the hider holds while ChatLog is not shown",() =>
        {
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("TenshoAssistant.Competitive.CompetitiveGate.Cut,gameGui.GameUiHidden);",StringComparison.Ordinal),"the three states passed (X-689: no longer Tensho's own hiding)");
            Check(window.Contains("internal bool ChatHidesGameMessages()=>ChatGameMessagesWant()==ChatHideWant.Hide;",StringComparison.Ordinal),"followed only while hidden");
            var hider=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/GameChatHider.Dalamud.cs"));
            Check(hider.Contains("ChatHideWants.InFrame(wanted(),chatLogShown:ChatLogShown(),competitiveCut:TenshoAssistant.Competitive.CompetitiveGate.Cut)",StringComparison.Ordinal)
                && ChatHideWants.InFrame(ChatHideWant.Hide,false,false)==ChatHideWant.Hold,"the game's chat not shown: held");
            Check(hider.Contains("place=ChatHideWants.ReadsPlace(now) ? Read(boxes) : null;",StringComparison.Ordinal) && window.Contains("var follow=ChatHidesGameMessages() ? ChatGamePlace?.Invoke():null;",StringComparison.Ordinal),
                "a place read while the chat is shown (0.1509, X-534 M1), used by the window only while hidden");
            Check(ChatHideWants.InFrame(ChatHideWant.Hide,true,false)==ChatHideWant.Hold,"a competitive cut never gives back over a hidden chat");
        }),
    ];
}
