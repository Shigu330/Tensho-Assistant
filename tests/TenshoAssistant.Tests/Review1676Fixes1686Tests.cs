using System.Text.Json;
using TenshoAssistant.Market;

// 0.1686 (X-700, review of 0.1671 to 0.1676): M1, a retainer that is not open takes its own city's rate when the game sent the rates (every
// retainer's town is read with them), and the footer says why the help's rate is used otherwise; M2, no tooltip text made by formatting a key
// with an empty {0} and trimming it (a translation with {0} inside would leave a hole); M3 left to X-684 (its definition of done holds it).
internal static class Review1676Fixes1686Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 6, 0, 0, TimeSpan.Zero);

    private static Dictionary<string, string> Texts(string code)
    {
        var values = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{code}.json").TrimStart('﻿'));
        foreach (var section in document.RootElement.EnumerateObject())
            if (section.Value.ValueKind == JsonValueKind.Object)
                foreach (var key in section.Value.EnumerateObject())
                    if (key.Value.ValueKind == JsonValueKind.String) values[section.Name + "." + key.Name] = key.Value.GetString()!;
        return values;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review fixes 1686: M1, any retainer whose town was read takes its city's rate; else the help's rate, with the reason", () =>
        {
            var rates = MarketTax.From([3, 5, 1, 4, 2, 0, 5, 3], Now.AddDays(3))!;
            var towns = new Dictionary<ulong, byte> { [11] = 1, [12] = 2, [13] = 3 };
            Check(MarketTax.ForRetainer(rates, towns, 12, Now) == (MarketTax.RateFor(rates, 2, Now)!.Value, MarketTax.Source.City), "a retainer not open: its own city's rate");
            Check(MarketTax.ForRetainer(rates, towns, 11, Now).From == MarketTax.Source.City && MarketTax.ForRetainer(rates, towns, 13, Now).From == MarketTax.Source.City, "each its own city");
            Check(MarketTax.ForRetainer(null, towns, 12, Now) == (MarketTax.HelpPercent, MarketTax.Source.NotReceived), "rates not received: the help's rate, said so");
            Check(MarketTax.ForRetainer(rates, towns, 12, Now.AddDays(4)) == (MarketTax.HelpPercent, MarketTax.Source.NotReceived), "rates of a past week: not used");
            Check(MarketTax.ForRetainer(rates, towns, 99, Now) == (MarketTax.HelpPercent, MarketTax.Source.TownUnknown) && MarketTax.ForRetainer(rates, null, 12, Now).From == MarketTax.Source.TownUnknown, "a retainer whose town was not read: said apart");
            Check(new RetainerSalesState(RetainerSalesStatus.Closed, [], new HashSet<string>(), "", false).Towns is null, "a state of before has no towns: unknown, never 0 % or a guess");
            var reader = Read("src/TenshoAssistant/Market/RetainerSalesReader.Dalamud.cs");
            Check(reader.Contains("if (retainer.RetainerId != 0 && (byte)retainer.Town != 0) towns[retainer.RetainerId] = (byte)retainer.Town;") && reader.Contains("openRetainer, town, towns));"), "every retainer's town read in the loop that was there");
            var page = Read("src/TenshoAssistant/MainWindow.RetainerSaleList.cs");
            Check(page.Contains("MarketTax.ForRetainer(MarketTaxRatesSource?.Invoke(), state.Towns, shown.Key.StorageId, DateTimeOffset.UtcNow)") && !page.Contains("shown.Key.StorageId == state.RetainerId ?"), "the page asks for the shown retainer");
            foreach (var code in new[] { "en", "fr" })
            {
                var texts = Texts(code);
                Check(texts.ContainsKey("RetainerSales.SaleListTaxTown") && texts.ContainsKey("RetainerSales.SaleListTaxHelp") && texts.ContainsKey("RetainerSales.SaleListTaxCity"), code + ": the three reasons");
            }
        }),
        ("Review fixes 1686: M2, the texts put under a tooltip are whole keys, never a key formatted with an empty {0} then trimmed", () =>
        {
            foreach (var code in new[] { "en", "fr" })
            {
                var texts = Texts(code);
                Check(!texts["ChatWindow.MapLinkTip"].Contains('{') && !texts["ChatWindow.QuestLinkTip"].Contains('{') && texts["LiveDetails.Alternatives"].Contains("{0}") && !texts["LiveDetails.Alternatives"].Contains("{1}"), code + ": whole texts");
            }
            Check(Read("src/TenshoAssistant/MainWindow.ChatWindow.cs").Contains("localization.Get(\"ChatWindow.MapLinkTip\")")
                  && Read("src/TenshoAssistant/MainWindow.CraftProjection.cs").Contains("localization.Format(\"LiveDetails.Alternatives\",row.Alternatives)"), "the two calls");
            foreach (var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories))
                Check(!System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(file), @"localization\.Format\(""[\w.]+"",""""[^;]*\)\.Trim\(\)"), "a key formatted with an empty {0} then trimmed: " + Path.GetFileName(file));
        }),
    ];
}
