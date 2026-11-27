using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1902, GPT review of 11/11, accepted by the user): the leves family out of Plugin.cs, after the diagnostics family (0.1901). The
// leve progress reader is created in one install path and released in one release path of the plugin's registrations, in this file;
// Plugin.cs calls it where its block stood, so the order of the registrations is the one of before. The leves' list is passed in (a local
// function of the constructor, built once from the game sheets); the reader is handed back. LevesFamily1902Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The leves accepted and completed, read in the game's memory when the Leves page asks. Returns the reader.</summary>
    private Leves.LeveProgressReader? RegisterLeveProgress(IFramework framework,IClientState client,IPlayerState player,IUnlockState unlocks,IDataManager data,
        Func<IReadOnlyList<Leves.LeveDefinition>> leveList)
    {
        Leves.LeveProgressReader? leveReader=null;
        registrations.Register(()=>leveReader=new Leves.LeveProgressReader(framework,client,player,unlocks,data,log,leveList),()=>leveReader?.Dispose());
        return leveReader;
    }
}
