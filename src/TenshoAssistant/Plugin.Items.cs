using Dalamud.Plugin.Services;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// X-153 (0.1928, GPT review of 11/11, accepted by the user): the items' family out of Plugin.cs, after the mini-games (0.1927). The desynthesis
// levels, the shop reminder, the items' uses loader, the Grand Company supply reader and the items' catalogue loader are each created in one
// install path and released in one release path of the plugin's registrations, in this file, with what the window reads from them; Plugin.cs
// calls the four blocks where they stood, so the order of the registrations is the one of before. ItemsFamily1928Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>An item's desynthesis (Item sheet) and the player's desynthesis levels, read on the framework thread.</summary>
    private void RegisterDesynthesis(IDataManager data,IFramework framework,IClientState client)
    {
        // X-197 (0.1761): an item's desynthesis (Item sheet) and your desynthesis levels (PlayerState, read on the framework thread).
        mainWindow.ItemDesynthOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(id) is { } desynthItem
            ? Items.ItemDesynth.Of(desynthItem.Desynth,desynthItem.ClassJobRepair.RowId,desynthItem.LevelItem.RowId):null;
        Items.DesynthesisLevels? desynthLevels=null;
        registrations.Register(()=>desynthLevels=new Items.DesynthesisLevels(framework,client,log),()=>desynthLevels?.Dispose());
        mainWindow.DesynthLevelOf=job=>desynthLevels?.Of(job);
    }

    /// <summary>At an NPC shop, the bags' items labelled « to sell » and « important » said in the chat (a sound if turned on).</summary>
    private void RegisterShopReminder(Configuration configuration,IDataManager data,IFramework framework,IGameGui gameGui,IPlayerState player,IChatGui chat)
    {
        // 0.347 (g, I-08): at an NPC shop, the bags' items labelled « to sell » and « important » (chat; sound if turned on).
        Items.ItemNoteShopReminder? shopReminder=null;
        registrations.Register(()=>shopReminder=new Items.ItemNoteShopReminder(framework,gameGui,configuration,()=>inventoryService.Current,()=>player.IsLoaded ? player.ContentId:0,
            (toSell,important)=>
            {
                var items=data.GetExcelSheet<Lumina.Excel.Sheets.Item>();
                string Names(IReadOnlyList<(uint Item,long Quantity)> list)=>string.Join(", ",list.Take(10).Select(entry=>$"{items.GetRowOrDefault(entry.Item)?.Name.ExtractText() ?? $"#{entry.Item}"} ×{entry.Quantity}"))
                    +(list.Count>10 ? " …":"");
                if(toSell.Count>0)chat.Print("[Tensho] "+localization.Format("ItemNotes.ShopToSell",Names(toSell)));
                if(important.Count>0)
                {
                    var message=localization.Format("ItemNotes.ShopImportant",Names(important));
                    chat.Print("[Tensho] "+message);
                    if(configuration.ItemNoteImportantSound){Alarms.AlarmService.PlaySound((configuration.AlarmOptions ?? new()).Sound,log);linkNotices.Add(message);}
                }
            },log),()=>shopReminder?.Dispose());
    }

    /// <summary>The items' uses from the game sheets; today's Grand Company request as last seen.</summary>
    private void RegisterItemUses(Configuration configuration,IDataManager data,IFramework framework)
    {
        ItemUseCatalogLoader? usageLoader=null;
        registrations.Register(()=>usageLoader=new ItemUseCatalogLoader(itemUses,data,framework,log),()=>usageLoader?.Dispose());
        // 0.469 (user request, in-game campaign): today's Grand Company request, read from the game's supply missions window.
        mainWindow.GcSupply.Restore(configuration.GcSupplySeen);
    }

    /// <summary>Today's Grand Company request read from its window (kept in the settings), and the items' catalogue from the game sheets.</summary>
    private void RegisterItemCatalog(Configuration configuration,IDataManager data,IFramework framework,IGameGui gameGui,IClientState client,IPlayerState player,
        SettingsSaver<Configuration> saver)
    {
        ItemCatalogLoader? catalogLoader = null;
        Items.GcSupplyReader? gcSupplyReader=null;
        registrations.Register(()=>gcSupplyReader=new Items.GcSupplyReader(framework,gameGui,client,player,data,log,()=>itemUses.State.Catalog,mainWindow.GcSupply,seen=>
        {
            configuration.GcSupplySeen=seen;
            try{saver.Save();}catch(Exception error){log.Warning(error,"Today's Grand Company request could not be saved.");}
        },developer:()=>configuration.DeveloperMode),()=>gcSupplyReader?.Dispose());
        registrations.Register(() => catalogLoader = new ItemCatalogLoader(itemCatalog, data, framework, log, dataCache),
            () => catalogLoader?.Dispose());
    }
}
