using TenshoAssistant.FreeCompany;
using TenshoAssistant.Maps;

// 0.603 (T23): the vessels of several companies are not mixed.
internal static class Workshops603Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Workshops: one frame per company, the latest reading first",() =>
        {
            var at=new DateTimeOffset(2026,11,5,12,0,0,TimeSpan.Zero);
            VoyageVessel Vessel(ulong house,int days)=>new(){HouseId=house,ReadAt=at.AddDays(days)};
            var groups=Voyages.Workshops([Vessel(1,0),Vessel(2,5),Vessel(1,1)]);
            Check(groups.Count==2 && groups[0][0].HouseId==2 && groups[1].Count==2,"two workshops");
            Check(Voyages.Workshops([]).Count==0,"none");
        }),
        ("Workshop projects (C22): quantity per delivery by deliveries, totals over the phases",() =>
        {
            var material=new TenshoAssistant.FreeCompany.WorkshopMaterial(5094,3,3);
            Check(material.Total==9,"the sheet's first row: 3 per delivery, 3 deliveries");
            var project=new TenshoAssistant.FreeCompany.WorkshopProject(1,100,"Sous-marin",[new("Coque",[new(1,[material,new(7020,2,5)]),new(2,[new(5094,1,4)])])]);
            var totals=project.Totals();
            Check(totals.Count==2 && totals[0]==(5094u,13) && totals[1]==(7020u,10),"each item once, its whole quantity, first come first");
            Check(TenshoAssistant.FreeCompany.WorkshopStock.Missing(9,4)==5 && TenshoAssistant.FreeCompany.WorkshopStock.Missing(9,20)==0 && TenshoAssistant.FreeCompany.WorkshopStock.Missing(9,-1)==9,"what is missing");
            var loader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/WorkshopProjectsLoader.Dalamud.cs"));
            Check(loader.Contains("process.SetQuantity[index],process.SetsRequired[index]"),"the two columns, by the same index");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.WorkshopProjects.cs")).Contains("workshopProjects??=Task.Run(load);"),"off the frame, once");
        }),
    ];
}
