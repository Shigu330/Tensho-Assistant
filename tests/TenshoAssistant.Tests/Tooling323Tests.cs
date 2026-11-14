using System.Text.RegularExpressions;

// 0.323 (queue step d): contributor tooling kept in step: every test class runs, ignored files, and inactive GitHub drafts.
internal static class Tooling323Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Root = Path.GetFullPath(".");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // X-267 (0.1275): the hand list of Program.cs is gone: every class is found by itself (TestRegistry). Same aim (a forgotten class
        // would never run), checked the other way: every type with a « Cases » member is a class the registry runs, and no hand list is back.
        ("Every test class with Cases is found by the test registry (a forgotten one would never run); Program.cs keeps no hand list",()=>
        {
            var program = File.ReadAllText(Path.Combine(Root,"tests","TenshoAssistant.Tests","Program.cs"));
            const System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var withCases = typeof(Tooling323Tests).Assembly.GetTypes()
                .Where(type => type.GetProperty("Cases",any) is not null || type.GetField("Cases",any) is not null).Select(type => type.Name).ToArray();
            var found = TestRegistry.Classes(typeof(Tooling323Tests).Assembly).Select(type => type.Name).ToHashSet();
            Check(found.Count >= 600,"test classes found: " + found.Count);
            var missing = withCases.Where(name => !found.Contains(name)).ToArray();
            Check(missing.Length == 0,"a « Cases » the registry does not run (a static class with a static property or field of tests): " + string.Join(", ",missing));
            Check(!Regex.IsMatch(program,@"\.Concat\(\w+\.Cases\)"),"no hand list of classes in Program.cs");
        }),
        ("Git ignores per-user and generated files; GitHub templates live in .github",()=>
        {
            var ignore = File.ReadAllLines(Path.Combine(Root,".gitignore")).Select(line => line.Trim()).ToHashSet();
            // Agent files (the agent's instructions, the agent's settings, the agent's notes, the agent's scripts): NoAgentFiles802Tests (audit #71).
            foreach (var entry in new[] { "**/bin/","**/obj/","checkpoints/","artifacts/","Thumbs.db","desktop.ini","__pycache__/" })
                Check(ignore.Contains(entry),".gitignore lists " + entry);
            foreach (var template in new[] { "ISSUE_TEMPLATE/bug_report.yml","ISSUE_TEMPLATE/feature_request.yml","ISSUE_TEMPLATE/translation.yml","ISSUE_TEMPLATE/config.yml","pull_request_template.md" })
                Check(File.Exists(Path.Combine(Root,".github",template)),"GitHub template: " + template);
            Check(File.Exists(Path.Combine(Root,"tools","README.md")),"probes documented");
        }),
    ];
}
