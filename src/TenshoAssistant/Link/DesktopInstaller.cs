using System.Diagnostics;
using TenshoAssistant.Desktop;

namespace TenshoAssistant.Link;

/// <summary>How the desktop application stands against this plugin: not installed, older, the same version, newer, or recorded but gone.</summary>
public enum DesktopInstallKind { NotInstalled, Older, Same, Newer, Missing }

/// <summary>The installed copy the plugin sees (update.json's Current) and how it compares with the plugin's version.</summary>
public sealed record DesktopInstallView(DesktopInstallKind Kind,string? Version,string? Folder);

/// <summary>
/// X-126 (0.1113, user's request of 2026-11-11, D-137): the desktop application installed from Settings › Advanced › Desktop application,
/// never mandatory, never started without a click. Pure rules (the requests and files are DesktopInstaller's).
/// D-137: an accepted exception to Dalamud's plugin rules (a plugin that downloads an exe), Tensho being on a custom repository. Should
/// Tensho ever aim at the official repository, build with -p:TenshoDesktopInstall=false (TenshoAssistant.csproj): Enabled is then false,
/// no installer is created and the section only links to the release page.
/// </summary>
public static class DesktopInstallRules
{
#if TENSHO_NO_DESKTOP_INSTALL
    public static bool Enabled { get; } = false;
#else
    public static bool Enabled { get; } = true;
#endif

    /// <summary>The release page of a version (opened in the browser on a click).</summary>
    public static string ReleasePage(Version plugin)=>"https://github.com/"+DesktopUpdate.Repository+"/releases/tag/"+DesktopUpdate.Tag(plugin);

    /// <summary>The installed copy (Current of update.json) against the plugin's version (major.minor.build); Missing when its exe is gone.</summary>
    public static DesktopInstallView View(UpdateState state,Version plugin,Func<string,bool> exists)
    {
        if(state.Current is not { } current)return new(DesktopInstallKind.NotInstalled,null,null);
        if(!exists(Path.Combine(current.Folder,DesktopUpdate.ExeName)))return new(DesktopInstallKind.Missing,current.Version,current.Folder);
        if(!Version.TryParse(current.Version,out var version))return new(DesktopInstallKind.Older,current.Version,current.Folder);
        var app=DesktopUpdate.Three(version);var own=DesktopUpdate.Three(plugin);
        var kind=app==own ? DesktopInstallKind.Same : app<own ? DesktopInstallKind.Older : DesktopInstallKind.Newer;
        return new(kind,current.Version,current.Folder);
    }

    /// <summary>The installed version an install compares a release with: none (0.0.0) when nothing usable is installed.</summary>
    public static Version Installed(DesktopInstallView view)
        => view.Kind is DesktopInstallKind.NotInstalled or DesktopInstallKind.Missing || !Version.TryParse(view.Version,out var version) ? new Version(0,0,0) : version;

    /// <summary>The folder versions go into: the one the player chose, else the app's own versions folder.</summary>
    public static string Folder(string own,string? chosen)=>string.IsNullOrWhiteSpace(chosen) ? DesktopInstall.VersionsFolder(own) : chosen.Trim();

    /// <summary>A chosen folder must be a full path (C:\…); the language key of the problem, or null (empty means the default folder).</summary>
    public static string? FolderProblem(string? chosen)
        => string.IsNullOrWhiteSpace(chosen) || Path.IsPathFullyQualified(chosen.Trim()) && chosen.Trim().IndexOfAny(Path.GetInvalidPathChars())<0 ? null : "Advanced.DesktopInstallFolderBad";

    /// <summary>After an install from the plugin: the new copy is Current, the copy that was current becomes Previous (the app's « previous
    /// version »); a version the app had set aside is forgotten once the player installs it on purpose.</summary>
    public static UpdateState AfterInstall(UpdateState state,InstalledCopy installed)
    {
        var previous=state.Current is { } current && !DesktopUpdate.SameFolder(current.Folder,installed.Folder) ? current : state.Previous;
        return state with {Current=installed,Previous=previous,Skipped=state.Skipped==installed.Version ? null : state.Skipped};
    }

    /// <summary>The releases asked in order: this plugin's own version first (R33: same number), then the latest Stable one when it has none yet.</summary>
    public static UpdatePlan[] Plans(Version plugin)=>[new(UpdateAsk.Tag,DesktopUpdate.Three(plugin),"Advanced.DesktopInstallChecking"),new(UpdateAsk.Latest,null,"Advanced.DesktopInstallChecking")];
}

/// <summary>
/// X-126: the plugin's installer of the desktop application. On « Install » / « Update » only (no request otherwise): asks this repository's
/// release of the plugin's version, else the latest Stable one (DesktopUpdate.Refusal: published, newer than the installed copy, both files at
/// this repository's download address), then DesktopInstall.Install (SHA-256 against the published one, unpacked beside, versions checked) in
/// the chosen folder, and records it as the app's Current copy (update.json), so the app starts there and keeps its own automatic update
/// (X-121). Off the game's thread (a task, cancelled at unload). « Open the app » starts the installed exe on a click; nothing else starts it.
/// </summary>
internal sealed class DesktopInstaller : IDisposable
{
    private readonly string own;
    private readonly Version plugin;
    private readonly IReleaseSource source;
    private readonly Action<string> log;
    private readonly CancellationTokenSource stop=new();
    private readonly object gate=new();
    private Task? work;
    private (string Key,string[] Args,bool Warning)? status;

    public DesktopInstaller(string own,Version plugin,IReleaseSource source,Action<string> log)
    {
        this.own=own;this.plugin=plugin;this.source=source;this.log=log;
    }

    public string Own=>own;
    public Version Plugin=>plugin;
    public bool Busy{get{lock(gate)return work is { IsCompleted: false };}}
    /// <summary>The last step or result (a language key of Advanced.DesktopInstall*, its values, a warning), or null before any click.</summary>
    public (string Key,string[] Args,bool Warning)? Status{get{lock(gate)return status;}}

    public UpdateState State=>DesktopInstall.Load(own);
    public DesktopInstallView View()=>DesktopInstallRules.View(State,plugin,File.Exists);

    private void Set(string key,bool warning,params string[] values){lock(gate)status=(key,values,warning);}

    /// <summary>Starts an install in the background (one at a time); false when one runs or the folder is not usable.</summary>
    public bool Install(string? chosenFolder)
    {
        if(DesktopInstallRules.FolderProblem(chosenFolder) is { } problem){Set(problem,true);return false;}
        var folder=DesktopInstallRules.Folder(own,chosenFolder);
        lock(gate)
        {
            if(work is { IsCompleted: false })return false;
            status=("Advanced.DesktopInstallChecking",[],false);
            work=Task.Run(()=>Run(folder,stop.Token));
        }
        return true;
    }

    internal Task? Work{get{lock(gate)return work;}}

    private async Task Run(string folder,CancellationToken cancel)
    {
        var installed=DesktopInstallRules.Installed(View());
        try
        {
            ReleaseInfo? release=null;
            foreach(var plan in DesktopInstallRules.Plans(plugin))
            {
                var json=await source.Release(plan,cancel).ConfigureAwait(false);
                if(json is null){log($"Desktop install: no release {(plan.Ask==UpdateAsk.Tag ? DesktopUpdate.Tag(plan.Version!) : "latest")}");continue;}
                release=DesktopUpdate.ParseRelease(json);
                if(release is null){Set("Advanced.DesktopInstallErrorRelease",true,plan.Version is null ? "Stable" : DesktopVersionCheck.Three(plan.Version));return;}
                var refusal=DesktopUpdate.Refusal(release,installed,null,plan.Ask==UpdateAsk.Tag ? plan.Version : null);
                var shown=release.Version is null ? release.Tag : DesktopVersionCheck.Three(release.Version);
                if(refusal=="Desktop.UpdateLatest"){Set("Advanced.DesktopInstallUpToDate",false,shown);return;}
                if(refusal is not null)
                {
                    log($"Desktop install: {release.Tag} not installed ({refusal})");
                    Set(refusal=="Desktop.UpdateErrorAddress" ? "Advanced.DesktopInstallErrorAddress" : refusal=="Desktop.UpdateErrorPackage" ? "Advanced.DesktopInstallErrorPackage" : "Advanced.DesktopInstallErrorRelease",true,shown);
                    return;
                }
                break;
            }
            if(release is null){Set("Advanced.DesktopInstallNoRelease",true,DesktopVersionCheck.Three(plugin));return;}
            var copy=await DesktopInstall.Install(own,folder,release,source,null,(step,version,done,total)=>Set(step switch
            {
                InstallStep.Downloading=>"Advanced.DesktopInstallDownloading",
                InstallStep.Unpacking=>"Advanced.DesktopInstallUnpacking",
                InstallStep.ErrorHash=>"Advanced.DesktopInstallErrorHash",
                InstallStep.ErrorFolder=>"Advanced.DesktopInstallErrorFolder",
                _=>"Advanced.DesktopInstallErrorPackage",
            },step is InstallStep.ErrorHash or InstallStep.ErrorPackage or InstallStep.ErrorFolder,version,done,total),log,cancel).ConfigureAwait(false);
            if(copy is null)return;
            // X-140: update.json read again and changed under the lock shared with the app (never a state read before the download).
            DesktopInstall.Update(own,state=>DesktopInstallRules.AfterInstall(state,copy));
            Set(DesktopUpdate.Three(Version.Parse(copy.Version))==DesktopUpdate.Three(plugin) ? "Advanced.DesktopInstallDone" : "Advanced.DesktopInstallDoneOther",false,copy.Version,DesktopVersionCheck.Three(plugin));
        }
        catch(OperationCanceledException) when(cancel.IsCancellationRequested){}
        catch(Exception error) when(error is HttpRequestException or IOException or UnauthorizedAccessException or TaskCanceledException or InvalidDataException or InvalidOperationException)
        {
            log($"Desktop install failed: {error.GetType().Name}: {error.Message}");
            Set("Advanced.DesktopInstallErrorNetwork",true,"GitHub");
        }
    }

    /// <summary>« Automatic update » of the app (X-121), kept in its update.json; false when it could not be written.</summary>
    public bool SetAutoUpdate(bool on)
    {
        try{DesktopInstall.Update(own,state=>state with {AutoUpdate=on},DesktopInstall.ClickWaitMs);return true;}
        catch(IOException){return false;}
        catch(UnauthorizedAccessException){return false;}
    }

    /// <summary>Starts the installed app, on the player's click only; null when started, else the language key of the problem.</summary>
    public string? Open()
    {
        var view=View();
        if(view.Kind is DesktopInstallKind.NotInstalled or DesktopInstallKind.Missing || view.Folder is null)return "Advanced.DesktopInstallOpenNone";
        try
        {
            var exe=Path.Combine(view.Folder,DesktopUpdate.ExeName);
            using var process=Process.Start(new ProcessStartInfo(exe){UseShellExecute=false,WorkingDirectory=view.Folder});
            return process is null ? "Advanced.DesktopInstallOpenFailed" : null;
        }
        catch(Exception error) when(error is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            log($"Desktop app could not start: {error.Message}");
            return "Advanced.DesktopInstallOpenFailed";
        }
    }

    public void Dispose()
    {
        stop.Cancel();
        try{Work?.Wait(TimeSpan.FromSeconds(2));}
        catch(AggregateException){}
        stop.Dispose();
    }
}
