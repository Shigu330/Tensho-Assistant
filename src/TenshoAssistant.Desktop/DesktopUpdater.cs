using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TenshoAssistant.Desktop;

/// <summary>Verify.ps1's fake release: a GitHub-shaped release JSON in a folder; each file is read from that folder by its name.</summary>
internal sealed class FolderRelease(string releaseJson) : IReleaseSource
{
    private readonly string folder=Path.GetDirectoryName(Path.GetFullPath(releaseJson))!;
    /// <summary>Test only: a file whose name is listed here is read with one byte changed (a damaged download).</summary>
    public string? Damaged;

    public Task<string?> Release(UpdatePlan plan,CancellationToken cancel)=>Task.FromResult<string?>(File.ReadAllText(releaseJson));
    public Task<string> Text(ReleaseAsset asset,CancellationToken cancel)=>Task.FromResult(File.ReadAllText(Path.Combine(folder,asset.Name)));

    public async Task Download(ReleaseAsset asset,Stream target,Action<long> progress,CancellationToken cancel)
    {
        await using var source=File.OpenRead(Path.Combine(folder,asset.Name));
        if(Damaged!=asset.Name){await DesktopInstall.Copy(source,target,progress,cancel).ConfigureAwait(false);return;}
        using var memory=new MemoryStream();
        await source.CopyToAsync(memory,cancel).ConfigureAwait(false);
        var bytes=memory.ToArray();bytes[bytes.Length/2]^=0x5A;
        await target.WriteAsync(bytes,cancel).ConfigureAwait(false);
        progress(bytes.Length);
    }
}

/// <summary>
/// X-121 (R33): the desktop application's automatic update. At start (and on « Check for update ») it asks the release the plan names
/// (DesktopUpdate.Plan: the installed plugin's version, else the latest Stable release), checks it (DesktopUpdate.Refusal), downloads
/// TenshoAssistant.Desktop.zip and its .sha256 from the same release, compares the SHA-256, unpacks it beside in versions\&lt;version&gt;
/// of %LOCALAPPDATA%\TenshoAssistant\Desktop, checks the versions inside, and records it as the copy to start (update.json), the running copy
/// as the previous one. The running exe is never overwritten: the new copy is started (when the window is untouched and the catalogues are
/// loaded), or at the next start, when the copy the user started hands over to it (Forward). « Previous version » goes back and sets the
/// version aside; a new copy that fails to start goes back by itself (RollBackAfterFailure). The setting « Automatic update » turns it off.
/// </summary>
internal sealed class DesktopUpdater
{
    private readonly string own;
    private readonly Version running;
    private readonly string runningFolder;
    private readonly string[] args;
    private readonly DesktopVersionResult plugin;
    private readonly IReleaseSource source;
    private readonly object gate=new();
    private Task? work;
    private (string Key,object[] Args,bool Warning) status=("Desktop.UpdateOff",[],false);
    private InstalledCopy? pending;

    public DesktopUpdater(string own,Version running,string runningFolder,string[] args,DesktopVersionResult plugin,IReleaseSource source)
    {
        this.own=own;this.running=running;this.runningFolder=runningFolder;this.args=args;this.plugin=plugin;this.source=source;
        State=Load(own);
    }

    public UpdateState State { get; private set; }
    /// <summary>Set once a restart into another copy was started: the window closes.</summary>
    public bool ExitRequested { get; private set; }
    /// <summary>A version installed this session, waiting for a restart.</summary>
    public InstalledCopy? Pending{get{lock(gate)return pending;}}
    public bool Busy{get{lock(gate)return work is { IsCompleted: false };}}
    public (string Key,object[] Args,bool Warning) Status{get{lock(gate)return status;}}

    public static string StatePath(string own)=>DesktopInstall.StatePath(own);
    public static string VersionsFolder(string own)=>DesktopInstall.VersionsFolder(own);

    public static UpdateState Load(string own)=>DesktopInstall.Load(own);

    private void Save(UpdateState state)
    {
        DesktopInstall.Save(own,state);
        State=state;
    }

    private void Set(string key,bool warning=false,params object[] values){lock(gate)status=(key,values,warning);}

    /// <summary>Starts a check in the background (at start when the setting is on, or on the button); one at a time.</summary>
    public void Check(bool manual)
    {
        lock(gate)
        {
            if(work is { IsCompleted: false } || pending is not null)return;
            if(!State.AutoUpdate && !manual){status=("Desktop.UpdateOff",[],false);return;}
            work=Task.Run(()=>Run(CancellationToken.None));
        }
    }

    public Task? Work{get{lock(gate)return work;}}

    public void SetAuto(bool on)
    {
        try{Save(State with {AutoUpdate=on});}
        catch(IOException){}
        catch(UnauthorizedAccessException){}
        if(!on && !Busy && Pending is null)Set("Desktop.UpdateOff");
    }

    private async Task Run(CancellationToken cancel)
    {
        var three=DesktopVersionCheck.Three(running);
        try
        {
            var plan=DesktopUpdate.Plan(running,plugin,State.Skipped);
            Console.WriteLine($"Update: plan {plan.Ask} {plan.Version} ({plan.Reason})");
            if(plan.Ask==UpdateAsk.None){Set(plan.Reason,plan.Reason!="Desktop.UpdateCurrent",DesktopVersionCheck.Three(plan.Version!),three);return;}
            Set("Desktop.UpdateChecking");
            var json=await source.Release(plan,cancel).ConfigureAwait(false);
            if(json is null){Set("Desktop.UpdateNoRelease",false,plan.Version is null ? "Stable" : DesktopUpdate.Tag(plan.Version),three);return;}
            var release=DesktopUpdate.ParseRelease(json);
            if(release is null){Set("Desktop.UpdateErrorRelease",true,plan.Version is null ? "Stable" : DesktopVersionCheck.Three(plan.Version),three);return;}
            var refusal=DesktopUpdate.Refusal(release,running,State.Skipped,plan.Ask==UpdateAsk.Tag ? plan.Version : null);
            if(refusal is not null)
            {
                Console.WriteLine($"Update: {release.Tag} not installed ({refusal})");
                Set(refusal,refusal.Contains("Error",StringComparison.Ordinal),release.Version is null ? release.Tag : DesktopVersionCheck.Three(release.Version),three);
                return;
            }
            var installed=await Install(release,cancel).ConfigureAwait(false);
            if(installed is null)return;
            lock(gate){pending=installed;status=("Desktop.UpdateInstalled",[installed.Version],false);}
        }
        catch(Exception error) when(error is HttpRequestException or IOException or UnauthorizedAccessException or TaskCanceledException or InvalidDataException or InvalidOperationException)
        {
            Console.WriteLine($"Update failed: {error.GetType().Name}: {error.Message}");
            Set("Desktop.UpdateErrorNetwork",true,"GitHub",three);
        }
    }

    /// <summary>X-126 (0.1113): the install is DesktopInstall.Install, shared with the plugin; here its steps worded for the app, then the
    /// state (the running copy becomes the previous one) and the older copies removed.</summary>
    internal async Task<InstalledCopy?> Install(ReleaseInfo release,CancellationToken cancel)
    {
        var three=DesktopVersionCheck.Three(running);
        var installed=await DesktopInstall.Install(own,VersionsFolder(own),release,source,runningFolder,(step,version,done,total)=>
        {
            switch(step)
            {
                case InstallStep.Downloading:Set("Desktop.UpdateDownloading",false,version,done,total);break;
                case InstallStep.Unpacking:Set("Desktop.UpdateUnpacking",false,version);break;
                case InstallStep.ErrorHash:Set("Desktop.UpdateErrorHash",true,version,three);break;
                default:Set("Desktop.UpdateErrorPackage",true,version,three);break;
            }
        },message=>Console.WriteLine(message),cancel).ConfigureAwait(false);
        if(installed is null)return null;
        Save(DesktopUpdate.AfterInstall(State,installed,new InstalledCopy(three,runningFolder)));
        foreach(var old in DesktopUpdate.Obsolete(State,Directory.GetDirectories(VersionsFolder(own)),runningFolder))
            try{Directory.Delete(old,true);}catch(IOException){}catch(UnauthorizedAccessException){}
        Console.WriteLine($"Update: previous copy {runningFolder}");
        return installed;
    }

    /// <summary>Called once per frame: an installed version restarts the app when nothing is going on (DesktopUpdate.MayRestart).</summary>
    public void Tick(bool loading,bool used)
    {
        var ready=Pending;
        if(ready is not null && !ExitRequested && DesktopUpdate.MayRestart(loading,used))Restart(ready);
    }

    public void RestartNow(){if(Pending is { } ready)Restart(ready);}

    /// <summary>« Previous version »: back to the previous copy, the version left set aside, and restart into it.</summary>
    public void GoBack()
    {
        if(DesktopUpdate.RollBack(State) is not { } back || back.Current is not { } current)return;
        Save(back);
        lock(gate)pending=null;
        if(DesktopUpdate.SameFolder(current.Folder,runningFolder)){Set("Desktop.UpdateSkipped",false,back.Skipped ?? "",DesktopVersionCheck.Three(running));return;}
        Restart(current);
    }

    private void Restart(InstalledCopy copy)
    {
        var exe=Path.Combine(copy.Folder,DesktopUpdate.ExeName);
        Console.WriteLine($"Update: restarting into {exe}");
        Set("Desktop.UpdateRestarting",false,copy.Version);
        Start(exe,DesktopUpdate.RestartArgs(args));
        ExitRequested=true;
    }

    internal static Process? Start(string exe,IEnumerable<string> arguments)
    {
        var info=new ProcessStartInfo(exe){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(exe)!};
        foreach(var argument in arguments)info.ArgumentList.Add(argument);
        return Process.Start(info);
    }

    /// <summary>At start, before anything else: the copy the user started hands over to the newest installed copy; true when it did.</summary>
    public static bool Forward(string own,Version running,string runningFolder,string[] args)
    {
        if(args.Contains("--no-forward"))return false;
        var exe=DesktopUpdate.ForwardTo(Load(own),running,runningFolder,File.Exists);
        if(exe is null)return false;
        try{return Start(exe,DesktopUpdate.RestartArgs(args)) is not null;}
        catch(System.ComponentModel.Win32Exception){return false;}
    }

    /// <summary>A copy started as Current that fails before its window: back to the previous copy (version set aside); the text to show, or null.</summary>
    public static string? RollBackAfterFailure(string own,string runningFolder)
    {
        var state=Load(own);
        if(state.Current is not { } current || !DesktopUpdate.SameFolder(current.Folder,runningFolder) || DesktopUpdate.RollBack(state) is not { } back)return null;
        try
        {
            Directory.CreateDirectory(own);
            File.WriteAllText(StatePath(own),DesktopUpdate.WriteState(back));
        }
        catch(IOException){return null;}
        catch(UnauthorizedAccessException){return null;}
        return $"Tensho Assistant (desktop) {current.Version} could not start: the next start goes back to {back.Current!.Version}.";
    }
}
