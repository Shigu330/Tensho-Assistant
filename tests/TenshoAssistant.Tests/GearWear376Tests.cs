using TenshoAssistant.Gear;

// 0.376 (g, M-06): wear and spiritbond — thresholds clamped, unknown readings left out, the alert said once per crossing, the game's
// percentage function called only once resolved.
internal static class GearWear376Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static EquippedPiece Piece(int? condition,int? bond)=>new(GearSlot.Head,1,false,[],null,condition,bond);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gear wear: pieces to repair and to extract against clamped thresholds; unknown readings left out",() =>
        {
            var settings = new GearWearSettings();
            var state = GearWear.Evaluate([Piece(5,10000),Piece(50,4000),Piece(null,null)],settings);
            Check(state==new GearWearState(1,1,5,100),"evaluated");
            Check(GearWear.SpiritbondPercent(12345) is null && GearWear.SpiritbondPercent(5050)==50.5,"spiritbond scale");
            settings.RepairBelow=0;settings.SpiritbondAt=500;
            Check(settings.Repair==1 && settings.Spiritbond==100,"clamped");
            Check(GearWear.Evaluate([],settings)==new GearWearState(0,0,null,null),"nothing worn");
        }),
        ("Gear wear: the alert is said once per crossing and armed again after a repair",() =>
        {
            var tracker = new GearWearAlertTracker();
            var due = new GearWearState(1,0,5,20);var fine = new GearWearState(0,0,100,20);var unknown = new GearWearState(0,0,null,null);
            Check(tracker.Evaluate(due)==(true,false),"first crossing");
            Check(tracker.Evaluate(due)==(false,false),"not repeated");
            Check(tracker.Evaluate(unknown)==(false,false) && tracker.Evaluate(due)==(false,false),"unknown changes nothing");
            tracker.Evaluate(fine);
            Check(tracker.Evaluate(due)==(true,false),"armed again after the repair");
        }),
        ("Gear wear: percentage function checked before the call, each reading guarded, bar off and alert on by default",() =>
        {
            var loader = Read("Gear/GearLoader.Dalamud.cs");
            Check(loader.Contains("if((nint)InventoryItem.MemberFunctionPointers.GetConditionPercentage!=0)condition=item->GetConditionPercentage();",StringComparison.Ordinal)
                && loader.Contains("catch(Exception){bond=null;}",StringComparison.Ordinal),"guarded");
            var defaults = new GearWearSettings();
            Check(defaults.Alert && !defaults.Bar && defaults.Repair==10 && defaults.Spiritbond==100,"defaults");
            Check(TenshoAssistant.ConfigurationReset.Reset.Contains("GearWear"),"reset list");
        }),
    ];
}
