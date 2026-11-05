using TenshoAssistant.Inventory;

// 0.595 (I21): zones for the gear of a job and for the pieces of gear sets. The matching runs through InventorySortRules.Build in the older
// tests; here the rule itself, and what guards a real sort.
internal static class SortGear595Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly System.Reflection.MethodInfo Matches=typeof(InventorySortRules).GetMethod("Matches",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
    private static bool Takes(InventorySortZone zone,InventorySortMetadata label)
    {
        var stack=(InventoryPreviewStack)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(InventoryPreviewStack));
        return (bool)Matches.Invoke(null,[stack,zone,label])!;
    }
    private static InventorySortMetadata Piece(ulong? jobs,bool? inGearset)=>new("Piece",1,Jobs:jobs,InGearset:inGearset);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sort gear: a job takes the gear it can wear, nothing else",() =>
        {
            var paladin=new InventorySortZone("Tank",0,5,Job:19);
            Check(Takes(paladin,Piece(1UL<<19,null)),"its gear");
            Check(!Takes(paladin,Piece(1UL<<20,null)),"another job's");
            Check(!Takes(paladin,Piece(null,null)),"not gear");
            Check(Takes(new InventorySortZone("Any",0,5),Piece(null,null)),"no filter");
        }),
        ("Sort gear: gear sets yes, no, and unread matching neither",() =>
        {
            var yes=new InventorySortZone("Sets",0,5,InGearset:InventoryPropertyFilter.Yes);var no=yes with {InGearset=InventoryPropertyFilter.No};
            Check(Takes(yes,Piece(null,true)) && !Takes(yes,Piece(null,false)),"yes");
            Check(Takes(no,Piece(null,false)) && !Takes(no,Piece(null,true)),"no");
            Check(!Takes(yes,Piece(null,null)) && !Takes(no,Piece(null,null)),"unread: neither");
        }),
        ("Sort gear: older zones keep working, a bad value is refused, and a real sort waits for what it needs",() =>
        {
            var scope=InventorySortScope.Personal(1);
            var profile=InventorySortRules.Default(scope);
            InventorySortRules.Validate(profile with {Zones=[new InventorySortZone("Old",0,5)]},scope);
            try{InventorySortRules.Validate(profile with {Zones=[new InventorySortZone("Bad",0,5,Job:64)]},scope);throw new Exception("a job outside the mask");}
            catch(InvalidOperationException){}
            Check(!InventorySortRules.NeedsGear(profile) && !InventorySortRules.NeedsGearsets(profile),"no zone: nothing needed");
            Check(InventorySortRules.NeedsGear(profile with {Zones=[new InventorySortZone("J",0,5,Job:19)]}),"a job");
            Check(InventorySortRules.NeedsGearsets(profile with {Zones=[new InventorySortZone("S",0,5,InGearset:InventoryPropertyFilter.No)]}),"gear sets");
            Check(!InventorySortRules.NeedsGear(profile with {Zones=[new InventorySortZone("P",0,5,Preserve:true,Job:19)]}),"a protected zone takes nothing by filter");
            var button=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.InventorySortButton.cs"));
            Check(button.Contains("SortButton.GearsetsNotRead") && button.Contains("SortButton.GearNotReady") && button.IndexOf("SortButton.GearsetsNotRead")<button.IndexOf("InventoryVisualSortPlan.Build"),"refused before any plan is made");
        }),
    ];
}
