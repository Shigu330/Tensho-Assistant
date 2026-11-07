using TenshoAssistant.Craft;

// 0.600 (C14): what a recipe asks and allows.
internal static class RecipeFacts600Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static CraftRecipeParameters Base()=>new(80,100,100,100,100,100,100,0,0,0);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Recipe facts: HQ is always said, the rest only when it applies",() =>
        {
            Check(RecipeFacts.Of(Base(),_=>null) is [("NoHq","")],"no HQ");
            Check(RecipeFacts.Of(Base() with {CanHq=true},_=>null) is [("Hq","")],"HQ");
        }),
        ("Recipe facts: quick synthesis, master book, specialist, expert, item and status",() =>
        {
            var facts=RecipeFacts.Of(Base() with {QuickSynth=true,QuickSynthCraftsmanship=1000,QuickSynthControl=800,MasterBook="Book X",Specialist=true,Expert=true,RequiredItem=7,RequiredStatus="Focus"},id=>id==7 ? "Tool":null);
            Check(facts.SequenceEqual([("NoHq",""),("QuickFrom","1000|800"),("Book","Book X"),("Specialist",""),("Expert",""),("Item","Tool"),("Status","Focus")]),"each one");
            Check(RecipeFacts.Of(Base() with {QuickSynth=true},_=>null).Contains(("Quick","")),"quick without a minimum");
            Check(RecipeFacts.Of(Base() with {RequiredItem=9},_=>null).Contains(("Item","#9")),"an item not known yet: its number, not nothing");
        }),
        ("Recipe facts: from the sheet, on the item sheet and the simulator; the catalogue cache follows the shape",() =>
        {
            var copy=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Craft/RecipeSimulationCopy.Lumina.cs"));
            Check(copy.Contains("row.CanQuickSynth") && copy.Contains("row.SecretRecipeBook") && copy.Contains("row.ItemRequired") && copy.Contains("row.StatusRequired"),"the game's fields");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Items.cs")).Contains("DrawRecipeFacts(recipe);"),"item sheet");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftReadiness.cs")).Contains("DrawRecipeFacts(recipe);"),"simulator");
        }),
    ];
}
