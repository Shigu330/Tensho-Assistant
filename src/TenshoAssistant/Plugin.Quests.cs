using Dalamud.Plugin.Services;
using TenshoAssistant.Caching;
using TenshoAssistant.Collections;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// X-153 (0.1925, GPT review of 11/11, accepted by the user): the quests' family out of Plugin.cs, after the crafting family (0.1924). The quest
// reader and the collection reader (with the completion lists kept in Tensho's data file) are each created in one install path and released in
// one release path of the plugin's registrations, in this file; Plugin.cs calls the block where it stood, so the order of the registrations is
// the one of before. QuestsFamily1925Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The quests and the collections read from the game, cached for the time the settings give.</summary>
    private void RegisterQuests(Configuration configuration,IDataManager data,IFramework framework,IPlayerState player,IClientState client,IUnlockState unlocks,
        TenshoDataFile tenshoData)
    {
        QuestReader? questReader = null;
        registrations.Register(() => questReader = new QuestReader(quests, data, framework, player, client, unlocks, log,()=>configuration.Cache.Duration(CacheArea.Quests),dataCache),
            () => questReader?.Dispose());
        CollectionReader? collectionReader = null;
        registrations.Register(() => collectionReader = new CollectionReader(collections, data, framework, player, client, unlocks, log,()=>configuration.Cache.Duration(CacheArea.Collections),dataCache,()=>configuration.Cache.Duration(CacheArea.Completion),
            new CompletionListsData(tenshoData)),
            () => collectionReader?.Dispose());
    }
}
