// 0.873 (X-73.17): kills count in field content and deep dungeons; a deep dungeon gain with nothing spent is a floor coffer.
using TenshoAssistant.Discoveries;
internal static class FieldContent873Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Field content: a kill counts in Eureka or Bozja; in a deep dungeon, a gain with nothing spent is a coffer",() =>
        {
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,9000,true,null,false,false)==ItemGains.Cause.None,"other duties: unchanged");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,9000,true,null,false,false,field:true)==ItemGains.Cause.MonsterDrop,"field: a kill");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,-100_000,true,null,false,false,deepDungeon:true)==ItemGains.Cause.DungeonCoffer,"deep dungeon coffer");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,-100_000,true,null,false,false,field:true)==ItemGains.Cause.None,"field without kill: nothing");
            Check(ItemGains.Attribute(false,true,10_000,-100_000,0,-100_000,true,null,false,false,deepDungeon:true)==ItemGains.Cause.None,"busy: nothing");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("events->GetInstanceContentDeepDungeon()!=null") && watcher.Contains("PublicContentDirectorType.Bozja"),"told by the game");
        }),
    ];
}
