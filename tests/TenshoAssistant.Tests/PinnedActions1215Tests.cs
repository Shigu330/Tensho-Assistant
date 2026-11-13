using System.Text.RegularExpressions;

// 0.1215 (X-148 part, GPT audit of 11/11, A16): every action the workflows use is pinned to the commit of its version (a moved tag can no longer
// change what runs with the repository's token), the version kept in a comment that Dependabot updates with the hash. Still open in X-148: the
// Dalamud distribution's latest.zip checked by hash (goatcorp publishes no hash, only {"AssemblyVersion"}: a hash kept here would break the CI at
// each Dalamud update) and CodeQL on the desktop application (net10.0-windows: a Windows job, to try with the CI watched).
internal static class PinnedActions1215Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Pinned actions 1215: every action of the workflows is pinned to a commit, with its version in a comment; Dependabot keeps them up to date",() =>
        {
            var uses=0;
            foreach(var file in Directory.GetFiles(Path.GetFullPath(".github/workflows"),"*.yml"))
                foreach(var line in File.ReadAllLines(file).Where(line=>line.TrimStart().StartsWith("uses:",StringComparison.Ordinal)))
                {
                    uses++;
                    Check(Regex.IsMatch(line,@"^\s*uses: [\w.-]+/[\w./-]+@[0-9a-f]{40} # v\d+\.\d+\.\d+\s*$"),$"{Path.GetFileName(file)}: not pinned to a commit with its version: {line.Trim()}");
                }
            Check(uses>=8,"the workflows' actions read: "+uses);
            var dependabot=File.ReadAllText(Path.GetFullPath(".github/dependabot.yml"));
            Check(dependabot.Contains("package-ecosystem: \"github-actions\"",StringComparison.Ordinal),"Dependabot updates the actions (hash and comment)");
        }),
    ];
}
