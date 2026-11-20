using TenshoAssistant.Chat;

// 0.1512 (X-540 M1, review of 0.1510): the place of the game chat's messages was read in every frame the game's chat was shown, also with
// the option « Hide the game chat's messages » off (its default, so for every player) and during a competitive cut (DEC-038: all of Tensho
// stops); in developer mode its log line was written at each change, and a reading error stopped the hider (« Hiding the game chat's
// messages stopped ») for players who do not use it. The place is now read only while the option is wanted, outside a competitive cut.
internal static class ChatPlaceOption1512Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // What the hider does in a frame: (state applied, place read).
    private static (ChatHideWant Now,bool Reads) Frame(bool option,bool cut,bool chatShown,bool covered,bool combat)
    {
        // X-689 (0.1657): Tensho's window hiding itself (covered, combat) no longer changes the choice: still passed, to show it.
        _=(covered,combat);
        var now=ChatHideWants.InFrame(ChatHideWants.Of(option,cut,false),cut,chatShown);
        return (now,ChatHideWants.OptionReadsPlace(option,cut) && ChatHideWants.ReadsPlace(now));
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat place option 1512: behaviour: option off or competitive cut: never read; option wanted: read, also while given back",() =>
        {
            Check(Frame(false,false,true,false,false)==(ChatHideWant.Release,false),"option off (the default): given back, nothing read");
            Check(Frame(true,true,true,false,false)==(ChatHideWant.Release,false),"competitive cut: given back, nothing read (DEC-038)");
            Check(Frame(true,false,true,false,false)==(ChatHideWant.Hide,true),"option on: hidden, place read");
            Check(Frame(true,false,true,true,false)==(ChatHideWant.Hide,true),"option on, a game window over Tensho's: still hidden (X-689), place read");
            Check(Frame(true,false,true,false,true)==(ChatHideWant.Hide,true),"option on, in combat with « Hide in combat »: still hidden (X-689), place read");
            Check(Frame(true,false,false,false,false)==(ChatHideWant.Hold,false),"game chat not shown: held, nothing read");
        }),
        ("Chat place option 1512: wiring: the hider reads the place only through OptionReadsPlace, the plugin gives it the option",() =>
        {
            var hider=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/GameChatHider.Dalamud.cs"));
            Check(hider.Contains("if(ChatHideWants.OptionReadsPlace(OptionWanted(),TenshoAssistant.Competitive.CompetitiveGate.Cut))",StringComparison.Ordinal)
                && hider.Contains("public Func<bool> OptionWanted{get;init;}=()=>false;",StringComparison.Ordinal),"read only while wanted; nothing when unset");
            Check(hider.Split("Read(boxes)").Length==2,"one reading of the place");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("OptionWanted=mainWindow.ChatGameMessagesOption}",StringComparison.Ordinal),"the plugin passes the option");
        }),
    ];
}
