// N-09 (0.441): living hunt marks on the Tensho map, by rank.
internal static class HuntMarks441Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunt marks: NotoriousMonster ranks 1-3, living and targetable only, own layer and colours",() =>
        {
            var reader=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("mark.Rank is >= 1 and <= 3)huntMarks.TryAdd(mark.BNpcBase.RowId,mark.Rank)"),"ranks from the sheet");
            Check(reader.Contains("IBattleNpc { CurrentHp:>0 }=>MapObjectKind.HuntMark") && reader.Contains("if(kind is null || !item.IsTargetable)continue;"),"alive, targetable");
            var view=Read("MainWindow.MapView.cs");
            Check(view.Contains("MapObjectKind.HuntMark=>!settings.ShowHuntMarks") && view.Contains("MapView.HuntMarkLabel"),"layer setting and label");
            Check(Read("Configuration.cs").Contains("public bool ShowHuntMarks { get; set; } = true;"),"on by default");
        }),
    ];
}
