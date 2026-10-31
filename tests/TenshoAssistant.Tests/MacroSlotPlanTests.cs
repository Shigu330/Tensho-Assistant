using TenshoAssistant.Macros;

internal static class MacroSlotPlanTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Crafting macros only go into the chosen range, into empty or Tensho slots unless overwriting is allowed",()=>
        {
            // Slots 80–84: 80 the player's macro, 81 empty, 82 a former Tensho page, 83 empty, 84 the player's macro.
            MacroSlotState Slot(int index) => index switch { 81 or 83 => new(true,""), 82 => new(false,"Tensho 1/2"), _ => new(false,"Mine") };
            var range = new MacroCopySettings { Start = 80, End = 84 };
            var plan = MacroSlotPlan.Plan(3,range,Slot);
            Check(plan.Problem is null && plan.Slots.SequenceEqual([81,82,83]),"skips the player's macros");
            Check(MacroSlotPlan.Plan(4,range,Slot) is { Problem:"NoRoom", Slots.Count:0 },"all pages or none");
            Check(MacroSlotPlan.Plan(4,new MacroCopySettings { Start = 80, End = 84, OverwriteAny = true },Slot).Slots.SequenceEqual([80,81,82,83]),"overwrite allowed: consecutive slots");
            Check(MacroSlotPlan.Plan(1,new MacroCopySettings { Start = 90, End = 80 },Slot).Problem == "Range" && MacroSlotPlan.Plan(1,new MacroCopySettings { Book = 2 },Slot).Problem == "Range","invalid range or book");
            Check(MacroSlotPlan.Name("Tensho",2,3) == "Tensho 2/3" && MacroSlotPlan.Name("Very long prefix name",1,2).Length == 20,"names fit the game's 20 characters");
        }),
    ];
}
