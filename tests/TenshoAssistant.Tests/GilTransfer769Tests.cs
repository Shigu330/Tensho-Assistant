using TenshoAssistant.Inventory;

// 0.769 (X-19, user report): a deposit with a retainer is not spent gil.
internal static class GilTransfer769Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil transfer: the deposit of the report is a transfer on both sides; a rise beyond it is market sales",() =>
        {
            Check(GilJournal.SplitRetainerRise(55569,55569)==(55569,0,0),"the report: 55 569 deposited, 55 569 more with the retainers");
            Check(GilJournal.SplitRetainerRise(1200,500)==(500,700,0),"sold while deposited");
            Check(GilJournal.SplitRetainerRise(300,1000)==(300,0,700),"part of a deposit seen");
            Check(GilJournal.SplitRetainerRise(-400,1000)==(-400,0,1000),"a withdrawal");
            Check(GilJournal.IsTransfer(GilCategory.Retainer) && GilJournal.IsTransfer(GilCategory.Company) && !GilJournal.IsTransfer(GilCategory.Market),"which ones");
        }),
        ("Gil transfer: out of the net, and the open retainer read live",() =>
        {
            var page=Read("MainWindow.GilJournal.cs");
            Check(page.Contains("totals.Where(total=>!GilJournal.IsTransfer(total.Category))"),"net");
            var watcher=Read("Inventory/GilWatcher.Dalamud.cs");
            Check(watcher.Contains("InventoryType.RetainerGil") && watcher.Contains("GilJournal.SplitRetainerRise(delta,retainerDeposits)"),"live and split");
        }),
    ];
}
