using System.Text.RegularExpressions;
using TenshoAssistant.UI;

// 0.1038 (night tests of 2026-11-10, T-0.502-1, T-0.912-1): every character a text of Tensho shows must exist in the game's AXIS font
// (Dalamud's default font and the body font), or ImGui draws Dalamud's fallback mark « 〓 », which reads as « = » (the « ↺ » of the
// Settings help). The allowed set beyond Latin-1 is proven by tools/UldProbe --fdt (docs/evidence/0.1038-game-font-glyphs.md). The
// sidebar category titles follow the theme's text colour (they were pale in the light theme), and the « Settings › … » paths of the
// texts name tabs that exist (the « Interface » and « Modules » tabs were merged into others in 0.999).
internal static class FontGlyphs1038Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    // Proven in AXIS_12/14/18.fdt beyond printable ASCII and Latin-1 (U+00A0–U+00FF, all present).
    private const string Extra="ŒœΣ–—‘’“”…›‹←↑→↓≠≤≥●○◆★－";
    private static bool Allowed(char character,bool japanese)
        =>character is '\r' or '\n' or '\t' || character>=' ' && character<='~' || character>=' ' && character<='ÿ' || Extra.Contains(character)
          || japanese && (character>='　' && character<='ヿ' || character>='一' && character<='鿿' || character>='＀' && character<='￯');

    // Texts that leave the game: sent to a Discord channel, where emoji are drawn.
    private static readonly string[] SentElsewhere=["    \"DefaultTemplate\": ","    \"TestMessage\": "];
    // Not shown: Universalis region names put in a web address.
    private static readonly string[] NotShownFiles=["MarketBoardService.cs"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game font: every character of the language files exists in the game's AXIS font (no « ↺ », « ≈ », « ⌊ », « ⚠ »)",() =>
        {
            foreach(var language in new[]{"en","fr","de","ja"})
            {
                var number=0;
                foreach(var line in File.ReadAllLines(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")))
                {
                    number++;
                    if(SentElsewhere.Any(prefix=>line.StartsWith(prefix,StringComparison.Ordinal)))continue;
                    var missing=line.Where(character=>!Allowed(character,language=="ja")).Distinct().ToArray();
                    Check(missing.Length==0,$"{language}.json line {number}: « {new string(missing)} » is missing from the game font");
                }
            }
        }),
        ("Game font: no string literal of the plugin's code uses a character missing from the game font",() =>
        {
            var literal=new Regex("\"(?:[^\"\\\\\\n]|\\\\.)*\"");
            foreach(var path in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories))
            {
                if(path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))continue;
                if(NotShownFiles.Contains(Path.GetFileName(path)))continue;
                var number=0;
                foreach(var line in File.ReadAllLines(path))
                {
                    number++;
                    if(line.TrimStart().StartsWith("//",StringComparison.Ordinal))continue;
                    foreach(Match match in literal.Matches(line))
                    {
                        var missing=match.Value.Where(character=>!Allowed(character,false)).Distinct().ToArray();
                        Check(missing.Length==0,$"{Path.GetFileName(path)}:{number}: « {new string(missing)} » is missing from the game font");
                    }
                }
            }
        }),
        ("Game font: the reset button is named in words in the help texts, and the warning marks are icons",() =>
        {
            foreach(var language in new[]{"en","fr"})
            {
                var texts=Source($"Localization/Languages/{language}.json");
                Check(!texts.Contains('↺') && !texts.Contains('⚠'),$"{language}: glyph in a text");
            }
            Check(Source("Localization/Languages/en.json").Contains("the reset button (curved arrow)",StringComparison.Ordinal),"en: reset button named");
            Check(Source("Localization/Languages/fr.json").Contains("le bouton de retour au défaut (flèche courbe)",StringComparison.Ordinal),"fr: reset button named");
            var vessels=Source("MainWindow.VesselBuilds.cs");
            Check(vessels.Contains("FontAwesomeIcon.ExclamationTriangle.ToIconString()",StringComparison.Ordinal) && !vessels.Contains("VesselBuilds.Problem",StringComparison.Ordinal),"vessel problem mark from the icon font");
        }),
        ("Light theme: sidebar category titles and the context item header use the theme's text colour, readable on every palette",() =>
        {
            var navigation=Source("UI/Navigation/MainWindow.CategoryNavigation.cs");
            Check(navigation.Contains("ImGui.PushStyleColor(ImGuiCol.Text,collapsed && holdsPage ? Accent:Text);",StringComparison.Ordinal),"category title colour");
            Check(!navigation.Contains("? Accent:Muted",StringComparison.Ordinal),"muted category title");
            var window=Source("MainWindow.cs");
            Check(window.Contains("ImGui.TextColored(Text,localization.Get(\"Navigation.ContextItem\"));",StringComparison.Ordinal),"context item header colour");
            Check(!window.Contains("ImGui.TextDisabled(localization.Get(\"Navigation.ContextItem\"))",StringComparison.Ordinal),"muted context item header");
            foreach(var (theme,palette) in ThemePalettes.All)
            {
                Check(ThemePalettes.Contrast(palette.Text,palette.Background)>=7 && ThemePalettes.Contrast(palette.Text,palette.Panel)>=7,$"{theme}: title text");
                Check(ThemePalettes.Contrast(palette.Accent,palette.Background)>=4.5 && ThemePalettes.Contrast(palette.Accent,palette.Panel)>=4.5,$"{theme}: highlighted title");
            }
        }),
        ("Settings paths: every « Settings › … » of the texts names an existing tab (no « Interface », « Modules » or « Crafting » tab)",() =>
        {
            var tabs=Regex.Matches(Source("MainWindow.Settings.cs"),"SettingsTab\\(\"(\\w+)\"").Select(match=>match.Groups[1].Value).ToArray();
            Check(tabs.Contains("General") && tabs.Contains("Panels") && !tabs.Contains("Modules") && !tabs.Contains("Interface"),"tabs read");
            foreach(var (language,word) in new[]{("en","Settings"),("fr","Paramètres")})
            {
                using var document=System.Text.Json.JsonDocument.Parse(Source($"Localization/Languages/{language}.json"));
                var sections=document.RootElement.GetProperty("SettingsSections");
                var first=tabs.Select(tab=>sections.GetProperty(tab).GetString()!.Split(' ')[0]).ToHashSet();
                var texts=Source($"Localization/Languages/{language}.json");
                foreach(Match match in Regex.Matches(texts,$"(?<!(?:Dalamud|github\\.com) › ){word} › (\\w+)"))
                    Check(first.Contains(match.Groups[1].Value),$"{language}: « {match.Value} » names no tab");
            }
        }),
        ("Tool details: the reference line is translated (Tensho's own tool, or the reference plugin)",() =>
        {
            var tools=Source("MainWindow.Tools.cs");
            Check(tools.Contains("localization.Get(\"ToolDetails.OwnTool\")",StringComparison.Ordinal) && tools.Contains("localization.Format(\"ToolDetails.Reference\",tool.Reference)",StringComparison.Ordinal),"translated reference");
            Check(!tools.Contains("ImGui.TextDisabled(tool.Reference);",StringComparison.Ordinal),"raw reference");
        }),
    ];
}
