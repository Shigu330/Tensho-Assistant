// 0.370 (g, X-02): the music playing (memory only, no function called), its roll by file path, favourite rolls kept on reset.
internal static class OrchestrionPlayer370Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Orchestrion player: BGM read in memory only, roll matched by file path, favourites kept",() =>
        {
            var reader = Read("Collections/BgmReader.Dalamud.cs");
            Check(reader.Contains("system->Scenes[index].PlayingBgmId",StringComparison.Ordinal) && !reader.Contains("SetBGM",StringComparison.Ordinal)
                && !reader.Contains("ResetBGM",StringComparison.Ordinal),"read only, the music is never changed");
            Check(Read("Plugin.cs").Contains("OrchestrionPath>()",StringComparison.Ordinal) && Read("Plugin.cs").Contains("()=>bgm?.Dispose()",StringComparison.Ordinal),"roll by path; released");
            Check(Read("Configuration.cs").Contains("\"OrchestrionFavorites\",",StringComparison.Ordinal),"favourites kept on reset");
        }),
    ];
}
