using System.Text.RegularExpressions;
using TenshoAssistant.UI;

// 0.1696 (X-684, last part; M3 of the review X-700): each part of Tensho's own tooltip bodies (place, quest, monster, action, FATE, NPC, duty,
// recipe, player, gathering point) can be turned off in Settings › General › Other tooltips, as the item panel's parts; one table of parts
// (UI/TooltipParts.All) for the settings, the bodies and this test.
internal static class TooltipParts1696Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Tooltip parts 1696: shown by default; turned off and on again; each name once; the list replaced, never changed in place", () =>
        {
            Check(TooltipParts.Shown(null, "Place.Weather") && TooltipParts.Shown([], "Place.Weather"), "nothing turned off: all shown");
            var before = new List<string> { "Npc.Shops" };
            var off = TooltipParts.With(before, "Place.Weather", false);
            Check(!TooltipParts.Shown(off, "Place.Weather") && !TooltipParts.Shown(off, "Npc.Shops") && TooltipParts.Shown(off, "Place.Region"), "off");
            Check(before.Count == 1 && !ReferenceEquals(before, off), "the former list is left as it was (the caches compare by reference)");
            Check(TooltipParts.With(off, "Place.Weather", false).Count(name => name == "Place.Weather") == 1, "each name once");
            Check(TooltipParts.Shown(TooltipParts.With(off, "Place.Weather", true), "Place.Weather"), "on again");
        }),
        ("Tooltip parts 1696: every part has its setting, its label in both languages, and is read by its body; no part twice", () =>
        {
            var names = TooltipParts.Names.ToList();
            Check(names.Count == names.Distinct().Count() && names.Count >= 30, "parts: " + names.Count);
            var settings = Read("MainWindow.TooltipParts.cs");
            var bodies = string.Join("\n", new[] { "PlaceTooltip", "MonsterTooltip", "ActionTooltip", "FateTooltip", "NpcTooltip", "DutyTooltip", "RecipeTooltip", "PlayerTooltip", "GatheringTooltip" }
                .Select(file => Read("MainWindow." + file + ".cs")).Append(Read("MainWindow.Quests.cs")));
            var en = Read("Localization/Languages/en.json");
            var fr = Read("Localization/Languages/fr.json");
            foreach (var name in names)
            {
                var key = "Tip" + name.Replace(".", "");
                Check(settings.Contains($"BoolSetting(\"Settings.{key}\",\"Settings.TipPartTip\",()=>TipPart(\"{name}\"),value=>SetTipPart(\"{name}\",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,\"{name}\"));", StringComparison.Ordinal), name + ": its setting");
                Check(en.Contains($"\"{key}\": \"", StringComparison.Ordinal) && fr.Contains($"\"{key}\": \"", StringComparison.Ordinal), name + ": its label");
                Check(bodies.Contains($"TipPart(\"{name}\")", StringComparison.Ordinal), name + ": read by its body");
            }
            foreach (var (kind, _) in TooltipParts.All)
                Check(en.Contains($"\"TipKind{kind}\": \"", StringComparison.Ordinal) && fr.Contains($"\"TipKind{kind}\": \"", StringComparison.Ordinal), kind + ": its heading");
            var used = Regex.Matches(bodies + settings, "TipPart\\(\"(\\w+\\.\\w+)\"\\)").Select(match => match.Groups[1].Value).Distinct();
            Check(used.All(names.Contains), "no part read that the table does not name: " + string.Join(", ", used.Where(name => !names.Contains(name))));
        }),
        ("Tooltip parts 1696: wiring: the category, the saved setting, the caches built again on a change", () =>
        {
            Check(Read("MainWindow.SettingsCategories.cs").Contains("new(\"General\",\"OtherTooltips\",DrawTooltipPartSettings),", StringComparison.Ordinal), "the category");
            var configuration = Read("Configuration.cs");
            Check(configuration.Contains("public List<string> TooltipPartsHidden { get; set; } = [];", StringComparison.Ordinal) && configuration.Contains("\"TooltipPartsHidden\",", StringComparison.Ordinal), "saved, reset with the settings");
            Check(Read("MainWindow.RecipeTooltip.cs").Contains("ReferenceEquals(cached.Parts,configuration.TooltipPartsHidden)", StringComparison.Ordinal), "the recipe's cache");
            Check(Read("MainWindow.PlayerTooltip.cs").Contains("!ReferenceEquals(partyTipsParts,configuration.TooltipPartsHidden)", StringComparison.Ordinal), "the party's cache");
            Check(Read("MainWindow.TooltipParts.cs").Contains("configuration.TooltipPartsHidden=TooltipParts.With(", StringComparison.Ordinal), "replaced whole");
        }),
    ];
}
