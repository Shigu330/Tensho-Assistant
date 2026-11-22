using System.Text.RegularExpressions;

// X-800 (0.1808, the user's request of 22/11): the Crucible panel in three tabs (the 10 to bring, the team taken, the best team), each with
// its line; the best team among every beast of the game, the beasts not yet yours marked; the board's other spaces folded under the tabs.
internal static class BeastTabs1808Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static string Between(string text, string from, string to)
    {
        var start = text.IndexOf(from, StringComparison.Ordinal);
        Check(start >= 0, "missing: " + from);
        var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
        return end < 0 ? text[start..] : text[start..end];
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast tabs 1808: the whole board is three tabs through the shared bar, each with its line, the spaces folded under them", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            var whole = Between(page, "private void DrawBeastBoardWhole(", "private void DrawBeastSpaces(");
            Check(whole.Contains("OrderedTabs(\"BeastBoardTabs\",") && !whole.Contains("BeginTabBar"), "the shared tab bar (no bar by hand, no child window in an auto-sized panel)");
            var keys = Regex.Matches(whole, @"new\(""(\w+)"", localization\.Get\(""BeastUi\.Tab(\w+)""\)").Select(match => match.Groups[1].Value).ToArray();
            Check(keys.SequenceEqual(["Ten", "Current", "Best"]), "the 10 to bring first, then the team taken, then the best team: " + string.Join(",", keys));
            foreach (var key in new[] { "TabTenNote", "TabCurrentNote", "TabBestNote" }) Check(whole.Contains($"Note(\"BeastUi.{key}\");"), "each tab says what it shows: " + key);
            Check(whole.Contains("DrawBeastPlan(plan, board, catalog, state.Obtained is null, wrapAt, fights: false);"), "the 10: the plan's team only");
            Check(whole.Contains("if (team is not { Count: > 0 }) { EmptyState(\"BeastUi.TabCurrentNone\"); return; }") && whole.Contains("DrawBeastTeamRead(board, plan, team, catalog);"), "the team taken, or a line saying how to read it");
            Check(whole.Contains("(held is null ? plan : BeastBestPlanFor(board, state))") && whole.Contains("DrawBeastPlan(best, board, catalog, false, wrapAt, notHeldOf: held);"), "the best team, its beasts not held marked");
            Check(whole.IndexOf("OrderedTabs(", StringComparison.Ordinal) < whole.IndexOf("DrawBeastSpaces(board, plan, team, catalog);", StringComparison.Ordinal), "campsites, shops and coffers under the tabs, still folded (their own node)");
            Check(page.Contains("TreeNode(localization.Format(\"BeastUi.Spaces\", spaces.Count) + \"###BeastSpaces\")"), "the spaces keep their folding node");
            Check(Between(page, "internal void DrawBeastBoardPanel()", "internal Func<uint, string>? BeastActionName").Contains("foreach (var index in now) DrawBeastFightNow("), "the fights the game shows stay first (X-486)");
        }),
        ("Beast tabs 1808: the best team is worked out in the background among every beast, once per board and rules; not held beasts are marked", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            var best = Between(page, "private BoardPlan? BeastBestPlanFor(", "private void DrawBeastPlanLines(");
            Check(best.Contains("BeastTeams.Key(null, ranks, floors, BeastMarkOf(catalog))") && best.Contains("Task.Run(() => BeastTeams.Plan(board, catalog.Beasts, null, ranks, floors))"), "every beast (held: null), off the frame (R07)");
            Check(best.Contains("kept.Key != key") && best.Contains("kept.Plan.IsCompletedSuccessfully ? kept.Plan.Result : null"), "again only when the key changes; nothing shown before it is done");
            var lines = Between(page, "private void DrawBeastPlanLines(", "private (BoardPlan Plan, IReadOnlySet<uint>? Held");
            Check(lines.Contains("bool NotHeld(uint beast) => notHeldOf is not null && !notHeldOf.Contains(beast);"), "marked only when the beasts held are known");
            Check(lines.Contains("if (NotHeld(beast.Id)) { ImGui.SameLine(); ImGui.TextColored(Warning, localization.Get(\"BeastUi.NotHeld\")); }") && lines.Contains("localization.Format(\"BeastUi.BestNotHeld\""), "in the team's rows and under each fight");
            Check(lines.IndexOf("if (!fights) return;", StringComparison.Ordinal) < lines.IndexOf("foreach (var battle in plan.Battles)", StringComparison.Ordinal), "the 10 tab: no fights");
            Check(!page.Contains("SendAction") && !page.Contains("UseAction"), "display only, nothing sent to the game (R03.3)");
        }),
        ("Beast tabs 1808: the tabs' texts in English and French", () =>
        {
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                var section = Between(text, "\n  \"BeastUi\": {", "\n  },");
                foreach (var key in new[] { "TabTen", "TabTenNote", "TabCurrent", "TabCurrentNote", "TabCurrentNone", "TabBest", "TabBestNote", "NotHeld", "BestNotHeld" })
                    Check(section.Contains($"\n    \"{key}\": \""), $"{language}: BeastUi.{key}");
            }
            Check(Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("\"TabTen\": \"Les 10 à emmener\""), "the user's words");
        }),
    ];
}
