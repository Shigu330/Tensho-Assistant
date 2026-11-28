using Dalamud.Plugin.Services;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// X-153 (0.1940, GPT review of 11/11, accepted by the user): the inventory family's third slice out of Plugin.cs, after the gear wear (0.1939).
// The inventory's visual sort, the item under the mouse and the inventory's reader are each created in one install path and released in one
// release path of the plugin's registrations, in this file; Plugin.cs calls each where its block stood, so the order of the registrations is
// the one of before. InventoryReadersFamily1940Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The inventory's visual sort: the game's inventory read, the order Tensho computes shown over it.</summary>
    private void RegisterVisualSort(IGameInventory inventory,IPlayerState player,IClientState client,IFramework framework,BankInventoryBridge bankBridge)
    {
        InventoryVisualSortBridge? sortBridge=null;
        registrations.Register(()=>sortBridge=new InventoryVisualSortBridge(visualSort,inventory,player,client,framework,log,bankBridge),()=>sortBridge?.Dispose());
    }

    /// <summary>The item under the mouse in the game's windows (its price, its uses), hidden in duties as the map's « Hide in duties ».</summary>
    private void RegisterItemHover(Configuration configuration,IGameGui gameGui,IPlayerState player,IClientState client,IFramework framework,IKeyState keyState)
    {
        ItemHoverReader? hoverReader = null;
        registrations.Register(() => hoverReader = new ItemHoverReader(configuration, market, itemHover, itemCatalog, gameGui, player, client, framework, log) { InDuty = () => mapLive?.State is { InDuty: true }, Keys = keyState, Developer=()=>configuration.DeveloperMode, },
            () => hoverReader?.Dispose());
    }

    /// <summary>The player's inventory, read from the game into the inventory service.</summary>
    private void RegisterInventoryReader(IGameInventory inventory,IPlayerState player,IClientState client,IFramework framework)
    {
        DalamudInventoryReader? inventoryReader = null;
        registrations.Register(() => inventoryReader = new DalamudInventoryReader(inventoryService, inventory, player, client, framework, log),
            () => inventoryReader?.Dispose());
    }
}
