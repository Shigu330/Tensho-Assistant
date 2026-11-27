using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1897, GPT review of 11/11, accepted by the user): the inventory family out of Plugin.cs, first slice, after the hunting family
// (0.1896). The storage windows' highlight, the game's inventory window and the gear sets read on request are each created in one install
// path and released in one release path of the plugin's registrations, in this file; Plugin.cs calls each where its block stood, so the
// order of the registrations is the one of before. InventoryFamily1897Tests counts those paths. 0.1898, second slice: the currency watcher
// and the gil journal's watcher, handed back to the constructor, which gives them to the server bar (InventoryFamily1898Tests).
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

    /// <summary>0.344 (g, I-02): currency balances and threshold alerts (chat and sound with the alarm options, Discord in its own category),
    /// and their server bar entry. Registered after the Discord notifier, so released before it. Returns the watcher, which the server bar reads.</summary>
    private Inventory.CurrencyWatcher? RegisterCurrencies(Configuration configuration,IFramework framework,IClientState client,IPlayerState player,IDataManager data,
        ICondition condition,IChatGui chat,IObjectTable objects,Alarms.DiscordNotifier discord)
    {
        Inventory.CurrencyWatcher? currencies=null;
        // Framework thread. One chat line and one sound for one alert; silence in duties as for the alarms.
        void CurrencyNotice(string message)
        {
            var options=configuration.AlarmOptions ?? new();
            var inDuty=options.MuteInDuties && condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
            if(options.ChatAllowed(inDuty))chat.Print("[Tensho] "+message);
            if(options.SoundAllowed(inDuty))Alarms.AlarmService.PlaySound(options.Sound,log);
            if(options.ChatAllowed(inDuty) || options.SoundAllowed(inDuty))linkNotices.Add(message);
            var settings=configuration.Discord;
            if(Alarms.DiscordWebhook.ShouldSend(settings,Alarms.AlarmSource.Currency))
                try
                {
                    var character=settings.IncludeCharacter ? objects.LocalPlayer?.Name.TextValue:null;
                    var content=Alarms.DiscordWebhook.Format(settings,message,message,message,0,DateTimeOffset.Now,character);
                    discord.Enqueue(new(Alarms.DiscordWebhook.UrlFor(settings,Alarms.AlarmSource.Currency),Alarms.DiscordWebhook.Payload(settings,content)));
                }
                catch(Exception error){log.Warning("Currency alert could not be queued for Discord ({Error}).",error.GetType().Name);}
        }
        registrations.Register(()=>currencies=new Inventory.CurrencyWatcher(framework,client,player,data,configuration,log)
        {
            Crossed=crossed=>CurrencyNotice(localization.Format("Currencies.Alert",string.Join(" · ",crossed.Select(Alarms.CurrencyDtrEntry.Line)))),
            // 0.606: the weekly limit of the limited tomestones reached.
            WeeklyCapReached=(got,limit)=>CurrencyNotice(localization.Format("Currencies.WeeklyCapAlert",got.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current),limit.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current))),
        },()=>currencies?.Dispose());
        mainWindow.Currencies=()=>currencies;
        return currencies;
    }

    /// <summary>0.351 (g, I-04): gil journal per character, kept 90 days; since 0.1139 (X-173) in TenshoData.db; the files of before
    /// (GilJournal/&lt;id&gt;.json) left aside, never read (X-574, 0.1793). Returns the watcher, which the server bar reads.</summary>
    private Inventory.GilWatcher? RegisterGilJournal(IDalamudPluginInterface pluginInterface,IFramework framework,IPlayerState player,IGameGui gameGui,ICondition condition,
        IObjectTable objects,TenshoDataFile tenshoData)
    {
        Inventory.GilWatcher? gilWatcher=null;
        var gilStore=new Inventory.GilJournalStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"GilJournal"),tenshoData);
        registrations.Register(()=>gilWatcher=new Inventory.GilWatcher(framework,player,gameGui,gilStore,log,
            ()=>mainWindow.GameWindowSource?.Invoke().Select(window=>window.Name).ToArray() ?? [],()=>mainWindow.DeveloperMode,chatGui),()=>gilWatcher?.Dispose());
        mainWindow.GilJournalState=()=>gilWatcher?.State;
        // X-557 (0.1774): a gain in a duty that nothing else explains is the duty's; the character's own names kept out of the « Other » context (R25).
        if(gilWatcher is not null)
        {
            gilWatcher.InDuty=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
            gilWatcher.OwnNames=()=>[player.CharacterName,player.HomeWorld.ValueNullable?.Name.ExtractText(),player.CurrentWorld.ValueNullable?.Name.ExtractText(),objects.LocalPlayer?.CompanyTag.TextValue];
        }
        return gilWatcher;
    }
}
