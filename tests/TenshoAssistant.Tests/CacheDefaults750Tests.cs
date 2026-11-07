using TenshoAssistant.Caching;

// 0.750 (X-02): longer cache defaults, applied once to saved settings.
internal static class CacheDefaults750Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Cache defaults: longer for a new install; prices and the Fashion Report keep theirs",() =>
        {
            var fresh=new CachePreferences();
            Check(fresh.Quests==CacheRetention.OneHour && fresh.Collections==CacheRetention.SevenDays && fresh.Sources==CacheRetention.Never && fresh.CraftCalculations==CacheRetention.SevenDays,"longer");
            Check(fresh.Prices==CacheRetention.OneDay && fresh.Fashion==CacheRetention.SevenDays,"kept");
        }),
        ("Cache defaults: saved old defaults move once; a choice of the player stays",() =>
        {
            var old=new CachePreferences{Quests=CacheRetention.FiveMinutes,Collections=CacheRetention.OneDay,Sources=CacheRetention.SevenDays,CraftCalculations=CacheRetention.OneDay};
            Check(old.LongerDefaults() && old.Quests==CacheRetention.OneHour && old.Sources==CacheRetention.Never,"moved");
            var chosen=new CachePreferences{Quests=CacheRetention.OneDay,Collections=CacheRetention.OneHour,Sources=CacheRetention.OneHour,CraftCalculations=CacheRetention.FiveMinutes};
            Check(!chosen.LongerDefaults() && chosen.Quests==CacheRetention.OneDay && chosen.Sources==CacheRetention.OneHour,"kept");
            Check(Read("Plugin.cs").Contains("if (!configuration.CacheLongerDefaults) { configuration.Cache.LongerDefaults(); configuration.CacheLongerDefaults = true; }"),"once");
            Check(Read("Localization/Languages/fr.json").Contains("\"Never\": \"Seulement si absent, ou sur Actualiser\""),"the word says what it does");
        }),
    ];
}
