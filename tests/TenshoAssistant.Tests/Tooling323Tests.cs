using System.Text.RegularExpressions;

// 0.323 (queue step d): contributor tooling kept in step: every test class runs, ignored files, and inactive GitHub drafts.
internal static class Tooling323Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Root = Path.GetFullPath(".");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Every test class with Cases is registered in the test list (a forgotten one would never run)",()=>
        {
            var program = File.ReadAllText(Path.Combine(Root,"tests","TenshoAssistant.Tests","Program.cs"));
            var classes = typeof(Tooling323Tests).Assembly.GetTypes()
                .Where(type => type.IsAbstract && type.IsSealed && type.GetProperty("Cases",System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) is not null)
                .Select(type => type.Name).ToArray();
            Check(classes.Length >= 50,"test classes found: " + classes.Length);
            var missing = classes.Where(name => !Regex.IsMatch(program,$@"\b{Regex.Escape(name)}\.Cases\b")).ToArray();
            Check(missing.Length == 0,"test classes not in the list (add .Concat(<Class>.Cases)): " + string.Join(", ",missing));
        }),
        ("Git ignores per-user and generated files; GitHub drafts stay inactive outside .github",()=>
        {
            var ignore = File.ReadAllLines(Path.Combine(Root,".gitignore")).Select(line => line.Trim()).ToHashSet();
            foreach (var entry in new[] { "**/bin/","**/obj/","checkpoints/","artifacts/",".claude/settings.local.json","Thumbs.db","desktop.ini","__pycache__/" })
                Check(ignore.Contains(entry),".gitignore lists " + entry);
            foreach (var draft in new[] { "workflows/CodeQL.yml","workflows/build.yml","dependabot.yml","README.md" })
                Check(File.Exists(Path.Combine(Root,"publish","github",draft)),"draft kept in publish/github: " + draft);
            foreach (var template in new[] { "ISSUE_TEMPLATE/bug_report.yml","ISSUE_TEMPLATE/feature_request.yml","ISSUE_TEMPLATE/translation.yml","ISSUE_TEMPLATE/config.yml","pull_request_template.md" })
                Check(File.Exists(Path.Combine(Root,".github",template)),"GitHub template: " + template);
            Check(File.Exists(Path.Combine(Root,"tools","README.md")),"probes documented");
        }),
    ];
}
