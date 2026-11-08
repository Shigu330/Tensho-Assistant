using TenshoAssistant.Gathering;

// X-96 (0.959): a zone with a story weather is forecast only once the game confirmed the standard forecast there; the in-game check records it.
internal static class IndividualWeather959Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Individual weather 959: unknown until confirmed, forecast once confirmed, the check uses the standard table",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/WeatherForecast.cs"));
            Check(code.Contains("else if(zone.Individual && !evenIndividual && StoryWeatherConfirmed?.Invoke(territory)!=true)reason="),"unknown until confirmed");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("Forecast(territory,at,1,true)") && plugin.Contains("Gathering.WeatherCatalog.StoryWeatherConfirmed=territory=>configuration.WeatherStoryZones?.Contains(territory)==true;"),"wired");
            Check(TenshoAssistant.ConfigurationReset.Kept.Contains("WeatherStoryZones"),"saved data kept");
        }),
    ];
}
