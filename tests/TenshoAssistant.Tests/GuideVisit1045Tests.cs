using System.Text.RegularExpressions;
using TenshoAssistant.Guide;

// 0.1045 (X-122, user decision D-132 of 2026-11-10): the pages left out of the guide's visit become stops, filtered by interests; only Home
// (where the guide starts and ends) and Diagnostics (developer mode) stay out.
internal static class GuideVisit1045Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path));
    private static HashSet<string> Pages(IReadOnlySet<GuideInterest>? chosen)=>GuideFlow.Stops(chosen).Select(stop=>stop.Page).ToHashSet();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Guide visit 1045: every page but Home and Diagnostics is a stop, each once",() =>
        {
            var pages=Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"),@"enum Page \{([^}]*)\}").Groups[1].Value
                .Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
            var all=GuideFlow.EveryStop.Select(stop=>stop.Page).ToArray();
            Check(all.Length==all.Distinct().Count(),"a page is a stop twice");
            Check(GuideFlow.LeftOutOfVisit.Keys.OrderBy(key=>key).SequenceEqual(new[]{"Diagnostics","Home"}),"only Home and Diagnostics stay out");
            foreach(var page in pages.Where(page=>page is not "Home" and not "Diagnostics"))Check(all.Contains(page),"not a stop: "+page);
            Check(all.Length==pages.Length-2,"stops and pages agree");
        }),
        ("Guide visit 1045: each interest keeps its own stops and the common ones, and drops the others",() =>
        {
            var everyone=GuideFlow.EveryStop.Select(stop=>stop.Page).ToHashSet();
            foreach(var interest in Enum.GetValues<GuideInterest>())
            {
                var kept=Pages(new HashSet<GuideInterest>{interest});
                Check(kept.Count<everyone.Count,interest+" drops something");
                Check(GuideFlow.EveryStop.Any(stop=>stop.Interest==interest),interest+" has a stop of its own");
                Check(kept.Contains("Items") && kept.Contains("Maps") && kept.Contains("Lexicon") && kept.Contains("Help"),interest+" keeps the common stops");
            }
            var craft=Pages(new HashSet<GuideInterest>{GuideInterest.Crafting});
            Check(craft.Contains("CraftLive") && craft.Contains("MacroChain") && !craft.Contains("Leves") && !craft.Contains("Garden"),"crafting");
            var content=Pages(new HashSet<GuideInterest>{GuideInterest.Content});
            Check(content.Contains("Leves") && content.Contains("DeepDungeons") && content.Contains("Beastmaster") && content.Contains("DutyLoot") && !content.Contains("CraftLive"),"content");
            var saucer=Pages(new HashSet<GuideInterest>{GuideInterest.GoldSaucer});
            Check(saucer.Contains("Fashion") && saucer.Contains("MiniCactpot") && !saucer.Contains("TripleTriad"),"Gold Saucer");
            var island=Pages(new HashSet<GuideInterest>{GuideInterest.GardenAndIsland});
            Check(island.Contains("Garden") && island.Contains("IslandSanctuary") && !island.Contains("Fashion"),"garden and island");
            var both=Pages(new HashSet<GuideInterest>{GuideInterest.Crafting,GuideInterest.Content});
            Check(both.Contains("CraftLive") && both.Contains("Leves"),"two interests add up");
        }),
        ("Guide visit 1045: a position saved with the old, shorter visit still comes back inside it",() =>
        {
            var count=GuideFlow.EveryStop.Count;
            Check(count>=49,"the visit has its new stops");
            Check(GuideFlow.Valid((int)GuideStep.Visit,16,count)==new GuidePosition(GuideStep.Visit,16),"an old position");
            Check(GuideFlow.Valid((int)GuideStep.Visit,count+5,count)==new GuidePosition(GuideStep.Visit,count-1),"beyond the end");
        }),
    ];
}
