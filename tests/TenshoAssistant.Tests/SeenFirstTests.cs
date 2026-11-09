// Test audit of 2026-11-09, batch 2 (0.1021): SeenSources865Tests, SeenFirst875Tests, SeenFirst876Tests merged into SeenFirstTests; every case is kept.
internal static class SeenFirstTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From SeenSources865Tests: 0.865 (X-74): the sources seen in game first in the item's « How to get it ».
        ("Seen sources: first, counted as known (no fallback asked), cached by revision",() =>
        {
            var hover=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketHover.cs"));
            Check(hover.Contains("foreach(var seen in DiscoveredSourcesOf(id)){known++;"),"first, known");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.Contains("if(discoveredSources.Item==item && discoveredSources.Revision==store.Revision)return discoveredSources.Lines;"),"cached");
        }),
        // From SeenFirst875Tests: 0.875 (X-75.2): what was learned in game comes first on the pages that still read community data.
        ("Seen first: monsters page, nearby drops and beasts, duties, source items and what an item gives read the discoveries before the community lists",() =>
        {
            Check(Read("MainWindow.Discoveries.cs").Contains("if(seenIndex.Revision==store.Revision)return seenIndex.Index;"),"index once per change");
            var monsters=Read("MainWindow.Monsters.cs");
            Check(monsters.IndexOf("###SeenMonsters",StringComparison.Ordinal) is > 0 and var seen && seen<monsters.IndexOf("###LuminaMonsters",StringComparison.Ordinal),"monsters page: seen first");
            var nearby=Read("MainWindow.NearbyMonsters.cs");
            Check(nearby.Contains("(seenDrops?.GetValueOrDefault(id) ?? []).Concat(data.ItemsDroppedBy(id))") && nearby.Contains("foreach(var (monster,list) in seenBeasts"),"nearby: drops and beasts");
            var links=Read("MainWindow.SourceLinks.cs");
            Check(links.Contains("(Seen()?.DutiesByItem.GetValueOrDefault(item.Id) ?? []).Concat(LuminaDutiesOf(item.Id))") && links.Contains("SourcesByItem.GetValueOrDefault(item.Id)"),"sources");
            var uses=Read("MainWindow.ItemUses.cs");
            Check(uses.IndexOf("###UseSeenGives",StringComparison.Ordinal) is > 0 and var gives && gives<uses.IndexOf("var given=data.GivesOf(id);",StringComparison.Ordinal),"uses: seen first");
        }),
        // From SeenFirst876Tests: 0.876 (X-75.2): duty chests, the item menu and deep dungeon coffers read what was seen in game first.
        ("Seen first: duty loot lists, the item menu routes and deep dungeon coffers",() =>
        {
            var lumina=Read("MainWindow.Lumina.cs");
            Check(lumina.Contains("cached.Seen==seenRevision") && lumina.Contains("new DutyLootEntry(item,DutyLootSourceKind.Seen,0,\"\")"),"duty lists, cache follows the base");
            var menu=Read("MainWindow.ItemContextMenu.cs");
            Check(menu.Contains("(Seen()?.DutiesByItem.GetValueOrDefault(itemId) ?? []).Concat(LuminaDutiesOf(itemId))") && menu.Contains("Seen()?.MonstersByItem.ContainsKey(itemId)==true"),"menu routes");
            var deep=Read("MainWindow.DeepDungeons.cs");
            Check(deep.IndexOf("###DeepDungeonSeen",StringComparison.Ordinal) is > 0 and var seen && seen<deep.IndexOf("DeepDungeon.LootOff",StringComparison.Ordinal),"deep dungeons: before the switch");
            Check(Enum.IsDefined(typeof(TenshoAssistant.Duties.DutyLootSourceKind),3),"Seen kind");
        }),
    ];
}
