using TenshoAssistant.Market;

// X-719 (0.1726, the user's request of 21/11): the sale list's « Copy » copies the lowest price seen minus 1 gil (never under 1); the « Price
// from » cell is short (the time today, the date another day), the whole time on hover; the « Copy » column keeps its width.
internal static class SaleListCopy1726Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Sale list copy 1726: the lowest price seen minus 1, never under 1; the price seen's time short", () =>
        {
            Check(RetainerSaleList.Paste(12500) == 12499 && RetainerSaleList.Paste(2) == 1 && RetainerSaleList.Paste(1) == 1, "minus 1, never under 1");
            var now = new DateTimeOffset(2026, 11, 21, 15, 0, 0, TimeSpan.FromHours(1));
            var today = RetainerSaleList.Short(now.AddHours(-2), now);
            var before = RetainerSaleList.Short(now.AddDays(-2), now);
            Check(today == now.AddHours(-2).ToString("t", System.Globalization.CultureInfo.CurrentCulture) && before == now.AddDays(-2).ToString("d", System.Globalization.CultureInfo.CurrentCulture) && today != before, "today: the time; another day: the date (" + today + " / " + before + ")");
        }),
        ("Sale list copy 1726: wiring: the button copies the price to paste, the tooltip says it, the copy column fixed, the whole time on hover", () =>
        {
            var page = Read("MainWindow.RetainerSaleList.cs");
            Check(page.Contains("var paste = RetainerSaleList.Paste(unit.Unit);", StringComparison.Ordinal) && page.Contains("() => paste.ToString());", StringComparison.Ordinal) && !page.Contains("() => unit.Unit.ToString()", StringComparison.Ordinal), "the price to paste");
            Check(page.Contains("column == titles.Length - 1 ? ImGuiTableColumnFlags.WidthFixed", StringComparison.Ordinal) && page.Contains("if (seen is { } whole) TipText(localization.Format(key, whole.ToString(\"g\")));", StringComparison.Ordinal), "the width");
            Check(Read("Localization/Languages/fr.json").Contains("Copie l'offre la plus basse vue moins 1 gil (jamais sous 1)", StringComparison.Ordinal)
                && Read("Localization/Languages/en.json").Contains("Copies the lowest offer seen minus 1 gil (never under 1)", StringComparison.Ordinal), "the tooltip in en and fr");
        }),
    ];
}
