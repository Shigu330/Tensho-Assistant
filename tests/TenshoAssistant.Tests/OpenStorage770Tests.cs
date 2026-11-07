// 0.770 (X-20, user report): what is put in an open storage shows without reopening it.
internal static class OpenStorage770Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Open storage: read again every 2 seconds while open, the character's own every 2 minutes as before",() =>
        {
            var bridge=Read("Inventory/BankInventoryBridge.Dalamud.cs");
            Check(bridge.Contains("OpenStorageRefresh = TimeSpan.FromSeconds(2)") && bridge.Contains("(always ? TimeSpan.FromMinutes(2) : OpenStorageRefresh)"),"interval");
            Check(!bridge.Contains("TimeSpan.FromSeconds(30))))"),"no longer 30 s");
        }),
    ];
}
