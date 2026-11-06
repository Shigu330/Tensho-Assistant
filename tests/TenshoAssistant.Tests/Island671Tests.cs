using TenshoAssistant.Island;

// N-10 (0.671): the island's reading checks and the value of a craft.
internal static class Island671Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static readonly DateTimeOffset At=new(2026,11,6,12,0,0,TimeSpan.Zero);
    internal static IslandCraft Craft(uint id,int hours,int value,int level,params uint[] themes)=>new(id,1000+id,"c"+id,hours,value,level,themes);
    internal static IslandCatalog Sample(params IslandCraft[] crafts)
    {
        const int rows=10;
        var popularity=new byte[rows];
        Array.Fill(popularity,(byte)3);
        popularity[1]=1;
        return new(rows,crafts,[popularity],[0,140,120,100,80],[160,130,100,80,60],[0,100,110,120,130,140],new Dictionary<uint,string>{[1]="t1"});
    }
    internal static IslandSnapshot Snapshot(int rank=20,uint groove=0,byte fill=0x22,params IslandWorkshop[] workshops)
    {
        var bytes=new byte[10];
        Array.Fill(bytes,fill);
        return new(IslandReadStatus.Loaded,At,rank,workshops,groove,null,0,0,0,bytes);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Island: supply and demand are read from each half of the byte, and a value out of range is refused",() =>
        {
            Check(IslandValue.Supply(0x31)==3 && IslandValue.Demand(0x31)==1,"nibbles");
            Check(IslandValue.Supply(0x50) is null && IslandValue.Demand(0x05) is null,"out of range");
        }),
        ("Island: the reading says what is missing",() =>
        {
            var catalog=Sample(Craft(1,4,28,1,1));
            var good=new byte[10];good[1]=0x22;
            Check(IslandValue.Check(catalog,5,0,0,false,good)==IslandReadStatus.Loaded,"loaded");
            Check(IslandValue.Check(catalog,5,0,0,true,good)==IslandReadStatus.NotLoaded,"the game says not loaded");
            Check(IslandValue.Check(catalog,5,0,0,false,new byte[10])==IslandReadStatus.NotLoaded,"all zero: not loaded");
            Check(IslandValue.Check(catalog,5,0,0,false,new byte[9])==IslandReadStatus.Unavailable,"another length: a game update");
            Check(IslandValue.Check(catalog,21,0,0,false,good)==IslandReadStatus.Unavailable,"impossible rank");
            Check(IslandValue.Check(catalog,5,1,0,false,good)==IslandReadStatus.Unavailable,"no such popularity");
            var bad=new byte[10];bad[1]=0x72;
            Check(IslandValue.Check(catalog,5,0,0,false,bad)==IslandReadStatus.Unavailable,"unknown supply");
            Check(!(catalog with{Popularity=[new byte[9]]}).Valid,"sheets of another shape");
        }),
        ("Island: the value multiplies the sheets' ratios, and supply slides by 8 units per band",() =>
        {
            var craft=Craft(1,4,100,1,1);
            var catalog=Sample(craft);
            var island=Snapshot(groove:10);
            var value=IslandValue.Unit(craft,catalog,island,2);
            Check(value is { } worth && Math.Abs(worth-100*1.10*1.10*1.40*1.00)<1e-9,"base × workshop × groove × popularity × supply");
            Check(Math.Abs(IslandValue.SupplyPercent(catalog,2,0)-100)<1e-9 && Math.Abs(IslandValue.SupplyPercent(catalog,2,4)-90)<1e-9 && Math.Abs(IslandValue.SupplyPercent(catalog,2,8)-80)<1e-9,"sliding");
            Check(Math.Abs(IslandValue.SupplyPercent(catalog,4,50)-60)<1e-9 && Math.Abs(IslandValue.SupplyPercent(catalog,0,0)-160)<1e-9,"bounded");
            Check(IslandValue.Unit(craft,catalog,island,0) is null && IslandValue.Unit(craft,catalog,island,6) is null,"no such workshop");
            var unknown=Craft(2,4,100,1,1);
            var catalogZero=catalog with{Popularity=[new byte[10]]};
            Check(IslandValue.Unit(unknown,catalogZero,island,1) is null,"popularity without a ratio");
        }),
        ("Island: the table keeps the crafts of the rank, the most valuable first",() =>
        {
            var catalog=Sample(Craft(1,4,100,1,1),Craft(2,4,100,1,1),Craft(3,4,500,10,1));
            var rows=IslandValue.Rows(catalog,Snapshot(rank:5,workshops:new IslandWorkshop(0,1,false)));
            Check(rows.Length==2 && rows[0].Craft.Id==1 && rows[1].Craft.Id==2,"popularity decides, then the row; rank 5 hides the last");
            Check(rows[0].Popularity==1 && rows[0].Supply==2 && rows[0].Demand==2,"the columns");
            Check(IslandValue.BestLevel(Snapshot(workshops:[new(0,2,false),new(1,4,true)]))==2,"not a workshop under work");
            Check(IslandValue.BestLevel(Snapshot())==1,"no workshop");
        }),
        ("Island: a reading from before 08:00 UTC is stale after it",() =>
        {
            var before=new DateTimeOffset(2026,11,6,7,59,0,TimeSpan.Zero);
            var after=new DateTimeOffset(2026,11,6,8,1,0,TimeSpan.Zero);
            Check(IslandValue.Stale(before,after) && !IslandValue.Stale(after,after.AddHours(5)) && !IslandValue.Stale(before,before.AddSeconds(30)) && IslandValue.Stale(after,after.AddDays(1)),"cycle change");
        }),
        ("Island: nothing is asked of the game or scheduled in it, and the read is a click",() =>
        {
            foreach(var file in new[]{"Island/IslandReader.Dalamud.cs","MainWindow.IslandSanctuary.cs","Diagnostics/ReadingCapture.Dalamud.cs","Plugin.cs"})
                foreach(var call in new[]{"RequestDemandFull","ScheduleCraft","SetDisplayedCycle","RequestFavorData"})
                    Check(!Read(file).Contains(call,StringComparison.Ordinal),$"{file}: {call}");
            Check(Read("MainWindow.IslandSanctuary.cs").Split("ReadIsland!(catalog)").Length==2 && Read("MainWindow.IslandSanctuary.cs").Contains("if(ImGui.Button(localization.Get(\"Island.Read\")))islandRead=ReadIsland!(catalog);"),"on a click");
            Check(Read("Plugin.cs").Contains("mainWindow.ReadIsland=catalog=>framework.RunOnFrameworkThread(()=>"),"on the framework thread");
        }),
    ];
}
