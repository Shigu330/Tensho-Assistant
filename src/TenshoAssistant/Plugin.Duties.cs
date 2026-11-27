using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TenshoAssistant.Community;
using TenshoAssistant.Duties;

namespace TenshoAssistant;

// X-153 (0.1899, GPT review of 11/11, accepted by the user): the duties family out of Plugin.cs, after the inventory family (0.1897, 0.1898).
// The Duty Finder's reader and its loot service, the duty catalogue's loader and the duty progress reader are each created in one install
// path and released in one release path of the plugin's registrations, in this file; Plugin.cs calls each group where its block stood, so
// the order of the registrations is the one of before. DutiesFamily1899Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The Duty Finder: the game's window read while it is open, and the loot of the duty chosen in it (Garland, its own use).</summary>
    private void RegisterDutyFinder(Configuration configuration,IFramework framework,IGameGui gameGui)
    {
        var dutyFinder=new Duties.DutyFinderService();
        Duties.DutyFinderReader? dutyFinderReader=null;
        registrations.Register(()=>dutyFinderReader=new Duties.DutyFinderReader(dutyFinder,framework,gameGui,log,()=>mainWindow.DeveloperMode),()=>dutyFinderReader?.Dispose());
        var dutyFinderLoot=new DutyLootService(garland){Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.GarlandDutyLoot)};registrations.Register(()=>{},dutyFinderLoot.Dispose);
        mainWindow.ConfigureDutyFinder(dutyFinder,dutyFinderLoot);
    }

    /// <summary>The duty catalogue (game sheets, off the frame) and, g, C-01 (0.355): unlocked / completed duties, read on the framework
    /// thread while the Duty loot page shows them.</summary>
    private void RegisterDutyCatalog(IDalamudPluginInterface pluginInterface,IDataManager data,IFramework framework,IClientState client,IUnlockState unlocks,IPlayerState player)
    {
        DutyCatalogLoader? dutyLoader = null;
        registrations.Register(() => dutyLoader = new DutyCatalogLoader(duties, data, framework, log), () => dutyLoader?.Dispose());
        DutyProgressReader? dutyProgress = null;
        registrations.Register(() => dutyProgress = new DutyProgressReader(framework, client, unlocks, data, log, () => duties.State.Catalog, player, Path.Combine(pluginInterface.GetPluginConfigDirectory(), "DutyProgress.json")), () => dutyProgress?.Dispose());
        mainWindow.DutyProgressSource = () => { dutyProgress?.Want(); return dutyProgress?.State; };
        mainWindow.DutyProgressUnavailable = () => dutyProgress?.Unavailable == true;
    }
}
