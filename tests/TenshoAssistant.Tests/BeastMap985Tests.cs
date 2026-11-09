// X-110 (0.985): the beasts not tamed yet, at the places seen in game, on the Tensho map, with the beast layer.
internal static class BeastMap985Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Beast map 985: untamed only, this map only, beast layer and Beastmaster (or always), click = flag",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("if(tamedAlready.Contains(wild.Id))continue;") && map.Contains("if(place.MapId!=map.MapId)continue;"),"filters");
            Check(map.Contains("settings.ShowBeastMonsters && (settings.BeastMonstersAlways || characterStats.State.JobId==BeastmasterJob)"),"layer");
            Check(map.Contains("FlagFromMap(map,place.X,place.Y);"),"flag");
        }),
    ];
}
