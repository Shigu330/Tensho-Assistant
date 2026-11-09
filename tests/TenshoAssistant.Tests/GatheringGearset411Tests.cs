using TenshoAssistant.Gear;

// R-10 (0.411): « Equip (job) » on each gathering source wears that job's gear set with the lowest number, on click only.
internal static class GatheringGearset411Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gathering gear set: job of the source, lowest set number of that job",() =>
        {
            // docs/evidence/0.411-gathering-jobs.md: GatheringType 0 Mining, 1 Quarrying, 2 Logging, 3 Harvesting, 5 unnamed.
            Check(GameGearsets.GatheringJob(0,false)==16 && GameGearsets.GatheringJob(1,false)==16,"miner");
            Check(GameGearsets.GatheringJob(2,false)==17 && GameGearsets.GatheringJob(3,false)==17,"botanist");
            Check(GameGearsets.GatheringJob(0,true)==18,"fishing spot");
            Check(GameGearsets.GatheringJob(5,false) is null && GameGearsets.GatheringJob(4,false) is null,"unknown type: no button");
            GameGearset Set(int number,uint job)=>new(number,"",job,0,[]);
            var sets=new[]{Set(9,17),Set(3,16),Set(12,16),Set(1,19)};
            Check(GameGearsets.ForJob(sets,16)?.Number==3 && GameGearsets.ForJob(sets,17)?.Number==9 && GameGearsets.ForJob(sets,18) is null,"lowest number");
        }),
        ("Gathering gear set: click only, through the checked equip request",() =>
        {
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GatheringGearset.cs"));
            Check(ui.Contains("if(ImGui.SmallButton(localization.Format(\"GatheringDetails.Equip\",JobName(job))))gearsets.Equip(set.Number);"),"click only");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gear/GameGearsetReader.Dalamud.cs"));
            Check(reader.Contains("MemberFunctionPointers.EquipGearset!=0") && reader.Contains("IsValidGearset(wanted)"),"address and set checked");
            var gathering=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Gathering.cs"));
            Check(gathering.Contains("DrawGatheringGearset(source);"),"on each source");
            // 0.412 (user's choice in GAPS): « Go there » also equips, only with the setting (off by default, in the reset list).
            Check(gathering.Contains("if(configuration.GoThereEquip)EquipGatheringGearset(source);"),"Go there equips with the setting");
            Check(!new TenshoAssistant.Configuration().GoThereEquip && TenshoAssistant.ConfigurationReset.Reset.Contains("GoThereEquip"),"setting off by default and listed");
        }),
    ];
}
