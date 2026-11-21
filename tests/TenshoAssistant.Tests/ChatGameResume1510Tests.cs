using TenshoAssistant.Chat;

// 0.1510 (X-534, review of 0.1507, minor M1 and M2). M1: the box of the game's messages was read only while they were hidden; when a game
// window over Tensho's chat window went away (or a fight ended with « Hide in combat »), Tensho's window was drawn one frame at the player's
// own place before taking the messages' place again, and if that place was covered by another game window, it was judged covered and the
// messages stayed shown. The box is now read while the game's chat is shown, also while the messages are given back. M2: the pure choice said
// « competitive: always given back » while the hider held over a hidden game chat; one rule now, ChatHideWants.InFrame, applied by the hider.
internal static class ChatGameResume1510Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat game resume 1510: behaviour: after a game window over it goes, the place is there in the same frame (M1)",() =>
        {
            // X-689 (0.1657): a game window over Tensho's window, or a fight, no longer gives the messages back: hidden, and the place read in
            // every frame, so it is there when the window shows again (no frame at its own place).
            var covered=ChatHideWants.InFrame(ChatHideWants.Of(true,false,false),false,true);
            Check(covered==ChatHideWant.Hide && ChatHideWants.ReadsPlace(covered),"covered or in combat: hidden, the place read");
            Check(ChatHideWants.ReadsPlace(ChatHideWant.Release),"given back: place read");
            // The game's chat not shown: nothing read (Hold), as before.
            Check(!ChatHideWants.ReadsPlace(ChatHideWants.InFrame(ChatHideWant.Hide,false,false)),"chat not shown: no place");
        }),
        ("Chat game resume 1510: behaviour: one rule for the frame, competitive cut and hidden chat (M2)",() =>
        {
            Check(ChatHideWants.InFrame(ChatHideWant.Hide,true,true)==ChatHideWant.Release,"competitive, chat shown: given back (DEC-038)");
            Check(ChatHideWants.InFrame(ChatHideWant.Hide,true,false)==ChatHideWant.Hold,"competitive, chat not shown (opening scene): held, never given back over a hidden chat");
            Check(ChatHideWants.InFrame(ChatHideWant.Release,true,false)==ChatHideWant.Hold,"whatever was wished");
            Check(ChatHideWants.InFrame(ChatHideWant.Hide,false,true)==ChatHideWant.Hide && ChatHideWants.InFrame(ChatHideWant.Release,false,true)==ChatHideWant.Release,"otherwise the wish");
            // Of keeps its own contract (competitive gives back even with the interface hidden); InFrame states what the hider then does.
            Check(ChatHideWants.Of(true,true,true)==ChatHideWant.Release && ChatHideWants.InFrame(ChatHideWants.Of(true,true,true),true,false)==ChatHideWant.Hold,"the two agree");
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatGameHide.cs"));
            Check(!source.Contains("always given back",StringComparison.Ordinal) && source.Contains("holds first (InFrame)",StringComparison.Ordinal),"the contract no longer says « always »");
            var hider=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/GameChatHider.Dalamud.cs"));
            Check(!hider.Contains("now=ChatHideWant.Release;",StringComparison.Ordinal) && !hider.Contains("now=ChatHideWant.Hold;",StringComparison.Ordinal),"the hider has no rule of its own");
        }),
    ];
}
