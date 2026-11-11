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
    private string? redirected;

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
        var linked=Link?.Invoke();
        if(linked is not null)lines.Add((host.Text("Desktop.Linked",linked.Plugin,Place(linked)),false));
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
