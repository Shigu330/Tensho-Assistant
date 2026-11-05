using TenshoAssistant.FreeCompany;

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
    ];
}
