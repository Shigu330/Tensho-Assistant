using TenshoAssistant.UI;

// 0.1511 (X-539, user capture of the title screen, 17/11): Tensho drew « Suivi Tensho » and its chat window on the title screen, before
// any character was logged in. Nothing is drawn until a character is logged in and loaded; the windows' open state is left untouched, so
// the same windows come back after the login, and a logout followed by a login gives the same state again.
internal static class CharacterGate1511Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // A made frame loop: the windows the player left open, and what one frame draws (the gate never changes what is open).
    private static IReadOnlyList<string> Frame(HashSet<string> open,bool loggedIn,bool loaded)
        => CharacterGate.Draws(loggedIn,loaded) ? open.OrderBy(name=>name,StringComparer.Ordinal).ToList() : [];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Character gate 1511: behaviour: logged out nothing, logged in the state kept, logout then login the same state",() =>
        {
            var open=new HashSet<string>(StringComparer.Ordinal){"Suivi Tensho","Tensho chat"};
            Check(Frame(open,false,false).Count==0,"title screen: nothing drawn");
            Check(Frame(open,true,false).Count==0,"character selection or loading: logged in, character not loaded: nothing drawn");
            Check(Frame(open,false,true).Count==0,"never drawn without a login");
            var first=Frame(open,true,true);
            Check(first.SequenceEqual(["Suivi Tensho","Tensho chat"]),"logged in: the windows left open");
            Check(Frame(open,false,false).Count==0 && open.Count==2,"logout: nothing drawn, the open state kept");
            Check(Frame(open,true,true).SequenceEqual(first),"login again: the same windows");
        }),
        ("Character gate 1511: wiring: Draw and /tensho wait for a character, the open state is not touched",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("characterReady=()=>UI.CharacterGate.Draws(client.IsLoggedIn,player.IsLoaded);",StringComparison.Ordinal),"the gate reads the login and the player");
            var draw=plugin[plugin.IndexOf("    private void Draw()",StringComparison.Ordinal)..];
            draw=draw[..draw.IndexOf("    private void DrawFrame()",StringComparison.Ordinal)];
            Check(draw.Contains("if(!characterReady())return;",StringComparison.Ordinal),"Draw returns before any window or overlay");
            Check(draw.IndexOf("if(!characterReady())return;",StringComparison.Ordinal)<draw.IndexOf("DrawFrame()",StringComparison.Ordinal),"before any window or overlay (after the competitive cut, which needs a character anyway)");
            Check(!draw.Contains("IsOpen",StringComparison.Ordinal),"the gate never closes a window (state kept)");
            var command=plugin[plugin.IndexOf("    private void OnCommand(",StringComparison.Ordinal)..];
            command=command[..command.IndexOf("    private Dalamud.Game.Text.SeStringHandling.Payloads.DalamudLinkPayload? linkTestPayload;",StringComparison.Ordinal)];
            Check(command.Contains("if(!characterReady()){chatGui.Print(\"[Tensho] \"+localization.Get(\"Plugin.NeedsCharacter\"));return;}",StringComparison.Ordinal),"/tensho says a character is needed");
            foreach(var language in new[]{"en","fr"})
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).Contains("\"NeedsCharacter\":",StringComparison.Ordinal),$"{language}: the line");
        }),
    ];
}
