// 0.875 (X-75.2): what was learned in game comes first on the pages that still read community data.
internal static class SeenFirst875Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
