using TenshoAssistant.Gear;

// 0.1177 (X-191, user's report of 12/11: « Symbiose 0 % » on the bar while the game showed 1 %): the whole percent shown as the game shows it, any bond
// started at least 1 %, 100 % only once extractable; the bar and the Gear page use the same rounding.
internal static class Spiritbond1177Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Spiritbond 1177: bounds: 0 → 0 %, 0.49 % → 1 %, 0.5 % → 1 %, 0.99 % → 1 %, 1 % → 1 %, 1.01 % → 2 %, 99.99 % → 99 %, 100 % → 100 %", () =>
        {
            foreach (var (raw, shown) in new[] { (0, 0), (49, 1), (50, 1), (99, 1), (100, 1), (101, 2), (5000, 50), (9901, 99), (9999, 99), (10000, 100) })
            {
                var percent = GearWear.SpiritbondPercent(raw)!.Value;
                Check(GearWear.ShownSpiritbond(percent) == shown, $"raw {raw}: {GearWear.ShownSpiritbond(percent)} %, expected {shown} %");
            }
            Check(GearWear.SpiritbondPercent(10001) is null && GearWear.SpiritbondPercent(-1) is null, "out of range stays unknown");
        }),
        ("Spiritbond 1177: the bar and the Gear page both show the rounded percent, the exact value in the page's tip", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GearWear.cs"));
            Check(page.Contains("localization.Format(\"GearWear.BarSpiritbond\",GearWear.ShownSpiritbond(bond))", StringComparison.Ordinal), "the bar");
            Check(page.Contains("TenshoCulture.Percent(GearWear.ShownSpiritbond(percent))", StringComparison.Ordinal) && page.Contains("TipText(TenshoAssistant.Localization.TenshoCulture.Percent(percent,\"0.00\"));", StringComparison.Ordinal), "the page and its tip");
            Check(!page.Contains("bond.ToString(\"0\")", StringComparison.Ordinal), "no rounding to the nearest left");
        }),
    ];
}
