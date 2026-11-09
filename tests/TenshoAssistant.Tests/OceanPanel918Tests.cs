// X-85 V2 (0.918): the ocean voyage panel draws its fish as the usual fishing help (icons, bait in hand, bite, bite time of the spot)
// and shows the spectral current's fish of the stop, folded, while the current is not there.
internal static class OceanPanel918Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean panel 918: fish table like the fishing help, spectral fish of the stop, stopwatch, keys in both languages",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs"));
            Check(panel.Contains("private void DrawOceanFishTable(string id,IReadOnlyList<uint> fishList,uint spotId,uint? inHand,int? elapsed"),"shared table");
            Check(panel.Contains("var spotId=stop.Spectral ? stop.Spot.SpectralSpot:stop.Spot.MainSpot;") && panel.Contains("biteTimes?.Get(spotId)"),"bite times of the spot fished");
            Check(panel.Contains("DrawOceanFishTable(\"OceanSpectralFish\",spectralFish,stop.Spot.SpectralSpot,inHand,null,") && panel.Contains("if(!stop.Spectral && stop.Spot.SpectralSpot!=0"),"spectral fish folded outside the current");
            Check(panel.Contains("inHand==row.Route[0]") && panel.Contains("ItemCell(fish,fishId,22"),"icons and bait in hand");
            Check(panel.Contains("oceanFishingSince=DateTimeOffset.UtcNow"),"stopwatch");
            foreach(var lang in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{lang}.json"));
                Check(text.Contains("\"SpectralFish\": ") && text.Contains("\"SpectralFishTip\": ") && text.Contains("\"BiteTime\": "),"keys "+lang);
            }
        }),
    ];
}
