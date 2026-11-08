// X-76 P5 (0.932): island animals on the map show their size and leavings, read from MJIAnimals.
internal static class IslandAnimals932Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Island animals 932: size and leavings from the game data on the map hover",() =>
        {
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Contains("islandAnimalInfo.TryAdd(animal.BNpcBase.RowId,(animal.Size,animal.Reward.Select("),"read");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Contains(":item.Kind==MapObjectKind.IslandAnimal ? IslandAnimalLabel(item):item.Name;"),"shown");
        }),
    ];
}
