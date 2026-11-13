using TenshoAssistant.Reminders;

// 0.1263 (X-178, user request of 13/11: « up to three gardens (large house) »). The journal already knows patches 1 to 3 of a place; the
// icon view keeps one 3 × 3 grid per patch with its title and its « whole patch tended » button. Behaviour test with three patches at one
// place: three grids in order, and tending one patch leaves the two others alone.
internal static class GardenPatches1263Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Now=new(2026,11,13,15,0,0,TimeSpan.Zero);
    // Planted 30 h ago, ripe after 72 h, tending every 24 h: a tending is due.
    private static GardenBed Due(string place)=>new(){Place=place,Seed="Seed",PlantedAt=Now.AddHours(-30),GrowHours=72,TendHours=24};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden patches 1263: three patches at one place are three grids, in order, each with its own beds",() =>
        {
            const string house="Shirogane 2-17";
            var beds=new List<GardenBed>
            {
                Due($"{house} · potager 3, emplacement 8"),Due($"{house} · potager 1, emplacement 1"),Due($"{house} · potager 2, emplacement 4"),
                Due($"{house} · potager 1, emplacement 5"),Due($"{house} · potager 2, emplacement 4bis"),Due($"{house} · potager 4, emplacement 1"),
                Due("Mist 1-2 · potager 1, emplacement 1"),Due($"{house} · pot"),
            };
            var grids=GardenJournal.Grids(beds);
            var here=grids.Where(grid=>grid.Zone==house).ToArray();
            Check(here.Length==3 && here.Select(grid=>grid.Patch).SequenceEqual([1,2,3]),"patches 1, 2 and 3 of the house, in order");
            Check(here[0].Beds.Select(bed=>bed.Number).Order().SequenceEqual([1,5]) && here[1].Beds.Select(bed=>bed.Number).SequenceEqual([4]) && here[2].Beds.Select(bed=>bed.Number).SequenceEqual([8]),"each patch holds its own beds");
            Check(grids.Count==4 && grids.Any(grid=>grid.Zone=="Mist 1-2" && grid.Patch==1),"another place has its own grid");
            // A fourth patch does not exist in the game, a bed « 4bis » neither, and a pot has no patch: listed under the grids, never drawn in one.
            Check(beds.Count(bed=>GardenJournal.Slot(bed) is null)==3,"what is not a bed of patches 1 to 3 stays in the list");
        }),
        ("Garden patches 1263: « whole patch tended » tends that patch only",() =>
        {
            const string house="Shirogane 2-17";
            var first=Due($"{house} · potager 1, emplacement 1");var second=Due($"{house} · potager 2, emplacement 1");var third=Due($"{house} · potager 3, emplacement 1");
            var elsewhere=Due("Mist 1-2 · potager 2, emplacement 1");
            // In patch 2 too: a bed without a tending interval, and one tended an hour ago whose next tending would come after the harvest: nothing to tend.
            var noInterval=new GardenBed{Place=$"{house} · potager 2, emplacement 2",Seed="Seed",PlantedAt=Now.AddHours(-30),GrowHours=72,TendHours=0};
            var ripe=new GardenBed{Place=$"{house} · potager 2, emplacement 3",Seed="Seed",PlantedAt=Now.AddHours(-60),GrowHours=72,TendHours=24,TendedAt=Now.AddHours(-1)};
            var beds=new[]{first,second,third,elsewhere,noInterval,ripe};
            Check(beds.Take(4).All(bed=>GardenJournal.State(bed,Now)==GardenState.ToTend),"the four beds wait for a tending");
            Check(GardenJournal.TendPatch(beds,house,2,Now)==1,"one bed tended: the one of patch 2 that waited");
            Check(second.TendedAt==Now && GardenJournal.State(second,Now)==GardenState.Growing,"patch 2 is tended");
            Check(first.TendedAt is null && third.TendedAt is null && elsewhere.TendedAt is null,"patches 1 and 3 of the house and patch 2 of another place are left alone");
            Check(noInterval.TendedAt is null && ripe.TendedAt==Now.AddHours(-1),"a bed with nothing to tend is not touched");
            Check(GardenJournal.TendPatch(beds,house,2,Now)==1 && GardenJournal.TendPatch(null,house,2,Now)==0 && GardenJournal.TendPatch(beds,house,4,Now)==0,"again: the same bed (its next tending comes before the harvest); no beds, no such patch: nothing");
        }),
        ("Garden patches 1263: the page draws one grid, one title and one button per patch, in the application too",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Garden.cs");
            Check(page.Contains("foreach(var (zone,patch,planted) in GardenJournal.Grids(beds))") && page.Contains("UiComponents.SubTitle(localization.Format(\"Garden.GridTitle\",zone,patch));"),"a title per patch of a place");
            Check(page.Contains("ImGui.PushID($\"TendPatch{zone}{patch}\");") && page.Contains("if(tendAll){GardenJournal.TendPatch(beds,zone,patch,now);changed=true;}"),"the button of a patch tends that patch");
            Check(page.Contains("ImGui.BeginTable($\"GardenGrid{zone}{patch}\",3,"),"a 3 × 3 grid per patch, in icons and in text alike");
            var headless=Read("src/TenshoAssistant.Desktop/Headless.cs");
            Check(headless.Contains("(2,1,3,48,24,true)") && headless.Contains("(3,5,12,72,24,true)") && headless.Contains("Set(\"Place\",$\"Trial 1-1 · patch {patch}, bed {number}\");"),"the application's headless run draws three patches of one place");
        }),
    ];
}
