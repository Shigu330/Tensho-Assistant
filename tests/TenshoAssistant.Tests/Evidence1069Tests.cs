using System.Text.RegularExpressions;

// 0.1069 (audit A074): one naming rule for docs/evidence, written at the top of its README: <version>-<subject>.<ext> with the two-part
// version, or x<plan point>-<subject>.<ext>; the three-part names before 0.183 are kept because the code cites them (the old
// craft-log-api-probe.log, cited nowhere, was removed in 0.1085).
internal static class Evidence1069Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly Regex TwoPart = new(@"^0\.\d+-[a-z0-9][a-z0-9.-]*\.[a-z0-9]+$",RegexOptions.Compiled);
    private static readonly Regex PlanPoint = new(@"^x\d+-[a-z0-9][a-z0-9.-]*\.[a-z0-9]+$",RegexOptions.Compiled);
    private static readonly Regex ThreePart = new(@"^0\.(\d+)\.0-[a-z0-9][a-z0-9.-]*\.[a-z0-9]+$",RegexOptions.Compiled);

    internal static bool Allowed(string name)
    {
        if(name == "README.md") return true;
        if(TwoPart.IsMatch(name) || PlanPoint.IsMatch(name)) return true;
        var old = ThreePart.Match(name);
        return old.Success && int.Parse(old.Groups[1].Value) < 183;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Evidence naming: the rule accepts the three forms and refuses the rest",() =>
        {
            foreach(var good in new[]{"0.1004-discoveries-check.md","x93-load-test.md","0.99.0-crystal-probe.log"})
                Check(Allowed(good),"accepted: " + good);
            foreach(var bad in new[]{"0.1070.0-new-probe.log","probe.log","craft-log-api-probe.log","0.1070-Upper.md","X93-load.md","0.1070-no-extension"})
                Check(!Allowed(bad),"refused: " + bad);
        }),
        ("Evidence naming: every file of docs/evidence follows the rule written in its README",() =>
        {
            var folder = Path.GetFullPath("docs/evidence");
            var wrong = Directory.EnumerateFiles(folder).Select(Path.GetFileName).Where(name => name is not null && !Allowed(name!)).ToList();
            Check(wrong.Count == 0,"evidence files outside the naming rule: " + string.Join(", ",wrong));
            var readme = File.ReadAllText(Path.Combine(folder,"README.md"));
            Check(readme.Contains("Naming: `<version>-<subject>.<ext>`") && readme.Contains("Français : nommage :"),"the rule is written in both languages");
        }),
    ];
}
