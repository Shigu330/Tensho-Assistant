using System.Text.Json;
using System.Text.RegularExpressions;

// 0.1369 (X-381, X-356 review, D-160): the second look at the findings the first check had set aside for lack of proof. Each form below was
// settled by a line of the game's sheets or of Dalamud's own French texts (docs/evidence/0.1369-second-look-terms.md).
// A ratchet: the replaced forms do not come back.
internal static class SecondLook1369Tests
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

    private static void None(Dictionary<string,string> values,string pattern,string official,Func<string,bool>? where=null)
    {
        var found=values.Where(pair => (where is null || where(pair.Key)) && Regex.IsMatch(pair.Value,pattern,RegexOptions.IgnoreCase)).Select(pair => pair.Key).ToList();
        Check(found.Count==0,$"« {pattern} » instead of « {official} »: {string.Join(", ",found.Take(5))}");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Second look 1369: fr.json says the French client's words (Chroniques du zodiaque, cible d'élite, Client, vaincus)",() =>
        {
            var fr=Flat("fr");
            None(fr,@"livres? du zodiaque","Chroniques du zodiaque (Addon#8330.0, Quest#66972.0)");
            None(fr,@"marques? de chasse","cible d'élite (Achievement#979.2)");
            None(fr,@"\bmarques?\b","cible d'élite (Achievement#987.2)",key => key.StartsWith("SRank.",StringComparison.Ordinal));
            None(fr,@"\bmandateurs?\b","Client (LeveClient sheet)");
            None(fr,@"\bvictimes?\b|\bvictoires?\b","vaincus (LogMessage#1002.5, Addon#10441.0)",
                key => key.StartsWith("HuntingLog.",StringComparison.Ordinal) || key.StartsWith("PageHelp.HuntingLog",StringComparison.Ordinal) || key=="MapView.ShowHuntLogMonstersHelp");
            Check(fr["Leves.ColumnClient"]=="Client" && fr["HuntingLog.ColumnKills"]=="Vaincus" && fr["MapView.ShowHuntMarks"]=="Cibles d'élite proches"
                && fr["Relics.NoBook"].Contains("Chroniques du zodiaque",StringComparison.Ordinal),"the labels say the client's words");
        }),
        ("Second look 1369: en.json has one name for the book, the game's; the levemete gives the leve, not its client",() =>
        {
            var en=Flat("en");
            None(en,@"\brelic book\b|\bZodiac book\b","Trials of the Braves (Addon#8330.0)");
            Check(en["Relics.NoBook"].Contains("Trials of the Braves",StringComparison.Ordinal) && en["MapView.ShowRelicMonsters"].Contains("Trials of the Braves",StringComparison.Ordinal),"the book's name");
            var fr=Flat("fr");
            Check(!en["Leves.ClientTip"].Contains("gives",StringComparison.Ordinal) && !fr["Leves.ClientTip"].Contains("donne",StringComparison.Ordinal),"the client does not give the leve");
        }),
        ("Second look 1369: the FAQ cites Dalamud's own labels, in each language",() =>
        {
            var fr=Flat("fr");var en=Flat("en");
            Check(fr["Help.AnswerUpdate"].Contains("« Mettre à jour les plugins »",StringComparison.Ordinal) && en["Help.AnswerUpdate"].Contains("« Update plugins »",StringComparison.Ordinal),"the installer's button");
            Check(fr["Help.AnswerInstall"].Contains("Dalamud › Paramètres › Expérimental › Dépôts de plugins personnalisés",StringComparison.Ordinal)
                && en["Help.AnswerInstall"].Contains("Dalamud › Settings › Experimental › Custom Plugin Repositories",StringComparison.Ordinal),"the path of Dalamud's settings");
            Check(!fr.Values.Any(value => value.Contains("Custom Plugin Repositories",StringComparison.Ordinal)),"no English label of Dalamud in the French texts");
        }),
    ];
}
