// 0.863 (X-73.9): blue spells learned and beasts tamed, put down to their monster.
internal static class LearnedTamed863Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Learned and tamed: unlock events queued and cut in PvP; beasts by the obtained set's growth; the monster dead within 15 s or the target",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(disposed || TenshoAssistant.Competitive.CompetitiveGate.Cut || row.RowType!=typeof(Lumina.Excel.Sheets.AozAction)"),"queued, cut");
            Check(watcher.Contains("obtained.Where(id=>!known.Contains(id))") && watcher.Contains("now-death.At is >= 0 and <= 15_000"),"growth, monster");
            Check(watcher.Contains("if(unlocks is not null)unlocks.Unlock-=OnUnlock;"),"unsubscribed");
            var store=new TenshoAssistant.Discoveries.DiscoveryStore();
            Check(store.SeeSource(TenshoAssistant.Discoveries.DiscoveryKind.BlueSpellLearned,5,0,1,DateTimeOffset.UtcNow),"a spell with no monster is kept");
        }),
    ];
}
