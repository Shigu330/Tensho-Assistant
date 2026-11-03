using System.Net;
using TenshoAssistant;
using TenshoAssistant.Inventory;
using TenshoAssistant.Market;

// 0.329 (queue step e: items, bank, sorting and market audit). A cached armoury never makes a total « exact »; the 3 s Universalis
// pause neither wipes a shown price nor turns the hover into an error; the R-14 and bank fixes are pinned in the source.
internal static class Items329Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));
    private static HttpClient Counting(Action count) => new(new MarketTestHandler((_,_) =>
    {
        count();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"results\":[{\"itemId\":42}]}") });
    }));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Market pause: a forced refresh within 3 s keeps the price shown; a hover then waits without an error",() =>
        {
            var calls = 0; using var http = Counting(() => calls++);
            using var market = new MarketService(http); market.SetWorld(1,71,"World");
            market.RequestAsync(42).GetAwaiter().GetResult();
            market.RequestAsync(42,force: true).GetAwaiter().GetResult();
            Check(calls == 1 && market.State.Quote?.ItemId == 42 && !market.State.Loading,"price kept during the pause: " + market.State.Error);
            var hover = market.RequestHoverAsync(43,1,71).GetAwaiter().GetResult();
            Check(calls == 1 && hover.Status == MarketFetchStatus.Cooldown && hover.RetryAfter > TimeSpan.Zero && market.BackgroundState?.Error is null,"hover waits, no error: " + hover.Status);
        }),
        ("Bank: R-14 keeps the character signed in, armoury and dresser recorded whatever the setting, big dressers saved",() =>
        {
            var bridge = Read("Inventory/BankInventoryBridge.Dalamud.cs");
            Check(bridge.Contains("failedReads.Count > 0",StringComparison.Ordinal) && Read("MainWindow.Bank.cs").Contains("menus.ReadsUnavailable",StringComparison.Ordinal),"one unreadable storage stays said (a lasting flag, not a message an import overwrites)");
            Check(!bridge.Contains("new(0, null, null, null, error.Message)",StringComparison.Ordinal),"a failed read no longer signs the character out");
            Check(Read("Inventory/InventoryNativeReader.Dalamud.cs").Contains("catch",StringComparison.Ordinal),"visual order read guarded");
            Check(Read("Inventory/BankFileStore.cs").Contains("MaximumContainerSlots",StringComparison.Ordinal),"one container bound for save and load");
        }),
    ];
}
