using System.Text.RegularExpressions;
using TenshoAssistant.Reminders;

// X-410 (0.1378, review of 0.1365 to 0.1374): M1, a bed's menu closed without anything then a gain line within the 4 s is no harvest
// when the line names only other items; M3, the Duty progress filters agree with « mission » (feminine), « l'outil de mission » in lower case.
internal static class GardenMenu1378Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
    private const string Here = "Shirogane 2-17";
    // Garden data fixture: seed 7715 gives crop 7897 (shape of Reminders/Data/garden-seeds.csv: seed, harvest).
    private static bool Garden(uint id) => id is 7715 or 7897;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Garden menu 1378: menu closed without anything, then a gain of another item within 4 s is no harvest", () =>
        {
            var t = new DateTimeOffset(2026, 11, 15, 2, 0, 0, TimeSpan.Zero);
            var menu = new GardenMenuMemory();
            bool Harvest(DateTimeOffset at, params uint[] items) =>
                GardenChat.IsHarvest(62, "", 0, menu.Peek(Here, at, GardenMenuMemory.HarvestWindow) is not null, GardenChat.GardenItem(items, Garden));
            menu.Seen(1, 3, Here, t);
            foreach (var after in new[] { 1.6, 2.5, 3.9 })
                Check(!Harvest(t.AddSeconds(after), 4850), "a purchase " + after + " s after a menu closed: no harvest");
            Check(Harvest(t.AddSeconds(1.5), 7897), "the crop 1.5 s after the menu: harvest");
            Check(Harvest(t.AddSeconds(3.9), 7715, 4850), "a seed among the items: harvest");
            Check(Harvest(t.AddSeconds(2), []), "a line naming no item keeps the menu rule");
            Check(!Harvest(t.AddSeconds(5), 7897), "beyond the 4 s: no harvest");
            Check(GardenChat.IsHarvest(62, "", GardenChat.BedObject, false, false), "the bed targeted: harvest whatever the item");
            Check(GardenChat.GardenItem([], Garden) is null && GardenChat.GardenItem([4850u], Garden) == false && GardenChat.GardenItem([4850u, 7897u], Garden) == true, "GardenItem");
        }),
        ("Garden menu 1378: the watcher reads the line's items and the plugin gives seeds and crops", () =>
        {
            var watcher = Read("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs");
            Check(watcher.Contains("GardenChat.GardenItem(message.Message.Payloads.OfType<Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload>()", StringComparison.Ordinal), "the line's items");
            Check(watcher.Contains("menuMemory.Peek(place,at,GardenMenuMemory.HarvestWindow) is not null,gardenItem)", StringComparison.Ordinal), "given to IsHarvest");
            var plugin = Read("src/TenshoAssistant/Plugin.Reminders.cs");
            Check(plugin.Contains("isGardenItem:id=>mainWindow.GardenSeedCatalog?.Invoke().Any(row=>row.Item1.SeedId==id || row.Item1.HarvestId==id)==true", StringComparison.Ordinal), "seeds and crops");
        }),
        ("Garden menu 1378: the Duty progress filters are feminine, « l'outil de mission » in lower case", () =>
        {
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            var start = Regex.Match(fr, "^  \"DutyProgress\": \\{", RegexOptions.Multiline);
            Check(start.Success, "section");
            var section = fr[start.Index..(start.Index + Regex.Match(fr[start.Index..], "^  \\}", RegexOptions.Multiline).Index)];
            foreach (var masculine in new[] { "\"Tous\"", "terminés", "Débloqués", "Terminés", " lus\"" })
                Check(!section.Contains(masculine, StringComparison.Ordinal), "DutyProgress: " + masculine);
            Check(!fr.Contains("l'Outil de mission", StringComparison.Ordinal), "l'outil de mission");
        }),
    ];
}
