using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1936, GPT review of 11/11, accepted by the user): the reminders' family out of Plugin.cs, after the game interface's readers
// (0.1934). The preparation reminder's watcher and the garden's chat watcher are each created in one install path and released in one release
// path of the plugin's registrations, in this file; Plugin.cs calls the two blocks where they stood (the discoveries' item watcher given as a
// function, read when a harvest is seen, as before), so the order of the registrations is the one of before. RemindersFamily1936Tests counts
// those paths.
public sealed partial class Plugin
{
    /// <summary>The preparation reminder: a meal's Well Fed status or the chocobo companion about to end, or ended by itself, said in the chat and rung.</summary>
    private void RegisterPrepReminders(Configuration configuration,IDataManager data,IFramework framework,IClientState client,IObjectTable objects,ICondition condition,
        IChatGui chat)
    {
        // X-226 (0.1778): the preparation reminder (a meal's Well Fed status or the chocobo companion about to end, or ended by itself),
        // outside fights and duties only, once per end: chat and sound with the alarm options, as the wear alert.
        string WellFedName()=>data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(Reminders.PrepReminderWatcher.WellFed)?.Name.ExtractText() is { Length:>0 } fed ? fed:localization.Get("PrepReminders.WellFed");
        Reminders.PrepReminderWatcher? prepReminders=null;
        registrations.Register(()=>prepReminders=new Reminders.PrepReminderWatcher(framework,client,objects,condition,()=>configuration.PrepReminders ?? new(),log){Due=notice=>
        {
            var options=configuration.AlarmOptions ?? new();
            // The minutes left in the common format (X-312).
            var left=UI.TimeLeft.Write(TimeSpan.FromMinutes(notice.Minutes),(key,first,second)=>localization.Format(key,first,second));
            var text=notice.Kind==Reminders.PrepKind.Meal
                ? localization.Format(notice.Ended ? "PrepReminders.MealEnded":"PrepReminders.MealEnding",WellFedName(),left)
                : notice.Ended ? localization.Get("PrepReminders.ChocoboEnded"):localization.Format("PrepReminders.ChocoboEnding",left);
            // Never said in a duty (the watcher holds it back there): the alarm options as outside one.
            if(options.ChatAllowed(false))chat.Print("[Tensho] "+text);
            if(options.SoundAllowed(false))Alarms.AlarmService.PlaySound(options.Sound,log);
            if(options.ChatAllowed(false) || options.SoundAllowed(false))linkNotices.Add(text);
        },
        // X-774 (0.1780): the companion can only be out where mounts are allowed (HowToPage row 361; TerritoryType.Mount, probed).
        MountAllowed=territory=>territory==0 ? null:data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(territory)?.Mount},()=>prepReminders?.Dispose());
    }

    /// <summary>The plantings and harvests read from the chat and the bed's menu; a harvest is also the source of the items it gives.</summary>
    private void RegisterGardenWatcher(Configuration configuration,IDataManager data,IFramework framework,IClientState client,ITargetManager targets,IGameGui gameGui,
        Func<Discoveries.ItemDiscoveryWatcher?> itemWatcher)
    {
        // 0.460 (DEC-027 follow-up, user request): plantings read from the chat.
        Reminders.GardenChatWatcher? gardenWatcher=null;
        registrations.Register(()=>gardenWatcher=new Reminders.GardenChatWatcher(chatGui,framework,data,client,log,()=>configuration.GardenDetect,
            id=>mainWindow.GardenSeedCatalog?.Invoke().Any(row=>row.Item1.SeedId==id)==true,
            // X-382 (0.1360): a harvest is also the source of the items it gives (no « Item gain not attributed » for them).
            (found,seed,place)=>{if(found.Kind==Reminders.GardenChatKind.Harvested)itemWatcher()?.SourceSeen(Discoveries.GainSource.Garden,(uint)(found.Bed ?? 0));mainWindow.GardenPlanted(found,seed,place);},
            target:()=>targets.Target is { ObjectKind:Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj } bedObject ? bedObject.BaseId:0u,
            // X-410 M1 (0.1378): a harvest seen through the bed's menu names a seed or a crop.
            isGardenItem:id=>mainWindow.GardenSeedCatalog?.Invoke().Any(row=>row.Item1.SeedId==id || row.Item1.HarvestId==id)==true,
            gui:gameGui,developer:()=>configuration.DeveloperMode,
            // X-581 (0.1572): the game's state line of a bed, read when its menu opens.
            examined:(place,patch,bed,said,line)=>mainWindow.GardenExamined(place,patch,bed,said,line)),()=>gardenWatcher?.Dispose());
    }
}
