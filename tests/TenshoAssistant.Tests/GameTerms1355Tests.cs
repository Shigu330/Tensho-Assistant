using System.Text.Json;
using System.Text.RegularExpressions;

// 0.1355 (X-374, X-356 review, packets L03 to L14, D-160): more of the game's own words in the language files, each taken from the
// game's sheets (the review's glossary, probed by tools/XbmProbe). A ratchet: the replaced forms do not come back.
// « deck » stays Tensho's word for a Triple Triad deck (the client says « jeu », which would read as « the game »); the game's
// windows are named by their own labels (« Modifier le jeu », onglet « Jeux »).
internal static class GameTerms1355Tests
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

    private static void None(Dictionary<string,string> values,string pattern,string official)
    {
        var found=values.Where(pair => Regex.IsMatch(pair.Value,pattern)).Select(pair => pair.Key).ToList();
        Check(found.Count==0,$"« {pattern} » instead of « {official} »: {string.Join(", ",found.Take(5))}");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game terms 1355: fr.json says the French client's words (HE, vents éthérés, carnet d'artisanat, Triple Triade, valeur de collection…)",() =>
        {
            var fr=Flat("fr");
            None(fr,@"\bET\b","HE (Addon#1132.0)");
            None(fr,@"(?i)heure d'[ÉE]orzéa","heure éorzéenne (Addon#1129.0)");
            None(fr,@"(?i)courants? éthérés?","vents éthérés (Addon#2445.0)");
            None(fr,@"(?i)carnets? de synthèse","carnet d'artisanat (Addon#1400.0)");
            None(fr,@"\bTriple Triad\b(?!e)","Triple Triade (Addon#9529.0)");
            None(fr,@"Mini-Cactpot","Mini Cactpot (Addon#9260.0)");
            None(fr,@"(?i)centres? de données","centre de traitement de données (Addon#10890.0)");
            None(fr,@"(?i)panneau de contrôle des expéditions","panneau de contrôle aérien (LogMessage#6042.5)");
            None(fr,@"(?i)collect(ion)?(n)?abilité","valeur de collection (Addon#1056.0)");
            None(fr,@"(?i)condition (normale|favorable)","état Normal / Bon (Addon#215.0, Addon#226.0, Addon#227.0)");
            None(fr,@"Observation méticuleuse|Cœur et âme|Innovation éclair|Synthèse rapide|Toucher précipité","Changement de patron, Attention totale, Innovation instantanée, Travail rapide, Ouvrage hâtif (CraftAction)");
            None(fr,@"(?i)éditeur de decks","fenêtre « Modifier le jeu » (Addon#9425.0)");
            None(fr,@"(?i)\btranslucide\b","transparent (Addon#4235.0)");
            Check(fr["Theme.ClassicFF"]=="FF classique","FF classique (Addon#4234.0)");
            Check(fr["SortUi.TraitUntradable"]=="Personnel","Personnel (Addon#495.0)");
            Check(fr["MarketBoard.ColumnRetainer"]=="Servant","Servant (Addon#532.0)");
            Check(fr["RaceChocobo.Learned"].StartsWith("Aptitude acquise"),"Aptitude acquise (Addon#9388.0)");
            Check(fr["TriadNpcs.Tooltip"].StartsWith("Coût : "),"Coût (Addon#9207.0)");
            Check(fr["Settings.MarketScopeDataCenter"]=="Centre de traitement de données","Centre de traitement de données (Addon#10890.0)");
        }),
        ("Game terms 1355: en.json says the English client's words (Duty Finder, Acquired ability, Edit Deck, Fisher's Intuition)",() =>
        {
            var en=Flat("en");
            None(en,@"(?i)content finder","Duty Finder (Addon#2500.0)");
            None(en,@"(?i)learned ability","Acquired (Addon#9388.0)");
            None(en,@"(?i)deck editor","Edit Deck (Addon#9425.0)");
            Check(en["FishingWindows.Disclaimer"].Contains("Fisher's Intuition"),"Fisher's Intuition (Status#568.0)");
        }),
    ];
}
