using Dalamud.Plugin.Services;
using TenshoAssistant.Localization;

namespace TenshoAssistant;

// X-153 (0.1240, third slice, GPT review of 11/11, accepted by the user): the server bar family out of Plugin.cs, after the desktop (0.1225)
// and persistence (0.1239) families. The five entries of the game's server bar (alarms, reminders, currencies, gil, and the optional
// information entries of X-217), each created in one install path and released in one release path of the plugin's registrations
// (RegistrationScope: released in the reverse order, before the services they show, which are registered earlier; a failure does not stop
// the others), all in this file, in the order they were created before (the order the game's bar first shows them). Plugin.cs only calls
// the family, once the services they show exist. ServerBarFamily1240Tests counts those paths over every Plugin*.cs file.
public sealed partial class Plugin
{
    /// <summary>
    /// The server bar family: the alarms' entry, the reminders' (0.455), the currencies' (0.344, g, I-02), the gil journal's and the optional
    /// entries (bags, gear condition, zone and instance, X-217). What they show is read through the functions given (null while a service
    /// is not running); the tooltips drawn by the plugin's window system reach them through the fields alarmBar, reminderBar, currencyBar, gilBar.
    /// </summary>
    private void RegisterServerBar(IDtrBar dtrBar,IFramework framework,IGameGui gameGui,IPlayerState player,IClientState client,IGameInventory inventory,
        Configuration configuration,LocalizationManager localization,Func<Inventory.CurrencyWatchState?> currencyState,
        Func<IReadOnlyList<Inventory.GilEntry>?> gilEntries,Func<IReadOnlyList<Gear.EquippedPiece>> equipped)
    {
        Alarms.AlarmDtrEntry? alarms=null;
        registrations.Register(()=>alarms=new Alarms.AlarmDtrEntry(dtrBar,framework,gameGui,configuration,localization.Get,mainWindow.OpenAlarms),()=>{alarms?.Dispose();alarms=null;});
        alarmBar=()=>alarms;
        // 0.455 (user request): reminders apart from alarms in the server bar.
        Alarms.ReminderDtrEntry? reminders=null;
        registrations.Register(()=>reminders=new Alarms.ReminderDtrEntry(dtrBar,framework,gameGui,player,log,configuration,localization.Get,mainWindow.OpenReminders,mainWindow.ReminderTodoLines,mainWindow.TickRemindersFromGame),
            ()=>{reminders?.Dispose();reminders=null;});
        reminderBar=()=>reminders;
        // 0.344 (g, I-02): the currency balances' entry; registered after the Discord notifier, so released before it.
        Alarms.CurrencyDtrEntry? currencies=null;
        registrations.Register(()=>currencies=new Alarms.CurrencyDtrEntry(dtrBar,framework,gameGui,configuration,currencyState,localization.Get,mainWindow.OpenCurrencies),
            ()=>{currencies?.Dispose();currencies=null;});
        currencyBar=()=>currencies;
        Inventory.GilDtrEntry? gil=null;
        registrations.Register(()=>gil=new Inventory.GilDtrEntry(dtrBar,framework,gameGui,configuration,()=>configuration.GilServerBar,gilEntries,localization.Get,mainWindow.OpenGilJournal),
            ()=>{gil?.Dispose();gil=null;});
        gilBar=()=>gil;
        // X-217 (0.1226): bags, gear condition, zone and instance in the server bar (each off by default).
        Alarms.InfoDtrEntries? info=null;
        registrations.Register(()=>info=new Alarms.InfoDtrEntries(dtrBar,framework,client,inventory,configuration,equipped,
                id=>mainWindow.TerritoryName?.Invoke(id),localization.Get,localization.Format,mainWindow.OpenGilJournal,mainWindow.OpenGear,mainWindow.OpenMap),
            ()=>{info?.Dispose();info=null;});
    }
}
