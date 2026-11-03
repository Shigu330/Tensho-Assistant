using TenshoAssistant;
using TenshoAssistant.Gathering;
using TenshoAssistant.Macros;

// 0.330 (queue step e: crafting and gathering audit). Only macros named exactly as Tensho writes them count as Tensho's (the player's
// own « Crafting buffs » is never overwritten with overwriting off); a gathering window that crosses midnight rings once, at its start;
// the macro chain and the gathering plan fixes are pinned in the source (UI code is outside the managed test project).
internal static class CraftGather330Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Macro slots: Tensho's only when named exactly « prefix page/pages »; the player's own macros are kept",() =>
        {
            Check(MacroSlotPlan.IsTenshoName("Tensho","Tensho 2/3") && MacroSlotPlan.IsTenshoName("Tensho"," Tensho 2/3 "),"exact Tensho names");
            foreach (var name in new[] { "Tensho buffs","Tensho 2/3 x","Tensho 02/3","Tensho 4/3","Tensho 0/3","Tensho2/3","TenshoX 2/3" })
                Check(!MacroSlotPlan.IsTenshoName("Tensho",name),"not Tensho's: " + name);
            Check(!MacroSlotPlan.IsTenshoName("Craft","Crafting buffs") && !MacroSlotPlan.IsTenshoName("","1/3") && !MacroSlotPlan.IsTenshoName(null,"Tensho 1/3"),"prefix tests");
            var kept = MacroSlotPlan.Plan(1,new MacroCopySettings { Prefix = "Craft",Start = 80,End = 80 },_ => new(false,"Crafting buffs"));
            Check(kept.Problem == "NoRoom","the player's macro is not overwritten: " + kept.Problem);
            var allowed = MacroSlotPlan.Plan(1,new MacroCopySettings { Prefix = "Craft",Start = 80,End = 80,OverwriteAny = true },_ => new(false,"Crafting buffs"));
            Check(allowed.Slots.SequenceEqual([80]),"overwriting allowed: slot used");
        }),
        ("Gathering schedule: a window across midnight opens once, at its start; labels are translated keys",() =>
        {
            Check(GatheringSchedule.FromEphemeral(2200,400).OpeningMinutes.SequenceEqual([1320]),"ephemeral 22:00–04:00 opens at 22:00 only");
            Check(GatheringSchedule.FromRare([(2300,200),(30,200)]).OpeningMinutes.SequenceEqual([1380]),"rare window joined across midnight");
            Check(GatheringSchedule.FromRare([(0,200)]).OpeningMinutes.SequenceEqual([0]),"a real 00:00 start is kept");
            foreach (var language in new[] { "en","fr" })
            {
                var text = File.ReadAllText(Path.Combine(Source,"Localization","Languages",language + ".json"));
                Check(text.Contains("\"GatheringTime\": {",StringComparison.Ordinal),language + ": schedule texts translated");
            }
        }),
        ("Macro chain and gathering plan: only the pages written are startable; an unread value never becomes a guess",() =>
        {
            var simulation = Read("MainWindow.CraftSimulation.cs");
            Check(simulation.Contains("SequenceEqual(",StringComparison.Ordinal) && simulation.Contains("writtenChain",StringComparison.Ordinal),"Start only under the pages written");
            Check(Read("MainWindow.MacroChain.cs").Contains("macroPlan.Clear();",StringComparison.Ordinal),"the plan is reloaded, not appended");
            var panel = Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("GatheringPanel.Missing",StringComparison.Ordinal),"a missing value is said, not replaced");
        }),
    ];
}
