using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Items;
using TenshoAssistant.Market;

// 0.327 (queue step e, settings that need no decision, audit 2026-11-02): Market page scope saved, item hover panel hidden in duties,
// the four parts of that panel. Every default keeps the 0.326 behaviour.
internal static class Hover327Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));
    // A language line, trimmed and without its final comma (escapes kept: a new line reads « \n »).
    private static string Text(string code,string key) =>
        File.ReadLines(Path.Combine(Source,"Localization","Languages",code + ".json")).Select(line => line.Trim().TrimEnd(','))
            .FirstOrDefault(line => line.StartsWith("\"" + key + "\": ",StringComparison.Ordinal)) ?? "";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Market page scope: data centre by default and for older files, saved, reset; the page and Settings share it",() =>
        {
            var fresh = new Configuration();
            Check(fresh.MarketBoardScope == MarketScope.DataCenter && ConfigurationReset.Reset.Contains("MarketBoardScope"),"default data centre, reset with the settings");
            Check(JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!.MarketBoardScope == MarketScope.DataCenter,"older file: data centre");
            fresh.MarketBoardScope = MarketScope.World;
            Check(JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(fresh))!.MarketBoardScope == MarketScope.World,"choice kept");
            var page = Read("MainWindow.MarketBoard.cs");
            Check(!page.Contains("marketScope",StringComparison.Ordinal),"no window-only scope left");
            Check(page.Contains("Enum.IsDefined(configuration.MarketBoardScope) ? configuration.MarketBoardScope:MarketScope.DataCenter",StringComparison.Ordinal),"unknown saved value: data centre");
            Check(page.Contains("MarketScopeShown==scope))ChooseMarketScope(scope);",StringComparison.Ordinal),"a click on the page saves it");
            var settings = Read("MainWindow.Settings.cs");
            Check(settings.Contains("Tip(\"Settings.MarketScopeTip\");",StringComparison.Ordinal) && settings.Contains("DefaultButton(\"MarketBoardScope\",",StringComparison.Ordinal),"Settings row with tooltip and ↺");
            Check(Text("en","MarketScopeTip").EndsWith("\\nDefault: data center.\"",StringComparison.Ordinal) && Text("fr","MarketScopeTip").EndsWith("\\nPar défaut : centre de données.\"",StringComparison.Ordinal),"tips end with the default");
        }),
        ("Hover hidden in duties: off by default, reset; in a duty nothing is observed, so no panel and no price request",() =>
        {
            var fresh = new Configuration();
            Check(!fresh.HoverHideInDuty && ConfigurationReset.Reset.Contains("HoverHideInDuty"),"off by default, reset with the settings");
            var reader = Read("Items/ItemHoverReader.Dalamud.cs");
            Check(reader.Contains("var hidden=configuration.HoverHideInDuty && InDuty();",StringComparison.Ordinal)
                && reader.Contains("owner!=0 && !hidden && !gui.GameUiHidden ? gui.HoveredItem:0;",StringComparison.Ordinal),"the reader stops in a duty");
            Check(Read("Plugin.cs").Contains("{ InDuty = () => mapLive?.State is { InDuty: true },",StringComparison.Ordinal),"same detection as the map's « Hide in duties »");
            Check(Read("MainWindow.Settings.cs").Contains("BoolSetting(\"Settings.HoverHideInDuty\",\"Settings.HoverHideInDutyTip\",",StringComparison.Ordinal),"shared row (tooltip and ↺)");
            Check(Text("en","HoverHideInDutyTip").EndsWith("\\nDefault: off.\"",StringComparison.Ordinal) && Text("fr","HoverHideInDutyTip").EndsWith("\\nPar défaut : désactivé.\"",StringComparison.Ordinal),"tips end with the default");
            // Managed half of the reader in a duty: « nothing hovered » clears the item, so the panel is not drawn and nothing is asked.
            using var http = new HttpClient(new MarketTestHandler((_,_) => throw new Exception("unexpected HTTP")));
            using var market = new MarketService(http);
            using var context = new ItemHoverService(); using var coordinator = new ItemHoverCoordinator(context,market);
            var catalog = ItemCatalog.Build([new(42,"Marketable",true,IsMarketable:true)],new("fixture","French","v",1));
            var now = DateTimeOffset.UtcNow;
            coordinator.Observe(false,true,7,71,42,catalog,now);
            Check(context.State.Fresh(now),"hovered outside a duty");
            Check(coordinator.Observe(true,true,7,71,0,catalog,now) is null && context.State.Item is null && !context.State.Fresh(now),"in a duty: no item, no panel, no request");
        }),
        ("Hover panel parts: all four on by default and for older files, reset; only the panel under the game's tooltip follows them",() =>
        {
            var fresh = new Configuration(); var legacy = JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
            foreach (var name in new[] { "HoverShowCollection","HoverShowStock","HoverShowUsages","HoverShowSources" })
            {
                var property = typeof(Configuration).GetProperty(name)!;
                Check((bool)property.GetValue(fresh)! && (bool)property.GetValue(legacy)!,"on by default: " + name);
                Check(ConfigurationReset.Reset.Contains(name),"reset with the settings: " + name);
            }
            var hover = Read("MainWindow.MarketHover.cs");
            Check(hover.Contains("DrawItemInfoBody(item,hovered.ItemId,hoverPanel:true);",StringComparison.Ordinal),"the hover panel follows the parts");
            Check(hover.Contains("            DrawItemInfoBody(item,id);",StringComparison.Ordinal) && Read("MainWindow.ItemSheet.cs").Contains("try{DrawItemInfoBody(item,id);}",StringComparison.Ordinal),"Tensho's item tooltip and the item sheet keep every part");
            Check(hover.Contains("if(hoverPanel && !configuration.HoverShowSources)return;",StringComparison.Ordinal),"no « How to get it » (nor its Garland / wiki requests) when hidden");
            Check(hover.Contains("observed.Stable(now) && HoverInfoShown(item,hovered.ItemId);",StringComparison.Ordinal),"no empty panel when nothing is left to draw");
            var settings = Read("MainWindow.Settings.cs");
            foreach (var key in new[] { "HoverCollection","HoverStock","HoverUsages","HoverSources" })
            {
                Check(settings.Contains("BoolSetting(\"Settings." + key + "\",\"Settings." + key + "Tip\",",StringComparison.Ordinal),"shared row (tooltip and ↺): " + key);
                Check(Text("en",key + "Tip").EndsWith("\\nDefault: on.\"",StringComparison.Ordinal) && Text("fr",key + "Tip").EndsWith("\\nPar défaut : activé.\"",StringComparison.Ordinal),"tip ends with the default: " + key);
            }
        }),
    ];
}
