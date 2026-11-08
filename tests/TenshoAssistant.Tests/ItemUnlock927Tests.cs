// X-76 P4 (0.927): the game's item unlock state (IUnlockState) for every unlocking item: hover line, « not obtained only » filter, currency value.
internal static class ItemUnlock927Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Item unlock 927: asked to the game, cached, cut in PvP, used by hover, loot filter and currency value",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("unlocks.IsItemUnlockable(row) ? unlocks.IsItemUnlocked(row):null") && plugin.Contains("if(Competitive.CompetitiveGate.Cut || !client.IsLoggedIn || !player.IsLoaded)return null;"),"asked to the game, cut");
            Check(Read("MainWindow.ItemUnlocks.cs").Contains("now-known.At<5000"),"cached");
            Check(Read("MainWindow.MarketHover.cs").Contains("localization.Format(\"HoverUi.Unlock\"") && Read("MainWindow.LootFilters.cs").Contains("ItemAcquisition(item)==CollectionAcquisition.NotAcquired")
                && Read("MainWindow.CurrencyValue.cs").Contains("GameItemUnlocked(item.Id) is not null"),"used");
        }),
    ];
}
