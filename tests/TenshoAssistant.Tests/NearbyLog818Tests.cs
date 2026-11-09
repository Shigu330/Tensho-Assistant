// 0.818 (X-53): the nearby monsters' chain written to the log in developer mode, to find why a list's monster is not marked.
internal static class NearbyLog818Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Nearby monsters log: developer mode only, written when it changes",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.NearbyMonsters.cs"));
            Check(page.Contains("if(!developerMode || line==nearbyMonstersLogged)return;"),"developer mode, changes only");
            Check(page.Contains("list needs {needed.Count} items") && page.Contains("drop data {(lumina?.State.Data is null"),"what each step sees");
        }),
    ];
}
