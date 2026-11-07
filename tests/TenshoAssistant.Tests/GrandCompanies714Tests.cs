using TenshoAssistant.GrandCompanies;

// W-11 (0.714): the Grand Companies.
internal static class GrandCompanies714Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static GcRank Rank(int rank,params string[] names)=>new(rank,1000,100*rank,names,["","",""]);
    private static readonly GcCatalog Catalog=new(["","a","b","c"],[Rank(1,"a1","b1","c1"),Rank(2,"a2","","c2"),Rank(3,"a3","b3","c3")],[new(1,10,500,2),new(1,11,100,1),new(2,12,50,1),new(1,13,900,3)]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Grand Companies: the next grade skips a row with no name in this company, and the last has none",() =>
        {
            Check(GcBoard.Next(Catalog,1,1)?.Rank==2,"next");
            Check(GcBoard.Next(Catalog,2,1)?.Rank==3,"no name for this company");
            Check(GcBoard.Next(Catalog,1,3) is null,"last");
            Check(GcBoard.Next(Catalog,0,1) is null,"no company");
        }),
        ("Grand Companies: the shop keeps the company's items, by grade then cost, up to the grade asked",() =>
        {
            var all=GcBoard.Shop(Catalog,1,null);
            Check(all.Select(item=>item.Item).SequenceEqual([11u,10u,13u]),"order");
            Check(GcBoard.Shop(Catalog,1,2).Count==2,"up to grade 2");
            Check(GcBoard.Shop(Catalog,2,null).Single().Item==12,"its own company");
        }),
        ("Grand Companies: an impossible read is refused",() =>
        {
            Check(GcBoard.Check(1,[1,0,0],20)==GcReadStatus.Loaded,"good");
            Check(GcBoard.Check(4,[1,0,0],20)==GcReadStatus.Unavailable,"company");
            Check(GcBoard.Check(1,[20,0,0],20)==GcReadStatus.Unavailable,"rank");
        }),
        ("Grand Companies: the page only reads, on the framework thread",() =>
        {
            Check(Read("Plugin.cs").Contains("mainWindow.ReadGc=rows=>framework.RunOnFrameworkThread(()=>"),"framework thread");
            Check(Read("GrandCompanies/GrandCompanies.Dalamud.cs").Contains("player->IsLoaded"),"loaded first");
        }),
    ];
}
