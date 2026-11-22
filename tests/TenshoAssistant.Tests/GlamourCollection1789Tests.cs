using TenshoAssistant.Inventory;

// X-216 (the user's request of 12/11, K-26), second part: Collections › Glamour: the glamour collection piece by piece (glamour dresser,
// armoire, bags, armoury, saddlebags, retainers): each piece once, every place it is in, the other pieces of the same look.
internal static class GlamourCollection1789Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly Dictionary<uint, ItemLook> Looks = new()
    {
        [10] = new(0x1_0001, 0, 4, 1),        // a body piece
        [11] = new(0x1_0001, 0, 4, 2),        // its recolour of the same model: the same look
        [12] = new(0x1_0002, 0, 4, 1),        // another model variant: its own look
        [14] = new(0x2_0005, 0x3_0001, 1, 0), // a weapon
        [16] = new(0, 0, 0, 0),               // no model: not a glamour piece
    };

    private static ItemLook? Look(uint id) => Looks.TryGetValue(id, out var look) ? look : null;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Glamour collection 1789: behaviour: each piece once with all its places; the other pieces of its look; no look or unknown left out", () =>
        {
            var index = LookIndex.Build([("Glamour dresser", [10u, 12u, 16u]), ("Armoire", [11u]), ("Bags", [10u, 14u, 99u]), ("Retainer", [10u, 10u])], Look);
            var pieces = index.Collection();
            Check(pieces.Select(piece => piece.Item).Order().SequenceEqual([10u, 11u, 12u, 14u]), "each piece with a look once: " + string.Join(", ", pieces.Select(piece => piece.Item)));
            var body = pieces.Single(piece => piece.Item == 10);
            Check(body.Places.SequenceEqual(["Glamour dresser", "Bags", "Retainer"]), "item 10: every place it is in, once each, in the order read: " + string.Join(", ", body.Places));
            Check(body.SameLook.SequenceEqual([11u]), "item 10: its recolour 11 has the same look");
            Check(pieces.Single(piece => piece.Item == 11).SameLook.SequenceEqual([10u]), "and the other way round");
            Check(pieces.Single(piece => piece.Item == 12).SameLook.Count == 0 && pieces.Single(piece => piece.Item == 14).SameLook.Count == 0, "a look held once: no other piece");
            Check(!pieces.Any(piece => piece.Item is 16 or 99), "no model (16) or unknown item (99): not a glamour piece");
            Check(LookIndex.Build([], Look).Collection().Count == 0, "nothing read: an empty collection");
        }),
        ("Glamour collection 1789: a Glamour tab next to Outfits in Collections, from the « same look » index; rows built again only on a change", () =>
        {
            var collections = Read("src/TenshoAssistant/MainWindow.Collections.cs");
            Check(collections.Contains("new(\"Glamour\", localization.Get(\"GlamourCollection.Title\"), () => collectionGlamourTab = true),", StringComparison.Ordinal)
                  && collections.Contains("if(collectionGlamourTab){DrawGlamourCollection();return;}", StringComparison.Ordinal)
                  && collections.Contains("collectionGlamourTab=false;", StringComparison.Ordinal), "the tab, set again on every frame it is shown");
            var page = Read("src/TenshoAssistant/MainWindow.GlamourCollection.cs");
            Check(page.Contains("if(LookIndexNow() is not { } index)", StringComparison.Ordinal) && page.Contains("index.Collection()", StringComparison.Ordinal), "from the index of 0.1783");
            Check(page.Contains("if(glamourKey!=key)", StringComparison.Ordinal), "rows built again only when the index, the filters or the language change");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(text.Contains("  \"GlamourCollection\": {", StringComparison.Ordinal), $"{language}: the section");
                foreach (var key in new[] { "Title", "Help", "NotReady", "Search", "SearchTip", "SlotTip", "Count", "None", "ColumnSameLook", "SameLookCount", "More" })
                    Check(text.Contains($"\"{key}\": ", StringComparison.Ordinal), $"{language}: {key}");
            }
        }),
    ];
}
