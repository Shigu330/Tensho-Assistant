using TenshoAssistant.Chat;

// 0.1507 (X-528, review of 0.1504, minor M1 to M4). M1: under a game window, Tensho's chat window set to « Hide » went away and the game's
// messages stayed hidden: no message at all; they now come back. M2: the panels of tabs detached elsewhere were hidden with nothing in their
// place; only the panels under Tensho's window are hidden now. (The other half of M2, a panel the game hid itself shown again on give-back, is
// set aside: the game hides its panels with its interface or its chat, where the hider now holds since 0.1506, and a docked panel's
// visibility is the hider's own while it is hidden.) M3: the tooltip and DEC-053 list every case. M4: « reduced » cleared on following.
internal static class ChatGameHideMinor1507Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat game hide minor 1507: behaviour: covered by a game window with « Hide », the game's messages stay hidden (M1 reversed by X-689, the user's report of 20/11)",() =>
        {
            Check(ChatHideWants.Of(true,false,false)==ChatHideWant.Hide,"Tensho's window hidden or not: the game's messages stay hidden");
            Check(ChatHideWants.Of(true,false,true)==ChatHideWant.Hold,"a cutscene still holds");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("return !ChatCover.Hidden(ChatOptions.WhenCovered,covered);",StringComparison.Ordinal),"the covered state only hides Tensho's window");
            // « Fade » keeps the messages hidden (the window stays there, faded): only « Hide » gives them back.
            Check(!ChatCover.Hidden(ChatCovered.Fade,true) && ChatCover.Hidden(ChatCovered.Hide,true) && !ChatCover.Hidden(ChatCovered.Hide,false),"only « Hide » under a window");
        }),
        ("Chat game hide minor 1507: behaviour: a tab detached elsewhere stays shown, the docked ones are hidden (M2)",() =>
        {
            var main=new ChatLogBox(1,824,706,221);
            Check(ChatFollow.Under(0,new ChatLogBox(1,824,706,221),null),"panel 0 always");
            Check(ChatFollow.Under(1,new ChatLogBox(1,824,706,221),main) && ChatFollow.Under(2,new ChatLogBox(1,824,706,221),main),"docked at panel 0's place (the 0.1502 reading)");
            Check(!ChatFollow.Under(1,new ChatLogBox(1200,300,400,200),main),"detached elsewhere: not under");
            Check(!ChatFollow.Under(3,new ChatLogBox(600,800,400,200),main),"mostly outside: not under");
            Check(ChatFollow.Under(3,new ChatLogBox(300,850,400,150),main),"mostly over it: under");
            Check(!ChatFollow.Under(1,new ChatLogBox(1,824,706,221),null) && !ChatFollow.Under(1,new ChatLogBox(1,824,0,0),main),"no panel 0, or no box: not under");
            var hide=new ChatGameHide();
            var (toHide,_)=hide.Step(ChatHideWant.Hide,[new(0,true,true,true),new(1,true,true,false),new(2,true,true,true)]);
            Check(toHide.SequenceEqual([0,2]) && hide.Hidden.SequenceEqual([0,2]),"the detached tab 1 is neither hidden nor given back later");
            var hider=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/GameChatHider.Dalamud.cs"));
            Check(hider.Contains("ChatFollow.Under(index,box,mainBox)",StringComparison.Ordinal),"the hider reads it");
        }),
        ("Chat game hide minor 1507: the tooltip and DEC-053 list every case; « reduced » cleared on following (M3, M4)",() =>
        {
            var en=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            foreach(var words in new[]{"« Hide in combat »","« Under a game window »","During a cutscene, in GPose","In the desktop app","detached elsewhere"})
                Check(en.Contains(words,StringComparison.Ordinal),"English tooltip: "+words);
            foreach(var words in new[]{"« Masquer en combat »","« Sous une fenêtre du jeu »","Pendant une scène cinématique, en GPose","Dans l'application de bureau","détaché ailleurs"})
                Check(fr.Contains(words,StringComparison.Ordinal),"French tooltip: "+words);
            var decisions=File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md"));
            Check(decisions.Contains("Made precise in 0.1506 and 0.1507 (X-527, X-528",StringComparison.Ordinal) && decisions.Contains("Précisée en 0.1506 et 0.1507 (X-527, X-528",StringComparison.Ordinal),"DEC-053, both halves");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("if(follow is not null)chatReduced=false;",StringComparison.Ordinal),"reduced cleared when following starts");
        }),
    ];
}
