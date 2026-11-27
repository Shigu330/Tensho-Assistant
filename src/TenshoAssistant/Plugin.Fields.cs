using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1895, GPT review of 11/11, accepted by the user): the field content family out of Plugin.cs, after the market (0.1893) and chat
// (0.1894) families. The shared FATE ranks and the Occult Crescent's state are each created in one install path and released in one release
// path of the plugin's registrations, in this file; Plugin.cs calls each where its block stood, so the order of the registrations is the one of
// before. The shared FATEs' save is passed in (SaveTimed is a local function of the constructor). FieldsFamily1895Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>X-213 (0.1781, the user's request of 12/11, K-23): the shared FATE ranks, read when the player opens the game's Shared FATE
    /// window, per character.</summary>
    private void RegisterSharedFates(Configuration configuration,IFramework framework,IClientState client,IPlayerState player,IGameGui gameGui,Action save)
    {
        Fields.SharedFateReader? sharedFates=null;
        registrations.Register(()=>sharedFates=new Fields.SharedFateReader(framework,client,player,gameGui,log,()=>configuration.SharedFates??=[],save),()=>sharedFates?.Dispose());
        mainWindow.SharedFatesNow=()=>sharedFates?.Shown;
    }

    /// <summary>X-188 (0.1176): the Occult Crescent's state, read on the island (the field content page shows the phantom jobs).</summary>
    private void RegisterOccultCrescent(IFramework framework)
    {
        Fields.OccultCrescentReader? occultReader=null;
        registrations.Register(()=>occultReader=new Fields.OccultCrescentReader(framework,log),()=>occultReader?.Dispose());
        mainWindow.OccultStateRead=()=>occultReader?.State;
        mainWindow.OccultOnIsland=()=>occultReader?.OnIsland==true;
    }
}
