// 0.1695 (X-684, thirteenth part, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the last callers of the
// survey: the field operations' actions and the racing chocobo's abilities through the one action tooltip; a relic stage's piece names carry
// the common item tooltip (the list of pieces kept under it); the garden's seeds draw the common item body under their lines; the fashion page
// no longer writes the item's name twice.
internal static class LastTips1695Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Last tips 1695: actions: field operations and the racing chocobo's abilities through the one action tooltip", () =>
        {
            var fieldPage = Read("MainWindow.FieldContent.cs");
            Check(fieldPage.Contains("if(ImGui.IsItemHovered())DrawActionTooltip(row.Icon,row.Name,row.Description,localization.Format(\"FieldContent.ActionTipDetail\",row.Category,row.Most));", StringComparison.Ordinal)
                  && fieldPage.Split("TipText(row.Description)").Length == 2, "field operations (the field records keep their description)");
            var chocobo = Read("MainWindow.ChocoboBarding.cs");
            Check(chocobo.Contains("DrawActionTooltip(0,ability.Name,ability.Description);", StringComparison.Ordinal) && !chocobo.Contains("TipText(ability.Description)", StringComparison.Ordinal), "the racing chocobo");
        }),
        ("Last tips 1695: items: relic pieces, the garden's seeds, the fashion page", () =>
        {
            var relic = Read("MainWindow.RelicWeapons.cs");
            Check(relic.Split("if(ImGui.IsItemHovered()){DrawItemInfoTooltip(pieces[0]);WrappedTooltip(").Length == 3, "both stage tables: the common tooltip, then the pieces");
            var garden = Read("MainWindow.GardenView.cs");
            Check(garden.Split("DrawItemInfoBody(item,item.Id);").Length == 3, "both seed tooltips draw the common body");
            Check(garden.IndexOf("DrawItemInfoBody(item,item.Id);ImGui.TextDisabled(localization.Get(\"Garden.TipClick\"));", StringComparison.Ordinal) > 0, "the click last");
            var fashion = Read("MainWindow.Fashion.cs");
            Check(fashion.Contains("localization.Format(\"FashionUi.ItemSlots\",FashionSlotNames(item.Equipment))", StringComparison.Ordinal)
                  && !fashion.Contains("localization.Format(\"UiFragments.Equipment\",item?.Name", StringComparison.Ordinal), "the name not written twice");
        }),
        ("Last tips 1695: the new texts in both languages, with their placeholders", () =>
        {
            foreach (var code in new[] { "en", "fr" })
            {
                var language = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{code}.json"));
                Check(System.Text.RegularExpressions.Regex.IsMatch(language, "\"ActionTipDetail\": \"\\{0\\}[^\"]*\\{1\\}\""), code + ": ActionTipDetail");
                Check(System.Text.RegularExpressions.Regex.IsMatch(language, "\"ItemSlots\": \"[^\"]*\\{0\\}\""), code + ": ItemSlots");
            }
        }),
    ];
}
