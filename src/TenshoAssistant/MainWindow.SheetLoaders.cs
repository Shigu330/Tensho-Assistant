
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

/// <summary>
/// X-285 (0.1300, the user's look at the gallery on 13/11): the page loaders that read only the game's sheets (no player, no game memory),
/// given by one method. The plugin calls it (Plugin.cs) and so does the desktop application (its Host, as ConfigureGarden): a page reads
/// its sheets the same way on both sides. Before, only Plugin.cs gave them, and the desktop application drew « the sheets could not be
/// read » (allied societies, Grand Companies, island, custom deliveries, Masked Carnivale, relic weapons, ocean fishing) or a load that
/// never ended (FATEs, field content, deep dungeons); its reference pictures kept those errors. Each one is read on demand, off the frame
/// where the page asks so, and a failure is logged and gives null (the page says so).
/// </summary>
internal sealed partial class MainWindow
{
    /// <summary>
    /// X-284 M2 (review of block 0.1278): the pages that read their sheets once, when they first open (allied societies, relic weapons),
    /// forget what they read: their next draw reads again with the loaders given last. The desktop application's reference pictures of the
    /// error state use it (sheets that cannot be read, as after a game update, R08), then give the real loaders back.
    /// </summary>
    internal void ForgetSheetPages()
    {
        alliedCatalog=null;alliedTried=false;alliedFailed=false;alliedRead=null;allied=null;alliedLoad=null;
        relicCatalog=null;relicCatalogTried=false;relicCatalogTask=null;relicReadFailed=false;relicRead=null;relicProgress=null;relicLines.Clear();
    }

    internal void UseSheetLoaders(Dalamud.Plugin.Services.IDataManager data,Dalamud.Plugin.Services.IPluginLog log)
    {
        T? Read<T>(string what,Func<T?> read) where T:class
        {
            try{return read();}
            catch(Exception error){log.Warning(error,"{What} unreadable (game update?).",what);return null;}
        }
        // g, R-01 (0.392): ocean fishing timetable from the game's loop and voyages, built on first use.
        var ocean=new Lazy<Gathering.OceanSchedule?>(()=>Read("Ocean fishing timetable",()=>Gathering.OceanScheduleLoader.Read(data)));
        OceanTimetable=()=>ocean.Value;
        // N-10 (0.671): the island's workshops, read on the player's click.
        IslandCatalogLoad=()=>Read("Island sheets",()=>Island.IslandCatalogLoader.Read(data));
        // W-01 (0.675): the custom delivery clients, read when the page opens and on the player's click.
        DeliveryClientsLoad=()=>Read("Custom delivery sheets",()=>Deliveries.DeliveryClientLoader.Read(data));
        // W-14b (0.721): every FATE's place, from the game's zone layouts, off the frame.
        FatesLoad=()=>Read("FATE places",()=>Fields.FieldCatalogLoader.ReadOpenZones(data,log));
        // W-16 (0.719): the lexicon, from the sheets, off the frame.
        LexiconLoad=()=>Read("Lexicon sheets",()=>Lexicon.LexiconLoader.Read(data));
        // W-15 (0.718): the deep dungeons, from the sheets, off the frame.
        DeepDungeonLoad=()=>Read("Deep dungeon sheets",()=>DeepDungeons.DeepDungeonLoader.Read(data));
        // W-14 (0.717): Eureka and Bozja, from the game's files, off the frame.
        FieldCatalogLoad=()=>Read("Eureka and Bozja sheets",()=>Fields.FieldCatalogLoader.Read(data,log));
        // W-11 (0.714): the Grand Companies, read when the page opens and on the player's click.
        GcCatalogLoad=feminine=>Read("Grand Company sheets",()=>GrandCompanies.GcCatalogLoader.Read(data,feminine));
        // W-10 (0.713): the allied societies, read when the page opens and on the player's click.
        AlliedCatalogLoad=()=>Read("Allied society sheets",()=>Societies.AlliedCatalogLoader.Read(data));
        // W-03 (0.708): the Zodiac weapons, read when the page opens and on the player's click.
        RelicCatalogLoad=()=>Read("Relic sheets",()=>Relics.RelicCatalogLoader.Read(data));
        // W-02 (0.683): the stages of the Masked Carnivale, read when the page opens and on the player's click.
        CarnivaleStagesLoad=()=>Read("Masked Carnivale sheets",()=>Carnivale.CarnivaleStageLoader.Read(data));
    }

    /// <summary>X-285 (0.1300): the text of a page whose data is read in the game only (a reader of the player's state): in the desktop
    /// application it never comes, so the page says « read in the game » there instead of a load that never ends.</summary>
    private string LoadingText(string key)=>localization.Get(inDesktopApp ? "Desktop.ReadInGame":key);

    /// <summary>X-285: where a page shows what it read of the player (allied societies, Grand Companies, island, deliveries, carnivale,
    /// relic book), the desktop application says it is read in the game (it has no reader of the game), instead of nothing.</summary>
    private void ReadInGameNote(){if(inDesktopApp)ImGui.TextDisabled(localization.Get("Desktop.ReadInGame"));}
}
