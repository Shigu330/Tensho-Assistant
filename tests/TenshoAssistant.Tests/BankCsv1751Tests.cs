using TenshoAssistant.Inventory;

// X-212 part 1 (0.1751, the user's request of 12/11): the Bank copied as CSV, one line per storage, item and quality, amounts added,
// in a stable order; quotes, commas and formula signs in names never break a spreadsheet.
internal static class BankCsv1751Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Name(uint id) => id switch { 5 => "Cristal de feu", 7 => "Toile \"fine\", teinte", 9 => "=SUM(A1)", _ => "#" + id };
    private static string Category(uint id) => id == 5 ? "Cristaux" : "Divers";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Bank CSV 1751: behaviour: one line per storage, item and quality, amounts of the same storage added, storages in the order they came, NQ before HQ", () =>
        {
            var text = BankList.Csv(
            [
                ("Personnage", 7u, true, 2L), ("Servant A", 5u, false, 300L), ("Personnage", 5u, false, 10L), ("Personnage", 7u, false, 1L),
                ("Servant A", 5u, false, 99L), ("Personnage", 5u, false, 5L), ("Servant A", 9u, false, 0L),
            ], Name, Category);
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Check(lines[0] == "Storage,ItemId,Item,Category,Quality,Quantity", "the header: " + lines[0]);
            Check(lines.Length == 5, "four lines (a stack of 0 left out): " + text);
            Check(lines[1] == "\"Personnage\",5,\"Cristal de feu\",\"Cristaux\",NQ,15", "the same storage's stacks added: " + lines[1]);
            Check(lines[2] == "\"Personnage\",7,\"Toile \"\"fine\"\", teinte\",\"Divers\",NQ,1" && lines[3].EndsWith(",HQ,2", StringComparison.Ordinal), "quotes doubled, a comma kept inside, NQ before HQ: " + lines[2] + " | " + lines[3]);
            Check(lines[4] == "\"Servant A\",5,\"Cristal de feu\",\"Cristaux\",NQ,399", "the next storage after: " + lines[4]);
            Check(BankList.Csv([("A", 9u, false, 1L)], Name, Category).Contains("\"'=SUM(A1)\"", StringComparison.Ordinal), "a formula sign prefixed");
            Check(BankList.Csv([], Name, Category) == "Storage,ItemId,Item,Category,Quality,Quantity\n", "nothing shown: the header alone");
        }),
        ("Bank CSV 1751: wiring: the button copies the tab shown with its filters, the list and the CSV read the same stacks", () =>
        {
            var list = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.BankList.cs")).ReplaceLineEndings("\n");
            Check(list.Contains("bankListRows=BankList.Sorted(BankList.Merge(BankQuantities(source,selected))", StringComparison.Ordinal)
                && list.Contains("var shown=BankQuantities(source,selected).Where(stack=>MatchesBankFilters(", StringComparison.Ordinal), "one reading of the stacks, the filters applied");
            Check(list.Contains("ImGui.SetClipboardText(text);", StringComparison.Ordinal) && list.Contains("Tip(\"BankUi.CopyCsvTip\");", StringComparison.Ordinal), "copied, explained");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Bank.cs")).Contains("DrawBankCsvCopy(sourceKey,selectedEntries);", StringComparison.Ordinal), "shown above the list and the grids");
        }),
    ];
}
