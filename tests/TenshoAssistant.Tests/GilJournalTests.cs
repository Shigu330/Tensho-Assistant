// Test audit of 2026-11-09, batch 2 (0.1021): GilBar986Tests, GilJournal351Tests, GilJournal768Tests, GilLoad993Tests, GilMail903Tests, GilTeleport775Tests, GilTransfer769Tests merged into GilJournalTests; every case is kept.
using TenshoAssistant.Inventory;
internal static class GilJournalTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From GilBar986Tests: X-107 (0.986): the character's gil of the day, transfers left out, in the server bar when chosen.
        ("Gil bar 986: since local midnight, character only, transfers out; off by default",() =>
        {
            var now=DateTimeOffset.Now;var midnight=new DateTimeOffset(now.Date,now.Offset);
            var entries=new[]{
                new GilEntry(midnight.AddMinutes(5),1000,0,GilSource.Character,GilCategory.Market),
                new GilEntry(midnight.AddMinutes(6),-300,0,GilSource.Character,GilCategory.Teleport),
                new GilEntry(midnight.AddMinutes(7),5000,0,GilSource.Character,GilCategory.Retainer),
                new GilEntry(midnight.AddMinutes(-5),777,0,GilSource.Character,GilCategory.Market),
                new GilEntry(midnight.AddMinutes(8),900,0,GilSource.Retainers,GilCategory.Market)};
            Check(GilJournal.Today(entries,now)==(1000,300),"today");
            Check(!new TenshoAssistant.Configuration().GilServerBar,"off by default");
        }),
        // From GilJournal351Tests: 0.351 (g, I-04): gil journal — category from the last game window seen, 90 days kept, totals per category, file round trip.
        ("Gil journal: category from the window seen within 15 s (the latest one wins), else Other",() =>
        {
            var now = DateTimeOffset.UnixEpoch.AddDays(100);
            var seen = new Dictionary<string,DateTimeOffset> { ["Shop"] = now.AddSeconds(-3),["Repair"] = now.AddSeconds(-10),["Teleport"] = now.AddSeconds(-40) };
            Check(GilJournal.Classify(seen,now) == GilCategory.NpcShop,"the shop, seen last");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset> { ["Teleport"] = now.AddSeconds(-40) },now) == GilCategory.Other,"too old: Other");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset> { ["ItemSearchResult"] = now },now) == GilCategory.Market,"market");
        }),
        ("Gil journal: 90 days kept, newest 20 000 at most; totals per source and category; file written and read back, a bad file throws",() =>
        {
            var now = DateTimeOffset.UnixEpoch.AddDays(200);
            var entries = new List<GilEntry>
            {
                new(now.AddDays(-91),100,100,GilSource.Character,GilCategory.Market),
                new(now.AddDays(-2),-50,50,GilSource.Character,GilCategory.Repair),
                new(now.AddDays(-1),300,350,GilSource.Character,GilCategory.Market),
                new(now.AddHours(-1),-20,330,GilSource.Character,GilCategory.Market),
                new(now.AddHours(-1),5000,5000,GilSource.Retainers,GilCategory.Retainer),
            };
            var kept = GilJournal.Prune(entries,now);
            Check(kept.Count == 4 && kept[0].Delta == -50,"older than 90 days dropped, oldest first");
            var totals = GilJournal.Totals(kept,GilSource.Character,now.AddDays(-7));
            Check(totals[0] == (GilCategory.Market,300,20) && totals.Any(total => total == (GilCategory.Repair,0,50)),"gains and spendings per category, one source");
            var folder = Path.Combine(Path.GetTempPath(),"tensho-gil-" + Guid.NewGuid().ToString("N"));
            try
            {
                var path = Path.Combine(folder,"1.json");
                GilJournal.Write(path,kept);
                Check(GilJournal.Read(path).SequenceEqual(kept),"round trip");
                Check(GilJournal.Read(Path.Combine(folder,"none.json")).Count == 0,"missing file = empty journal");
                File.WriteAllText(path,"{\"Schema\":2,\"Entries\":[]}");
                var failed = false;
                try { GilJournal.Read(path); } catch (InvalidDataException) { failed = true; }
                Check(failed,"unknown schema refused (never written over)");
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder,true); }
            var watcher = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs"));
            Check(watcher.Contains("(nint)InventoryManager.MemberFunctionPointers.GetGil!=0",StringComparison.Ordinal) && watcher.Contains("if(!dirty || character==0 || loadFailed)return;",StringComparison.Ordinal),
                "function checked before the call; an unreadable journal is never overwritten");
        }),
        // From GilJournal768Tests: 0.768 (X-18, user request): the gil journal with icons, and what « Other » was.
        ("Gil journal: icons, colours, and the windows of an « Other » change in developer mode",() =>
        {
            var page=Read("MainWindow.GilJournal.cs");
            Check(page.Contains("GilCategoryIcon(category).ToIconString()") && page.Contains("Find(1)"),"icons");
            foreach(var category in Enum.GetValues<TenshoAssistant.Inventory.GilCategory>())Check(page.Contains("GilCategory."+category) || category==TenshoAssistant.Inventory.GilCategory.Other,category.ToString());
            var watcher=Read("Inventory/GilWatcher.Dalamud.cs");
            Check(watcher.Contains("if(category==GilCategory.Other && developerMode())") && watcher.Contains("\"Gil other: "),"what Other was");
        }),
        // From GilLoad993Tests: 0.993: the gil journal is read off the frame (it took 128 ms on the frame at each load); nothing is recorded before it is there.
        ("Gil load 993: read in a task, waited for before any change is recorded, a failed read never written over",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs"));
            // 0.1139 (X-173): read through GilJournalStore (TenshoData.db, the file of before converted once).
            Check(watcher.Contains("loading=id==0 ? null:Task.Run(()=>GilJournal.Prune(store.Load(id,now),now));"),"task");
            Check(watcher.Contains("if(!read.IsCompleted)return;") && watcher.Contains("else{loadFailed=true;"),"waited, failure kept");
        }),
        // From GilMail903Tests: 0.903 (X-84): gil taken from or attached to a letter are filed under « Mail » in the gil journal.
        ("Gil journal: the letter window gives « Mail », added last so saved numbers stay; not a transfer",() =>
        {
            var now=DateTimeOffset.UtcNow;
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>{["LetterList"]=now,["LetterViewer"]=now.AddSeconds(-2)},now)==GilCategory.Mail,"viewer");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>{["LetterEditor"]=now},now)==GilCategory.Mail,"editor");
            Check((int)GilCategory.Mail==7 && (int)GilCategory.Company==6 && !GilJournal.IsTransfer(GilCategory.Mail),"numbers, not a transfer");
        }),
        // From GilTeleport775Tests: 0.775 (X-18): a teleport paid after the loading screen.
        ("Gil teleport: a spending after a loading screen is a teleport; a gain is not",() =>
        {
            var now=new DateTimeOffset(2026,11,7,13,57,44,TimeSpan.Zero);
            var seen=new Dictionary<string,DateTimeOffset>{["NowLoading"]=now.AddSeconds(-2)};
            Check(GilJournal.ForDelta(GilJournal.Classify(seen,now),-1240)==GilCategory.Teleport,"the report");
            Check(GilJournal.ForDelta(GilJournal.Classify(seen,now),1000)==GilCategory.Other,"a gain stays Other");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>{["NowLoading"]=now.AddSeconds(-30)},now)==GilCategory.Other,"too long ago");
            var shop=new Dictionary<string,DateTimeOffset>{["NowLoading"]=now.AddSeconds(-10),["Shop"]=now.AddSeconds(-1)};
            Check(GilJournal.ForDelta(GilJournal.Classify(shop,now),-50)==GilCategory.NpcShop,"the latest window wins");
        }),
        // From GilTransfer769Tests: 0.769 (X-19, user report): a deposit with a retainer is not spent gil.
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
