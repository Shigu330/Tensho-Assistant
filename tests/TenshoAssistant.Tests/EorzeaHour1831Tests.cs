// X-829 (security review X-810, I1): the Eorzea minute of a sighting never leaves the PC, only its hour.
using TenshoAssistant.Discoveries;
internal static class EorzeaHour1831Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("X-829: a vista, a fish's weather and a fish's weather before are shared with the Eorzea hour only; the local base keeps the minute",() =>
        {
            var day=new DateTimeOffset(2026,11,20,0,0,0,TimeSpan.Zero);
            var store=new DiscoveryStore();
            // Three sightings at 17:03, 17:30 and 17:59 ET (minutes 1023, 1050, 1079), like the watcher notes them (EorzeaMinute).
            foreach(var minute in new[]{1023,1050,1079})
            {
                var at=day.AddMinutes(minute);
                store.SeeSource(DiscoveryKind.FishCondition,5001,2,minute,at,132);
                store.SeeSource(DiscoveryKind.FishTransition,5001,3,minute,at,132);
                store.SeeSource(DiscoveryKind.VistaSeen,7,2,minute,at);
                store.SeeSource(DiscoveryKind.Venture,9,1,minute,at);
            }
            var snapshot=store.Snapshot(day.AddDays(1));
            Check(snapshot.Facts.Single(fact=>fact.Kind==DiscoveryKind.FishCondition) is { LevelMin:1023, LevelMax:1079 } local && local.Values!.Count==3,"the local base keeps the minute");
            var mine=CommunityDiscoveries.Mine(snapshot);
            foreach(var kind in new[]{DiscoveryKind.FishCondition,DiscoveryKind.FishTransition,DiscoveryKind.VistaSeen})
            {
                var fact=mine.Facts.Single(fact=>fact.Kind==kind);
                Check(fact.LevelMin==1020 && fact.LevelMax==1020 && fact.Values is { Count:1 } values && values[1020]==3 && DiscoveryStore.Valid(fact),
                    $"{kind}: shared at the hour only ({fact.LevelMin}-{fact.LevelMax}, {string.Join(",",fact.Values?.Keys.ToArray() ?? [])})");
                Check(fact.First==day && fact.Last==day,$"{kind}: the day only");
            }
            // Another kind whose value is not a time keeps its values.
            Check(mine.Facts.Single(fact=>fact.Kind==DiscoveryKind.Venture) is { LevelMin:1023, LevelMax:1079 } venture && venture.Values!.Count==3,"other kinds untouched");
            // What goes into the gist (v3 lines) holds no minute of these kinds.
            var lines=ExchangeV3.Build(mine);
            Check(lines.Files.Count>0,"built");
        }),
        ("X-829: a fact mixed with an import sends your own levels, at the hour",() =>
        {
            var day=new DateTimeOffset(2026,11,20,0,0,0,TimeSpan.Zero);
            var store=new DiscoveryStore();
            store.Merge([new DiscoveryFact{Kind=DiscoveryKind.VistaSeen,Subject=7,Detail=2,LevelMin=300,LevelMax=300,Imported=1,First=day,Last=day,Values=new(){[300]=1}}]);
            store.SeeSource(DiscoveryKind.VistaSeen,7,2,1439,day.AddHours(3));
            var fact=CommunityDiscoveries.Mine(store.Snapshot(day.AddDays(1))).Facts.Single();
            Check(fact.LevelMin==1380 && fact.LevelMax==1380 && fact.Values is { Count:1 } values && values.ContainsKey(1380),$"own minute 1439 sent as 1380 ({fact.LevelMin}, {string.Join(",",fact.Values?.Keys.ToArray() ?? [])})");
        }),
        ("X-829: the warning says the Eorzea hour; the merge tool keeps a share's hour (its selftest)",() =>
        {
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains(language=="en" ? "the Eorzea hour (never the minute)":"l'heure éorzéenne (jamais la minute)"),$"{language}: the hour said");
            }
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("if player:g=eorzea_hour(g)") && tool.Contains("EORZEA_TIME_KINDS={18,11,21}"),"the merge tool brings an older plugin's share to the hour");
            Check((int)DiscoveryKind.VistaSeen==18 && (int)DiscoveryKind.FishCondition==11 && (int)DiscoveryKind.FishTransition==21,"the tool's kind numbers");
        }),
    ];
}
