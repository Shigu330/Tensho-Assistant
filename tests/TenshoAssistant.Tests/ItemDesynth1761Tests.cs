using TenshoAssistant.Items;

// X-197 (the user's request of 12/11/2026, K-06; « on a déjà désynthèse et les paquets de cartes »): two lines more under the game's item
// tooltip, each turned off in the tooltip settings: the item's desynthesis (crafter, item level, your level read in the game, above or below;
// unknown when not read) and what a coffer, a card pack or a container holds that a collection tracks (all obtained, or how many missing).
internal static class ItemDesynth1761Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Item desynth 1761: behaviour: an item that can be desynthesized, its level above, at or below yours; unknown when your level is not read", () =>
        {
            Check(ItemDesynth.Of(1, 8, 560) == new DesynthInfo(8, 560) && ItemDesynth.Of(0, 8, 560) is null && ItemDesynth.Of(1, 0, 560) is null
                && ItemDesynth.Of(1, 16, 560) is null && ItemDesynth.Of(1, 15, 0) is null, "Item.Desynth, a crafter, an item level");
            Check(ItemDesynth.Standing(560, 523.45f) == DesynthStanding.Above && ItemDesynth.Standing(523, 523.45f) == DesynthStanding.Same
                && ItemDesynth.Standing(500, 523.45f) == DesynthStanding.Below && ItemDesynth.Standing(560, null) == DesynthStanding.Unknown, "above, same (whole part), below, unknown");
            Check(ItemDesynth.Level(523.45f) == 523.45f && ItemDesynth.Level(0) is null && ItemDesynth.Level(-1) is null && ItemDesynth.Level(1500) is null
                && ItemDesynth.Level(float.NaN) is null && ItemDesynth.Level(float.PositiveInfinity) is null, "a level read outside 0 to 1000 is unknown, never a level made up");
        }),
        ("Item desynth 1761: behaviour: a coffer: all obtained, or how many missing among what a collection tracks; other items left out; a cut list said", () =>
        {
            var owned = new Dictionary<uint, bool?> { [1] = true, [2] = false, [3] = null, [4] = true, [5] = false };
            bool? Acquired(uint item) => owned.GetValueOrDefault(item);
            var coffer = ItemDesynth.Coffer([(7, 4, new uint[] { 1, 2, 3, 4 })], Acquired);
            Check(coffer == new CofferStanding(3, 1, 4, false), "a mount owned, a minion missing, gear left out: " + coffer);
            Check(ItemDesynth.Coffer([(7, 2, new uint[] { 1, 4 })], Acquired) is { Missing: 0, Collectibles: 2 }, "all obtained");
            Check(ItemDesynth.Coffer([(7, 1, new uint[] { 3 })], Acquired) is null && ItemDesynth.Coffer([], Acquired) is null, "nothing a collection tracks: no line");
            Check(ItemDesynth.Coffer([(1, 2, new uint[] { 2, 5 })], Acquired) is null, "what its desynthesis gives is not what it holds");
            var packs = ItemDesynth.Coffer([(6, 30, new uint[] { 2, 5 }), (3, 1, new uint[] { 2 })], Acquired);
            Check(packs == new CofferStanding(2, 2, 2, true), "a card pack whose list was cut (30 for 2 given), an item listed twice counted once: " + packs);
            Check(ItemDesynth.Holds(3) && ItemDesynth.Holds(6) && ItemDesynth.Holds(7) && !ItemDesynth.Holds(1) && !ItemDesynth.Holds(2) && !ItemDesynth.Holds(8), "the kinds");
        }),
        ("Item desynth 1761: wiring: the lines in the item panel, each turned off by its own setting (on by default, kept out of the saved data); your level read on the framework thread; both languages", () =>
        {
            var hover = Read("src/TenshoAssistant/MainWindow.MarketHover.cs");
            Check(hover.Contains("if(!hoverPanel || configuration.HoverShowDesynth)DrawItemDesynth(id);", StringComparison.Ordinal)
                && hover.Contains("if(!hoverPanel || configuration.HoverShowCoffer)MeasureMap(\"hover coffer\",()=>DrawItemCoffer(id));", StringComparison.Ordinal), "the two lines, each with its setting");
            Check(hover.Contains("configuration.HoverShowDesynth && DesynthOf(id) is not null || configuration.HoverShowCoffer && CofferOf(id) is not null", StringComparison.Ordinal), "the panel shows when only they have something");
            var settings = Read("src/TenshoAssistant/MainWindow.Settings.cs");
            Check(settings.Contains("BoolSetting(\"Settings.HoverDesynth\",\"Settings.HoverDesynthTip\",()=>configuration.HoverShowDesynth", StringComparison.Ordinal)
                && settings.Contains("BoolSetting(\"Settings.HoverCoffer\",\"Settings.HoverCofferTip\",()=>configuration.HoverShowCoffer", StringComparison.Ordinal), "the settings");
            var config = new TenshoAssistant.Configuration();
            Check(config.HoverShowDesynth && config.HoverShowCoffer && TenshoAssistant.ConfigurationReset.Reset.Contains("HoverShowDesynth") && TenshoAssistant.ConfigurationReset.Reset.Contains("HoverShowCoffer"), "on by default, settings (reset by « reset all »)");
            var page = Read("src/TenshoAssistant/MainWindow.ItemDesynth.cs");
            Check(page.Contains("if(desynthCache.TryGetValue(id,out var known))return known;", StringComparison.Ordinal) && page.Contains("now-known.At<5000", StringComparison.Ordinal), "once per item; the coffer kept 5 s");
            var levels = Read("src/TenshoAssistant/Items/DesynthesisLevels.Dalamud.cs");
            Check(levels.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);", StringComparison.Ordinal) && levels.Contains("!state->IsLoaded", StringComparison.Ordinal)
                && levels.Contains("ItemDesynth.Level(state->GetDesynthesisLevel(job))", StringComparison.Ordinal), "read on the framework thread, the player loaded, checked");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("Items.ItemDesynth.Of(desynthItem.Desynth,desynthItem.ClassJobRepair.RowId,desynthItem.LevelItem.RowId)", StringComparison.Ordinal) && plugin.Contains("()=>desynthLevels?.Dispose()", StringComparison.Ordinal), "the Item sheet; disposed");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(new[] { "Desynth", "DesynthUnknown", "DesynthAbove", "DesynthSame", "DesynthBelow", "CofferAll", "CofferMissing", "CofferPartial", "HoverDesynth", "HoverDesynthTip", "HoverCoffer", "HoverCofferTip" }
                    .All(key => text.Contains($"\"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            }
        }),
    ];
}
