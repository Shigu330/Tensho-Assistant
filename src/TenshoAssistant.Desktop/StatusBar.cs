using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Desktop;

/// <summary>
/// The desktop application's status bar (X-119): a small ImGui window at the bottom of the OS window, under the plugin's main window. It
/// says what the desktop cannot do on this page (a NeedsLink page: « Game not linked: the live parts of this page (…) stay empty; actions
/// are off »; a page that stays in the game, asked through a link: Home is shown with a line saying so), the catalogues still loading, the
/// start-up version check against the installed plugin (R33) and the app's version. Texts from the plugin's language files (Desktop.*).
/// </summary>
internal sealed class StatusBar(Host host,DesktopVersionResult version)
{
    /// <summary>Catalogues ready of the total while they load, else null.</summary>
    public (int Ready,int Total)? Loading;
    /// <summary>X-121: the automatic update (null for a diagnostic or headless run): one more row, its state and its buttons.</summary>
    public DesktopUpdater? Updater;
    /// <summary>X-119 step 2 (0.1081): the last fresh snapshot of the local link (LocalLinkClient.Current), null when not linked.</summary>
    public Func<TenshoAssistant.Link.LinkSnapshot?>? Link;
    /// <summary>X-149 (0.1119): the link protocol of a plugin that offers another one than this app's (LocalLinkClient.OtherProtocol), else null.</summary>
    public Func<int?>? OtherProtocol;
    private string? redirected;
    // X-119 step 2 (0.1114): the saved files the link says changed, each read once; when the settings were read again and whether it worked.
    private readonly TenshoAssistant.Link.LinkChangeReader changes=new();
    private DateTime? reloadedAt;
    private string? reloadProblem;

    /// <summary>X-150 (0.1120): the settings read again off the frame, the link's notices grouped (one reading for a burst of saves).</summary>
    private Grouped<object?>? reload;
    /// <summary>X-163 (0.1132): the application rings the alarms (off in a headless run); the lines shown for a minute after they rang.</summary>
    public bool Bell;
    /// <summary>X-163 (0.1154): the application's window, owner of the notification icon (zero: no notification, e.g. headless).</summary>
    public IntPtr Window;
    private readonly DesktopNotice notice=new();
    private DateTimeOffset nextBell;
    private readonly List<(string Text,DateTimeOffset Until)> rang=[];
    /// <summary>X-119 step 2, part 2 (0.1131): the plugin's folder and the app's copy of it (null: the saved data are not read, e.g. headless).</summary>
    public (string Source,string Mirror)? Data;
    private readonly Dictionary<string,Grouped<(int Copied,IReadOnlyList<string> Problems)>> copies=[];

    /// <summary>X-119 step 2, part 2: at start, every category the app reads copied then built (UI thread, before the first frame).</summary>
    public void ReadData()
    {
        if(Data is not { } data)return;
        foreach(var category in TenshoAssistant.Link.LocalLinkProtocol.DesktopCategories)Apply(category,Copy(data,category));
        discoveriesNext=DateTime.UtcNow+DesktopMirror.DiscoveriesEvery;
    }

    // X-119 with X-173 (0.1147): the player's data file is copied by SQLite's backup (the plugin's own code), the other categories file by file.
    // X-119 (0.1152): the discoveries' database by SQLite's backup too, that file only.
    private (int Copied,IReadOnlyList<string> Problems) Copy((string Source,string Mirror) data,string category)
        => category==TenshoAssistant.Link.LocalLinkProtocol.DataCategory ? host.CopyData(data.Source,data.Mirror)
            : category==TenshoAssistant.Link.LocalLinkProtocol.DiscoveriesCategory ? host.CopyDiscoveries(data.Source,data.Mirror)
            : DesktopMirror.Copy(data.Source,data.Mirror,category);

    // X-119 (0.1152): when the discoveries may be copied again, and whether a notice waits for that turn (DesktopMirror.Due).
    private DateTime discoveriesNext;
    private bool discoveriesWaiting;

    private void Apply(string category,(int Copied,IReadOnlyList<string> Problems) copy)
    {
        foreach(var problem in copy.Problems)Console.WriteLine($"Mirror: {category}: {problem}");
        Console.WriteLine("Data: "+host.UseData(Data!.Value.Mirror,category)+$" ({copy.Copied} file(s) copied)");
    }

    /// <summary>
    /// Once per frame, before the window is drawn: a « settings » notice of the link asks a reading of the plugin's settings off the frame
    /// (Host.ReadSettingsAgain: bounded, without secrets, closed list of types); a finished reading is applied at once between frames
    /// (Host.ApplySettings); a reading that failed keeps the last valid settings and is said. The other saved data are not read by the app
    /// yet (their pages, X-119).
    /// </summary>
    public void Poll()
    {
        Ring(DateTimeOffset.UtcNow);
        var categories=changes.Take(Link?.Invoke());
        if(categories.Contains(TenshoAssistant.Link.LocalLinkProtocol.SettingsCategory))(reload??=new(host.ReadSettingsAgain)).Request();
        // X-119 step 2, part 2 (0.1131): a category the app reads is copied again off the frame (a burst of notices = one copy), then its
        // stores are built again here, between frames.
        if(Data is { } data)
        {
            foreach(var category in DesktopMirror.Due(categories,DateTime.UtcNow,ref discoveriesNext,ref discoveriesWaiting))
            {
                if(!copies.TryGetValue(category,out var grouped))copies[category]=grouped=new(()=>Copy(data,category));
                grouped.Request();
            }
            foreach(var (category,grouped) in copies)
                if(grouped.Take() is { } copied)
                {
                    if(copied.Error is { } failed)Console.WriteLine($"Mirror: {category}: not copied ({failed.GetType().Name})");
                    else Apply(category,copied.Value);
                }
        }
        if(reload?.Take() is not { } result)return;
        var note=result.Error is { } error ? $"not read again ({error.GetType().Name}: {error.Message}); the last valid settings stay"
            : result.Value is { } fresh ? host.ApplySettings(fresh) : "not read again: no saved settings were read at start";
        Console.WriteLine("Link: plugin settings "+note);
        reloadedAt=DateTime.Now;
        reloadProblem=note.StartsWith("read again",StringComparison.Ordinal) ? null : note;
    }

    // X-163 (0.1132): once a second, the alarms due (the plugin's rule, AlarmBell), a Windows sound and a line in the bar.
    private void Ring(DateTimeOffset now)
    {
        if(!Bell || now<nextBell)return;
        nextBell=now.AddSeconds(1);
        rang.RemoveAll(line=>line.Until<now);
        var (lines,sound)=host.DueAlarms(now,DesktopGame.InFront());
        if(lines.Length==0)return;
        foreach(var line in lines){rang.Add((line,now.AddMinutes(1)));Console.WriteLine("Alarm: "+line);}
        if(sound)DesktopGame.Beep();
        if(Window!=IntPtr.Zero && host.AlarmNotification())notice.Show(Window,lines);
    }

    /// <summary>X-163 (0.1154): the notification icon removed when the application closes.</summary>
    public void Close()=>notice.Remove();

    public string VersionEnglish=>DesktopVersionCheck.English(version);

    public string VersionText
    {
        get{var (key,args)=DesktopVersionCheck.Text(version);return host.Text(key,args);}
    }

    /// <summary>A page that stays in the game, asked through a link: Home instead, and the line says why until another page is chosen.</summary>
    public void Redirect()
    {
        var asked=host.CurrentPage;
        var shown=DesktopPages.Shown(asked);
        if(shown!=asked){host.SetPage(shown);redirected=asked;}
        else if(redirected is not null && asked!="Home")redirected=null;
    }

    /// <summary>The lines of the bar for the page shown: (text, warning).</summary>
    public IReadOnlyList<(string Text,bool Warning)> Lines()
    {
        var lines=new List<(string,bool)>();
        var page=host.CurrentPage;
        if(redirected is not null)lines.Add((host.Text("Desktop.InGameOnly",PageName(redirected)),true));
        foreach(var (text,_) in rang)lines.Add((text,true));
        var linked=Link?.Invoke();
        if(linked is not null)lines.Add((host.Text("Desktop.Linked",linked.Plugin,Place(linked))+Live(linked),false));
        else if(OtherProtocol?.Invoke() is { } other)lines.Add((host.Text("Desktop.LinkOtherProtocol",other,TenshoAssistant.Link.LocalLinkProtocol.Version),true));
        if(linked is not null && reloadedAt is { } at)
            lines.Add((reloadProblem is null ? host.Text("Desktop.SettingsReloaded",at.ToString("HH:mm:ss")) : host.Text("Desktop.SettingsReloadFailed",reloadProblem),reloadProblem is not null));
        if(DesktopPages.LiveKey(page) is { } live)lines.Add((host.Text(linked is null ? "Desktop.NotLinked" : "Desktop.LinkedNotLive",host.Text(live)),true));
        if(Loading is { } loading)lines.Add((host.Text("Desktop.Loading",loading.Ready,loading.Total),false));
        lines.Add((host.Text("Desktop.AppVersion",DesktopVersionCheck.Three(Program.AppVersion))+"  ·  "+VersionText,version.State!=DesktopVersionState.Same));
        return lines;
    }

    /// <summary>Where the linked game is: paused (competitive cut), no character, or the zone's name (its number when it has none).</summary>
    private string Place(TenshoAssistant.Link.LinkSnapshot linked)
        => linked.Paused ? host.Text("Desktop.LinkPaused")
            : !linked.LoggedIn ? host.Text("Desktop.LinkNotLoggedIn")
            : host.Text("Desktop.LinkZone",linked.Zone.Length>0 ? linked.Zone : "#"+linked.Territory);

    /// <summary>X-119 step 2 (0.1114): the small live states after the place: job and level, GP, the bags' free slots (each only when read).</summary>
    private string Live(TenshoAssistant.Link.LinkSnapshot linked)
    {
        if(linked.Paused || !linked.LoggedIn || linked.Live is not { } live)return "";
        var parts=new List<string>();
        if(live.Job.Length>0 && live.Level>0)parts.Add(host.Text("Desktop.LinkLiveJob",live.Job,live.Level));
        if(live.MaxGp>0 && live.Gp>=0)parts.Add(host.Text("Desktop.LinkLiveGp",live.Gp,live.MaxGp));
        if(live.BagSize>0 && live.BagFree>=0)parts.Add(host.Text("Desktop.LinkLiveBags",live.BagFree,live.BagSize));
        return parts.Count==0 ? "" : "  ·  "+string.Join("  ·  ",parts);
    }

    private string PageName(string page)=>host.HasText("PageHelp."+page+"Title") ? host.Text("PageHelp."+page+"Title") : page;

    /// <summary>Height of the bar for this frame, in pixels.</summary>
    public float Height(int lines)=>(lines+(Updater is null ? 0 : 1))*ImGui.GetTextLineHeightWithSpacing()+ImGui.GetStyle().WindowPadding.Y*2;

    /// <summary>The update row's text: its state in the plugin's language.</summary>
    public (string Text,bool Warning)? UpdateLine()
    {
        if(Updater is null)return null;
        var (key,args,warning)=Updater.Status;
        return (host.Text(key,args),warning);
    }

    /// <summary>X-121: « Automatic update: on/off », « Check for update », « Restart now » (an installed version), « Previous version (x) », then the state.</summary>
    private void DrawUpdate()
    {
        if(Updater is null || UpdateLine() is not var (text,warning))return;
        var state=Updater.State;
        if(ImGui.SmallButton(host.Text(state.AutoUpdate ? "Desktop.UpdateAutoOn" : "Desktop.UpdateAutoOff")+"##TenshoUpdateAuto"))Updater.SetAuto(!state.AutoUpdate);
        ImGui.SameLine();
        if(ImGui.SmallButton(host.Text("Desktop.UpdateCheck")+"##TenshoUpdateCheck"))Updater.Check(manual:true);
        if(Updater.Pending is not null)
        {
            ImGui.SameLine();
            if(ImGui.SmallButton(host.Text("Desktop.UpdateRestart")+"##TenshoUpdateRestart"))Updater.RestartNow();
        }
        if(state.Previous is { } previous)
        {
            ImGui.SameLine();
            if(ImGui.SmallButton(host.Text("Desktop.UpdatePrevious",previous.Version)+"##TenshoUpdatePrevious"))Updater.GoBack();
        }
        ImGui.SameLine();
        if(warning)ImGui.TextColored(new Vector4(1f,0.78f,0.35f,1f),text);
        else ImGui.TextUnformatted(text);
    }

    /// <summary>Draws the bar at the bottom of the display.</summary>
    public void Draw(Vector2 display,IReadOnlyList<(string Text,bool Warning)> lines)
    {
        var height=Height(lines.Count);
        ImGui.SetNextWindowPos(new Vector2(0,display.Y-height));
        ImGui.SetNextWindowSize(new Vector2(display.X,height));
        if(ImGui.Begin("##TenshoDesktopStatus",ImGuiWindowFlags.NoDecoration|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoBringToFrontOnFocus))
            foreach(var (text,warning) in lines)
            {
                if(warning)ImGui.TextColored(new Vector4(1f,0.78f,0.35f,1f),text);
                else ImGui.TextUnformatted(text);
            }
        DrawUpdate();
        ImGui.End();
    }
}
