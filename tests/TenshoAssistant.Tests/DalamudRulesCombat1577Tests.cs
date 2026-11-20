// 0.1577 (X-630, review of 0.1573 to 0.1575, M1): the refusal of a parser or a boss timer (DalamudRulesCombat1575Tests, DEC-055) widened.
// The damage fly text (IFlyTextGui, FlyText…) is refused everywhere; the enemy cast reading (IsCasting, CastActionId…) is allowed only where it
// is used today, the discovery of the monsters that cast a blue spell (a fact for the shared discoveries, not an alert nor a timer), with a
// ratchet on its count. The desktop application's sources are searched too.
internal static class DalamudRulesCombat1577Tests
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

    /// <summary>The damage numbers the game shows over the actors: what a counter of the damage would read.</summary>
    private static readonly string[] FlyText = ["IFlyTextGui", "FlyText", "AddonFlyText", "FlyTextKind"];

    /// <summary>An enemy's cast: what an alert of a boss's cast or a cast timer would read.</summary>
    private static readonly string[] Cast = ["IsCasting", "CastActionId", "CastActionType", "CastTargetObjectId", "IsCastInterruptible", "CurrentCastTime", "TotalCastTime", "BaseCastTime", "GetCastInfo"];

    /// <summary>The uses allowed today, and how many times each token is written there (a ratchet: never more, never elsewhere).</summary>
    private static readonly Dictionary<string, int> CastAllowed = new()
    {
        // X-75.4 (0.878): monsters casting a blue spell, noted once per cast for the shared discoveries.
        ["src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"] = 2,
    };

    private static int Count(string text, string token)
    {
        var count = 0;
        for (var at = text.IndexOf(token, StringComparison.Ordinal); at >= 0; at = text.IndexOf(token, at + token.Length, StringComparison.Ordinal)) count++;
        return count;
    }

    private static bool Refused(string line) => FlyText.Concat(Cast).Any(token => line.Contains(token, StringComparison.Ordinal));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dalamud rules combat 1577: no damage fly text read, in the plugin nor the desktop application", () =>
        {
            var found = Sources().SelectMany(source => FlyText.Where(token => source.Text.Contains(token, StringComparison.OrdinalIgnoreCase)).Select(token => source.Name + ": " + token)).ToList();
            Check(found.Count == 0, "no damage counter from the fly text (DEC-055): " + string.Join(", ", found));
        }),
        ("Dalamud rules combat 1577: an enemy's cast is read only where it is allowed today, never more", () =>
        {
            var found = new List<string>();
            foreach (var (name, text) in Sources())
            {
                var count = Cast.Sum(token => Count(text, token));
                if (count == 0) continue;
                if (!CastAllowed.TryGetValue(name, out var allowed)) found.Add(name + ": " + count + " cast read(s) outside the allowed uses");
                else if (count > allowed) found.Add(name + ": " + count + " cast read(s), " + allowed + " allowed");
            }
            Check(found.Count == 0, "no boss cast alert nor cast timer (DEC-055); a new allowed use goes in CastAllowed with its reason: " + string.Join(", ", found));
            foreach (var (name, allowed) in CastAllowed)
            {
                var text = File.ReadAllText(Path.GetFullPath(name));
                Check(Cast.Sum(token => Count(text, token)) == allowed, name + ": the ratchet follows the code (lower it when a use goes)");
            }
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs")).Contains("if(spellOf(action) is not (> 0 and var spell))continue;"), "the allowed use only keeps blue spells");
        }),
        ("Dalamud rules combat 1577: the widened refusal finds a cast alert and a fly text counter", () =>
        {
            Check(Refused("if (boss.IsCasting && boss.CastActionId == 31234) ShowAlert(\"Stack!\");"), "a boss cast alert is refused");
            Check(Refused("var left = boss.TotalCastTime - boss.CurrentCastTime;"), "a cast timer is refused");
            Check(Refused("flyTextGui.FlyTextCreated += (ref FlyTextKind kind, ref int actor, ref int val1, ref int val2) => total += val1;"), "a damage counter from the fly text is refused");
            Check(!Refused("ImGui.TextColored(Success, enemy.Weakness);") && !Refused("var spell = spellOf(action);"), "an ordinary line of Tensho is not refused");
        }),
    ];
}
