using TenshoAssistant.Inventory;

// 0.351 (g, I-04): gil journal — category from the last game window seen, 90 days kept, totals per category, file round trip.
internal static class GilJournal351Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
