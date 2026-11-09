// Test audit of 2026-11-09, batch 2 (0.1021): FieldContent717Tests, FieldContent873Tests, FieldStates973Tests merged into FieldContentTests; every case is kept.
using TenshoAssistant.Fields;
using TenshoAssistant.Discoveries;
internal static class FieldContentTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static FieldEventRow Row(FieldEventKind kind,uint id,uint instance,int? level)=>new(kind,id,instance,"n"+id,"",level,0);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From FieldContent717Tests: W-14 (0.717): Eureka and Bozja.
        ("Eureka and Bozja: the layout places the rows of its own zone, once, FATEs first by level",() =>
        {
            FieldLayoutEntry[] entries=[new(10,1,2),new(20,3,4),new(30,5,6),new(10,9,9)];
            FieldEventRow[] rows=[Row(FieldEventKind.Engagement,1,30,null),Row(FieldEventKind.Fate,2,20,9),Row(FieldEventKind.Fate,3,10,4),Row(FieldEventKind.Fate,4,99,1)];
            var placed=FieldBoard.Place(entries,rows);
            Check(placed.Select(row=>row.Id).SequenceEqual([3u,2u,1u]),"order, and another zone's row left out");
            Check(placed[0].X==1 && placed[0].Z==2,"the first entry of an instance");
            Check(placed[2].Level is null,"no level stays unknown");
            Check(FieldBoard.Place(entries,[new(FieldEventKind.Fate,5,10,"","",1,0)]).Count==0,"a row with no name");
        }),
        ("FATEs: a search looks everywhere, a zone lists its own, and the zone you are in is found",() =>
        {
            FieldEvent One(uint id,string name)=>new(FieldEventKind.Fate,id,name,"",1,0,0,0);
            FieldZone[] zones=[new(134,15,"Noscea",[One(1,"Pomme"),One(2,"Poire")]),new(135,16,"Basse",[One(3,"Pomme verte")])];
            Check(FieldBoard.Find(zones,0,"").Select(row=>row.Row.Id).SequenceEqual([1u,2u]),"the zone");
            var found=FieldBoard.Find(zones,0,"pomme");
            Check(found.Select(row=>row.Row.Id).SequenceEqual([1u,3u]) && found[1].Zone.Territory==135,"every zone");
            Check(FieldBoard.Find(zones,0,"p").Count==2,"one letter stays on the zone");
            Check(FieldBoard.Find(zones,9,"").Count==0,"no such zone");
            Check(FieldBoard.ZoneOf(zones,135)==1 && FieldBoard.ZoneOf(zones,999)==0,"current zone, else the first");
            Check(Read("MainWindow.Fates.cs").Contains("fatesTask=Task.Run(load)") && Read("MainWindow.Fates.cs").Contains("PlaceMenu($\"FatePlace"),"off the frame, right click");
        }),
        ("Self-check: the pages coded without the game are read there, off the frame, and it waits for them",() =>
        {
            var pages=Read("MainWindow.SelfCheckPages.cs");
            foreach(var load in new[]{"FieldCatalogLoad","FatesLoad","DeepDungeonLoad","LexiconLoad","RelicCatalogLoad","AlliedCatalogLoad","GcCatalogLoad","DeliveryClientsLoad","CarnivaleStagesLoad"})
                Check(pages.Contains(load+"?.Invoke"),load);
            Check(pages.Contains("Task.Run(read)"),"off the frame");
            var check=Read("MainWindow.SelfCheck.cs");
            Check(check.Contains("selfCheckPages!.All(page => page.Read.IsCompleted)") && check.Contains(".. SelfCheckPageProbes()"),"waited for, then reported");
        }),
        ("Eureka and Bozja: the zones of the field uses (0.975: Occult Crescent too), and the search on name or text",() =>
        {
            Check(FieldBoard.FieldZoneUses.SequenceEqual([41u,48u,61u]),"zones");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FieldContent.cs")).Contains("zone.Use==FieldBoard.BozjaUse ? \"FieldContent.KindSkirmish\""),"skirmishes in Bozja only");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Fields/FieldBoard.Dalamud.cs")).Contains("FieldBoard.FieldZoneUses.Contains(territory.TerritoryIntendedUse.RowId)"),"from the sheet");
            Check(FieldBoard.Matches("",["a"]) && FieldBoard.Matches("BC",["x","abcd"]) && !FieldBoard.Matches("z",["x"]),"search");
        }),
        ("Eureka and Bozja: the files are read off the frame, only once checked, and a place has its right click",() =>
        {
            var page=Read("MainWindow.FieldContent.cs");
            Check(page.Contains("fieldLoad=Task.Run(load)"),"off the frame");
            Check(page.Contains("PlaceMenu($\"FieldPlace"),"right click");
            Check(Read("Fields/FieldBoard.Dalamud.cs").Contains("Npcs.NpcShopLinks.ValidLayout(bytes)"),"checked before parsing");
        }),
        // From FieldContent873Tests: 0.873 (X-73.17): kills count in field content and deep dungeons; a deep dungeon gain with nothing spent is a floor coffer.
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
        // From FieldStates973Tests: X-90 (0.973): Eureka, Bozja and Occult Crescent states written by the developer capture, on change only, in the field content only.
        ("Field states 973: three field directors read, written on change, bounded",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            foreach(var kind in new[]{"Eureka","Bozja","OccultCrescent"})Check(capture.Contains("PublicContentDirectorType."+kind+")"),kind);
            Check(capture.Contains("if(text==fieldState)return;") && capture.Contains("Line(\"fields\",120,text);"),"on change");
        }),
    ];
}
