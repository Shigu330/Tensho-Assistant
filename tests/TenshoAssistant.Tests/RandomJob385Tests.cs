using TenshoAssistant.Gear;

// 0.385 (g, X-08): a job at random — one draw per job, jobs at the top level left out on request, wearing only on click through the
// game function checked before the call.
internal static class RandomJob385Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static GameGearset Set(int number,uint job)=>new(number,"",job,600,[]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Random job: one draw per job, top level left out on request, worn only on click with the function checked",() =>
        {
            IReadOnlyList<GameGearset> sets = [Set(1,19),Set(2,19),Set(3,19),Set(4,21),Set(5,24)];
            var levels = new Dictionary<uint,int>{[19]=100,[21]=90};
            var seen = new HashSet<uint>();
            for (var pick = 0; pick < 3; pick++) seen.Add(GameGearsets.RandomJob(sets,levels,false,_=>pick)!.Job);
            Check(seen.SetEquals([19u,21u,24u]),"each job once in the pool");
            Check(GameGearsets.RandomJob(sets,levels,false,_=>0)!.Number==1,"lowest number of the job");
            var below = Enumerable.Range(0,2).Select(pick=>GameGearsets.RandomJob(sets,levels,true,_=>pick)!.Job).ToHashSet();
            Check(below.SetEquals([21u,24u]),"top level left out, unknown level kept");
            Check(GameGearsets.RandomJob([],levels,true,_=>0) is null,"no set");
            var reader = Read("Gear/GameGearsetReader.Dalamud.cs");
            Check(reader.Contains("(nint)RaptureGearsetModule.MemberFunctionPointers.EquipGearset!=0",StringComparison.Ordinal)
                && reader.Contains("gearsets->IsValidGearset(wanted))gearsets->EquipGearset(wanted,0);",StringComparison.Ordinal),"checked call");
            Check(Read("MainWindow.Gearsets.cs").Contains("if(ImGui.SmallButton(localization.Get(\"GameGearsets.Equip\"))){source.Equip(drawn.Number);",StringComparison.Ordinal),"click only");
        }),
    ];
}
