using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1225, GPT review of 11/11, accepted by the user): the plugin's wiring split by family, the desktop family first. Each service
// of a family is created in one install path and released in one release path of the plugin's registrations (RegistrationScope: released
// in the reverse order; a failure does not stop the others, all failures are thrown together at the end), all in the family's own file; Plugin.cs only calls the family.
// DesktopFamily1224Tests counts those paths over every Plugin*.cs file.
public sealed partial class Plugin
{
    /// <summary>The desktop family: the local link with the desktop application (X-119) and its installer (X-126).</summary>
    private void RegisterDesktop(IDalamudPluginInterface pluginInterface,Configuration configuration,IFramework framework,IClientState client,
        IPlayerState player,IGameInventory inventory,IObjectTable objects)
    {
        // X-119 step 2 (0.1081): the local link with the desktop application: a named pipe for the current user only, opened only while
        // Settings › Advanced « Link with the desktop app » is on; once a second, who and where (docs/evidence/x119-desktop-link.md).
        registrations.Register(()=>{linkFeed=new Link.LocalLinkFeed(framework,client,player,inventory,objects,pluginInterface.ConfigFile.FullName,pluginInterface.GetPluginConfigDirectory(),configuration,id=>mainWindow.TerritoryName?.Invoke(id),
            typeof(Plugin).Assembly.GetName().Version?.ToString(3) ?? "?",log,linkNotices){Held=mainWindow.AlarmHeldItems};mainWindow.LinkNotice=linkNotices.Add;mainWindow.DesktopLinkStatus=()=>linkFeed?.Status ?? (Link.LocalLinkState.Off,null);},
            ()=>{mainWindow.DesktopLinkStatus=null;mainWindow.LinkNotice=null;linkFeed?.Dispose();linkFeed=null;});
        // X-126 (0.1113, D-137): the desktop app installed from Settings › Advanced, on a click only; left out of a build made with
        // TenshoDesktopInstall=false. Requests only on « Install » / « Update » (this repository's release, docs/evidence/x126-desktop-install.md).
        if(Link.DesktopInstallRules.Enabled)
            registrations.Register(()=>mainWindow.DesktopInstaller=new Link.DesktopInstaller(
                    TenshoAssistant.Desktop.DesktopInstall.OwnFolder(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                    typeof(Plugin).Assembly.GetName().Version ?? new Version(0,0,0),new TenshoAssistant.Desktop.GitHubReleases(),message=>log.Information(message)),
                ()=>{mainWindow.DesktopInstaller?.Dispose();mainWindow.DesktopInstaller=null;});
    }
}
