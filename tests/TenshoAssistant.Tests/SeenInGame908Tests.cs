// 0.908 (X-76): the tools show every discovery seen in game, yours and imported ones alike, so their labels say « seen in game », not « seen by you »;
// one most-seen spearfishing shadow for the watcher and the spearfishing panel.
using System.Numerics;
using System.Text.Json;
using TenshoAssistant.Discoveries;
internal static class SeenInGame908Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    // The Discoveries page's own summary (seen by you / imported only) and what is shared (only yours) keep « by you ».
    private static bool Kept(string section,string key)=>section=="DiscoveriesUi" && (key.StartsWith("Summary",StringComparison.Ordinal) || key=="ShareEmpty");
    private static IEnumerable<string> Offending(string language,params string[] words)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
        var found=new List<string>();
        foreach(var section in document.RootElement.EnumerateObject())
        {
            if(section.Value.ValueKind!=JsonValueKind.Object)continue;
            foreach(var entry in section.Value.EnumerateObject())
                if(entry.Value.ValueKind==JsonValueKind.String && !Kept(section.Name,entry.Name)
                   && words.Any(word=>entry.Value.GetString()!.Contains(word,StringComparison.OrdinalIgnoreCase)))
                    found.Add(section.Name+"."+entry.Name);
        }
        return found;
    }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Seen in game: no tool label says « seen by you » (English) outside the Discoveries page's summary",() =>
        {
            var found=Offending("en","seen by you").ToArray();
            Check(found.Length==0,"seen by you: "+string.Join(", ",found));
        }),
        ("Seen in game: no tool label says « vu par vous » (French) outside the Discoveries page's summary",() =>
        {
            var found=Offending("fr","vu par vous","vus par vous","vue par vous","vues par vous").ToArray();
            Check(found.Length==0,"vu par vous: "+string.Join(", ",found));
        }),
        ("Most seen spearfishing shadow: yours and imported sightings counted together, none for an unknown fish",() =>
        {
            var at=new DateTimeOffset(2026,11,8,12,0,0,TimeSpan.Zero);
            var place=new Vector3(10,0,10);
            var store=new DiscoveryStore();
            // Fish 100: average and slow seen twice by you; large and fast seen once by you and imported twice.
            store.SeeSource(DiscoveryKind.SpearShadow,100,2,150,at,1,1,place);
            store.SeeSource(DiscoveryKind.SpearShadow,100,2,150,at.AddMinutes(1),1,1,place);
            store.SeeSource(DiscoveryKind.SpearShadow,100,3,200,at,1,1,place);
            Check(store.MostSeenSpearShadow(100)==(2,150),"yours first while ahead");
            var other=new DiscoveryStore();
            other.SeeSource(DiscoveryKind.SpearShadow,100,3,200,at,1,1,place);
            var shared=other.Snapshot(at,saving:false).Facts;
            store.Merge(shared);store.Merge(shared);
            Check(store.MostSeenSpearShadow(100)==(3,200),"imported sightings count");
            Check(store.Find(DiscoveryKind.SpearShadow,subject=>subject==100,8).Select(DiscoveryFact.TimesOf).SequenceEqual([3,2]),"times: seen and imported");
            Check(store.MostSeenSpearShadow(101) is null,"unknown fish");
        }),
    ];
}
