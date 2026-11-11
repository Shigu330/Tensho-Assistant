// 0.1109 (X-130, user request of 2026-11-11): what each planted bed may give, from the journal, by the wiki's rule and the community pairs.
using TenshoAssistant.Reminders;

internal static class GardenForecast1109Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden forecast: neighbours right, down, up, left, never the centre or a diagonal",() =>
        {
            Check(GardenForecasts.Neighbours(1).SequenceEqual([2,8]),"bed 1 (top left): right, down");
            Check(GardenForecasts.Neighbours(2).SequenceEqual([3,1]),"bed 2 (top middle): right, left; the centre is no bed");
            Check(GardenForecasts.Neighbours(5).SequenceEqual([4,6]),"bed 5 (bottom right): up, left");
        }),
        ("Garden forecast: the first compatible neighbour already there; later, same or unpaired neighbours skipped; unknown said",() =>
        {
            var data=new GardenCrossData([new(100,10,20),new(101,10,20),new(102,10,30)],[]);
            var pairs=GardenForecasts.Index(data);
            Check(pairs[(10,20)].SequenceEqual([100u,101u]) && pairs[(10,30)].SequenceEqual([102u]),"pairs, lower id first");
            var t=new DateTimeOffset(2026,11,11,20,0,0,TimeSpan.Zero);
            bool Known(uint seed)=>data.Seeds.Contains(seed);
            // Bed 1 first (10), bed 2 next (20): bed 2 crosses with bed 1. Bed 3 (10) later: its right neighbour... bed 3's are 4 (down) and 2 (left).
            var forecasts=GardenForecasts.For([(1,10u,t),(2,20u,t.AddMinutes(1)),(3,10u,t.AddMinutes(2)),(8,99u,t.AddMinutes(3)),(7,30u,t.AddMinutes(4))],pairs,Known);
            Check(forecasts[0].Kind==GardenForecastKind.NoPartner,"bed 1, planted first, has no partner");
            Check(forecasts[1] is { Kind:GardenForecastKind.Crosses,Partner:1,PartnerSeed:10 } && forecasts[1].Outcomes.SequenceEqual([100u,101u]),"bed 2 crosses with bed 1");
            Check(forecasts[2] is { Kind:GardenForecastKind.Crosses,Partner:2 },"bed 3 crosses with bed 2 (on its left), planted before it");
            Check(forecasts.Single(f=>f.Bed==8).Kind==GardenForecastKind.UnknownSeed,"an unknown seed is said");
            Check(forecasts.Single(f=>f.Bed==7) is { Kind:GardenForecastKind.UnknownNeighbour,Partner:8 },"bed 7 (30): its right neighbour, bed 6, is empty; up is bed 8, unknown: not guessed");
            var same=GardenForecasts.For([(1,10u,t),(2,10u,t.AddMinutes(1))],pairs,Known);
            Check(same[1].Kind==GardenForecastKind.NoPartner,"the same seed does not cross");
            var unnamed=GardenForecasts.For([(1,null,t)],pairs,Known);
            Check(unnamed[0].Kind==GardenForecastKind.UnknownSeed,"a name that matches no seed");
        }),
        ("Garden forecast: drawn under each planted bed of a patch, with its source and the seeds not held or wished",() =>
        {
            var garden=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Garden.cs")).Replace("\r\n","\n");
            Check(garden.Contains("var forecasts=GardenGridForecasts(planted);") && garden.Contains("DrawGardenForecast(forecast,planted);"),"under each bed");
            Check(garden.Contains("if(forecastShown)ImGui.TextDisabled(localization.Get(\"Garden.ForecastSource\"));"),"the source said");
            var crosses=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GardenCrosses.cs")).Replace("\r\n","\n");
            Check(crosses.Contains("configuration.Wishlist?.Contains(seed)==true || GetRecipeHoldings(seed) is { HasObservation:true,Total:0 }"),"not held or wished put forward");
            Check(crosses.Contains("if(gardenForecastIndex is not { } index || !ReferenceEquals(index.Data,data))"),"index built once per catalogue");
        }),
    ];
}
