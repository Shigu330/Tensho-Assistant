using Dalamud.Plugin.Services;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

// X-153 (0.1927, GPT review of 11/11, accepted by the user): the mini-games' family out of Plugin.cs, after the gathering readers (0.1926). The
// Triple Triad NPCs' loader, the Mini Cactpot reader and the Triple Triad reader are each created in one install path and released in one
// release path of the plugin's registrations, in this file; Plugin.cs calls the two blocks where they stood, so the order of the registrations
// is the one of before. MiniGamesFamily1927Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The Triple Triad NPCs' catalogue, built from the game's sheets, and which of them the character has beaten; given to the window.</summary>
    private void RegisterTriadNpcs(IDataManager data,IFramework framework,IClientState client,IPlayerState player,IUnlockState unlocks)
    {
        var triadNpcs=new TriadNpcService();
        TriadNpcLoader? triadNpcLoader=null;
        registrations.Register(()=>triadNpcLoader=new TriadNpcLoader(triadNpcs,data,framework,client,player,unlocks,log),()=>triadNpcLoader?.Dispose());
        mainWindow.ConfigureTriadNpcs(triadNpcs);
    }

    /// <summary>The Mini Cactpot ticket and the Triple Triad board, read from their game windows while they are open.</summary>
    private void RegisterMiniGameReaders(IFramework framework,IPlayerState player,IClientState client,IGameGui gameGui,ITargetManager targets,IDataManager data)
    {
        TriadObservationReader? triadReader = null;
        MiniCactpotObservationReader? cactpotReader=null;
        registrations.Register(()=>cactpotReader=new MiniCactpotObservationReader(cactpotObservation,framework,player,client,gameGui,log),()=>cactpotReader?.Dispose());
        registrations.Register(() => triadReader = new TriadObservationReader(triadObservation, framework, player, client, gameGui, log, () => targets.Target?.BaseId ?? 0, data, () => mainWindow.DeveloperMode),
            () => triadReader?.Dispose());
    }
}
