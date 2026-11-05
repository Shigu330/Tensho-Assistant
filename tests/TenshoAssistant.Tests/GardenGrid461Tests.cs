using TenshoAssistant.Reminders;

// 0.461 (in-game campaign): the garden journal draws each patch as a grid.
internal static class GardenGrid461Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden grid: a bed's slot read from its place in each language; pots and free text stay out; grouped by zone and patch",() =>
        {
            var now=new DateTimeOffset(2026,11,4,12,0,0,TimeSpan.Zero);
            GardenBed Bed(string place)=>GardenJournal.Plant(place,"Seed",1,0,now);
            Check(GardenJournal.Slot(Bed("Shirogane 2-17 · potager 1, emplacement 6")) is { Zone:"Shirogane 2-17", Patch:1, Bed:6 },"French");
            Check(GardenJournal.Slot(Bed("Mist 3-4 · patch 2, bed 8")) is { Zone:"Mist 3-4", Patch:2, Bed:8 },"English");
            foreach(var other in new[]{"Maison, pot 1","Shirogane 2-17 · pot","Chez moi · potager 4, emplacement 1","Chez moi · potager 1, emplacement 9","potager 1, emplacement 2"})
                Check(GardenJournal.Slot(Bed(other)) is null,other);
            var grids=GardenJournal.Grids([Bed("B 1-1 · patch 2, bed 1"),Bed("A 1-1 · patch 1, bed 3"),Bed("A 1-1 · patch 1, bed 1"),Bed("A pot")]);
            Check(grids.Count==2 && grids[0] is { Zone:"A 1-1", Patch:1 } && grids[0].Beds.Count==2 && grids[1] is { Zone:"B 1-1", Patch:2 },"grouped");
            Check(GardenJournal.Grids(null).Count==0,"no journal");
        }),
        ("Garden grid: every bed has one cell, the centre none, the same drawing as the plan",() =>
        {
            var cells=Enumerable.Range(1,8).Select(GardenCrosses.BedCell).ToArray();
            Check(cells.Distinct().Count()==8 && !cells.Contains((1,1)) && cells[0]==(0,0),"8 cells, bed 1 at the top left");
            for(var bed=1;bed<=8;bed++)Check(GardenCrosses.BedAt(GardenCrosses.BedCell(bed).Row,GardenCrosses.BedCell(bed).Column)==bed,"round trip "+bed);
            Check(GardenCrosses.BedAt(1,1) is null,"centre");
            var plan=GardenCrosses.Plan(new(1,2,[9]),_=>null);
            Check(plan.All(step=>GardenCrosses.BedCell(step.Order)==(step.Row,step.Column)),"same as the plan");
        }),
    ];
}
