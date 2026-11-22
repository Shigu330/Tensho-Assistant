using Dalamud.Bindings.ImGui;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// X-226 (0.1778): the preparation reminder's settings (Settings › Tracking › Alarms): the meal and the chocobo companion, each with its
// threshold in minutes, greyed while its reminder is off.
internal sealed partial class MainWindow
{
    private PrepReminderSettings PrepSettings=>configuration.PrepReminders??=new();

    private void DrawPrepReminderSettings()
    {
        var defaults=new PrepReminderSettings();
        BoolSetting("PrepReminders.Meal","PrepReminders.MealTip",()=>PrepSettings.Meal,value=>PrepSettings.Meal=value,c=>(c.PrepReminders ?? new()).Meal);
        ImGui.BeginDisabled(!PrepSettings.Meal);
        try{SliderSetting("PrepReminders.MealMinutes","PrepReminders.MealMinutesTip",()=>PrepSettings.MealThreshold,value=>PrepSettings.MealMinutes=value,0,PrepReminderSettings.MaximumMinutes,defaults.MealThreshold,"%d min");}
        finally{ImGui.EndDisabled();}
        BoolSetting("PrepReminders.Chocobo","PrepReminders.ChocoboTip",()=>PrepSettings.Chocobo,value=>PrepSettings.Chocobo=value,c=>(c.PrepReminders ?? new()).Chocobo);
        ImGui.BeginDisabled(!PrepSettings.Chocobo);
        try{SliderSetting("PrepReminders.ChocoboMinutes","PrepReminders.ChocoboMinutesTip",()=>PrepSettings.ChocoboThreshold,value=>PrepSettings.ChocoboMinutes=value,0,PrepReminderSettings.MaximumMinutes,defaults.ChocoboThreshold,"%d min");}
        finally{ImGui.EndDisabled();}
    }
}
