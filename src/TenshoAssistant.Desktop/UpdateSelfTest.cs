using System.Diagnostics;

namespace TenshoAssistant.Desktop;

/// <summary>
/// X-121: Verify.ps1's test of the update on a fake local release (no network): `TenshoAssistant.Desktop --update-selftest=&lt;release.json&gt;
/// [--update-own=&lt;folder&gt;]`. The release JSON has GitHub's shape and this repository's download addresses; its two files
/// (TenshoAssistant.Desktop.zip and .sha256, the ones Verify just made) are read from its folder. A copy « 0.0.1 » plays the running app and
/// the installed plugin has the release's version. Checked: the plan asks that release; the zip is downloaded, its SHA-256 matches the one
/// published, it is unpacked beside, its versions checked and recorded with the previous copy; the copy the user started would hand over to
/// it, and the installed exe starts and says its version; « Previous version » goes back and sets the version aside, which is then not asked
/// again; a damaged zip (one byte changed) and an address outside the repository are refused and install nothing. Exit 0 when all pass.
/// </summary>
internal static class UpdateSelfTest
{
    /// <summary>
    /// `--update-probe`: the real GitHub request only (the latest Stable release), read and checked as the update would, nothing downloaded;
    /// prints the tag, its files and what the update would do with it from 0.0.1. Exit 0 when the answer was read.
    /// </summary>
    public static int Probe()
    {
        var json=new GitHubReleases().Release(new UpdatePlan(UpdateAsk.Latest,null,"Desktop.UpdateChecking"),CancellationToken.None).GetAwaiter().GetResult();
        var release=DesktopUpdate.ParseRelease(json);
        if(release is null){Console.WriteLine("FAIL update probe: no readable latest release");return 1;}
        Console.WriteLine($"PASS update probe: latest release {release.Tag} (version {release.Version}, draft {release.Draft}, pre-release {release.Prerelease}), files: {string.Join(", ",release.Assets.Select(a=>a.Name))}");
        Console.WriteLine($"  from 0.0.1 the update would: {DesktopUpdate.Refusal(release,new Version(0,0,1),null,null) ?? "download and install it"}");
        return 0;
    }

    public static int Run(string releaseJson,string? ownGiven)
    {
        var failures=0;
        void Check(bool ok,string what){Console.WriteLine((ok ? "PASS update: " : "FAIL update: ")+what);if(!ok)failures++;}
        var root=Path.GetFullPath(ownGiven ?? Path.Combine(Path.GetTempPath(),"tensho-update-selftest"));
        if(Directory.Exists(root))Directory.Delete(root,true);
        try
        {
            var release=DesktopUpdate.ParseRelease(File.ReadAllText(releaseJson));
            Check(release?.Version is not null,$"fake release read ({release?.Tag})");
            if(release?.Version is not { } version)return 1;
            var three=DesktopVersionCheck.Three(version);
            var running=new Version(0,0,1);
            var plugin=new DesktopVersionResult(DesktopVersionState.Different,running,version,"plugin");

            // 1. Install: plan, download, SHA-256, unpack beside, versions inside, state.
            var own=Path.Combine(root,"good");
            var startedFolder=Path.Combine(root,"started copy");
            Directory.CreateDirectory(startedFolder);
            var plan=DesktopUpdate.Plan(running,plugin,null);
            Check(plan.Ask==UpdateAsk.Tag && plan.Version==DesktopUpdate.Three(version),$"plan asks {DesktopUpdate.Tag(version)}, the installed plugin's version");
            var updater=new DesktopUpdater(own,running,startedFolder,["--page=Home"],plugin,new FolderRelease(releaseJson));
            updater.Check(manual:false);
            updater.Work?.GetAwaiter().GetResult();
            var installed=updater.Pending;
            var target=Path.Combine(DesktopUpdater.VersionsFolder(own),three);
            Check(installed is not null && DesktopUpdate.SameFolder(installed.Folder,target),$"installed beside in {target} (status {updater.Status.Key})");
            Check(DesktopUpdater.CheckCopy(target,version) is null,"the unpacked app and its plugin are "+three);
            var state=DesktopUpdater.Load(own);
            Check(state.Current?.Version==three && DesktopUpdate.SameFolder(state.Previous?.Folder,startedFolder) && state.Previous?.Version=="0.0.1","update.json: current "+three+", previous the started copy");
            Check(!Directory.Exists(Path.Combine(own,"downloads")) || !Directory.EnumerateFiles(Path.Combine(own,"downloads")).Any(),"the download is removed once unpacked");

            // 2. Hand-over at start and the installed exe itself.
            var forward=DesktopUpdate.ForwardTo(state,running,startedFolder,File.Exists);
            Check(forward is not null && DesktopUpdate.SameFolder(Path.GetDirectoryName(forward),target),"the started copy hands over to the installed one");
            Check(DesktopUpdate.ForwardTo(state,version,target,File.Exists) is null,"the installed copy does not hand over to itself");
            if(forward is not null)
            {
                // --print-version stops at once; a copy that does not know it (or hangs) is stopped after 60 s, so no window stays open.
                using var process=Process.Start(new ProcessStartInfo(forward){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,ArgumentList={"--print-version","--no-forward","--no-update"}})!;
                var output=process.StandardOutput.ReadToEndAsync();
                _=process.StandardError.ReadToEndAsync();
                var exited=process.WaitForExit(60_000);
                if(!exited)process.Kill(entireProcessTree:true);
                var said=exited && output.Wait(5_000) ? output.Result.Trim() : "(no answer in 60 s: stopped)";
                Check(exited && process.ExitCode==0 && said=="TenshoAssistant.Desktop "+three,$"the installed exe starts and says {said}");
            }

            // 3. Previous version: back to the started copy, the version set aside and not asked again.
            updater.GoBack();
            state=DesktopUpdater.Load(own);
            Check(DesktopUpdate.SameFolder(state.Current?.Folder,startedFolder) && state.Skipped==three && !updater.ExitRequested,"'Previous version' goes back to the started copy and sets "+three+" aside");
            Check(DesktopUpdate.Plan(running,plugin,state.Skipped).Ask==UpdateAsk.None && DesktopUpdate.ForwardTo(state,running,startedFolder,File.Exists) is null,"a version set aside is neither asked again nor started");

            // 4. A damaged download is refused and installs nothing.
            var damagedOwn=Path.Combine(root,"damaged");
            var damaged=new DesktopUpdater(damagedOwn,running,startedFolder,[],plugin,new FolderRelease(releaseJson){Damaged=DesktopUpdate.ZipName});
            damaged.Check(manual:false);
            damaged.Work?.GetAwaiter().GetResult();
            Check(damaged.Status.Key=="Desktop.UpdateErrorHash" && damaged.Pending is null && !Directory.Exists(Path.Combine(DesktopUpdater.VersionsFolder(damagedOwn),three))
                && DesktopUpdater.Load(damagedOwn).Current is null,"a zip with one byte changed is refused by its SHA-256 (status "+damaged.Status.Key+")");

            // 5. Addresses outside the repository, a draft, an older version: refused before any download.
            var moved=release with {Assets=[..release.Assets.Select(asset=>asset with {Url=asset.Url.Replace(DesktopUpdate.Repository,"someone/else",StringComparison.Ordinal)})]};
            Check(DesktopUpdate.Refusal(moved,running,null,version)=="Desktop.UpdateErrorAddress","a file outside this repository's downloads is refused");
            Check(DesktopUpdate.Refusal(release with {Draft=true},running,null,version)=="Desktop.UpdateErrorRelease" && DesktopUpdate.Refusal(release,version,null,null)=="Desktop.UpdateLatest","a draft is refused; the same version is not installed again");
        }
        catch(Exception error)
        {
            Check(false,$"{error.GetType().Name}: {error.Message}");
        }
        finally
        {
            try{if(Directory.Exists(root))Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}
        }
        Console.WriteLine(failures==0 ? "PASS update selftest: a fake release installed, handed over, gone back from; a damaged zip and a foreign address refused" : $"FAIL update selftest: {failures} check(s) failed");
        return failures==0 ? 0 : 1;
    }
}
