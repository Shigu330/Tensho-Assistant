// 0.797 (X-37, user request): nearby monsters of a running FATE; the leve's targets captured first.
internal static class FateMonsters797Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("FATE monsters: read from the game object, told per monster, behind their map setting",() =>
        {
            Check(Read("Maps/MapLiveReader.Dalamud.cs").Contains("native==null ? 0u:native->FateId,native==null ? 0u:native->EventId.Id"),"read");
            var notes=Read("MainWindow.NearbyMonsters.cs");
            Check(notes.Contains("if(monster.FateId==0 || !configuration.MapView.ShowFateMonsters)return byName;"),"per monster, setting");
            Check(Read("MainWindow.MapView.cs").Contains("MonsterNoteKind.Fate=>settings.ShowFateMonsters,"),"map filter");
        }),
        ("Leve targets: captured in developer mode before anything is told",() =>
        {
            var capture=Read("Diagnostics/ReadingCapture.Dalamud.cs");
            Check(capture.Contains("private void ReadMonsterEvents()") && capture.Contains("Line(\"monsterEvents\",60,now);"),"captured, limited");
        }),
    ];
}
