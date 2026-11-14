using System.Text.Json;
using System.Text.RegularExpressions;

// 0.1356 (X-375, X-356 review, packets L15 to L25 and D02 to D06, D-160; vocabulary D-163): more of the game's own words in the
// language files and documents, each taken from the game's sheets (the review's glossary, probed by tools/XbmProbe). A ratchet: the
// replaced forms do not come back. « Recettes expertes » is the game's name of the Master Recipes (Addon#14212.0), not of an expert
// recipe (« recette avancée », still checked by GameTerms1352Tests).
internal static class GameTerms1356Tests
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
        ("Game terms 1356: fr.json says the French client's words (lieux notoires, synthèse, cibles d'élite, sertissage avancé, Aventures imaginaires…)",() =>
        {
            var fr=Flat("fr");
            None(fr,@"(?i)panoram","lieu notoire (LogMessage#1272.5)");
            None(fr,@"(?i)repères? d'exploration","lieu notoire (MainCommand#64.6)");
            None(fr,@"(?i)\bcrafts?\b","synthèse (Addon#210.0) or artisanat (Addon#1400.0)");
            None(fr,@"(?i)surmatérialisation|surmat\.","sertissage avancé (HowTo#99.0)");
            None(fr,@"(?i)livre d'or","Aventures imaginaires de Khloe (Addon#5600.0)");
            None(fr,@"(?i)\bmarques? (A|S|de rang|notées?|rencontrées|\{1\}|en vue)","cible d'élite (Achievement#979.2)");
            None(fr,@"(?i)archives occultes","Calepin du croissant (McGuffinUIData#7.2)");
            None(fr,@"(?i)croissant occulte","Île de Lunule (ContentType#38.0)");
            None(fr,@"(?i)quêtes? (tribales?|de tribu)","quête des peuples alliés (AchievementCategory#37.0)");
            None(fr,@"(?i)chocobo de compagnie","compagnon chocobo");
            None(fr.Where(pair => pair.Key!="GatheringPanel.Bountiful").ToDictionary(),@"(?i)\bAbondance\b","Extraction / Récolte abondante (Action#272.0, Action#273.0); the compact panel label « Abondance +{0} » stays, like English « Bountiful +{0} »");
            None(fr,@"(?i)cloche des servants","sonnette (PlaceName#1235.0)");
            Check(!fr["CollectablePanel.UseRestoreTip"].Contains("Eurêka") && !fr["CollectablePanel.AdviceNote"].Contains("Eurêka"),"Étalage des connaissances préparé (Status#2765.0)");
            Check(fr["ExtraCollections.Glasses"]=="Accessoires de visage","Accessoires de visage (MainCommand#95.5)");
            Check(fr["Relics.Monsters"]=="Ennemis","Ennemis (RelicNoteCategory#1.1)");
            Check(fr["ItemFilters.KeyItems"]=="Objets-clefs","Objets-clefs (Addon#536.0)");
            Check(fr["ChatLinks.ChannelShout"]=="Hurler" && fr["ChatLinks.ChannelYell"]=="Crier","Shout → Hurler, Yell → Crier (Addon#645.0, Addon#647.0)");
            Check(fr["WikiTerms.VariantDungeon"]=="Donjon à embranchements" && fr["WikiTerms.CriterionDungeon"]=="Donjon alternatif","Completion#5108.3, Completion#5109.3");
        }),
        ("Game terms 1356: en.json says the English client's words (Facewear, Enemies, summoning bell, Eureka Moment, critical encounter)",() =>
        {
            var en=Flat("en");
            Check(!en["FieldContent.OccultAlerts"].Contains("engagement") && en["FieldContent.OccultEngagementAlert"].StartsWith("Critical encounter"),"critical encounter in the Occult Crescent (Addon#16619.0); critical engagement stays Bozja's (Addon#13988.0)");
            None(en,@"(?i)retainer bell","summoning bell (PlaceName#1235.0)");
            None(en,@"(?i)\bIsland rank\b","Sanctuary rank");
            Check(en["ExtraCollections.Glasses"]=="Facewear","Facewear (MainCommand#95.5)");
            Check(en["Relics.Monsters"]=="Enemies","Enemies (RelicNoteCategory#1.1)");
            Check(!Regex.IsMatch(en["CollectablePanel.AdviceNote"],@"Eureka (?!Moment)"),"Eureka Moment (Status#2765.0)");
        }),
        ("Game terms 1356: documents say the game's words (Crucible of the Unbroken, tenue, pêche au vif, Île de Lunule)",() =>
        {
            string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
            Check(!Read("docs/project/PUBLISHING.md").Contains("Labyrinth"),"Crucible of the Unbroken (PlaceName#5596.0) in PUBLISHING.md");
            Check(!Read("docs/project/PUBLISHING.md").Contains("ensemble d'équipement") && !Read("SECURITY.md").Contains("ensemble d'équipement"),"tenue (Addon#756.0)");
            Check(!Read("tools/docgen.py").Contains("remises à l'eau"),"pêche au vif (Action#297.0) in the network table");
            Check(!Read("docs/project/ROADMAP.md").Contains("onglet « Triple Triad »"),"Triple Triade (Addon#9529.0) in ROADMAP.md");
        }),
    ];
}
