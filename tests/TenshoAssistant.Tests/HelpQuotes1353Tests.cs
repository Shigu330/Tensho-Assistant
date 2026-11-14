using System.Text.Json;

// 0.1353 (X-370, X-356 review, user's choice A, D-160): a page's help that quotes a button, a filter or a message quotes the label the
// page really shows, in both languages; the French Home page keeps the English name (« Accueil », not « Accueil & outils »).
internal static class HelpQuotes1353Tests
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

    private static void Quotes(Dictionary<string,string> values,string help,string label,string code)
    {
        var quoted="« "+values[label].TrimEnd('.')+" »";
        Check(values[help].Contains(quoted,StringComparison.Ordinal),$"{code} {help} quotes {quoted}");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Help quotes 1353: the help quotes the real labels (Refresh, Unconfirmed, Restore all defaults, the Mini Cactpot message)",() =>
        {
            foreach(var code in new[]{"en","fr"})
            {
                var values=Flat(code);
                Quotes(values,"PageHelp.CustomDeliveriesStart","CustomDeliveries.Read",code);
                Quotes(values,"PageHelp.MaskedCarnivaleStart","MaskedCarnivale.Read",code);
                Quotes(values,"PageHelp.RelicWeaponsStart","Relics.Read",code);
                Quotes(values,"PageHelp.AlliedSocietiesStart","Allied.Read",code);
                Quotes(values,"PageHelp.CollectionsStart","CollectionsBrowser.Unconfirmed",code);
                Quotes(values,"PageHelp.SettingsEmpty","Settings.ResetAll",code);
                Quotes(values,"PageHelp.MiniCactpotEmpty","CactpotUi.Absent",code);
            }
        }),
        ("Help quotes 1353: the Home page has one name per language, the same in the side bar and in its help",() =>
        {
            var fr=Flat("fr");
            Check(fr["Navigation.Home"]=="Accueil" && fr["PageHelp.HomeTitle"]=="Accueil","fr: « Accueil »");
            Check(!fr.Values.Any(value => value.Contains("Accueil & outils",StringComparison.Ordinal)),"fr: no « Accueil & outils » left");
            var en=Flat("en");
            Check(en["Navigation.Home"]=="Home" && en["PageHelp.HomeTitle"]=="Home","en: « Home »");
        }),
    ];
}
