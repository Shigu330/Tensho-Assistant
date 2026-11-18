// X-570 (audit of 17/11, A.6 and A.9): the build tools pick the .NET SDK by one rule, with no version written in them, and the CodeQL
// configuration that no workflow read is gone.
internal static class ToolPaths1539Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Tool paths 1539: Verify.ps1 and client-snapshot.py take the newest ~/.codex/dotnet-10.*, no old folder, no version written", () =>
        {
            var verify = Read("scripts/Verify.ps1");
            Check(!verify.Contains("ia-code", StringComparison.OrdinalIgnoreCase), "Verify.ps1: the old C:\\ia-code folder");
            Check(verify.Contains("-Filter 'dotnet-10.*'", StringComparison.Ordinal), "Verify.ps1: the newest dotnet-10.*");
            var snapshot = Read("tools/client-snapshot.py");
            Check(!snapshot.Contains("dotnet-10.0.", StringComparison.Ordinal), "client-snapshot.py: a version written");
            Check(snapshot.Contains("glob(\"dotnet-10.*/dotnet.exe\")", StringComparison.Ordinal), "client-snapshot.py: the newest dotnet-10.*");
            Check(!Read("docs/project/TESTING.md").Contains("ia-code", StringComparison.OrdinalIgnoreCase), "TESTING.md: the old folder");
        }),
        ("Tool paths 1539: no CodeQL configuration file that no workflow reads", () =>
        {
            var workflows = string.Join("\n", Directory.EnumerateFiles(Path.GetFullPath(".github/workflows"), "*.yml").Select(File.ReadAllText));
            if (Directory.Exists(Path.GetFullPath(".github/codeql")))
                foreach (var file in Directory.EnumerateFiles(Path.GetFullPath(".github/codeql")))
                    Check(workflows.Contains(".github/codeql/" + Path.GetFileName(file), StringComparison.Ordinal), "unused: " + Path.GetFileName(file));
        }),
    ];
}
