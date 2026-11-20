// 0.1579 (X-632, review of 0.1576 to 0.1578, M1): the refusal of a boss cast alert or a cast timer (DalamudRulesCombat1575Tests,
// DalamudRulesCombat1577Tests, DEC-055) widened to the game's cast bar windows. An alert could read the text or the bar of these windows
// (_TargetInfoCastBar, _CastBar, FocusTargetInfo, CastBarEnemy…) instead of IsCasting or CastActionId and pass the earlier tests.
// The only use allowed today: the list of what is not a window in front of anything (GameWindow.NotWindows), which skips the cast bars.
internal static class DalamudRulesCastBar1579Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static IEnumerable<(string Name, string Text)> Sources()
    {
        foreach (var folder in new[] { "src/TenshoAssistant", "src/TenshoAssistant.Desktop" })
        {
            var root = Path.GetFullPath(folder);
            foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) || path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
                yield return (folder + "/" + Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path));
            }
        }
    }

    /// <summary>The game's cast bar windows and their ClientStructs types, by name or start of name (case ignored).</summary>
    private static readonly string[] CastBarWindows = ["CastBar", "TargetInfo", "FocusTarget", "AddonCastBar", "AddonTargetInfo", "AddonFocusTargetInfo"];

    /// <summary>The uses allowed today, and how many times a cast bar name is written there (a ratchet: never more, never elsewhere).</summary>
    private static readonly Dictionary<string, int> Allowed = new()
    {
        // 0.537: the cast bars are HUD, never a window in front of a panel: skipped, not read.
        ["src/TenshoAssistant/UI/GameWindow.cs"] = 1,
    };

    private static int Count(string text)
    {
        var count = 0;
        // Overlapping names (AddonCastBar holds CastBar) are counted once: the longest match at a position wins by skipping past it.
        for (var at = 0; at < text.Length; at++)
        {
            var hit = CastBarWindows.Where(token => string.Compare(text, at, token, 0, token.Length, StringComparison.OrdinalIgnoreCase) == 0).Select(token => token.Length).DefaultIfEmpty(0).Max();
            if (hit == 0) continue;
            count++;
            at += hit - 1;
        }
        return count;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dalamud rules cast bar 1579: no cast bar window is read, in the plugin nor the desktop application", () =>
        {
            var found = new List<string>();
            foreach (var (name, text) in Sources())
            {
                var count = Count(text);
                if (count == 0) continue;
                if (!Allowed.TryGetValue(name, out var allowed)) found.Add(name + ": " + count + " cast bar name(s) outside the allowed uses");
                else if (count > allowed) found.Add(name + ": " + count + " cast bar name(s), " + allowed + " allowed");
            }
            Check(found.Count == 0, "no boss cast alert nor cast timer from the cast bar windows (DEC-055): " + string.Join(", ", found));
            foreach (var (name, allowed) in Allowed)
                Check(Count(File.ReadAllText(Path.GetFullPath(name))) == allowed, name + ": the ratchet follows the code (lower it when a use goes)");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/GameWindow.cs")).Contains("public static bool Ignored(string name)=>NotWindows.Any("), "the allowed use only skips the cast bars");
        }),
        ("Dalamud rules cast bar 1579: the refusal finds a read of the cast bar windows", () =>
        {
            Check(Count("var bar = gameGui.GetAddonByName(\"_TargetInfoCastBar\");") > 0, "the target's cast bar is refused");
            Check(Count("addonLifecycle.RegisterListener(AddonEvent.PostDraw, \"_CastBar\", OnCast);") > 0, "the player's cast bar is refused");
            Check(Count("var focus = (AddonFocusTargetInfo*)gameGui.GetAddonByName(\"_FocusTargetInfo\");") > 0, "the focus target's cast bar is refused");
            Check(Count("foreach (var bar in names.Where(n => n.StartsWith(\"CastBarEnemy\")))") > 0, "the enemies' cast bars are refused");
            Check(Count("var gathering = gameGui.GetAddonByName(\"Gathering\");") == 0 && Count("ImGui.TextColored(Success, enemy.Weakness);") == 0, "an ordinary line of Tensho is not refused");
        }),
    ];
}
