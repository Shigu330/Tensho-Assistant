using TenshoAssistant.Monsters;

// 0.798 (X-38, user request): nearby monsters that teach a blue magic spell not learned yet.
internal static class BlueNearby798Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Blue spell monsters: the wiki's English names matched to monster rows, each spell once",() =>
        {
            var names=new Dictionary<string,uint[]>(StringComparer.OrdinalIgnoreCase){["Tiny Mandragora"]=[10,11],["Kraken"]=[20]};
            IReadOnlyList<uint> Ids(string name)=>names.TryGetValue(name,out var ids) ? ids:[];
            var bySpell=new Dictionary<uint,IReadOnlyList<WikiMonsterRow>>{[1]=[new("tiny mandragora","5","x"),new("Unknown thing","1","y")],[2]=[new(" Kraken ","50","z"),new("Tiny Mandragora","5","x")]};
            var map=BlueSpellMonsterIndex.Map(bySpell,Ids);
            Check(map[10].SequenceEqual(new uint[]{1,2}) && map[11].SequenceEqual(new uint[]{1,2}) && map[20].SequenceEqual(new uint[]{2}),"matched, case ignored, trimmed");
            Check(map.Count==3,"an unknown name gives nothing");
        }),
        ("Blue spell monsters: pages read on a click only, three seconds apart; as Blue Mage unless always; settings on the map and the Blue magic page",() =>
        {
            var index=Read("Monsters/BlueSpellMonsterIndex.cs");
            Check(index.Contains("await Task.Delay(WikiMonsterSources.RequestInterval+TimeSpan.FromMilliseconds(100),token)") && index.Contains("public void LoadCached(") && index.Contains("ReadCachedAsync"),"cadence, disk first");
            Check(Read("MainWindow.BlueSpellSources.cs").Contains("if(SourceButton(localization.Format(\"BlueSpellSources.NearbyFetch\",missing),configuration.CommunityWiki,true))index.FetchMissing(spells);"),"click only");
            Check(Read("MainWindow.NearbyMonsters.cs").Contains("(configuration.MapView.BlueMageMonstersAlways || characterStats.State.JobId==BlueMageJob)"),"job");
            Check(!new TenshoAssistant.MapViewSettings().BlueMageMonstersAlways,"off by default");
        }),
    ];
}
