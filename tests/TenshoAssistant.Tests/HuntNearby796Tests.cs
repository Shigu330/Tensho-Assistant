using TenshoAssistant.Hunting;

// 0.796 (X-36, user request): nearby monsters the hunting log still asks for.
internal static class HuntNearby796Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static HuntSlot Slot(int rank,params byte[] first)=>new(rank,[first,new byte[4],new byte[4],new byte[4],new byte[4],new byte[4],new byte[4],new byte[4],new byte[4],new byte[4]]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunting log missing: current rank only, targets not done, by monster",() =>
        {
            // Gladiator (key 1, slot 0): rank 0 entry 1 with two targets; rank 1 entry 1.
            var entries=new[]{new HuntEntry(10001,"Gladiateur 01",[new HuntTarget("bombo",3,"",900),new HuntTarget("rat",2,"",901)]),new HuntEntry(10011,"Gladiateur 11",[new HuntTarget("loup",3,"",902)])};
            var slots=Enumerable.Repeat(Slot(0,3,1),12).ToArray();
            var missing=HuntingLog.Missing(entries,slots,null);
            Check(!missing.ContainsKey(900) && missing[901].Single()==("Gladiateur 01",1,2),"done left out, the rest with its kills");
            Check(!missing.ContainsKey(902),"rank not reached: not counted by the game");
            Check(HuntingLog.Missing(entries,[],null).Count==0,"not read: nothing");
            Check(HuntingLog.Missing([new HuntEntry(10001,"x",[new HuntTarget("old",3,"")])],slots,null).Count==0,"no monster row: nothing");
        }),
        ("Hunting log missing: the monster row comes from the sheet; one map layer, set on the map and on the log page",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Plugin.cs").Contains(".Where(name=>name.Length>0)),pair.target.Value.BNpcName.RowId))"),"name row");
            Check(Read("MainWindow.HuntingLog.cs").Contains("()=>configuration.MapView.ShowHuntLogMonsters"),"page setting");
            Check(Read("MainWindow.NearbyMonsters.cs").Contains("HuntingLog.Missing(entries,slots,HuntGrandCompany?.Invoke())"),"used");
        }),
    ];
}
