using System.Text.Json;
using System.Text.RegularExpressions;

// 0.406 (user rule, 2026-11-03): game data in the game's language; Tensho's own texts in the plugin's language with English as the
// original; developer mode covered; the log in English. So no French text is written in the code (outside the language files), except
// the few listed here that read the game's French text or a command typed by the player. Every internal message key has its French and
// English text. A new message: write it in English in the code, and add it to UI/InternalMessages.cs with its two translations.
internal static class Languages406Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // Files whose French strings recognise the game's French text, or a command the player types.
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "Items/CrystalClassification.cs",   // French crystal names of the French client (parsed)
        "Events/Festivals.cs",              // French event names built from the guide NPC (French client only)
        "Alarms/GameAlarmCommand.cs",       // the game's own /alarm command syntax
    };
    private static readonly HashSet<string> AllowedLiterals = ["pêche"]; // « /tensho pêche », typed by players

    private static readonly Regex Literal = new(@"\$?""((?:[^""\\\n]|\\.)*)""", RegexOptions.Compiled);
    private static readonly Regex Accented = new("[éèàêçôîûùÉÈÀÊÇœ]", RegexOptions.Compiled);
    private static readonly Regex FrenchWords = new(@"(?i)(?:^|\s)(lecture|impossible|indisponible|consultez|ouvrez|choisissez|terminez|personnage|rangement|recette|carnet|annulée?|chargement|les|des|une|pour|avec|dans|sans|aucune?)(?=[\s,.;:!?…]|$)", RegexOptions.Compiled);
    private static readonly Regex EnglishWords = new(@"(?i)\b(the|is|are|not|of|and|to|with|was|has|could|cannot|unknown|invalid|missing|available)\b", RegexOptions.Compiled);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Languages: no French text written in the code outside the language files (except text read from the game)",() =>
        {
            var root = Path.GetFullPath("src/TenshoAssistant");
            var found = new List<string>();
            foreach (var path in Directory.EnumerateFiles(root,"*.cs",SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root,path).Replace('\\','/');
                if (relative.StartsWith("Localization/",StringComparison.Ordinal) || relative.StartsWith("bin/",StringComparison.Ordinal)
                    || relative.StartsWith("obj/",StringComparison.Ordinal) || Allowed.Contains(relative)) continue;
                var number = 0;
                foreach (var line in File.ReadLines(path))
                {
                    number++;
                    var code = line.TrimStart();
                    if (code.StartsWith("//",StringComparison.Ordinal)) continue;
                    foreach (Match match in Literal.Matches(line))
                    {
                        var text = match.Groups[1].Value;
                        if (AllowedLiterals.Contains(text)) continue;
                        var french = Accented.IsMatch(text) || FrenchWords.Matches(text).Count >= 2 && !EnglishWords.IsMatch(text);
                        if (french) found.Add($"{relative}:{number}: {text}");
                    }
                }
            }
            Check(found.Count == 0,"French text written in the code (write it in English and translate it through the language files):\n"+string.Join("\n",found.Take(20)));
        }),
        ("Languages: every internal message key has its French and English text",() =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/InternalMessages.cs"));
            var keys = Regex.Matches(code,@"""InternalMessages\.([A-Za-z0-9]+)""").Select(match=>match.Groups[1].Value).ToArray();
            Check(keys.Length >= 100 && keys.Distinct().Count() == keys.Length,"keys listed once");
            foreach (var language in new[]{"fr","en"})
            {
                using var json = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
                var section = json.RootElement.GetProperty("InternalMessages");
                var missing = keys.Where(key=>!section.TryGetProperty(key,out var text) || string.IsNullOrWhiteSpace(text.GetString())).ToArray();
                Check(missing.Length == 0,$"{language}.json misses: {string.Join(", ",missing.Take(10))}");
            }
        }),
    ];
}
