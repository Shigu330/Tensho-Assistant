// 0.1219 (X-254, X-255, first-player review of 12/11, G1, G4, D9, D10, D13): the README opens with how to install Tensho, for a player
// (prerequisites, repo.json, FAQ); the repository structure, the build and the development builds live in CONTRIBUTING. The GitHub
// templates speak to a player: the bug form's « open problems » link leaves out the work list (label roadmap) and the shared discoveries
// (label discoveries), its version example is not a stale number, each form says French is welcome, and ROADMAP and GAPS are shown as
// work in progress.
internal static class FirstPlayerDocs1219Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static (string En,string Fr) Halves(string path)
    {
        var text = Read(path);
        var half = text.IndexOf("\n---\n",StringComparison.Ordinal);
        Check(half > 0,path + ": English, ---, French");
        return (text[..half],text[half..]);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("First player 1219: the README starts with the install, in both languages",() =>
        {
            var (en,fr) = Halves("README.md");
            foreach (var (half,install,overview) in new[] { (en,"## Install\n","## Overview\n"),(fr,"## Installation\n","## Vue d’ensemble\n") })
            {
                var at = half.IndexOf(install,StringComparison.Ordinal);
                Check(at > 0 && at < half.IndexOf(overview,StringComparison.Ordinal),"install before the page table: " + install.Trim());
                var section = half[at..half.IndexOf(overview,StringComparison.Ordinal)];
                foreach (var needed in new[] { "XIVLauncher","API","15","https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json","/tensho","docs/FAQ.md","CONTRIBUTING.md" })
                    Check(section.Contains(needed,StringComparison.Ordinal),install.Trim() + " names " + needed);
            }
            foreach (var gone in new[] { "## Repository structure","## Build and verification","## Installation in-game","## Structure du dépôt","## Build et validation","## Installation en jeu","TenshoAssistant.csproj","Development builds","Versions de développement :" })
                Check(!(en + fr).Contains(gone,StringComparison.Ordinal),"README: no developer part left: " + gone);
        }),
        ("First player 1219: CONTRIBUTING carries the structure, the outputs and the development builds",() =>
        {
            var (en,fr) = Halves("CONTRIBUTING.md");
            Check(en.Contains("├── src/TenshoAssistant/",StringComparison.Ordinal) && en.Contains("Development builds in game:",StringComparison.Ordinal) && en.Contains("latest.zip",StringComparison.Ordinal),"English part");
            Check(fr.Contains("Arborescence du dépôt",StringComparison.Ordinal) && fr.Contains("Versions de développement en jeu :",StringComparison.Ordinal) && fr.Contains("latest.zip",StringComparison.Ordinal),"French part");
        }),
        ("First player 1219: the GitHub forms speak to a player",() =>
        {
            var bug = Read(".github/ISSUE_TEMPLATE/bug_report.yml");
            Check(bug.Contains("issues?q=is%3Aissue+is%3Aopen+label%3Aplayer-report",StringComparison.Ordinal),"the open problems are the players' reports (0.1323, X-342: apart from the plan and the discoveries)");
            Check(!bug.Contains("0.323.0",StringComparison.Ordinal),"no stale version example");
            foreach (var form in new[] { "bug_report.yml","feature_request.yml","translation.yml","tool_proposal.yml" })
                Check(Read(".github/ISSUE_TEMPLATE/" + form).Contains("Vous pouvez écrire en français",StringComparison.Ordinal),form + ": French is welcome");
            Check(Read(".github/ISSUE_TEMPLATE/feature_request.yml").Contains("Work in progress, not a promise",StringComparison.Ordinal),"ROADMAP and GAPS shown as work in progress");
        }),
    ];
}
