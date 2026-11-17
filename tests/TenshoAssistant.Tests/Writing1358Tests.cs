using System.Text.Json;
using System.Text.RegularExpressions;

// 0.1358 (X-377, X-356 review, D-160): English, French spelling and typography, {0} markers. A ratchet on the forms the review fixed:
// counts written « (s) » when the number can be 1, British spelling, « frames a second », « the earlier files » in SECURITY,
// lower case after « Pour les joueurs : » in the CHANGELOG (new-version.py writes it so); X-543 M4 (0.1515): a name keeps its capital
// (« Tensho n'affiche plus… »), the same list as new-version.py's (scripts of the maintainer, PROPER_NAMES).
internal static class Writing1358Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // X-543 M4 (0.1515): names that keep their capital after « Pour les joueurs : ».
    private static readonly string[] Names=["Tensho","Dalamud","Discord","GitHub","Raphael","Lodestone","Universalis","Eorzéa","Eorzea","Windows","Steam",
        "Square","Final","Eulmore","Teamcraft","Garland","Allagan","Mogstation","Excel","Ishgard","Limsa","Gridania","Ul'dah"];

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

    private static void None(Dictionary<string,string> values,string pattern,string wanted)
    {
        var found=values.Where(pair => Regex.IsMatch(pair.Value,pattern)).Select(pair => pair.Key).ToList();
        Check(found.Count==0,$"« {pattern} » instead of « {wanted} »: {string.Join(", ",found.Take(5))}");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Writing 1358: en.json and fr.json keep the review's fixes (counts, British spelling, wording)",() =>
        {
            var en=Flat("en");
            var fr=Flat("fr");
            None(en,@"images a second","frames a second");
            None(en,@"Disciples Of The","Disciples of the");
            None(en,@"(?i)\bfertiliz","fertilis-");
            None(en,@"Right clicking","Right-clicking");
            None(en,@"could not be read nor","could be neither read nor");
            None(fr,@"(?i)\bréouvr","rouvrir");
            None(fr,@"(?i)\bprévenance\b","préavis");
            None(fr,@"(?i)\bcliquez (un|une|le|la|les) ","cliquez sur");
            Check(en["Gear.Imported"].Contains("{0} piece(s)") && fr["Gear.Imported"].Contains("{0} pièce(s)"),"Gear.Imported counts with (s)");
            Check(en["MonsterUi.Count"].StartsWith("{0} monster reference(s)") && fr["MonsterUi.Count"].StartsWith("{0} référence(s) de monstre"),"MonsterUi.Count counts with (s)");
            Check(en["CraftStock.ValueMarket"].Contains("{1} day(s)") && fr["CraftStock.ValueMarket"].Contains("{1} jour(s)"),"CraftStock.ValueMarket counts with (s)");
            Check(fr["Gear.Wasted"].StartsWith("{0} points de {1}") && en["Gear.Wasted"].StartsWith("{0} points of {1}"),"Gear.Wasted names the points");
        }),
        ("Writing 1358: documents keep the review's fixes (SECURITY, CONTRIBUTING, community discoveries, CHANGELOG)",() =>
        {
            string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
            Check(!Regex.IsMatch(Read("SECURITY.md"),@"the files? of\s+before"),"SECURITY: « the earlier file(s) »");
            Check(!Read("CONTRIBUTING.md").Contains("organization") && !Read("CONTRIBUTING.md").Contains("neighboring"),"CONTRIBUTING: British spelling");
            Check(Read("community-discoveries/README.md").Contains("200,000 facts per kind"),"community-discoveries/README: English thousands separator");
            var capital=Regex.Matches(Read("CHANGELOG.md"),@"^- Pour les joueurs : (?!(?:"+string.Join("|",Names)+@")\b)[A-ZÉ][a-zé]",RegexOptions.Multiline).Count;
            Check(capital==0,$"CHANGELOG: {capital} « Pour les joueurs : » line(s) with a capital after the colon");
        }),
    ];
}
