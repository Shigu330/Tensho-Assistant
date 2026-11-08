// X-87 (0.968): an accepted leve's marker is dropped once that leve is under way (and any marker the game does not draw); leve targets get the leve icon.
internal static class Leves968Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Leves 968: ShouldRender and active objective filter, active leve read, targets drawn with its icon",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("if(info->ShouldRender && (active==0 || info->ObjectiveId!=active))From(info);"),"filter");
            Check(reader.Contains("{ActiveLeve=ReadActiveLeve()}"),"active leve");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("(monster.EventId & 0xFFFF)!=leve.Objective") && map.Contains("\"MapView.LeveTarget\""),"targets");
        }),
    ];
}
