// Test audit of 2026-11-09, batch 2 (0.1021): GardenSeeds449Tests, GardenCrosses450Tests, GardenPlan456Tests, GardenGrid461Tests merged into GardenCrossesTests; every case is kept.
using TenshoAssistant.Reminders;
internal static class GardenCrossesTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From GardenSeeds449Tests: 0.449 (DEC-027, point 3): the garden journal's hours filled in from the embedded Lotlab seeds (GPL-3.0), still editable.
        ("Garden seeds: the embedded Lotlab table is read with its notice, hours bounded, wilting hours become the tending reminder",() =>
        {
            var csv=Read("Reminders/Data/garden-seeds.csv");
            Check(csv.Contains("# Source: Lotlab/FFXIV-Gardening-Tracker") && csv.Contains("GNU General Public License v3.0"),"source and licence in the header");
            var seeds=GardenSeeds.Read(new StringReader(csv));
            Check(seeds.Single(seed=>seed.SeedId==51189) is {HarvestId:51190,GrowHours:24,WiltHours:0},"0.540: cornflower seeds, GardeningSeed row 108");
            Check(seeds.Count==108 && seeds.Select(seed=>seed.SeedId).Distinct().Count()==108,"108 seeds");
            var pepper=seeds.Single(seed=>seed.SeedId==7715);
            Check(pepper.HarvestId==4785 && GardenSeeds.GrowHours(pepper)==120 && GardenSeeds.TendHours(pepper)==48,"paprika seeds 120 h, wilt after 48 h");
            Check(seeds.Any(seed=>seed.WiltHours==0) && seeds.Where(seed=>seed.WiltHours==0).All(seed=>GardenSeeds.TendHours(seed)==0),"no wilting: no tending reminder");
            Check(seeds.All(seed=>GardenSeeds.GrowHours(seed) is >= 1 and <= GardenJournal.MaximumGrowHours),"growing hours within the journal's bounds");
        }),
        ("Garden seeds: malformed and repeated rows skipped; names kept within the journal's 40 characters",() =>
        {
            var seeds=GardenSeeds.Read(new StringReader("# note\nseed,harvest,grow,wilt\n1,2,24,0\n1,2,48,0\nx,2,3,4\n3,4,0,0\n5,6,24,-1\n7,8,24\n9,10,30,12\n"));
            Check(seeds.Count==2 && seeds[0]==new GardenSeed(1,2,24,0) && seeds[1]==new GardenSeed(9,10,30,12),"two valid rows");
            Check(GardenSeeds.JournalName("  Graines de poivron  ")=="Graines de poivron" && GardenSeeds.JournalName(new string('x',50))==new string('x',40)
                && GardenSeeds.JournalName("")==null && GardenSeeds.JournalName(null)==null,"journal names");
        }),
        ("Garden seeds: picked from a list in the game's language, the hours stay editable, embedded and credited",() =>
        {
            var ui=Read("MainWindow.Garden.cs");
            Check(ui.Contains("newBedGrow=GardenSeeds.GrowHours(seed);") && ui.Contains("newBedTend=GardenSeeds.TendHours(seed);")
                && ui.Contains("InputInt(localization.Get(\"Garden.Grow\")"),"picker fills, inputs remain");
            Check(Read("TenshoAssistant.csproj").Contains("<EmbeddedResource Include=\"Reminders\\Data\\garden-seeds.csv\" />"),"embedded");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("\"TenshoAssistant.Reminders.Data.garden-seeds.csv\"") && plugin.Contains("data.GetExcelSheet<Lumina.Excel.Sheets.Item>();"),"names from the game's Item sheet");
            Check(Read("Credits/CreditCatalog.cs").Contains("new(\"LotlabGardening\", CreditKind.Embedded"),"credited");
        }),
        // From GardenCrosses450Tests: N-14 (0.450, DEC-027, point 2): crossbreeding pairs of nick75g (MIT), English names linked to item ids, unknown names left out and reported.
        ("Garden crosses: the embedded table keeps its MIT notice and gives 3 768 distinct pairs (3 886 rows, 118 repeated) once every name is known",() =>
        {
            var csv=Read("Reminders/Data/garden-crosses.csv");
            Check(csv.Contains("# Source: nick75g/FFXIV-Crossbreed-Helper") && csv.Contains("# Copyright (c) 2024 Nick75") && csv.Contains("# THE SOFTWARE IS PROVIDED \"AS IS\""),"notice");
            var names=csv.Split('\n').Where(line=>line.Length>0 && !line.StartsWith('#') && !line.StartsWith("target\t",StringComparison.Ordinal))
                .SelectMany(line=>line.Split('\t')).Distinct().Select((name,index)=>(Id:(uint)index+1,Name:name)).ToArray();
            var data=GardenCrosses.Read(new StringReader(csv),GardenCrosses.EnglishIndex(names));
            Check(data.Unmatched.Count==0 && data.Crosses.Count==3768,"distinct pairs: "+data.Crosses.Count);
            Check(data.Crosses.Select(cross=>cross.Target).Distinct().Count()==49,"49 seeds given by a crossbreed");
        }),
        ("Garden crosses: unknown names reported and their rows left out; a pair counted once whatever its order",() =>
        {
            var index=GardenCrosses.EnglishIndex([(10,"Target Seeds"),(20,"First Seeds"),(30,"Second Seeds"),(5,"first seeds"),(0,"Zero"),(40,"")]);
            Check(index("FIRST SEEDS")==5 && index("Zero") is null && index("") is null && index("Other") is null,"case ignored, lowest id, no id 0");
            var data=GardenCrosses.Read(new StringReader("# note\ntarget\tfirst\tsecond\nTarget Seeds\tFirst Seeds\tSecond Seeds\nTarget Seeds\tSecond Seeds\tFirst Seeds\n"
                +"Target Seeds\tGysahl Green Seeds\tFirst Seeds\nbad line\nTarget Seeds\t \tFirst Seeds\n"),index);
            Check(data.Crosses.Count==1 && data.Crosses[0]==new GardenCross(10,5,30),"one pair");
            Check(data.Unmatched is ["Gysahl Green Seeds"],"reported");
            Check(data.ProducersOf(10).Count==1 && data.ProducersOf(5).Count==0,"producers");
            Check(data.UsesOf(5) is [(30,10)] && data.UsesOf(30) is [(5,10)] && data.Seeds.Count==3,"uses");
        }),
        ("Garden crosses: read once off the frame through the English Item sheet, shown in the game's language, embedded and credited",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("mainWindow.GardenCrossCatalog=()=>gardenCrosses??=Task.Run(()=>") && plugin.Contains("GetExcelSheet<Lumina.Excel.Sheets.Item>(Dalamud.Game.ClientLanguage.English)")
                && plugin.Contains("\"TenshoAssistant.Reminders.Data.garden-crosses.csv\""),"background, English sheet, embedded file");
            var ui=Read("MainWindow.GardenCrosses.cs");
            Check(ui.Contains("if(!task.IsCompletedSuccessfully)") && !ui.Contains("http") && !ui.Contains("unsafe"),"waits for the task, nothing online, no game memory");
            Check(Read("TenshoAssistant.csproj").Contains("<EmbeddedResource Include=\"Reminders\\Data\\garden-crosses.csv\" />"),"embedded");
            Check(Read("Credits/CreditCatalog.cs").Contains("new(\"CrossbreedHelper\", CreditKind.Embedded"),"credited (wired: Todo458Tests)");
        }),
        // From GardenPlan456Tests: 0.456 (in-game campaign): the planting plan to get as many of a seed as possible (rules: docs/evidence/0.456-garden-grid.md).
        ("Garden plan: the pairs that give the fewest other seeds come first",() =>
        {
            var data=new GardenCrossData([new(100,1,2),new(200,1,2),new(100,3,4),new(100,5,6),new(300,5,6),new(400,5,6)],[]);
            var best=data.BestPairsFor(100);
            Check(best.Count==3 && best[0] is { First:3, Second:4 } && best[0].Outcomes.Count==1,"only this seed first");
            Check(best[1] is { First:1, Second:2 } && best[1].Outcomes.Count==2 && best[2].Outcomes.Count==3,"then by number of outcomes");
            Check(data.BestPairsFor(999).Count==0,"no pair");
        }),
        ("Garden plan: 8 beds in a ring around an empty centre, seeds alternating, 4 of each, slower seed first, bed 1 without a cross",() =>
        {
            var pair=new GardenCrossPair(1,2,[100]);
            var plan=GardenCrosses.Plan(pair,seed=>seed==2 ? 120:24);
            Check(plan.Count==8 && plan.Select(bed=>(bed.Row,bed.Column)).Distinct().Count()==8 && !plan.Any(bed=>bed is { Row:1, Column:1 }),"ring");
            Check(plan[0] is { Order:1, Row:0, Column:0, Seed:2, Crosses:false } && plan.Skip(1).All(bed=>bed.Crosses),"bed 1 at the top left, slower seed, no cross");
            Check(plan.Count(bed=>bed.Seed==1)==4 && plan.Count(bed=>bed.Seed==2)==4,"4 of each");
            for(var index=0;index<8;index++)
            {
                var next=plan[(index+1)%8];
                Check(next.Seed!=plan[index].Seed && Math.Abs(next.Row-plan[index].Row)+Math.Abs(next.Column-plan[index].Column)==1,"neighbours differ: "+index);
            }
            Check(GardenCrosses.Plan(pair,_=>null)[0].Seed==1,"unknown durations: the pair's order");
        }),
        ("Garden plan: chosen by click, nothing read from the game, credited with its evidence",() =>
        {
            var ui=Read("MainWindow.GardenCrosses.cs");
            Check(ui.Contains("if(ImGui.Selectable(label+") && ui.Contains("if(gardenPair is { } chosen)DrawGardenPlan(chosen,name);") && !ui.Contains("unsafe"),"click");
            Check(ui.Contains("best.Seed!=gardenCrossSeed"),"pairs kept while the seed stays");
            Check(Read("Plugin.cs").Contains("crosses.Seeds.Concat([MainWindow.GardenTopsoil3,MainWindow.GardenTopsoil2])"),"soil names in the game's language");
            Check(Read("Credits/CreditCatalog.cs").Contains("new(\"GardeningGuide\", CreditKind.Rule") && File.ReadAllText(Path.GetFullPath("docs/evidence/0.456-garden-grid.md")).Contains("7766"),"credit and evidence");
        }),
        // From GardenGrid461Tests: 0.461 (in-game campaign): the garden journal draws each patch as a grid.
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
