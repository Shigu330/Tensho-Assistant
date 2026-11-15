using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1443, eleventh slice, GPT review of 11/11, accepted by the user): the chat family out of Plugin.cs, after the desktop (0.1225),
// persistence (0.1239), server bar (0.1240), panels (0.1249), discoveries (0.1250), voyages (0.1403), map (0.1404), gathering (0.1412), ocean
// fishing (0.1417) and chocobo (0.1419) families. The Tensho chat window's reader (X-403, in memory, only while the window is on), the chat's
// map links kept for the Maps page (M-03) and the chat map link under the mouse (X-365), each created in one install path and released in one
// release path of the plugin's registrations, in this file, in the order of before. Plugin.cs calls the family once, at the same place.
// ChatFamily1443Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The chat family: what Tensho reads of the game's chat, and what the main window reads from it.</summary>
    private void RegisterChat(Configuration configuration,IClientState client,IAddonLifecycle addonLifecycle)
    {
        // X-403 (0.1434, D-166): the Tensho chat window's copy of the messages, in memory, only while the window is on.
        Chat.ChatReader? chatReader=null;
        registrations.Register(()=>chatReader=new Chat.ChatReader(chatGui,client,log,()=>configuration.Chat,mainWindow.ChatMessages),()=>chatReader?.Dispose());
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
}
