// 0.1105 (R20 changed on 2026-11-11, plan 4.20): a release goes out on the « lancement ok » of its version, checked by the agent in
// dalamud.log. Tensho says at load which version is loaded; the release notes say « checked at launch, not tried in detail yet ».
using System.Text.RegularExpressions;

internal static class LaunchCheck1105Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Launch check: Tensho writes its version at load, in the shape the agent's log reader looks for",() =>
        {
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            const string template="Tensho Assistant {0} loaded; logged in: {1}, territory {2}.";
            var at=plugin.IndexOf("log.Information(\""+template+"\",typeof(Plugin).Assembly.GetName().Version?.ToString(3)",StringComparison.Ordinal);
            Check(at>0,"the version line is written");
            Check(at<plugin.IndexOf("DEC-038: decided first",StringComparison.Ordinal),"before anything else starts (a load that fails later still names its version)");
            var line=string.Format(template,"0.1105.0",true,129);
            var match=Regex.Match(line,@"^Tensho Assistant (\d+(?:\.\d+){1,3}) loaded; logged in: (\w+)");
            Check(match.Success && match.Groups[1].Value=="0.1105.0" && match.Groups[2].Value=="True","read back by logread.py (VERSION_LINE)");
        }),
        ("Launch check: a release that is not an urgent fix says it was checked at launch, English then French",() =>
        {
            var release=Read("scripts/Release.ps1");
            var function=release.IndexOf("function Add-HotfixNote",StringComparison.Ordinal);
            var branch=release.IndexOf("if (-not $Hotfix) {",function,StringComparison.Ordinal);
            // 0.1145 (X-170): the positive note is built from the proof by Format-LaunchNote (scripts/LaunchProof.ps1).
            var call=release.IndexOf("Format-LaunchNote $launchProof",branch,StringComparison.Ordinal);
            var shared=Read("scripts/LaunchProof.ps1");
            var note=shared.IndexOf("function Format-LaunchNote",StringComparison.Ordinal);
            var english=shared.IndexOf("**Checked at launch, not tried in detail yet**",note,StringComparison.Ordinal);
            var french=shared.IndexOf("**Vérifié au lancement, pas encore essayé en détail**",note,StringComparison.Ordinal);
            var hotfix=release.IndexOf("**Urgent fix** ($reason)",function,StringComparison.Ordinal);
            Check(function>0 && branch>function && call>branch && note>0 && english>note && french>english,"both notes, English first, when -Hotfix is not given");
            Check(call<hotfix,"the urgent fix keeps its own note");
            Check(Regex.Matches(release,@"\$notes = Add-HotfixNote \$notes").Count==2,"new release and resumed release");
        }),
    ];
}
