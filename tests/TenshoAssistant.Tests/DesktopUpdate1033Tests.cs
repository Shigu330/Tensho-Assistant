using System.Text.RegularExpressions;
using TenshoAssistant.Desktop;

// X-121 (R33, 2026-11-10): the desktop application updates itself from this repository's GitHub releases. Its pure part (DesktopUpdate) is
// linked here: which release to ask, the release and its addresses checked, the published SHA-256 read, the saved state (current, previous,
// set aside), the hand-over at start, going back, when a restart may happen. The wiring: the release carries the .sha256 and Release.ps1
// refuses one without it, Verify.ps1 writes it and tries the update on a fake local release, the network source is declared, the FAQ says it.
// X-123 (D-100): code of conduct in both languages, CODEOWNERS, pre-push hook that reuses Verify's stamp.
internal static class DesktopUpdate1033Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file));

    private const string Tag = "Stable-0.1040.0";
    private static string Url(string name) => DesktopUpdate.DownloadPrefix+Tag+"/"+name;
    private static string ReleaseJson(string tag = Tag,bool draft = false,string? zipUrl = null) => $$"""
        {"tag_name":"{{tag}}","draft":{{(draft ? "true" : "false")}},"prerelease":false,"assets":[
          {"name":"latest.zip","browser_download_url":"https://github.com/Shigu330/Tensho-Assistant/releases/download/{{tag}}/latest.zip","size":5},
          {"name":"TenshoAssistant.Desktop.zip","browser_download_url":"{{zipUrl ?? Url("TenshoAssistant.Desktop.zip")}}","size":38000000},
          {"name":"TenshoAssistant.Desktop.zip.sha256","browser_download_url":"{{Url("TenshoAssistant.Desktop.zip.sha256")}}","size":94}]}
        """;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop update plan: the installed plugin's version, else the latest Stable release; never older, never a version set aside",() =>
        {
            var app = new Version(0,1033,0,0);
            var same = new DesktopVersionResult(DesktopVersionState.Same,app,app,"p");
            Check(DesktopUpdate.Plan(app,same,null) is { Ask: UpdateAsk.None, Reason: "Desktop.UpdateCurrent" },"same version as the plugin: no request at all");
            var newer = new DesktopVersionResult(DesktopVersionState.Different,app,new Version(0,1040,0,0),"p");
            var plan = DesktopUpdate.Plan(app,newer,null);
            Check(plan.Ask == UpdateAsk.Tag && plan.Version == new Version(0,1040,0) && DesktopUpdate.Tag(plan.Version) == Tag,"a newer plugin: its release by tag");
            Check(DesktopUpdate.Plan(app,newer,"0.1040.0") is { Ask: UpdateAsk.None, Reason: "Desktop.UpdateSkipped" },"a version set aside is not asked again");
            var older = new DesktopVersionResult(DesktopVersionState.Different,app,new Version(0,1020,0,0),"p");
            Check(DesktopUpdate.Plan(app,older,null) is { Ask: UpdateAsk.None, Reason: "Desktop.UpdatePluginOlder" },"an older plugin: the app never goes back by itself");
            var none = new DesktopVersionResult(DesktopVersionState.NotFound,app,null,null);
            Check(DesktopUpdate.Plan(app,none,null).Ask == UpdateAsk.Latest,"no plugin found: the latest Stable release");
            Check(DesktopUpdate.TagVersion("Stable-0.1040.0") == new Version(0,1040,0) && DesktopUpdate.TagVersion("v0.1040.0") is null && DesktopUpdate.TagVersion("Stable-0.1040") is null,"only Stable-x.y.z tags");
        }),
        ("Desktop update release: GitHub's JSON read, this repository's download addresses only, drafts and older versions refused",() =>
        {
            var app = new Version(0,1033,0,0);
            var release = DesktopUpdate.ParseRelease(ReleaseJson());
            Check(release is { Tag: Tag, Draft: false } && release.Version == new Version(0,1040,0) && release.Assets.Count == 3,"release read");
            Check(release!.Asset(DesktopUpdate.ZipName)?.Size == 38000000 && release.Asset(DesktopUpdate.HashName) is not null,"the zip and its .sha256");
            Check(DesktopUpdate.Refusal(release,app,null,new Version(0,1040,0)) is null && DesktopUpdate.Refusal(release,app,null,null) is null,"a good release passes");
            Check(DesktopUpdate.Refusal(release,app,null,new Version(0,1039,0)) == "Desktop.UpdateErrorRelease","the release must be the version asked");
            Check(DesktopUpdate.Refusal(release,new Version(0,1040,0,0),null,null) == "Desktop.UpdateLatest","the same version is not installed again");
            Check(DesktopUpdate.Refusal(release,app,"0.1040.0",null) == "Desktop.UpdateSkipped","a version set aside is not installed");
            Check(DesktopUpdate.Refusal(DesktopUpdate.ParseRelease(ReleaseJson(draft:true))!,app,null,null) == "Desktop.UpdateErrorRelease","a draft is refused");
            Check(DesktopUpdate.Refusal(release with {Assets=[..release.Assets.Where(a=>a.Name!=DesktopUpdate.HashName)]},app,null,null) == "Desktop.UpdateErrorRelease","no .sha256 in the release: refused");
            foreach (var bad in new[]{"http://github.com/Shigu330/Tensho-Assistant/releases/download/"+Tag+"/TenshoAssistant.Desktop.zip",
                         "https://github.com/someone/Tensho-Assistant/releases/download/"+Tag+"/TenshoAssistant.Desktop.zip",
                         "https://github.com/Shigu330/Tensho-Assistant/releases/download/Stable-0.1039.0/TenshoAssistant.Desktop.zip",
                         "https://evil.example/Shigu330/Tensho-Assistant/releases/download/"+Tag+"/TenshoAssistant.Desktop.zip"})
                Check(DesktopUpdate.Refusal(DesktopUpdate.ParseRelease(ReleaseJson(zipUrl:bad))!,app,null,null) == "Desktop.UpdateErrorAddress","address refused: "+bad);
            Check(DesktopUpdate.Refusal(release with {Assets=[..release.Assets.Select(a=>a.Name==DesktopUpdate.ZipName ? a with {Size=DesktopUpdate.MaxZipBytes+1} : a)]},app,null,null) == "Desktop.UpdateErrorPackage","a package too large is refused");
            Check(DesktopUpdate.ParseRelease("not json") is null && DesktopUpdate.ParseRelease("""{"message":"Not Found"}""") is null && DesktopUpdate.ParseRelease(null) is null,"unreadable answers");
        }),
        ("Desktop update hash: the .sha256 file read as sha256sum writes it, for the zip only",() =>
        {
            var hex = new string('a',32)+new string('B',32);
            Check(DesktopUpdate.ParseHash(hex+"  TenshoAssistant.Desktop.zip\n",DesktopUpdate.ZipName) == hex.ToLowerInvariant(),"« hex  name »");
            Check(DesktopUpdate.ParseHash(hex+" *TenshoAssistant.Desktop.zip\r\n",DesktopUpdate.ZipName) == hex.ToLowerInvariant(),"binary mark and CRLF");
            Check(DesktopUpdate.ParseHash(hex,DesktopUpdate.ZipName) == hex.ToLowerInvariant(),"the hex alone");
            Check(DesktopUpdate.ParseHash(hex+"  latest.zip",DesktopUpdate.ZipName) is null,"another file's hash is not taken");
            Check(DesktopUpdate.ParseHash("1234  TenshoAssistant.Desktop.zip",DesktopUpdate.ZipName) is null && DesktopUpdate.ParseHash(null,DesktopUpdate.ZipName) is null,"not a SHA-256");
            Check(DesktopUpdate.HashLine(hex,DesktopUpdate.ZipName) == hex.ToLowerInvariant()+"  TenshoAssistant.Desktop.zip\n" && DesktopUpdate.ParseHash(DesktopUpdate.HashLine(hex,DesktopUpdate.ZipName),DesktopUpdate.ZipName) == hex.ToLowerInvariant(),"written and read back");
        }),
        ("Desktop update state: installed beside with the previous copy kept, hand-over at start, going back sets the version aside, old copies removed",() =>
        {
            var started = new InstalledCopy("0.1033.0",@"D:\Tensho");
            var installed = new InstalledCopy("0.1040.0",@"C:\Own\versions\0.1040.0");
            var state = DesktopUpdate.AfterInstall(new UpdateState(Skipped:"0.1035.0"),installed,started);
            Check(state.Current == installed && state.Previous == started && state.Skipped is null && state.AutoUpdate,"current the new copy, previous the running one");
            Check(DesktopUpdate.ReadState(DesktopUpdate.WriteState(state with {AutoUpdate=false})) == state with {AutoUpdate=false},"update.json read back");
            Check(DesktopUpdate.ReadState("{broken") == new UpdateState() && DesktopUpdate.ReadState(null).AutoUpdate,"a broken file: defaults, update on");
            var exe = Path.Combine(installed.Folder,DesktopUpdate.ExeName);
            Check(DesktopUpdate.ForwardTo(state,new Version(0,1033,0,0),started.Folder,path=>path == exe) == exe,"the started copy hands over to the newer installed one");
            Check(DesktopUpdate.ForwardTo(state,new Version(0,1040,0,0),installed.Folder,_=>true) is null,"no hand-over to itself");
            Check(DesktopUpdate.ForwardTo(state,new Version(0,1041,0,0),@"E:\Newer",_=>true) is null,"a newer copy taken by hand stays itself");
            Check(DesktopUpdate.ForwardTo(state,new Version(0,1033,0,0),started.Folder,_=>false) is null,"no exe, no hand-over");
            var back = DesktopUpdate.RollBack(state);
            Check(back is not null && back.Current == started && back.Previous is null && back.Skipped == "0.1040.0","going back: the previous copy, the version left set aside");
            Check(DesktopUpdate.ForwardTo(back!,new Version(0,1033,0,0),started.Folder,_=>true) is null && DesktopUpdate.RollBack(back!) is null,"after going back: no hand-over, nothing more to go back to");
            var obsolete = DesktopUpdate.Obsolete(state,[@"C:\Own\versions\0.1030.0",installed.Folder,@"C:\Own\versions\0.1041.0.partial",@"C:\Own\versions\0.1034.0"],@"C:\Own\versions\0.1034.0");
            Check(obsolete.SequenceEqual([@"C:\Own\versions\0.1030.0"]),"only copies neither current, previous nor running are removed: "+string.Join(" | ",obsolete));
            Check(DesktopUpdate.RestartArgs(["--page=Weather","--no-forward"]).SequenceEqual(["--page=Weather","--no-forward"]) && DesktopUpdate.RestartArgs([]).SequenceEqual(["--no-forward"]),"a restarted copy never hands over again");
            Check(DesktopUpdate.MayRestart(loading:false,used:false) && !DesktopUpdate.MayRestart(loading:true,used:false) && !DesktopUpdate.MayRestart(loading:false,used:true),"never while loading or once the window was used");
        }),
        ("Desktop update wiring: the release carries the SHA-256, Verify tries the update on a fake release, the source is declared, the FAQ and SECURITY say the limit",() =>
        {
            var verify = Read("scripts/Verify.ps1");
            foreach (var part in new[]{"TenshoAssistant.Desktop.zip.sha256","--update-selftest=","fake-release.json","PASS update selftest"})
                Check(verify.Contains(part,StringComparison.Ordinal),"Verify.ps1: "+part);
            var release = Read("scripts/Release.ps1");
            Check(Regex.Matches(release,@"\$desktopHash").Count >= 4,"Release.ps1 checks and publishes the .sha256 (normal and resumed)");
            Check(release.Contains("refuses a release without the SHA-256",StringComparison.Ordinal),"Release.ps1 refuses a release without it");
            var program = Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.Contains("DesktopUpdater.Forward(",StringComparison.Ordinal) && program.Contains("RollBackAfterFailure",StringComparison.Ordinal) && program.Contains("--no-update",StringComparison.Ordinal),"hand-over, going back after a failure, the switch");
            var updater = Read("src/TenshoAssistant.Desktop/DesktopUpdater.cs");
            Check(updater.Contains("https://api.github.com/repos/",StringComparison.Ordinal) && !Regex.IsMatch(updater,@"Authorization|Bearer|GITHUB_TOKEN",RegexOptions.IgnoreCase),"GitHub's public API, no token");
            Check(updater.Contains("SHA256.HashDataAsync",StringComparison.Ordinal) && updater.Contains("ExtractToDirectory(zip,partial)",StringComparison.Ordinal) && updater.Contains("CheckCopy(partial",StringComparison.Ordinal),"hash, unpack beside, versions checked before it is recorded");
            var docgen = Read("tools/docgen.py");
            Check(docgen.Contains("TenshoAssistant.Desktop/DesktopUpdater.cs",StringComparison.Ordinal) && docgen.Contains("Desktop.UpdateAutoOn",StringComparison.Ordinal),"the network source is declared (SECURITY)");
            Check(Regex.IsMatch(Read("src/TenshoAssistant/Help/Faq.cs"),@"new\(""DesktopUpdate""\)"),"the FAQ answers it (R32)");
            var security = Read("SECURITY.md");
            Check(security.Contains("stolen GitHub account",StringComparison.Ordinal) && security.Contains("compte GitHub volé",StringComparison.Ordinal),"SECURITY says the limit of the SHA-256 in both languages");
        }),
        ("Repository files of D-100: code of conduct in both languages, CODEOWNERS, pre-push hook reusing Verify's stamp",() =>
        {
            var conduct = Read("CODE_OF_CONDUCT.md").Replace("\r\n","\n");
            var halves = conduct.Split("\n---\n");
            Check(halves.Length == 2 && halves[0].Contains("# Code of conduct",StringComparison.Ordinal) && halves[1].Contains("# Code de conduite",StringComparison.Ordinal),"English, then French");
            Check(Regex.Matches(halves[0],@"^## ",RegexOptions.Multiline).Count == Regex.Matches(halves[1],@"^## ",RegexOptions.Multiline).Count,"the same sections in both halves");
            Check(Regex.IsMatch(Read(".github/CODEOWNERS"),@"^\*\s+@Shigu330\s*$",RegexOptions.Multiline),"CODEOWNERS: the repository's owner for every file");
            var hook = Read(".githooks/pre-push");
            Check(hook.Contains("scripts/Verify.ps1",StringComparison.Ordinal) && hook.Contains("stamp",StringComparison.Ordinal) && !Regex.IsMatch(hook,@"Verify\.ps1[^\n]*-Force"),"the hook runs Verify without -Force: the stamp of the same sources skips a second run");
        }),
    ];
}
