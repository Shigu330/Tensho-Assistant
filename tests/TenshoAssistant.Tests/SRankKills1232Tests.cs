using TenshoAssistant.Hunting;

// 0.1232 (X-222, user's request of 12/11, K-38): the player's own kills for the S ranks counted by kills. Pure tests of the counter (which marks,
// which monster in which zone, bounds, clearing one mark only) and of the wiring (message 557 matched to a corpse, settings reset, clear button
// greyed in the desktop application).
internal static class SRankKills1232Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("S rank kills 1232: only « Kill » marks, the monster of the list in the mark's zone",() =>
        {
            var kill=SRankConditions.Marks.First(mark=>mark.Condition=="Kill");
            var monster=kill.Refs.First(item=>item.Kind==SRankRefKind.Mob).Id;
            Check(SRankKills.KeysFor(monster,kill.Territory).SequenceEqual(new[]{SRankKills.Key(kill.Name,monster)}),"counted in its zone");
            Check(SRankKills.KeysFor(monster,kill.Territory+100_000).Count==0,"not in another zone");
            Check(SRankKills.KeysFor(1,kill.Territory).Count==0 && SRankKills.KeysFor(0,0).Count==0,"another monster or nothing: not counted");
            Check(SRankConditions.Marks.Where(mark=>mark.Condition is "Gather" or "Discard" or "Extraordinary").All(mark=>!SRankKills.Counted(mark)),"other conditions not counted");
            Check(SRankKills.AnyIn(kill.Territory) && !SRankKills.AnyIn(0),"zones with a counted mark");
        }),
        ("S rank kills 1232: adds, bounds, clears one mark only, reads back bounded",() =>
        {
            var counts=new Dictionary<string,int>();
            Check(SRankKills.Add(counts,[SRankKills.Key(1,10),SRankKills.Key(1,11)]) && counts[SRankKills.Key(1,10)]==1,"first kill");
            SRankKills.Add(counts,[SRankKills.Key(1,10)]);SRankKills.Add(counts,[SRankKills.Key(2,10)]);
            Check(SRankKills.Get(counts,1,10)==2 && SRankKills.Get(counts,2,10)==1 && SRankKills.Get(counts,3,10)==0,"read back");
            counts[SRankKills.Key(4,1)]=SRankKills.MaxCount;
            Check(!SRankKills.Add(counts,[SRankKills.Key(4,1)]) && counts[SRankKills.Key(4,1)]==SRankKills.MaxCount,"never beyond the bound");
            counts[SRankKills.Key(5,1)]=-7;
            Check(SRankKills.Get(counts,5,1)==0 && SRankKills.Add(counts,[SRankKills.Key(5,1)]) && counts[SRankKills.Key(5,1)]==1,"damaged value read as 0, counted from 0");
            Check(SRankKills.Clear(counts,1) && SRankKills.Get(counts,1,10)==0 && SRankKills.Get(counts,1,11)==0 && SRankKills.Get(counts,2,10)==1,"mark 1 cleared, mark 2 kept");
            Check(!SRankKills.Clear(counts,1),"nothing left to clear");
            Check(SRankKills.Get(null,1,1)==0,"no settings yet");
        }),
        ("S rank kills 1232: wiring: defeat message matched to a corpse, own kills only, settings, page",() =>
        {
            var watcher=Source("src/TenshoAssistant/Hunting/SRankKillWatcher.Dalamud.cs");
            Check(watcher.Contains("message.LogMessageId!=ItemGains.DefeatMessage") && watcher.Contains("ItemGains.DefeatedByMe(") && watcher.Contains("ItemGains.Corpse(")
                  && watcher.Contains("if(!SRankKills.AnyIn(client.TerritoryType))return;"),"own defeats, matched to a corpse, only where a mark counts");
            Check(Source("src/TenshoAssistant/Configuration.cs").Contains("\"GearPlans\", \"SRankKills\","),"cleared with the player's data");
            Check(Source("src/TenshoAssistant/Plugin.cs").Contains("Hunting.SRankKills.Add(configuration.SRankKills ??= [],keys))SaveTimed(\"S rank kills\")"),"saved on a counted kill");
            var page=Source("src/TenshoAssistant/MainWindow.SRankConditions.cs");
            Check(page.Contains("if(SRankKills.Counted(mark))DrawSRankKills(mark);") && page.Contains("DesktopEditBegin();"),"page lines, clear greyed in the desktop application");
        }),
    ];
}
