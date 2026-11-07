using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Items;

// 0.327 (queue step e, settings that need no decision): number of item search results; opening page (Home or the last page used).
internal static class Settings327Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));
    private static string Text(string language,string key)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Source,"Localization","Languages",language + ".json")));
        var node = document.RootElement;
        foreach (var part in key.Split('.'))
            if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(part,out node)) return "";
        return node.ValueKind == JsonValueKind.String ? node.GetString() ?? "" : "";
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Item search limit: 30 by default, bounded 10–100 when read, never outside what the catalogue accepts, reset with the settings",() =>
        {
            var fresh = new Configuration();
            Check(fresh.ItemSearchLimit == 30 && ConfigurationReset.Reset.Contains("ItemSearchLimit"),"default 30, reset with the settings");
            Check(Configuration.BoundedItemSearchLimit(int.MinValue) == 10 && Configuration.BoundedItemSearchLimit(0) == 10
                && Configuration.BoundedItemSearchLimit(55) == 55 && Configuration.BoundedItemSearchLimit(int.MaxValue) == 100,"bounds 10–100");
            var catalog = ItemCatalog.Build(Enumerable.Range(1,200).Select(i => new ItemDefinition((uint)i,$"Objet {i:D3}",false)),
                new ItemCatalogMetadata("test/Item","French","game=test;schema=1",1));
            Check(catalog.Search("objet",Configuration.BoundedItemSearchLimit(fresh.ItemSearchLimit)).Count == 30,"default count unchanged");
            Check(catalog.Search("objet",Configuration.BoundedItemSearchLimit(5000)).Count == 100
                && catalog.Search("objet",Configuration.BoundedItemSearchLimit(-3)).Count == 10,"a hand-edited value never throws");
        }),
        ("Item search page: every search uses the setting, a new value searches again, the « at most » notice and the texts follow it",() =>
        {
            var items = Read("MainWindow.Items.cs");
            Check(!items.Contains("Count == 30",StringComparison.Ordinal),"no fixed 30 left in the notice");
            Check(!Regex.IsMatch(items,@"catalog\.Search\(itemQuery\)"),"every search passes the limit");
            Check(items.Contains("searchedLimit != limit",StringComparison.Ordinal),"a new limit searches again");
            Check(items.Contains("localization.Format(\"ItemsUi.Limit\", searchedLimit)",StringComparison.Ordinal)
                && items.Contains("localization.Format(\"ItemsUi.QueryTip\", ItemSearchLimit)",StringComparison.Ordinal),"notice and tooltip show the limit");
            Check(Read("MainWindow.Settings.cs").Contains("IntSetting(\"Settings.ItemSearchLimit\",\"SettingTips.ItemSearchLimit\"",StringComparison.Ordinal),"shared row with « ↺ »");
            foreach (var language in new[] { "en","fr" })
            {
                foreach (var key in new[] { "ItemsUi.Limit","ItemsUi.QueryTip" })
                    Check(Text(language,key).Contains("{0}",StringComparison.Ordinal) && !Text(language,key).Contains("30",StringComparison.Ordinal),language + " " + key + ": the limit, not a fixed 30");
                Check(Text(language,"Settings.ItemSearchLimit").Length > 0,language + ": label");
            }
            Check(Text("fr","SettingTips.ItemSearchLimit").EndsWith("\nPar défaut : 30.",StringComparison.Ordinal)
                && Text("en","SettingTips.ItemSearchLimit").EndsWith("\nDefault: 30.",StringComparison.Ordinal),"tooltip ends with the default");
        }),
        ("Opening page: Home by default as before; the last page used only when asked, written on close or unload, never per page change",() =>
        {
            var fresh = new Configuration();
            Check(!fresh.OpenOnLastPage && fresh.LastPage is null,"off by default, nothing remembered");
            Check(ConfigurationReset.Reset.Contains("OpenOnLastPage") && ConfigurationReset.Reset.Contains("LastPage"),"reset with the settings");
            var window = Read("MainWindow.cs");
            var built = window.IndexOf("page = InitialPage();",StringComparison.Ordinal); var developer = window.IndexOf("developerMode = configuration.DeveloperMode;",StringComparison.Ordinal);
            Check(built > 0 && developer > 0 && developer < built,"page chosen when the window is built, after developer mode is known");
            var initial = Regex.Match(window,@"private Page InitialPage\(\)[\s\S]*?\n    \}").Value;
            Check(initial.Contains("!configuration.OpenOnLastPage",StringComparison.Ordinal) && initial.Contains("return Page.Home;",StringComparison.Ordinal),"Home when off");
            Check(initial.Contains("Enum.IsDefined(saved)",StringComparison.Ordinal) && initial.Contains("saved==Page.Diagnostics && !developerMode",StringComparison.Ordinal),"unknown name, or Diagnostics without developer mode: Home");
            var remember = Regex.Match(window,@"private void RememberPage\(\)[\s\S]*?\n    \}").Value;
            Check(remember.Contains("if(!configuration.OpenOnLastPage",StringComparison.Ordinal) && !remember.Contains("save(",StringComparison.Ordinal),"nothing when off, no disk write per page change");
            Check(Regex.IsMatch(window,@"private void DrawMain\(\)\s*\{\s*RememberPage\(\);"),"remembered at the start of Draw");
            Check(window.Contains("public override void OnClose()=>SaveLastPage();",StringComparison.Ordinal)
                && Read("MainWindow.Lifetime.cs").Contains("SaveLastPage();",StringComparison.Ordinal),"written when the window closes or the plugin unloads");
            Check(Read("MainWindow.Settings.cs").Contains("BoolSetting(\"Settings.OpenOnLastPage\",\"SettingTips.OpenOnLastPage\"",StringComparison.Ordinal),"shared row with « ↺ »");
            Check(Text("fr","SettingTips.OpenOnLastPage").EndsWith("\nPar défaut : désactivé (Accueil).",StringComparison.Ordinal)
                && Text("en","SettingTips.OpenOnLastPage").EndsWith("\nDefault: off (Home).",StringComparison.Ordinal),"tooltip ends with the default");
        }),
    ];
}
