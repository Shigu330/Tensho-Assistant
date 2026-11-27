using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1901, GPT review of 11/11, accepted by the user): the diagnostics family out of Plugin.cs, after the map family (0.1900). The
// self-check runner and the reading capture (developer mode) are each created in one install path and released in one release path of the
// plugin's registrations, in this file; Plugin.cs calls each where its block stood, so the order of the registrations is the one of before.
// DiagnosticsFamily1901Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>0.494 (user request): the self-check, on its own in developer mode, on request otherwise; what it has seen, the game data's
    /// versions and the launch proof kept beside the settings.</summary>
    private void RegisterSelfCheck(IDalamudPluginInterface pluginInterface,Configuration configuration,IDataManager data,IFramework framework,IClientState client,IPlayerState player)
    {
        registrations.Register(()=>selfCheck=new Diagnostics.SelfCheckRunner(framework,client,player,log,()=>configuration.DeveloperMode,mainWindow.SelfCheckReady,mainWindow.PrepareSelfCheck,mainWindow.SelfCheckProbes,mainWindow.SelfCheckWindows,
            Path.Combine(pluginInterface.GetPluginConfigDirectory(),"SelfCheckSeen.json"),string.Join(";",data.GameData.Repositories.Values.OrderBy(repository=>repository.Name).Select(repository=>$"{repository.Name}:{repository.Version}")),
            Path.Combine(pluginInterface.GetPluginConfigDirectory(),Diagnostics.LaunchProof.FileName),pluginInterface.AssemblyLocation.FullName),()=>selfCheck?.Dispose());
        mainWindow.SelfCheck=selfCheck;
    }

    /// <summary>The reading capture of developer mode: numbers read in the game's windows and objects, on change, for the probes.</summary>
    private void RegisterReadingCapture(Configuration configuration,IFramework framework,IObjectTable objects,ICondition condition,IGameGui gameGui,IChatGui chat)
    {
        Diagnostics.ReadingCapture? readingCapture=null;
        registrations.Register(()=>readingCapture=new Diagnostics.ReadingCapture(framework,objects,condition,gameGui,chat,()=>configuration.DeveloperMode,log),()=>readingCapture?.Dispose());
    }
}
