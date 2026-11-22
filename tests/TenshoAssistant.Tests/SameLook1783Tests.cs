using TenshoAssistant.Inventory;

// X-216 (the user's request of 12/11, K-26), first part: « same look » on any item sheet: a piece held with the same models and the same
// equipment slot (Item.ModelMain, ModelSub, EquipSlotCategory; dyes aside), with a check or a cross and where it is.
internal static class SameLook1783Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly Dictionary<uint, ItemLook> Looks = new()
    {
        [10] = new(0x1_0001, 0, 4, 1),   // a body piece
        [11] = new(0x1_0001, 0, 4, 2),   // its recolour of the same model, two dye channels: the same look
        [12] = new(0x1_0002, 0, 4, 1),   // another model variant: not the same
        [13] = new(0x1_0001, 0, 5, 1),   // the same model in another slot: not the same
        [14] = new(0x2_0005, 0x3_0001, 1, 0), // a weapon with an off hand
        [15] = new(0x2_0005, 0x3_0002, 1, 0), // the same main model, another off hand: not the same
        [16] = new(0, 0, 0, 0),           // no model: never a look
    };

    private static ItemLook? Look(uint id) => Looks.TryGetValue(id, out var look) ? look : null;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Same look 1783: the same models and the same slot, dyes aside; held in several places; an item without a look finds nothing", () =>
        {
            var index = LookIndex.Build([("Glamour dresser", [11u, 12u, 16u]), ("Armoire", [11u]), ("Bags", [14u, 99u]), ("Retainer", [13u, 13u])], Look);
            Check(index.Pieces == 5, "pieces with a look, once per place: " + index.Pieces);
            var body = index.Find(Looks[10]);
            Check(body.Count == 2 && body[0] == new LookHeld(11, "Glamour dresser") && body[1] == new LookHeld(11, "Armoire"), "the recolour held twice, in order: " + string.Join(", ", body));
            Check(index.Find(Looks[12]).Single().Item == 12, "the other variant finds only itself");
            Check(index.Find(Looks[13]) is [{ Item: 13, Place: "Retainer" }], "the same model in another slot: its own slot only");
            Check(index.Find(Looks[15]).Count == 0 && index.Find(Looks[14]).Single().Place == "Bags", "a weapon: main and off hand models both");
            Check(index.Find(Looks[16]).Count == 0 && index.Find(new ItemLook(0x1_0001, 0, 0, 0)).Count == 0, "no model or no slot: nothing");
            Check(LookIndex.Build([], Look).Find(Looks[10]).Count == 0, "nothing held: a cross");
        }),
        ("Same look 1783: on the item sheet with the collection part, the index built once per change of the storages, the texts", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.SameLook.cs");
            Check(page.Contains("if(lookIndexKey==key)return lookIndex;", StringComparison.Ordinal) && page.Contains("DrawStateMark(held.Count>0);", StringComparison.Ordinal), "cached; a check or a cross");
            Check(Read("src/TenshoAssistant/MainWindow.MarketHover.cs").Contains("MeasureMap(\"hover same look\",()=>DrawItemSameLook(id));", StringComparison.Ordinal), "on the item sheet and the hover panel");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "SameLookHeld", "SameLookMore", "SameLookNone", "SameLookTip" })
                    Check(text.Contains($"\"{key}\": ", StringComparison.Ordinal), $"{language}: {key}");
            }
        }),
    ];
}
