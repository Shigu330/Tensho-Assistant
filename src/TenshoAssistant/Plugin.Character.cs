using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TenshoAssistant.Character;

namespace TenshoAssistant;

// X-153 (0.1930, GPT review of 11/11, accepted by the user): the character's family out of Plugin.cs, after the catalogues (0.1929). The
// character's status watcher (one status file per character: ventures, counters, « done » boxes, retainers) and the character's attributes'
// reader are each created in one install path and released in one release path of the plugin's registrations, in this file; Plugin.cs calls
// the two blocks where they stood, so the order of the registrations is the one of before. CharacterFamily1930Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The character's status file, with the daily roulettes, the retainers' jobs and towns named from the sheets and the items on sale.</summary>
    private void RegisterCharacterStatus(IDalamudPluginInterface pluginInterface,IDataManager data,IFramework framework,IPlayerState player,
        Reminders.RouletteRow[] dailyRoulettes,Market.RetainerSalesReader? retainerSales)
    {
        // g, M-04 (0.374): one status file per character (Characters/<local character id>.json): ventures, counters, « done » boxes.
        Character.CharacterStatusWatcher? characterStatus=null;
        registrations.Register(()=>characterStatus=new Character.CharacterStatusWatcher(framework,player,log,
            ()=>dailyRoulettes,Path.Combine(pluginInterface.GetPluginConfigDirectory(),"Characters"),
            // X-196 (0.1409): the retainers' job and market town, named from the sheets (ClassJob abbreviation; Town, whose rows are RetainerTown's values).
            job=>data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().GetRowOrDefault(job)?.Abbreviation.ExtractText() ?? "",
            town=>data.GetExcelSheet<Lumina.Excel.Sheets.Town>().GetRowOrDefault(town)?.Name.ExtractText() ?? "",
            // X-469 (0.1762, D-174): the open retainer's items on sale, as the sale list reader copies them.
            sales:()=>retainerSales?.State),()=>characterStatus?.Dispose());
        mainWindow.CharacterStatusSource=characterStatus;
    }

    /// <summary>The character's attributes, read from the game.</summary>
    private void RegisterCharacterStats(IDataManager data,IFramework framework,IPlayerState player,IClientState client,IObjectTable objects,IGameGui gameGui)
    {
        CharacterStatsReader? statsReader = null;
        registrations.Register(() => statsReader = new CharacterStatsReader(characterStats, framework, player, client, log,objects,data,gameGui), () => statsReader?.Dispose());
    }
}
