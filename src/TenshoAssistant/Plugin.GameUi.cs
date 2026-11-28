using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1934, GPT review of 11/11, accepted by the user): the game interface's readers out of Plugin.cs, after the macros' family (0.1933).
// The game's theme reader, the game's windows' reader and the game window follower are each created in one install path and released in one
// release path of the plugin's registrations, in this file, with what the main window and the bank overlay read from them (let go with
// them); Plugin.cs calls the two blocks where they stood, so the order of the registrations is the one of before. GameUiFamily1934Tests
// counts those paths.
public sealed partial class Plugin
{
    /// <summary>The game's interface theme option, copied at start and on every change, given to the main window.</summary>
    private void RegisterGameTheme(IGameConfig gameConfig)
    {
        UI.GameThemeReader? themeReader=null;
        registrations.Register(()=>themeReader=new UI.GameThemeReader(gameConfig,log),()=>themeReader?.Dispose());
        mainWindow.ConfigureTheme(()=>themeReader?.GameValue);
    }

    /// <summary>The game's windows read for Tensho, the follower that copies each tick the position of the game windows a panel asked about, and the item link put in the chat.</summary>
    private void RegisterGameWindows(IFramework framework,IClientState client,IGameGui gameGui)
    {
        UI.GameWindowsReader? gameWindows=null;
        registrations.Register(()=>{gameWindows=new UI.GameWindowsReader(framework,client,gameGui,log,()=>mainWindow.DeveloperMode);mainWindow.GameWindowSource=()=>gameWindows.Windows;},()=>{mainWindow.GameWindowSource=null;gameWindows?.Dispose();});
        mainWindow.ItemLinkToChat=id=>chatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder().AddItemLink(id,false).BuiltString);
        UI.GameWindowFollower? gameWindowFollower=null;
        registrations.Register(()=>{gameWindowFollower=new UI.GameWindowFollower(framework,gameGui,log);mainWindow.GameWindowFollow=gameWindowFollower.At;bankOverlay.Follow=gameWindowFollower.At;},()=>{mainWindow.GameWindowFollow=null;bankOverlay.Follow=null;gameWindowFollower?.Dispose();});
    }
}
