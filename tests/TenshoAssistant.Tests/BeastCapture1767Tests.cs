using System.Reflection;
using TenshoAssistant.Beastmaster;

// X-745 (0.1767, the user's idea I-027 of 18/11): the Beastmaster page's « To capture » tab. Where each beast is caught is the game's
// (XBMPet columns 6 and 7: 1 a zone, 2 a duty, 0 the beast the job starts with); the coordinates are the places seen in game (X-110),
// never guessed; the techniques the game's (XBMPet columns 9 and 10).
internal static class BeastCapture1767Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static Beast Beast(uint id, string name, int kind, string place) => new(id, name, 0, "Family", "", "Trick " + id, "Release " + id, [], place) { CatchKind = kind };

    private static readonly Beast[] Beasts =
    [
        Beast(1, "Cu sith", 0, ""), Beast(2, "Squirrel", 1, "Central Shroud"), Beast(3, "Lamb", 1, "Central Shroud"), Beast(4, "Pugil", 1, "Bluefog"),
        Beast(5, "Slime", 2, "Copperbell Mines"), Beast(6, "Dullahan", 2, "Halatali"), Beast(7, "Odd", 3, "Somewhere"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast capture 1767: behaviour: the open world first then the duties, by place then name; the starting beast and unknown kinds left out", () =>
        {
            var rows = BeastCapture.Rows(Beasts, null, true);
            Check(rows.Select(beast => beast.Name).SequenceEqual(["Pugil", "Lamb", "Squirrel", "Slime", "Dullahan"]), "the order: " + string.Join(", ", rows.Select(beast => beast.Name)));
            Check(BeastCapture.Missing(Beasts, null) is null, "the beasts obtained not known: no count");
        }),
        ("Beast capture 1767: behaviour: only those not captured yet, when the game's list is known; all of them otherwise", () =>
        {
            var obtained = new HashSet<uint> { 1, 3, 5 };
            Check(BeastCapture.Rows(Beasts, obtained, true).Select(beast => beast.Id).SequenceEqual([4u, 2u, 6u]), "the missing ones");
            Check(BeastCapture.Rows(Beasts, obtained, false).Count == 5, "the box unticked: all those that can be captured");
            Check(BeastCapture.Missing(Beasts, obtained) == 3, "three to capture: " + BeastCapture.Missing(Beasts, obtained));
            Check(BeastCapture.Rows(Beasts, new HashSet<uint> { 2, 3, 4, 5, 6 }, true).Count == 0 && BeastCapture.Missing(Beasts, new HashSet<uint> { 2, 3, 4, 5, 6 }) == 0, "every one captured: none left");
        }),
        ("Beast capture 1767: behaviour: the catalog keeps XBMPet column 6 as each beast's kind of place", () =>
        {
            var sheets = (Dictionary<string, IReadOnlyList<XbmRow>>)typeof(BeastCatalogTests).GetMethod("Sheets", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;
            sheets["XBMPet"][1].Cells[6] = (byte)0;
            sheets["XBMPet"][2].Cells[6] = (byte)1; sheets["XBMPet"][2].Cells[7] = (ushort)30;
            var catalog = BeastCatalog.Build(sheets, id => "beast " + id, family => "family", action => 2, id => "enemy", id => "action", id => ("Board", 30, 25),
                catchPlace: (kind, id) => kind == 1 ? "zone " + id : "");
            Check(catalog.Beasts.Single(beast => beast.Id == 1).CatchKind == 0 && catalog.Beasts.Single(beast => beast.Id == 2).CatchKind == 1
                && catalog.Beasts.Single(beast => beast.Id == 2).CatchPlace == "zone 30", "column 6 kept, column 7 named");
            Check(BeastCapture.Rows(catalog.Beasts, null, true).Select(beast => beast.Id).SequenceEqual([2u]), "the starting beast not listed");
        }),
        ("Beast capture 1767: wiring: the tab after the bestiary, the places seen as a link to Tensho's map with every place in the menu, the techniques; both languages; the self-check", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("(\"Capture\", localization.Get(\"BeastUi.Capture\"), () => DrawBeastCapture(state, catalog)),", StringComparison.Ordinal), "the tab");
            Check(page.Contains("beastCaptureRows = BeastCapture.Rows(catalog.Beasts, obtained, beastCaptureMissing);", StringComparison.Ordinal) && page.Contains("private bool beastCaptureMissing = true;", StringComparison.Ordinal), "the rows, worked out when something changes; missing ones by default");
            Check(page.Contains("var places = BeastPlacesOf(beast.Id);", StringComparison.Ordinal) && page.Contains("ShowMapOnTenshoMap(map.MapId);", StringComparison.Ordinal) && page.Contains("NpcMenu($\"BeastCapture{beast.Id}\", beast.Name, 0, places);", StringComparison.Ordinal), "the places seen, the map, the menu");
            Check(page.Contains("localization.Format(\"BeastUi.CaptureTechniquesLine\", beast.Assault, beast.Volley)", StringComparison.Ordinal), "the techniques");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(new[] { "Capture", "CaptureCount", "CaptureInDuty", "CaptureInDutyPlace", "CaptureInZone", "CaptureMissing", "CaptureMissingTip", "CaptureMore", "CaptureNone", "CaptureNotSeen", "CaptureNotSeenTip", "CapturePlaceTip", "CaptureTechniques", "CaptureTechniquesLine", "CaptureWhere" }
                    .All(key => text.Contains($"    \"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            }
            var check = Read("src/TenshoAssistant/MainWindow.SelfCheckPages.cs");
            Check(check.Contains("Page(\"beastmaster\",async ()=>", StringComparison.Ordinal) && check.Contains("beasts.Borrows.Count!=families ? Diagnostics.SelfCheck.Suspect(\"beastmaster\",text)", StringComparison.Ordinal), "the self-check counts the actions Borrow gives");
        }),
    ];
}
