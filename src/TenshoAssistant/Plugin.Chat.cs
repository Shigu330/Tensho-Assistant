using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1443, eleventh slice, GPT review of 11/11, accepted by the user): the chat family out of Plugin.cs, after the desktop (0.1225),
// persistence (0.1239), server bar (0.1240), panels (0.1249), discoveries (0.1250), voyages (0.1403), map (0.1404), gathering (0.1412), ocean
// fishing (0.1417) and chocobo (0.1419) families. The Tensho chat window's reader (X-403, in memory, only while the window is on), the chat's
// map links kept for the Maps page (M-03) and the chat map link under the mouse (X-365), each created in one install path and released in one
// release path of the plugin's registrations, in this file, in the order of before. Plugin.cs calls the family once, at the same place.
// ChatFamily1443Tests counts those paths. 0.1894: the rest of the family (the game chat's layout probe and hider, the chat window's player
// actions and the link watch), each block moved as it was and called where it stood (ChatFamily1894Tests).
public sealed partial class Plugin
{
    /// <summary>The chat family: what Tensho reads of the game's chat, and what the main window reads from it.</summary>
    private void RegisterChat(Configuration configuration,IClientState client,IAddonLifecycle addonLifecycle,IFramework framework,IPlayerState player,string pluginFolder,Chat.ChatSources? senders=null)
    {
        // X-403 (0.1434, D-166): the Tensho chat window's copy of the messages, in memory, only while the window is on. X-475: with the chat
        // history the player turned on (off by default), kept on this PC too, per character; the reader owns it and releases it.
        Chat.ChatReader? chatReader=null;
        registrations.Register(()=>
        {
            var history=new Chat.ChatHistoryKeeper(framework,client,player,log,()=>configuration.Chat,mainWindow.ChatMessages,pluginFolder);
            try{chatReader=new Chat.ChatReader(chatGui,client,log,()=>configuration.Chat,mainWindow.ChatMessages,()=>mainWindow.DeveloperMode,history,()=>player.CharacterName);}
            catch{history.Dispose();throw;}
            chatReader.Sources=senders;
            // X-733, X-734 (0.1734): the actions named in combat lines and the meals' statuses, read by the chat window's player actions.
            chatReader.LineData=()=>mainWindow.ChatPlayers;
            // X-447 B7 (0.1646): the alerts' and channels' sounds, the game's own (as the macros' <se.N>).
            chatReader.PlaySound=sound=>Alarms.AlarmService.PlayChatSound(sound,log);
            mainWindow.ChatHistoryFolder=history.Folder;
            mainWindow.EraseChatHistory=history.Erase;
            mainWindow.ChatHistoryBytes=()=>history.Bytes;
        },()=>chatReader?.Dispose());
        // g, M-03 (0.373): map links of the chat, kept for the Maps page this session (map settings switch, on by default).
        Maps.ChatMapLinkWatcher? chatLinks=null;
        registrations.Register(()=>chatLinks=new Maps.ChatMapLinkWatcher(chatGui,client,log,()=>configuration.MapView.ChatLinks),()=>chatLinks?.Dispose());
        mainWindow.ChatLinkHistory=chatLinks?.History;
        // X-365 (0.1351): the chat map link under the mouse, for the right-click Tensho menu (same map settings switch).
        Maps.ChatLinkHoverWatcher? chatLinkHover=null;
        registrations.Register(()=>chatLinkHover=new Maps.ChatLinkHoverWatcher(addonLifecycle,log,()=>configuration.MapView.ChatLinks,()=>mainWindow.DeveloperMode),
            ()=>chatLinkHover?.Dispose());
        mainWindow.ChatLinkHover=chatLinkHover?.Hover;
    }

    /// <summary>The game's own chat window: where it puts its parts (developer mode), and its messages hidden while Tensho's window shows them.</summary>
    private void RegisterGameChat(Configuration configuration,IFramework framework,IGameGui gameGui)
    {
        // X-467 (0.1502): where the game's chat puts its typing box and its message panels, in developer mode (read-only).
        Chat.ChatLogLayoutProbe? chatLayoutProbe=null;
        registrations.Register(()=>chatLayoutProbe=new Chat.ChatLogLayoutProbe(framework,gameGui,log,()=>configuration.DeveloperMode),()=>chatLayoutProbe?.Dispose());
        // X-467 (0.1504, D-181): the game chat's messages hidden while the option asks it, given back otherwise and on unload.
        Chat.GameChatHider? chatHider=null;
        registrations.Register(()=>{chatHider=new Chat.GameChatHider(framework,gameGui,log,mainWindow.ChatGameMessagesWant){DeveloperMode=()=>configuration.DeveloperMode,OptionWanted=mainWindow.ChatGameMessagesOption};mainWindow.ChatGamePlace=()=>chatHider?.Place;mainWindow.ChatGameChannelMenu=()=>chatHider?.ChannelMenuOpen==true;},()=>{mainWindow.ChatGamePlace=null;mainWindow.ChatGameChannelMenu=null;chatHider?.Dispose();});
    }

    /// <summary>What the chat window does with a player's name (its menu, a tell), and what the game's chat sends on such a click (developer mode).</summary>
    private void RegisterChatPlayers(Configuration configuration,IGameInteropProvider interop,IFramework framework,IClientState client,IObjectTable objects,ITargetManager targets,
        IPartyList partyList,IDataManager data,Chat.ChatSources chatSenders,IGameConfig gameConfig,ISeStringEvaluator seStrings,IContextMenu contextMenus)
    {
        Chat.ChatPlayerActions? chatPlayers=null;
        registrations.Register(()=>mainWindow.ChatPlayers=chatPlayers=new Chat.ChatPlayerActions(interop,framework,client,objects,targets,partyList,data,log,chatSenders,gameConfig,seStrings),
            ()=>{mainWindow.ChatPlayers=null;chatPlayers?.Dispose();chatSenders.Clear();});
        // X-715 (0.1718, D-202 b): developer mode, what the game's chat sends on a click on a player's name, read only.
        Chat.ChatLinkWatch? chatLinkWatch=null;
        registrations.Register(()=>chatLinkWatch=new Chat.ChatLinkWatch(interop,framework,contextMenus,log,()=>configuration.DeveloperMode),()=>chatLinkWatch?.Dispose());
    }
}
