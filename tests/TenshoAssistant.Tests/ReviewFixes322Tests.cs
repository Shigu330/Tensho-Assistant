using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

// 0.322 (queue step d, review 0.316 point 7): translated storage names, crystals on any client, and the tables that turn the services'
// French sentences into translated keys kept in step with the sentences the code really emits.
internal static class ReviewFixes322Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Message tables: every sentence mapped to a key is one the code emits, and every key exists in English and French",()=>
        {
            var files = Directory.EnumerateFiles(Source,"*.cs",SearchOption.AllDirectories).Where(file => !Path.GetRelativePath(Source,file).Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Any(part => part is "obj" or "bin")).ToArray();
            using var english = JsonDocument.Parse(File.ReadAllText(Path.Combine(Source,"Localization","Languages","en.json")));
            using var french = JsonDocument.Parse(File.ReadAllText(Path.Combine(Source,"Localization","Languages","fr.json")));
            static bool Has(JsonDocument document,string key)
            {
                var node = document.RootElement;
                foreach (var part in key.Split('.')) if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(part,out node)) return false;
                return node.ValueKind == JsonValueKind.String;
            }
            var pair = new Regex("^\\s*\"((?:[^\"\\\\]|\\\\.)+)\"\\s*=>\\s*\"([A-Za-z]+\\.[A-Za-z0-9.]+)\"");
            var stale = new List<string>(); var missingKeys = new List<string>(); var checkedCount = 0;
            foreach (var table in new[] { Path.Combine(Source,"UI","UiMessages.cs"),Path.Combine(Source,"MainWindow.CraftMessages.cs") })
            {
                var others = string.Join("\n",files.Where(file => file != table).Select(File.ReadAllText));
                foreach (var line in File.ReadAllLines(table))
                {
                    var match = pair.Match(line); if (!match.Success) continue;
                    checkedCount++;
                    var sentence = match.Groups[1].Value; var key = match.Groups[2].Value;
                    if (!others.Contains("\"" + sentence + "\"",StringComparison.Ordinal)) stale.Add($"{Path.GetFileName(table)}: {sentence}");
                    if (!Has(english,key) || !Has(french,key)) missingKeys.Add(key);
                }
            }
            Check(checkedCount >= 40,"message tables found: " + checkedCount);
            Check(stale.Count == 0,"mapped sentences no service emits (changed wording falls back to the generic message): " + string.Join(" | ",stale));
            Check(missingKeys.Count == 0,"keys missing in en/fr: " + string.Join(", ",missingKeys));
        }),
        ("Storage rows carry a label for their translated name; only a retainer keeps its own name",()=>
        {
            var builder = File.ReadAllText(Path.Combine(Source,"Inventory","ItemHoldings.cs"));
            foreach (var literal in new[] { "Sacs du personnage","Cristaux","Sacoche","Servant","non importé","Arsenal","Coffre","Armoire" })
                Check(!builder.Contains("\"" + literal,StringComparison.Ordinal),"no French storage name built in ItemHoldings: " + literal);
            var localization = new TenshoAssistant.Localization.LocalizationManager((error,message) => throw new InvalidOperationException(message,error));
            localization.SetLanguage("en");
            var armoury = StorageNames.Of(localization,BankStorageKind.Armoury,"Arsenal");
            Check(armoury.Length > 0 && armoury != "Arsenal" && !armoury.StartsWith('['),"the saved French name is not shown: " + armoury);
            Check(StorageNames.Of(localization,BankStorageKind.Retainer,"Mira").Contains("Mira",StringComparison.Ordinal),"a retainer keeps its game name");
            Check(StorageNames.Of(localization,new StorageHolding(StorageCategory.Retainer,"",InventoryCoverage.Unknown,false,null,0,0) { Label = StorageLabel.RetainersMissing }) == "Retainers not imported","placeholder row translated");
        }),
        ("The plugin manifest names every network source of the Credits page",()=>
        {
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Source,"TenshoAssistant.json")));
            var description = manifest.RootElement.GetProperty("Description").GetString() ?? "";
            var words = new Dictionary<string,string> { ["Universalis"] = "Universalis", ["GarlandTools"] = "Garland", ["ConsoleGamesWiki"] = "Wiki",
                ["LuminaSupplemental"] = "LuminaSupplemental", ["GatherBuddy"] = "GatherBuddy", ["FFXIVTeamcraft"] = "Teamcraft", ["Discord"] = "Discord",
                ["FFXIVGachaSpreadsheet"] = "FFXIVGachaSpreadsheet",
                // 0.349 (g, X-07): Lodestone pages opened in the browser on a click (credited with the game).
                ["FinalFantasyXIV"] = "Lodestone" };
            foreach (var credit in TenshoAssistant.Credits.CreditCatalog.All.Where(credit => credit.Hosts.Count > 0))
                Check(words.TryGetValue(credit.Id,out var word) && description.Contains(word,StringComparison.OrdinalIgnoreCase),"the manifest description names " + credit.Id);
        }),
        ("Crystals are classified from the English name on every client, the French name as a fallback",()=>
        {
            Check(CrystalClassifier.Classify("Feuerscherbe","Fire Shard") is { Element: "Feu", Size: "Éclats" },"German client, English name");
            Check(CrystalClassifier.Classify("雷のクリスタル","Lightning Crystal") is { Element: "Foudre", Size: "Cristaux" },"Japanese client, English name");
            Check(CrystalClassifier.Classify("x","Water Cluster") is { Element: "Eau", Size: "Agrégats" },"cluster");
            Check(CrystalClassifier.Classify("Cristal de foudre","") is { Element: "Foudre", Size: "Cristaux" },"French fallback");
            Check(CrystalClassifier.Classify("Fire Shard Hat","Fire Shard Hat") is null,"not a crystal");
        }),
    ];
}
