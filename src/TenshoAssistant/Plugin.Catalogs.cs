using Dalamud.Plugin.Services;
using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// X-153 (0.1929, GPT review of 11/11, accepted by the user): the catalogues built from the game sheets, out of Plugin.cs, after the items'
// family (0.1928). The NPC vendors', maps', weather, GP actions', recipes' and gathering points' loaders are each created in one install path
// and released in one release path of the plugin's registrations, in this file; Plugin.cs calls the four blocks where they stood, so the order
// of the registrations is the one of before. CatalogsFamily1929Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The NPC vendors' catalogue from the game sheets, given to the window.</summary>
    private void RegisterNpcVendors(IDataManager data,IFramework framework)
    {
        var npcVendors=new Npcs.NpcVendorService();
        Npcs.NpcVendorLoader? npcLoader=null;
        registrations.Register(()=>npcLoader=new Npcs.NpcVendorLoader(npcVendors,data,framework,log,dataCache),()=>npcLoader?.Dispose());
        mainWindow.ConfigureNpcVendors(npcVendors);
    }

    /// <summary>The maps' catalogue from the game sheets.</summary>
    private void RegisterMapCatalog(IDataManager data,IFramework framework)
    {
        MapCatalogLoader? mapLoader = null;
        registrations.Register(() => mapLoader = new MapCatalogLoader(maps, data, framework, log), () => mapLoader?.Dispose());
    }

    /// <summary>The weather and the GP actions' catalogues from the game sheets.</summary>
    private void RegisterWeatherAndGpCatalogs(IDataManager data,IFramework framework)
    {
        GpCatalogLoader? gpLoader=null;
        WeatherCatalogLoader? weatherLoader=null;
        registrations.Register(()=>weatherLoader=new WeatherCatalogLoader(weather,data,framework,log),()=>weatherLoader?.Dispose());
        registrations.Register(()=>gpLoader=new GpCatalogLoader(gpActions,data,framework,log),()=>gpLoader?.Dispose());
    }

    /// <summary>The recipes' and the gathering points' catalogues from the game sheets, cached.</summary>
    private void RegisterRecipeAndGatheringCatalogs(IDataManager data,IFramework framework)
    {
        RecipeCatalogLoader? recipeLoader = null;
        registrations.Register(() => recipeLoader = new RecipeCatalogLoader(recipeCatalog, data, framework, log, dataCache),
            () => recipeLoader?.Dispose());
        GatheringCatalogLoader? gatheringLoader = null;
        registrations.Register(() => gatheringLoader = new GatheringCatalogLoader(gatheringCatalog, data, framework, log, dataCache),
            () => gatheringLoader?.Dispose());
    }
}
