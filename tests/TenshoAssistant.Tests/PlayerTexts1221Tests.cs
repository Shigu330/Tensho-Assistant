// 0.1221 (X-259, first-player review of 12/11, G8, D4, D12): the footer says how retainers and chocobo are really saved (when you
// open them, or on request, from the AutoBankCapture setting); no internal id (DEC-…, X-…, D-…, a version like 0.410) shows in the
// interface texts; the Erase button of the character status page says it erases the status and its file, and where the storages are
// erased.
using System.Text.RegularExpressions;

internal static class PlayerTexts1221Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Player texts 1221: no internal id in the interface texts, in either language",() =>
        {
            foreach (var language in new[] { "en","fr" })
            {
                var found = Read($"src/TenshoAssistant/Localization/Languages/{language}.json").Split('\n')
                    .Where(line => Regex.IsMatch(line,@"DEC-\d|\bX-\d{2,3}\b|\bD-\d{3}\b|\(0\.\d{3,4}\)|\b(?:since|before|depuis|d’avant|d'avant) (?:la )?0\.\d{3,4}\b")).ToArray();
                Check(found.Length == 0,language + ".json: " + string.Join(" | ",found.Take(3).Select(line => line.Trim()[..Math.Min(90,line.Trim().Length)])));
            }
        }),
        ("Player texts 1221: the footer follows the real setting, the status Erase says what it erases",() =>
        {
            Check(Read("src/TenshoAssistant/MainWindow.cs").Contains("localization.Get(configuration.AutoBankCapture ? \"UiFragments.Footer\":\"UiFragments.FooterManual\")",StringComparison.Ordinal),"footer wired to AutoBankCapture");
            var english = Read("src/TenshoAssistant/Localization/Languages/en.json");
            var french = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(english.Contains("Retainers and chocobo: saved when you open them (dated)",StringComparison.Ordinal) && english.Contains("Retainers and chocobo: saved on request (dated)",StringComparison.Ordinal),"English footers");
            Check(french.Contains("Servants et chocobo : enregistrés à l'ouverture (datés)",StringComparison.Ordinal) && french.Contains("Servants et chocobo : enregistrés sur demande (datés)",StringComparison.Ordinal),"French footers");
            Check(!english.Contains("dated manual imports",StringComparison.Ordinal) && !french.Contains("imports manuels datés",StringComparison.Ordinal),"no « manual imports »");
            Check(english.Contains("deletes its status file; its storages, gil journal and achievement progress are erased in Storage › Characters",StringComparison.Ordinal)
                  && french.Contains("supprime son fichier d'état ; ses rangements, son journal des gils et sa progression des hauts faits s'effacent dans Rangements › Personnages",StringComparison.Ordinal),"status Erase says what it erases");
        }),
    ];
}
