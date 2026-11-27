using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1888, GPT review of 11/11, accepted by the user): the gear family out of Plugin.cs, after the Beastmaster (0.1886) and retainer
// market (0.1887) families. Each service is created in one install path and released in one release path of the plugin's registrations
// (RegistrationScope: released in the reverse order; a failure does not stop the others), all in this file; Plugin.cs calls the family
// where these services were made, so the order of the registrations is the one of before. GearFamily1888Tests counts those paths over
// every Plugin*.cs file.
public sealed partial class Plugin
{
    /// <summary>The gear family: the gear loader (the catalogue and what is worn), the game's gear sets and job levels read while the Gear
    /// page shows them, and the bonus points left of a manual's Grace. Hands the three back: the plugin's later wiring reads them.</summary>
    private (Gear.GearLoader? Gear,Gear.GameGearsetReader? Gearsets,Gear.GraceWatcher? Grace) RegisterGear(Configuration configuration,IDataManager data,
        IFramework framework,IClientState client,IPlayerState player,IObjectTable objects,IChatGui chat,Action saveGrace)
    {
        Gear.GearLoader? gear=null;
        registrations.Register(()=>gear=new Gear.GearLoader(framework,data,client,log),()=>gear?.Dispose());
        // g, M-12 (0.383): the game's gear sets and the job levels (ClassJob ExpArrayIndex), read while the Gear page shows them.
        // R-14 (0.452): a sheet read here that no longer loads threw out of this constructor and Dalamud refused the whole plugin; it now
        // turns its own feature off (logged).
        var expIndex=LoadSheet("ClassJob",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().Where(row=>row.ExpArrayIndex>=0 && !row.Name.IsEmpty).ToDictionary(row=>row.RowId,row=>(int)row.ExpArrayIndex),[]);
        // T21 (0.646): the game's order of the jobs and the experience of each level.
        var jobOrder=LoadSheet("ClassJob",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().ToDictionary(row=>row.RowId,row=>(int)row.UIPriority),[]);
        var expToNext=LoadSheet("ParamGrow",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ParamGrow>().ToDictionary(row=>(int)row.RowId,row=>row.ExpToNext),[]);
        Gear.GameGearsetReader? gameGearsets=null;
        registrations.Register(()=>gameGearsets=new Gear.GameGearsetReader(framework,client,log,expIndex,jobOrder,expToNext),()=>gameGearsets?.Dispose());
        mainWindow.GearsetSource=gameGearsets;
        // X-221 (0.1760, the user's request of 12/11, K-37): the bonus points left of a manual's Grace, counted from your experience lines.
        Gear.GraceWatcher? graceWatcher=null;
        registrations.Register(()=>graceWatcher=new Gear.GraceWatcher(framework,client,player,objects,chat,log,expIndex,()=>configuration.GraceCounts??=[],saveGrace,
            status=>data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(status)?.Name.ExtractText() ?? status.ToString(System.Globalization.CultureInfo.InvariantCulture)),()=>graceWatcher?.Dispose());
        return (gear,gameGearsets,graceWatcher);
    }
}
