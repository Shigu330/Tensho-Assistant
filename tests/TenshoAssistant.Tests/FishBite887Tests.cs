// 0.887 (X-73.18): bite strength and bite time from the cast, proved by the in-game capture (docs/evidence/0.887-fishing-bite.md).
using TenshoAssistant.Discoveries;
internal static class FishBite887Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fish bite: 292/293/294 are ! / !! / !!!, measured from CastingOut, Chum casts left out, kept with the place",() =>
        {
            Check(ItemGains.BiteStrength(292)==1 && ItemGains.BiteStrength(293)==2 && ItemGains.BiteStrength(294)==3 && ItemGains.BiteStrength(278)==0,"strengths");
            Check(DiscoveryStore.HasPlace(DiscoveryKind.FishBite),"place");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(state==1){castStarted=now;bitAt=-100_000;castChum=me.StatusList.Any(status=>status.StatusId==763);}"),"cast start, chum");
            Check(watcher.Contains("if(bitAt>castStarted && castStarted>0 && !castChum"),"measured only after a cast, without chum");
        }),
    ];
}
