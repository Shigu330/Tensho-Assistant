using TenshoAssistant.Inventory;

// 0.605: an unchanged storage is not rewritten at each automatic reading.
internal static class BankSave605Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,5,12,0,0,TimeSpan.Zero);
    private static BankEntry Entry(long quantity,DateTimeOffset at,string name="Bags",InventoryCoverage coverage=InventoryCoverage.Complete)
        =>new(new(7,BankStorageKind.Retainer,9),name,new InventorySnapshot(at,"test",[new InventoryContainerSnapshot(new(new(9),new(10000)),coverage,[new(new(new(5),false),quantity,0,0)])]));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Bank save: same contents under an hour after the last write: no write",() =>
        {
            var saved=new Dictionary<BankKey,DateTimeOffset>{[Entry(1,At).Key]=At};
            Check(BankEntryCompare.CanSkipSave(Entry(1,At),Entry(1,At.AddMinutes(2)),saved),"unchanged");
            Check(!BankEntryCompare.CanSkipSave(Entry(1,At),Entry(2,At.AddMinutes(2)),saved),"a quantity changed");
            Check(!BankEntryCompare.CanSkipSave(Entry(1,At),Entry(1,At.AddMinutes(2),"Other"),saved),"a name changed");
            Check(!BankEntryCompare.CanSkipSave(Entry(1,At),Entry(1,At.AddMinutes(2),coverage:InventoryCoverage.Partial),saved),"coverage changed");
            Check(!BankEntryCompare.CanSkipSave(Entry(1,At),Entry(1,At.AddHours(1)),saved),"an hour later: the date is written");
        }),
        ("Bank save: always written the first time, when none is known, and after a failed save",() =>
        {
            Check(!BankEntryCompare.CanSkipSave(Entry(1,At),Entry(1,At.AddMinutes(2)),new Dictionary<BankKey,DateTimeOffset>()),"not written in this session");
            Check(!BankEntryCompare.CanSkipSave(null,Entry(1,At),new Dictionary<BankKey,DateTimeOffset>{[Entry(1,At).Key]=At}),"erased since");
            var service=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/BankService.cs"));
            Check(service.Contains("next = store.Apply(change, Array.AsReadOnly(memory)); if (!unchanged && request.Entry is { } saved) savedAt[saved.Key]"),"remembered only after the write succeeded");
        }),
    ];
}
