// 0.901 (X-83): the island's wild animals on the map, from MJIAnimals (BNpcBase → icon).
using TenshoAssistant;
internal static class IslandAnimals901Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Island animals: MJIAnimals by BNpcBase, living ones only, their own icon, a setting on by default",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("if(animal.BNpcBase.RowId!=0 && animal.Icon>0)") && reader.Contains("islandAnimals.TryAdd(animal.BNpcBase.RowId,(uint)animal.Icon);"),"sheet");
            Check(reader.Contains("BattleNpc when islandAnimals.ContainsKey(item.BaseId)"),"by base");
            Check(new MapViewSettings().ShowIslandAnimals,"default on");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("MapObjectKind.IslandAnimal=>!settings.ShowIslandAnimals"),"setting used");
        }),
    ];
}
