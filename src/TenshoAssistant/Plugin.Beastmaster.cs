using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1886, GPT review of 11/11, accepted by the user): the Beastmaster family out of Plugin.cs, the first family of the game's readers
// (after the desktop, persistence, server bar and panel families). Each service is created in one install path and released in one release
// path of the plugin's registrations (RegistrationScope: released in the reverse order; a failure does not stop the others), all in this
// file; Plugin.cs calls the family at the two places these services were made, so the order of the registrations is the one of before.
// BeastFamily1886Tests counts those paths over every Plugin*.cs file.
public sealed partial class Plugin
{
    /// <summary>The Beastmaster catalogue (beasts, boards, ranks) and its loader, read from the game's sheets once the character is in.</summary>
    private Beastmaster.BeastService RegisterBeastCatalog(IDataManager data,IFramework framework,IClientState client,IPlayerState player)
    {
        var beasts=new Beastmaster.BeastService();
        Beastmaster.BeastCatalogLoader? beastLoader=null;
        registrations.Register(()=>beastLoader=new Beastmaster.BeastCatalogLoader(beasts,data,framework,client,player,log),()=>beastLoader?.Dispose());
        mainWindow.Beasts=beasts;
        return beasts;
    }

    /// <summary>The game's Beastmaster windows: the board reader and the panel beside the board, the familiar on the field and the hotbars
    /// for the combo highlight (off by default), and the developer probe of the windows.</summary>
    private void RegisterBeastBoard(Configuration configuration,IDataManager data,IFramework framework,IGameGui gameGui,IObjectTable objects)
    {
        registrations.Register(()=>boardReader=new Beastmaster.BeastBoardReader(framework,gameGui,log,()=>mainWindow.Beasts?.State.Catalog?.Beasts,mainWindow.BeastRanksRead,mainWindow.BeastResultRead,mainWindow.BeastBookRead)
            {ItemIcon=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(id)?.Icon ?? 0u},()=>boardReader?.Dispose());
        registrations.Register(()=>{boardWindow=new BeastBoardWindow(mainWindow){Windows=()=>boardReader?.State ?? default};boardWindows.AddWindow(boardWindow);},()=>{if(boardWindow is not null)boardWindows.RemoveWindow(boardWindow);});
        showBoardPanel=()=>configuration.ShowBeastBoardPanel;
        // X-549 (0.1522): the board the game's board or team window is about, for the panel and the page.
        mainWindow.BeastBoardContent=()=>boardReader?.State.Content ?? 0;
        // X-550 (0.1526): the team the game's team window shows.
        mainWindow.BeastTeamRead=()=>boardReader?.State.Team;
        // X-486 (0.1529): the fights the game's board window shows.
        mainWindow.BeastFightsShown=()=>boardReader?.State.Shown;
        // X-136 (0.1550): the beast called and the hotbars for the combo highlight (off by default). X-655 (0.1599): the beast read on the
        // field first (the player's familiar), the battlehorns window only when none is found (the window is closed during the fights).
        Beastmaster.BeastFamiliarReader? familiarReader=null;
        registrations.Register(()=>familiarReader=new Beastmaster.BeastFamiliarReader(framework,objects,log,()=>configuration.HighlightBeastCombos,()=>mainWindow.Beasts?.State.Catalog?.Beasts,()=>boardReader?.ActiveBeast ?? 0),()=>familiarReader?.Dispose());
        mainWindow.BeastCalledNow=()=>familiarReader?.Called ?? Beastmaster.BeastCalledRead.None;
        Gathering.ActionBarLocator? beastBarLocator=null;
        registrations.Register(()=>mainWindow.BeastBarLocator=beastBarLocator=new Gathering.ActionBarLocator(framework,gameGui,log),()=>{mainWindow.BeastBarLocator=null;beastBarLocator?.Dispose();});
        // X-136 (0.1530): the names of the beasts' Tricks and of the beastmaster's skills, from the game's Action sheet.
        mainWindow.BeastActionName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";
        mainWindow.BeastBoardOfTerritory=territory=>mainWindow.Beasts?.State.Catalog?.Boards.FirstOrDefault(board=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>().GetRowOrDefault(board.Content)?.TerritoryType.RowId==territory)?.Id ?? 0;
        Beastmaster.BeastWindowProbe? beastProbe=null;
        registrations.Register(()=>beastProbe=new Beastmaster.BeastWindowProbe(framework,gameGui,log,()=>configuration.DeveloperMode),()=>beastProbe?.Dispose());
    }
}
