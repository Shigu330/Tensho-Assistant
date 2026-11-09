// X-40 (0.1002, DEC-049): three more uses of the nearby monsters: every crafting list, the relic book's targets, the wishes' drops.
internal static class MonsterUses1002Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Monster uses 1002: new kinds with their layer, icon above and glyph; on by default; all-lists inside the list layer",() =>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.ListMonstersAllLists && settings.ShowRelicMonsters && settings.AboveRelic && settings.ShowWishMonsters && settings.AboveWish,"defaults");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("MonsterNoteKind.Relic=>settings.ShowRelicMonsters,") && map.Contains("MonsterNoteKind.Wish=>settings.ShowWishMonsters,"),"layers");
            var nearby=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.NearbyMonsters.cs"));
            Check(nearby.Contains("if(configuration.MapView.ListMonstersAllLists)needed.UnionWith(AllCraftListNeeds());"),"all lists");
            Check(nearby.Contains("NearbyMonsters.ListDrops(id,Drops,wishes)") && nearby.Contains("relicNeeds.TryGetValue(monster.Name.Trim(),out var left)"),"wishes, relic");
        }),
    ];
}
