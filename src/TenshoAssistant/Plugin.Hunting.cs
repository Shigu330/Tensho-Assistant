using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1896, GPT review of 11/11, accepted by the user): the hunting family out of Plugin.cs, after the field content family (0.1895).
// The S rank kills watcher and the hunting log reader are each created in one install path and released in one release path of the plugin's
// registrations, in this file; Plugin.cs calls each where its block stood, so the order of the registrations is the one of before. The S rank
// kills' save is passed in (SaveTimed is a local function of the constructor). HuntingFamily1896Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>X-222 (0.1232): the player's own kills for the S ranks counted by kills, from the game's « You defeat » message.</summary>
    private void RegisterSRankKills(Configuration configuration,IFramework framework,IClientState client,IObjectTable objects,IChatGui chat,Action save)
    {
        Hunting.SRankKillWatcher? srankKills=null;
        registrations.Register(()=>srankKills=new Hunting.SRankKillWatcher(framework,client,objects,chat){Killed=(monster,territory)=>
        {
            if(Hunting.SRankKills.KeysFor(monster,territory) is { Count:>0 } keys && Hunting.SRankKills.Add(configuration.SRankKills ??= [],keys))save();
        }},()=>srankKills?.Dispose());
    }

    /// <summary>0.364: the hunting log's ranks and kills, read in the game's memory when the Hunting page asks.</summary>
    private void RegisterHuntingLog(IFramework framework,IClientState client)
    {
        Hunting.HuntingLogReader? huntReader=null;
        registrations.Register(()=>huntReader=new Hunting.HuntingLogReader(framework,client,log),()=>huntReader?.Dispose());
        mainWindow.HuntGrandCompany=()=>huntReader?.GrandCompany;
        mainWindow.HuntSlots=()=>{huntReader?.Want();return huntReader?.State;};
        mainWindow.HuntUnavailable=()=>huntReader?.Unavailable==true;
    }
}
