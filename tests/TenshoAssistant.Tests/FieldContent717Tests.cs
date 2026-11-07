using TenshoAssistant.Fields;

// W-14 (0.717): Eureka and Bozja.
internal static class FieldContent717Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static FieldEventRow Row(FieldEventKind kind,uint id,uint instance,int? level)=>new(kind,id,instance,"n"+id,"",level,0);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
        ("Eureka and Bozja: the six zones, and the search on name or text",() =>
        {
            Check(FieldBoard.Territories.SequenceEqual([732u,763u,795u,827u,920u,975u]),"zones");
            Check(FieldBoard.Matches("",["a"]) && FieldBoard.Matches("BC",["x","abcd"]) && !FieldBoard.Matches("z",["x"]),"search");
        }),
        ("Eureka and Bozja: the files are read off the frame, only once checked, and a place has its right click",() =>
        {
            var page=Read("MainWindow.FieldContent.cs");
            Check(page.Contains("fieldLoad=Task.Run(load)"),"off the frame");
            Check(page.Contains("PlaceMenu($\"FieldPlace"),"right click");
            Check(Read("Fields/FieldBoard.Dalamud.cs").Contains("Npcs.NpcShopLinks.ValidLayout(bytes)"),"checked before parsing");
        }),
    ];
}
