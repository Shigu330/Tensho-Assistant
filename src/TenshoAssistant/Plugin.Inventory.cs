using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1897, GPT review of 11/11, accepted by the user): the inventory family out of Plugin.cs, first slice, after the hunting family
// (0.1896). The storage windows' highlight, the game's inventory window and the gear sets read on request are each created in one install
// path and released in one release path of the plugin's registrations, in this file; Plugin.cs calls each where its block stood, so the
// order of the registrations is the one of before. InventoryFamily1897Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>X-54 (0.820): the storage windows' cells holding what the lists need.</summary>
    private void RegisterStorageHighlight(Configuration configuration,IFramework framework,IGameGui gameGui,IGameInventory inventory)
    {
        Inventory.StorageHighlightReader? storageHighlight=null;
        registrations.Register(()=>mainWindow.StorageHighlight=storageHighlight=new Inventory.StorageHighlightReader(framework,gameGui,log,()=>configuration.DeveloperMode,inventory),()=>storageHighlight?.Dispose());
    }

    /// <summary>0.388 (user request): position of the game's inventory window, for the sort button drawn above it.</summary>
    private void RegisterInventoryWindow(Configuration configuration,IFramework framework,IGameGui gameGui)
    {
        Inventory.InventoryWindowReader? inventoryWindow=null;
        registrations.Register(()=>inventoryWindow=new Inventory.InventoryWindowReader(framework,gameGui,()=>configuration.InventorySortButton,log),()=>inventoryWindow?.Dispose());
        mainWindow.InventoryWindow=inventoryWindow;
    }

    /// <summary>0.348 (g, I-10): gear sets read on request for the storage report; stack sizes from the game data.</summary>
    private void RegisterGearsets(IFramework framework,IPlayerState player)
    {
        Inventory.GearsetReader? gearsets=null;
        registrations.Register(()=>gearsets=new Inventory.GearsetReader(framework,player,log),()=>gearsets?.Dispose());
        mainWindow.Gearsets=()=>gearsets;
    }
}
