// 0.876 (X-75.2): duty chests, the item menu and deep dungeon coffers read what was seen in game first.
internal static class SeenFirst876Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
