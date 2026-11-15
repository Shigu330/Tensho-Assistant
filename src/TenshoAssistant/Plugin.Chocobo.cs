using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1419, tenth slice, GPT review of 11/11, accepted by the user): the chocobo family out of Plugin.cs, after the desktop (0.1225),
// persistence (0.1239), server bar (0.1240), panels (0.1249), discoveries (0.1250), voyages (0.1403), map (0.1404), gathering (0.1412) and
// ocean fishing (0.1417) families. The companion chocobo's reader (its bardings) and the racing chocobo's reader, read on the framework thread
// while their pages show them, each created in one install path and released in one release path of the plugin's registrations, in this file,
// in the order of before; the racing ranks and abilities from the sheets. Plugin.cs calls the family once, at the same place.
// ChocoboFamily1419Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The chocobo family: the companion chocobo and the racing chocobo; and what the main window reads from them.</summary>
    private void RegisterChocobo(IDataManager data,IFramework framework,IClientState client,IPlayerState player,IUnlockState unlocks,ICondition condition)
    {
        // 0.340 (R-11 step 4): the companion chocobo and its bardings, read on the framework thread while the page shows them.
        Gear.ChocoboReader? chocobo=null;
        registrations.Register(()=>chocobo=new Gear.ChocoboReader(framework,client,player,unlocks,data,log),()=>chocobo?.Dispose());
        mainWindow.Chocobo=()=>{chocobo?.Want();return chocobo?.State;};
        mainWindow.ChocoboUnavailable=()=>chocobo?.Unavailable==true;
        // X-204 (0.1235): the racing chocobo, read only once the game loaded it (never asked of the server); ranks and abilities from the sheets.
        MiniGames.RaceChocoboReader? raceChocobo=null;
        registrations.Register(()=>raceChocobo=new MiniGames.RaceChocoboReader(framework,client,player,condition,log),()=>raceChocobo?.Dispose());
        mainWindow.RaceChocobo=()=>{raceChocobo?.Want();return raceChocobo?.State;};
        mainWindow.RaceChocoboUnavailable=()=>raceChocobo?.Unavailable==true;
        mainWindow.RaceChocoboRanks=new Lazy<MiniGames.RaceChocoboRank[]>(()=>LoadSheet("ChocoboRaceRank",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ChocoboRaceRank>()
            .Where(row=>row.Name.RowId!=0).Select(row=>new MiniGames.RaceChocoboRank(row.RowId,row.RatingMin,row.RatingMax,row.Name.ValueNullable?.Text.ExtractText() ?? "")).Where(rank=>rank.Name.Length>0).ToArray(),[]));
        mainWindow.RaceChocoboAbility=id=>id==0 ? null:data.GetExcelSheet<Lumina.Excel.Sheets.ChocoboRaceAbility>().GetRowOrDefault(id) is { } ability && !ability.Name.IsEmpty
            ? new MiniGames.RaceChocoboAbility(id,ability.Name.ExtractText(),ability.Description.ExtractText()):null;
    }
}
