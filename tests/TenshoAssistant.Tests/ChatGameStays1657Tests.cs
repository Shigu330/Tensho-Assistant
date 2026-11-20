using TenshoAssistant.Chat;

// 0.1657 (X-689, X-694, the user's reports of 20/11 around 22:45 and 22:50): while Tensho's chat replaces the game's, the game's chat stays
// hidden when Tensho's window hides itself (combat, duty, cutscene, under a game window, faded), and a hidden panel no longer answers the
// mouse (its root node hidden too). It comes back only with the option off, Tensho's window closed, « both chats », a competitive cut
// (R03.2) or at unload (docs/evidence/0.1657-game-chat-stays-hidden.md).
internal static class ChatGameStays1657Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static List<ChatPanelState> Panels(bool visible)=>[new(0,true,visible),new(1,true,visible),new(2,true,visible)];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat game stays 1657: behaviour: the frames of 20/11 22:58 (covered by the game's tooltip, then uncovered): the game's messages never come back",()=>
        {
            var hide=new ChatGameHide();
            var (hidden,_)=hide.Step(ChatHideWants.InFrame(ChatHideWants.Of(true,false,false),false,true),Panels(true));
            Check(hidden.SequenceEqual([0,1,2]),"option on: the three panels hidden");
            // Tensho's window covered by ItemDetail with « Hide », or hidden in combat, in a duty, faded: the wish is the same.
            for(var frame=0;frame<8;frame++)
            {
                var (more,shown)=hide.Step(ChatHideWants.InFrame(ChatHideWants.Of(true,false,false),false,true),Panels(false));
                Check(shown.Count==0 && more.Count==0,"Tensho's window hidden: nothing shown again (frame "+frame+")");
            }
            Check(hide.Hidden.SequenceEqual([0,1,2]),"still Tensho's to give back");
        }),
        ("Chat game stays 1657: behaviour: given back only with the option off, in a competitive cut (R03.2), or at unload; held while the game hides its interface",()=>
        {
            var hide=new ChatGameHide();
            hide.Step(ChatHideWant.Hide,Panels(true));
            Check(hide.Step(ChatHideWants.InFrame(ChatHideWants.Of(true,false,true),false,false),Panels(false)).Show.Count==0,"cutscene, GPose: held");
            Check(hide.Step(ChatHideWants.InFrame(ChatHideWants.Of(true,true,false),true,true),Panels(false)).Show.SequenceEqual([0,1,2]),"competitive cut: given back");
            hide.Step(ChatHideWant.Hide,Panels(true));
            Check(hide.Step(ChatHideWants.InFrame(ChatHideWants.Of(false,false,false),false,true),Panels(false)).Show.SequenceEqual([0,1,2]),"option off (or window closed, « both chats »): given back");
            hide.Step(ChatHideWant.Hide,Panels(true));
            Check(hide.GiveBack().SequenceEqual([0,1,2]) && hide.Hidden.Count==0,"unload: given back");
        }),
        ("Chat game stays 1657: wiring: the wish no longer reads Tensho's own hiding; the window still hides itself",()=>
        {
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var start=window.IndexOf("internal ChatHideWant ChatGameMessagesWant()",StringComparison.Ordinal);
            var body=window[start..window.IndexOf(';',start)];
            Check(body.Contains("ChatHideWants.Of(ChatGameMessagesOption(),TenshoAssistant.Competitive.CompetitiveGate.Cut,gameGui.GameUiHidden)",StringComparison.Ordinal)
                  && !body.Contains("ChatHiddenNow",StringComparison.Ordinal) && !body.Contains("ChatCover",StringComparison.Ordinal),"the wish: option, cut, the game's interface");
            Check(window.Contains("&& !ChatHiddenNow();",StringComparison.Ordinal) && window.Contains("return !ChatCover.Hidden(ChatOptions.WhenCovered,covered);",StringComparison.Ordinal),
                  "Tensho's window still hides itself in combat and under a game window");
            var option=window[window.IndexOf("internal bool ChatGameMessagesOption()",StringComparison.Ordinal)..];
            option=option[..option.IndexOf(';')];
            Check(option.Contains("ChatOptions.WindowOpen",StringComparison.Ordinal) && option.Contains("!ChatOptions.BothChats",StringComparison.Ordinal)
                  && option.Contains("ChatOptions.HideGameMessages",StringComparison.Ordinal),"the window closed, « both chats », the option off still give back");
        }),
        ("Chat game stays 1657: wiring (X-694): a hidden panel's root node is hidden too, kept hidden, and given back before the flag",()=>
        {
            var hider=Read("src/TenshoAssistant/Chat/GameChatHider.Dalamud.cs");
            Check(hider.Contains("{unit->IsVisible=false;PanelRoot(unit,false);}",StringComparison.Ordinal),"hidden: the flag and the root node");
            Check(hider.Split("{PanelRoot(unit,true);unit->IsVisible=true;}").Length==3,"shown again (frame and unload): the root node first, then the flag");
            Check(hider.Contains("if(now==ChatHideWant.Hide)foreach(var index in hide.Hidden){var unit=Panel(index);if(unit!=null)PanelRoot(unit,false);}",StringComparison.Ordinal),
                  "kept hidden while hidden");
            Check(hider.Contains("if(root!=null && root->IsVisible()!=shown)root->ToggleVisibility(shown);",StringComparison.Ordinal),"only a root node there, only when it differs");
        }),
        ("Chat game stays 1657: the option's tooltip and DEC-053 say it, in both languages",()=>
        {
            foreach(var (language,stays) in new[]{("en","they stay hidden"),("fr","ils restent cachés")})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                var tip=text[text.IndexOf("\"HideGameMessagesTip\"",StringComparison.Ordinal)..];
                tip=tip[..tip.IndexOf('\n')];
                Check(tip.Contains(stays,StringComparison.Ordinal) && !tip.Contains("also in combat when",StringComparison.Ordinal) && !tip.Contains("aussi en combat quand",StringComparison.Ordinal),language);
            }
            var decisions=Read("docs/project/DECISIONS.md");
            Check(decisions.Contains("Changed in 0.1657 (X-689 and X-694",StringComparison.Ordinal) && decisions.Contains("Changée en 0.1657 (X-689 et X-694",StringComparison.Ordinal),"DEC-053, both halves");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1657-game-chat-stays-hidden.md")),"the evidence");
        }),
    ];
}
