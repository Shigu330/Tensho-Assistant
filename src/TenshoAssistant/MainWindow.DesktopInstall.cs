using Dalamud.Bindings.ImGui;
using TenshoAssistant.Link;

namespace TenshoAssistant;

// X-126 (0.1113, user's request of 2026-11-11, D-137): Settings › Advanced › Desktop application: its state against this plugin, Install or
// Update (this repository's release, SHA-256 checked, installed beside), the install folder, the app's automatic update (X-121), open the
// app or its folder, and the release page. Nothing is downloaded or started without a click.
internal sealed partial class MainWindow
{
    /// <summary>The plugin's installer of the desktop app, set by Plugin.cs; null in the desktop app itself and in a build that leaves it out.</summary>
    internal DesktopInstaller? DesktopInstaller { get; set; }
    // update.json read again every two seconds at most (never every frame), and at once after a click.
    private DesktopInstallView? desktopView;
    private TenshoAssistant.Desktop.UpdateState? desktopState;
    private long desktopViewAt=-100_000;
    private string? desktopOpenProblem;

    private void DrawDesktopInstall()
    {
        var installer=DesktopInstaller;
        if(installer is null)
        {
            // A build without the installer (TenshoDesktopInstall=false): the release page only. In the desktop app itself: nothing.
            if(!DesktopInstallRules.Enabled && typeof(MainWindow).Assembly.GetName().Version is { } built)
            {
                ImGui.TextColored(Muted,localization.Get("Advanced.DesktopInstallOff"));
                if(ImGui.SmallButton(localization.Get("Advanced.DesktopInstallRelease")))Dalamud.Utility.Util.OpenLink(DesktopInstallRules.ReleasePage(built));
                Tip("Advanced.DesktopInstallReleaseTip");
            }
            return;
        }
        var now=Environment.TickCount64;
        if(desktopView is null || now-desktopViewAt>2000){desktopState=installer.State;desktopView=DesktopInstallRules.View(desktopState,installer.Plugin,File.Exists);desktopViewAt=now;}
        var view=desktopView;
        var plugin=TenshoAssistant.Desktop.DesktopVersionCheck.Three(installer.Plugin);
        ImGui.TextColored(view.Kind is DesktopInstallKind.Older or DesktopInstallKind.Missing ? Warning : Muted,view.Kind switch
        {
            DesktopInstallKind.NotInstalled=>localization.Get("Advanced.DesktopInstallNotInstalled"),
            DesktopInstallKind.Older=>localization.Format("Advanced.DesktopInstallOlder",view.Version ?? "?",view.Folder ?? "",plugin),
            DesktopInstallKind.Same=>localization.Format("Advanced.DesktopInstallSame",view.Version ?? "?",view.Folder ?? ""),
            DesktopInstallKind.Newer=>localization.Format("Advanced.DesktopInstallNewer",view.Version ?? "?",view.Folder ?? "",plugin),
            _=>localization.Format("Advanced.DesktopInstallMissing",view.Version ?? "?",view.Folder ?? ""),
        });
        var folder=configuration.DesktopInstallFolder ?? "";var folderBefore=folder;
        ImGui.TextUnformatted(localization.Get("Advanced.DesktopInstallFolder"));
        ImGui.SameLine();ImGui.SetNextItemWidth(Math.Max(200,ImGui.GetContentRegionAvail().X-40));
        if(ImGui.InputTextWithHint("##DesktopInstallFolder",DesktopInstallRules.Folder(installer.Own,null),ref folder,260))configuration.DesktopInstallFolder=folder;
        Tip("Advanced.DesktopInstallFolderTip");
        SaveWhenEditEnds("Desktop install folder",value=>configuration.DesktopInstallFolder=value,folderBefore);
        if(DesktopInstallRules.FolderProblem(folder) is { } folderProblem)ImGui.TextColored(Warning,localization.Get(folderProblem));
        var busy=installer.Busy;
        ImGui.BeginDisabled(busy || view.Kind is DesktopInstallKind.Same or DesktopInstallKind.Newer);
        try
        {
            var update=view.Kind==DesktopInstallKind.Older;
            if(ImGui.Button(localization.Get(update ? "Advanced.DesktopUpdateButton":"Advanced.DesktopInstallButton")+"###DesktopInstall")){installer.Install(configuration.DesktopInstallFolder);desktopView=null;}
            Tip(update ? "Advanced.DesktopUpdateButtonTip":"Advanced.DesktopInstallButtonTip");
        }
        finally{ImGui.EndDisabled();}
        if(view.Kind is not (DesktopInstallKind.NotInstalled or DesktopInstallKind.Missing) && view.Folder is { } installed)
        {
            ImGui.SameLine();
            if(ImGui.Button(localization.Get("Advanced.DesktopInstallOpen")))desktopOpenProblem=installer.Open();
            Tip("Advanced.DesktopInstallOpenTip");
            ImGui.SameLine();
            if(ImGui.Button(localization.Get("Advanced.DesktopInstallOpenFolder")))Dalamud.Utility.Util.OpenLink(installed);
            Tip("Advanced.DesktopInstallOpenFolderTip");
        }
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("Advanced.DesktopInstallRelease")))Dalamud.Utility.Util.OpenLink(DesktopInstallRules.ReleasePage(installer.Plugin));
        Tip("Advanced.DesktopInstallReleaseTip");
        if(installer.Status is { } status)ImGui.TextColored(status.Warning ? Warning : Muted,localization.Format(status.Key,status.Args));
        if(desktopOpenProblem is { } openProblem)ImGui.TextColored(Warning,localization.Get(openProblem));
        if(view.Kind is not (DesktopInstallKind.NotInstalled or DesktopInstallKind.Missing) && desktopState is { } state)
        {
            if(SettingBox("Advanced.DesktopAutoUpdate",state.AutoUpdate,value=>installer.SetAutoUpdate(value),true))desktopView=null;
        }
    }
}
