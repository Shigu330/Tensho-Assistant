using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// X-153 (0.1239, second slice, GPT review of 11/11, accepted by the user): the persistence family out of Plugin.cs, after the desktop family
// (0.1225, Plugin.Desktop.cs). The player's data file and the bank kept in it, with the bank's bridge to the game's inventory: each service
// is created in one install path and released in one release path of the plugin's registrations (RegistrationScope: released in the
// reverse order; a failure does not stop the others), all in this file; Plugin.cs only calls the family. PersistenceFamily1239Tests counts
// those paths over every Plugin*.cs file.
public sealed partial class Plugin
{
    /// <summary>
    /// The persistence family: the player's data file (TenshoData.db, X-173: every family moved into it, opened on first use), the bank
    /// (0.345, I-03: one entry per character; X-173, 0.1149: kept in TenshoData.db, the files of before converted at the first load and kept
    /// aside) and its bridge to the game's inventory. Made before the main window, which shows them.
    /// </summary>
    private (TenshoDataFile Data,InventoryBankService Bank,BankInventoryBridge Bridge) RegisterPersistence(IDalamudPluginInterface pluginInterface,
        Configuration configuration,IDataManager data,IGameInventory inventory,IGameGui gameGui,IPlayerState player,IClientState client,IFramework framework)
    {
        var folder=pluginInterface.GetPluginConfigDirectory();
        // TenshoDataFile holds no open handle (each use opens and closes the database): nothing to release.
        var tenshoData=new TenshoDataFile(Path.Combine(folder,TenshoDataDatabase.FileName),(error,message)=>log.Warning(error,message),message=>log.Information(message));
        InventoryBankService? bank=null;
        registrations.Register(()=>bank=new InventoryBankService(new PerCharacterBankStore(Path.Combine(folder,"InventoryBank"),Path.Combine(folder,"InventoryBank.json"),data:tenshoData)),
            ()=>{bank?.Dispose();bank=null;});
        IReadOnlyList<(uint,uint)>? cabinet=null;
        BankInventoryBridge? bridge=null;
        registrations.Register(()=>bridge=new BankInventoryBridge(bank!,inventory,gameGui,player,client,framework,log,()=>configuration.AutoBankCapture,
                ()=>cabinet??=data.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>().Where(row=>row.Item.RowId!=0).Select(row=>(row.RowId,row.Item.RowId)).ToArray(),
                set=>data.GetExcelSheet<Lumina.Excel.Sheets.MirageStoreSetItem>().TryGetRow(set,out var outfit)
                    ? new[]{outfit.MainHand.RowId,outfit.OffHand.RowId,outfit.Head.RowId,outfit.Body.RowId,outfit.Hands.RowId,outfit.Legs.RowId,
                        outfit.Feet.RowId,outfit.Earrings.RowId,outfit.Necklace.RowId,outfit.Bracelets.RowId,outfit.Ring.RowId}
                    : []),
            ()=>{bridge?.Dispose();bridge=null;});
        return (tenshoData,bank!,bridge!);
    }
}
