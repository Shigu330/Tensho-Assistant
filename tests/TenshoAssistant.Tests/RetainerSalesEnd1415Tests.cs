using System.Text.RegularExpressions;
using TenshoAssistant.Character;
using TenshoAssistant.Desktop;

// 0.1415, X-429 and X-430 (review of block 0.1405-0.1412, I1 and I2, on X-196's retainer table).
// X-429: the end of a retainer's sales is the time the game itself writes in its retainer list (Addon 2314, « until <settime(lnum4)> »),
// proven in docs/evidence/0.1415-retainer-sales-end.md; one reading of it (SalesEnd) for the table and the alarm, nothing before the game
// began. X-430: the table's expected picture: « Reminders-retainers », trial retainers in every state of a sale.
internal static class RetainerSalesEnd1415Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 14, 0, 0, TimeSpan.Zero);
    private const string Evidence = "docs/evidence/0.1415-retainer-sales-end.md";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Retainer sales end 1415: the evidence quotes the game's retainer list line in English and French, the end time as lnum4", () =>
        {
            var text = Read(Evidence);
            Check(text.Contains("| 2314 |", StringComparison.Ordinal) && text.Contains(" until <settime(lnum4)>", StringComparison.Ordinal), "the English line and its time");
            Check(text.Contains("en cours jusqu'au <settime(lnum4)>", StringComparison.Ordinal), "the French line and its time");
            Check(text.Contains("<num(lnum3)>", StringComparison.Ordinal) && text.Contains("lnum2==1],<icon(51)>", StringComparison.Ordinal), "the count and the town beside it");
            Check(text.Contains("`Town`", StringComparison.Ordinal) && text.Contains("`MarketItemCount`", StringComparison.Ordinal) && text.Contains("`MarketExpire`", StringComparison.Ordinal), "the three fields named");
            Check(Read("src/TenshoAssistant/Character/CharacterStatus.cs").Contains("docs/evidence/0.1415-retainer-sales-end.md", StringComparison.Ordinal), "the code cites it");
        }),
        ("Retainer sales end 1415: one reading of the end for the table and the alarm; no end without sales, without a time, or before the game began", () =>
        {
            var end = Now.AddDays(3);
            Check(RetainerStates.SalesEnd(4, (uint)end.ToUnixTimeSeconds()) == end, "a time read as it is");
            Check(RetainerStates.SalesEnd(0, (uint)end.ToUnixTimeSeconds()) is null && RetainerStates.SalesEnd(4, 0) is null, "nothing on sale, no time");
            Check(RetainerStates.SalesEnd(4, 999) is null && RetainerStates.SalesEnd(4, (uint)RetainerStates.GameStart.AddSeconds(-1).ToUnixTimeSeconds()) is null, "before the game began: no time");
            Check(RetainerStates.SalesEnd(4, (uint)RetainerStates.GameStart.ToUnixTimeSeconds()) == RetainerStates.GameStart, "from the game's start on");
            var odd = RetainerStates.From("A", "BTN", 50, 10, 5, 4, 999, 0, 0, "Gridania", null);
            Check(odd.MarketEnds is null && RetainerStates.Sale(odd, Now) == RetainerSale.Selling, "an odd time: the count alone, never « ended » in red");
            Check(RetainerStates.SaleAlarmAt(4, 999) is null, "and no alarm");
            var code = Read("src/TenshoAssistant/Character/CharacterStatus.cs");
            Check(code.Contains("MarketEnds=SalesEnd(marketItems,marketExpire),", StringComparison.Ordinal) && code.Contains("SaleAlarmAt(int marketItems,uint marketExpire)=>SalesEnd(marketItems,marketExpire);", StringComparison.Ordinal), "the table and the alarm read it once");
            Check(Regex.Matches(code, "FromUnixTimeSeconds\\(marketExpire\\)").Count == 1, "one conversion of the game's time");
        }),
        ("Retainer sales end 1415 (X-430): the table's picture, with trial retainers in every state of a sale and of a venture", () =>
        {
            var plan = ReferencePlan.Plan(DesktopPages.AsIs.Concat(DesktopPages.NeedsLink));
            Check(plan.Count(shot => shot.Id == "Reminders-retainers" && shot.Page == "Reminders" && shot.Setup == "retainers") == 1, "the picture is planned");
            var trial = ReferencePlan.RetainerTrial;
            var states = trial.Select(row => RetainerStates.Sale(new RetainerState { MarketItems = row.MarketItems, MarketEnds = row.MarketEndsIn is { } h ? Now.AddHours(h) : null }, Now)).ToHashSet();
            Check(states.SetEquals(Enum.GetValues<RetainerSale>()), "every state of a sale: " + string.Join(",", states));
            Check(trial.Any(row => row.VentureDoneIn > 0) && trial.Any(row => row.VentureDoneIn < 0) && trial.Any(row => row.Venture == 0), "a venture running, one done, none");
            Check(trial.Any(row => row.Stacks is not null) && trial.Any(row => row.Stacks is null) && trial.Any(row => row.Job.Length == 0), "a stock known, one unknown, a job unknown");
            Check(trial.Select(row => row.ContentId).Distinct().Count() == 2, "two characters");
            var references = Read("src/TenshoAssistant.Desktop/References.cs");
            Check(references.Contains("Headless.RetainerTrial(host,trial)", StringComparison.Ordinal) && references.Contains("Headless.RetainerTrialEnd(host);", StringComparison.Ordinal), "given before the picture, taken back after");
            var headless = Read("src/TenshoAssistant.Desktop/Headless.cs");
            Check(headless.Contains("host.UseData(folder,\"characters\")", StringComparison.Ordinal) && headless.Contains("focus.SetValue(host.Window,\"Status\");", StringComparison.Ordinal), "read by the plugin's own reader, on the Status tab");
        }),
    ];
}
