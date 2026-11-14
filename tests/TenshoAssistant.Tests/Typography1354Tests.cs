// 0.1354 (X-371, X-356 review, user's choice A, D-160; apostrophes D-162): one apostrophe in the language files, the straight one that
// players type in the search fields; counts around a marker say « (s) » when the number can be 1.
internal static class Typography1354Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Typography 1354: no curly apostrophe in en.json and fr.json",() =>
        {
            foreach(var code in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{code}.json");
                var at=text.IndexOf('’');
                Check(at<0,$"{code}.json: curly apostrophe near « {(at<0 ? "" : text.Substring(Math.Max(0,at-30),Math.Min(60,text.Length-Math.Max(0,at-30))))} »");
            }
        }),
        ("Typography 1354: a count that can be 1 says « (s) »; the French self-check note agrees with its numbers",() =>
        {
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");
            var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(en.Contains("\"PlacesSeen\": \"({0} place(s) seen)\"",StringComparison.Ordinal) && fr.Contains("\"PlacesSeen\": \"({0} lieu(x) vu(s))\"",StringComparison.Ordinal),"places seen");
            Check(en.Contains("({1} item(s) without a known price or stock)",StringComparison.Ordinal) && fr.Contains("({1} objet(s) sans prix ou sans stock connu)",StringComparison.Ordinal),"items without a price");
            var proof=Read("scripts/LaunchProof.ps1");
            Check(proof.Contains("function Format-FrenchCount",StringComparison.Ordinal) && !proof.Contains(" non lus, $(",StringComparison.Ordinal),"French counts agree (ReleaseProof1145Tests runs the note)");
        }),
    ];
}
