using System.Text.Json;
using System.Text.RegularExpressions;

// 0.1352 (X-369, X-356 review, user's choice A, D-160): the game's own words in the language files, taken from the game's sheets
// (the review's glossary, probed by tools/XbmProbe) instead of words translated by hand. A ratchet: the replaced forms do not come back.
// Kept on purpose: « Eureka Helper » (a community tool), the Triple Triad rule « Roulette », « monnaie » for the allied societies
// (Addon#5751.0), « succès » meaning success (macros), English « mission » for the game's missions (supply, squadron, ocean fishing).
internal static class GameTerms1352Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static Dictionary<string,string> Flat(string code)
    {
        var values=new Dictionary<string,string>();
        void Walk(JsonElement node,string prefix)
        {
            foreach(var property in node.EnumerateObject())
            {
                if(property.Value.ValueKind==JsonValueKind.Object)Walk(property.Value,prefix+property.Name+".");
                else if(property.Value.ValueKind==JsonValueKind.String)values[prefix+property.Name]=property.Value.GetString()!;
            }
        }
        using var document=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{code}.json")));
        Walk(document.RootElement,"");
        return values;
    }

    private static void None(Dictionary<string,string> values,string pattern,string official,params string[] kept)
    {
        var found=values.Where(pair => !kept.Contains(pair.Key) && Regex.IsMatch(pair.Value,pattern)).Select(pair => pair.Key).ToList();
        Check(found.Count==0,$"« {pattern} » instead of « {official} »: {string.Join(", ",found.Take(5))}");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game terms 1352: fr.json says the French client's words (ALÉA, Eurêka, coiffeuse mirage, bahut personnel, aéronef, hauts faits, job…)",() =>
        {
            var fr=Flat("fr");
            None(fr,@"\bALEA\b","ALÉA (Addon#2275.0)");
            None(fr,@"\bEureka\b(?! Helper)","Eurêka (ContentType#26.0)");
            None(fr,@"(?i)donjons? profonds?","donjon sans fond (ContentType#21.0)");
            None(fr,@"(?i)coffres? à apparence","coiffeuse mirage (Addon#3735.0)");
            None(fr,@"(?i)\barmoires?\b","bahut personnel (Addon#3734.0)");
            None(fr,@"(?i)désynth","recyclage (Addon#1816.0)");
            None(fr,@"(?i)dirigeable","aéronef (Addon#6489.0)");
            None(fr,@"(?i)\ballocations?\b","autorisation (Addon#456.0)");
            None(fr,@"Fashion Report","Revue de mode (Addon#8819.0)");
            None(fr,@"(?i)carnets? de chasse","bestiaire (Addon#1880.0)");
            None(fr,@"(?i)monstres? de chasse","cible d'élite (Achievement#979.2)");
            None(fr,@"(?i)recettes? expertes?","recette avancée (Addon#1452.0)","ExtraCollections.MasterBook");
            None(fr,@"(?i)\bmétiers?\b","job (Addon#804.0)");
            None(fr,@"Grande Compagnie","Grande compagnie (Addon#791.0)");
            None(fr,@"Enclave de Doma","Quartier enclavé de Doma (PlaceName#2507.0)");
            None(fr,@"(?i)académie de Sharlayan","Académie de magie (PlaceName#3800.0)");
            None(fr,@"(?i)\bsuccès\b","haut fait (Addon#3350.0)","MacroUi.Help","MacroObserverUi.Help");
            None(fr,@"(?i)\bmonnaies?\b","devise (Addon#761.0)","PageHelp.AlliedSocietiesPurpose","Allied.ColumnCurrency");
            None(fr,@"(?i)\broulettes?\b","missions aléatoires (Addon#8605.0)","TriadRequest.Roulette");
            None(fr,@"^Reliques$","Armes antiques (AchievementCategory#62.0)");
            Check(fr["Reminders.TabSquadron"]=="Escouade","Escouade (Addon#8586.0)");
        }),
        ("Game terms 1352: en.json says the English client's words (Crucible, logs, tradecraft leves, Constitution, Isleworks Agenda)",() =>
        {
            var en=Flat("en");
            None(en,@"(?i)\blabyrinths?\b","Crucible of the Unbroken (PlaceName#5596.0)");
            None(en,@"(?i)\bnotebooks?\b","log (MainCommand#38.5)");
            None(en,@"(?i)crafting orders?","tradecraft leves (HowTo#67.0)");
            None(en,@"(?i)workshops' agenda","Isleworks Agenda (Addon#15104.0)");
            None(en,@"(?i)\btam(e|ed)\b","captured (Addon#17703.0)");
            None(en,@"\b(Assault|Volley):","Trick / Tempered Release (Addon#17715.0, Addon#17714.0)");
            Check(en["BeastUi.Vitality"]=="CON" && en["Gear.Param3"]=="Vitality","CON for beasts (Addon#17943.0); Vitality stays the gear attribute");
        }),
    ];
}
