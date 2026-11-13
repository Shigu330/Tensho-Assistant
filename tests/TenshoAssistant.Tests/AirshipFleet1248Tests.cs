using TenshoAssistant.FreeCompany;

// 0.1248 (X-201, slice 2, user request of 2026-11-12): one line per registered airship with its own best route for the goal chosen. The
// game computes one speed's leg table at a time (framework thread): the page's speed first, then the others, one per update, at most eight
// kept; the list is closed by default and asks nothing of the game while closed. Here: the speeds' rules (pure) and the wiring.
internal static class AirshipFleet1248Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static AirshipLegTable T(int speed)=>new(speed,new Dictionary<(uint,uint),(uint,uint)>(),new Dictionary<uint,uint>());

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Airship fleet 1248: speeds wanted clamped, once each, eight at most; the page's speed computed first, then the others in order",() =>
        {
            Check(AirshipSpeeds.Wanted([120,0,120,-5,99999,80]).SequenceEqual([1,80,120,short.MaxValue]),"clamped, once, in order: "+string.Join(",",AirshipSpeeds.Wanted([120,0,120,-5,99999,80])));
            Check(AirshipSpeeds.Wanted(Enumerable.Range(1,20)).Length==AirshipSpeeds.MaxSpeeds,"eight at most");
            var none=new Dictionary<int,AirshipLegTable>();
            Check(AirshipSpeeds.Next(100,[80,120],none)==100,"the page's speed first");
            Check(AirshipSpeeds.Next(100,[80,120],new Dictionary<int,AirshipLegTable>{[100]=T(100)})==80,"then the others in order");
            Check(AirshipSpeeds.Next(100,[80,120],new Dictionary<int,AirshipLegTable>{[100]=T(100),[80]=T(80),[120]=T(120)}) is null,"nothing left: no call to the game");
            Check(AirshipSpeeds.Next(0,[80],none)==80,"no page speed yet: the others");
        }),
        ("Airship fleet 1248: kept tables bounded, the page's speed and the wanted ones kept first; a new dictionary each time",() =>
        {
            IReadOnlyDictionary<int,AirshipLegTable> kept=new Dictionary<int,AirshipLegTable>();
            for(var speed=1;speed<=12;speed++)kept=AirshipSpeeds.Keep(kept,100,[],T(speed));
            Check(kept.Count==AirshipSpeeds.MaxSpeeds,"eight at most: "+kept.Count);
            var before=kept;
            var after=AirshipSpeeds.Keep(kept,100,[2,3],T(100));
            Check(!ReferenceEquals(before,after) && before.Count==AirshipSpeeds.MaxSpeeds && !before.ContainsKey(100),"the old dictionary, still read by the page, is untouched");
            Check(after.ContainsKey(100) && after.Count==AirshipSpeeds.MaxSpeeds,"the page's speed kept");
            var next=AirshipSpeeds.Keep(after,100,[2,3],T(50));
            Check(next.ContainsKey(100) && next.ContainsKey(50) && (next.ContainsKey(2) || !after.ContainsKey(2)) && (next.ContainsKey(3) || !after.ContainsKey(3)),"the wanted ones before the rest");
            Check(next[50].Speed==50 && next[100].Speed==100,"each table under its speed");
        }),
        ("Airship fleet 1248: the native side computes one speed per update over the same sectors, the page asks only while the list is open",() =>
        {
            var table=Read("src/TenshoAssistant/FreeCompany/AirshipVoyageTable.Dalamud.cs");
            Check(table.Contains("if(AirshipSpeeds.Next(speed,others,kept) is not { } next)return;"),"nothing to compute: no call");
            Check(table.Contains("if(!ReferenceEquals(tablesFor,points)){tablesFor=points;PublishTables(new Dictionary<int,AirshipLegTable>());}"),"tables of other sectors dropped");
            Check(table.Contains("HousingManager.GetAirshipVoyageTimeAndDistance((byte)from,(byte)to,(short)next,&time,&distance);") && table.Contains("HousingManager.GetAirshipSurveyDuration((byte)to,(short)next);"),"computed for the speed chosen");
            Check(table.IndexOf("if(AirshipSpeeds.Next(",StringComparison.Ordinal)<table.IndexOf("if(!client.IsLoggedIn)",StringComparison.Ordinal)
                  && table.IndexOf("if(!client.IsLoggedIn)",StringComparison.Ordinal)<table.IndexOf("MemberFunctionPointers.GetAirshipVoyageTimeAndDistance==0",StringComparison.Ordinal),"logged in and functions resolved before any call");
            Check(table.Contains("if(!mainDone && kept.TryGetValue(speed,out var cached)){computedFor=points;Publish(new(cached,speed,false,false));return;}"),"a speed already computed is not asked again");
            var page=Read("src/TenshoAssistant/MainWindow.AirshipPlanner.cs");
            Check(page.Contains("if(!open){RequestAirshipSpeeds?.Invoke([]);return;}"),"closed: no other speed asked");
            Check(page.Contains("AirshipPlanner.Plan(sectors,table,ship.Rank==0 ? int.MaxValue:ship.Rank,ship.Range,goal,1,token,targets).FirstOrDefault()"),"each airship: its rank, range, goal and target");
            Check(page.Contains("airshipFleetTask=Task.Run(") && page.Contains("airshipFleetCancel?.Cancel();"),"searched on a worker, the previous search cancelled");
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("mainWindow.RequestAirshipSpeeds=speeds=>airshipTable?.RequestAlso(speeds);") && plugin.Contains("mainWindow.AirshipTableFor=speed=>airshipTable?.TableFor(speed);"),"wired");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var key in new[]{"Fleet","FleetTip","FleetNone","FleetShip","FleetRoute","FleetNoRoute","FleetUseTip"})
                Check(en.Contains($"    \"{key}\": ") && fr.Contains($"    \"{key}\": "),"text: "+key);
        }),
    ];
}
