// 0.1227 (X-224, user request of 12/11, plugin comparison K-40): the item sheet says which game patch added the item, when Garland Tools
// gives it in the item document it already reads for the sources (« patch », e.g. 7.3 for 46246, 1.0 for 5057:
// docs/evidence/0.1227-garland-patch.md); nothing when it is absent or not a plausible patch (never a guessed patch).
using System.Text.Json;
using TenshoAssistant.Community;

internal static class GarlandPatch1227Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Doc(string patch) => "{\"item\":{\"id\":46246,\"name\":\"x\"" + patch + "},\"partials\":[]}";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garland patch 1226: the patch is read when it is a plausible one, else nothing",() =>
        {
            var now = DateTimeOffset.UtcNow;
            Check(GarlandItemSourceParser.Parse(Doc(",\"patch\":7.3"),46246,now).Patch == 7.3,"7.3");
            Check(GarlandItemSourceParser.Parse(Doc(",\"patch\":1"),46246,now).Patch == 1.0,"1.0 written as 1");
            Check(GarlandItemSourceParser.Parse(Doc(""),46246,now).Patch is null,"absent");
            foreach (var wrong in new[] { ",\"patch\":0",",\"patch\":-2",",\"patch\":99",",\"patch\":\"7.3\"",",\"patch\":null" })
                Check(GarlandItemSourceParser.Parse(Doc(wrong),46246,now).Patch is null,"refused: " + wrong);
            Check(GarlandItemSourceParser.Parse(Doc(",\"patch\":6.55"),46246,now).Patch == 6.55,"two decimals kept");
        }),
        ("Garland patch 1226: the item sheet shows it from what is already read, in both languages",() =>
        {
            var sheet = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ItemSheet.cs"));
            // X-363 (0.1350): Peek alone left the header empty for any item whose sources the game data knows; the header now asks (Get).
            Check(sheet.Contains("(ask ? itemSources?.Get(id):itemSources?.Peek(id))?.List?.Patch is { } patch",StringComparison.Ordinal),"read with Get when asked (the header), Peek otherwise");
            foreach (var language in new[] { "en","fr" })
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).Contains("\"Patch\": ",StringComparison.Ordinal),language + ": ItemSheet.Patch");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1227-garland-patch.md")),"evidence");
        }),
    ];
}
