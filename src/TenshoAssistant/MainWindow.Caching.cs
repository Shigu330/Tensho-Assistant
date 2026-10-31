using Dalamud.Bindings.ImGui;
using TenshoAssistant.Caching;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private DataCache? dataCacheStore;
    internal void ConfigureDataCache(DataCache store){dataCacheStore=store;craftLogSession.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.CraftCalculations),store);}
    private static readonly string[] CacheChoiceKeys=["FiveMinutes","OneHour","OneDay","SevenDays"];
    private void DrawCacheSettings()
    {
        if(!ImGui.CollapsingHeader(localization.Get("CacheUi.Title"),ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextWrapped(localization.Get("CacheUi.Help"));
        foreach(var (area,label) in new[]{(CacheArea.Prices,localization.Get("CacheUi.Prices")),(CacheArea.Quests,localization.Get("CacheUi.Quests")),
            (CacheArea.Collections,localization.Get("CacheUi.Collections")),(CacheArea.Completion,localization.Get("CacheUi.Completion")),(CacheArea.Sources,localization.Get("CacheUi.Sources")),
            (CacheArea.Fashion,localization.Get("CacheUi.Fashion")),(CacheArea.ImportedStock,localization.Get("CacheUi.Stocks")),(CacheArea.CraftCalculations,localization.Get("CacheUi.Craft"))})
        {
            var selected=(int)configuration.Cache.Get(area);ImGui.SetNextItemWidth(155);
            if(ImGui.BeginCombo(label+"###Cache"+area,localization.Get("CacheDuration."+CacheChoiceKeys[selected])))
            {
                try
                {
                    for(var index=0;index<CacheChoiceKeys.Length;index++)
                    {
                        if(!ImGui.Selectable(localization.Get("CacheDuration."+CacheChoiceKeys[index]),selected==index))continue;
                        var previous=configuration.Cache;configuration.Cache=previous.With(area,(CacheRetention)index);
                        try{save();saveError=null;}
                        catch(Exception error){configuration.Cache=previous;saveError=localization.Get("Common.SaveError");log.Warning(error,"Could not persist cache policy.");}
                    }
                }
                finally{ImGui.EndCombo();}
            }
        }
        ImGui.TextWrapped(localization.Get("CacheUi.QuestHelp"));
        ImGui.TextWrapped(localization.Get("CacheUi.StockHelp"));
        ImGui.TextWrapped(localization.Get("CacheUi.NativeHelp"));
        ImGui.TextWrapped(localization.Get("CacheUi.LiveHelp"));
        if(dataCacheStore?.Error is { } errorText)DrawDataError(errorText,"ServiceErrors.Cache");
    }
    private void DrawCacheStamp(CacheArea area,DateTimeOffset? date,bool restored=false)
    {
        if(date is not { } observed){ImGui.TextDisabled(localization.Get("CacheUi.NoDate"));return;}
        var duration=configuration.Cache.Duration(area);var fresh=CacheAge.Fresh(observed,DateTimeOffset.UtcNow,duration);
        ImGui.TextColored(fresh ? Muted:Accent,localization.Format("CacheUi.Stamp",localization.Get(restored ? "CacheUi.Disk":"CacheUi.Latest"),observed.ToLocalTime().ToString("g"),localization.Get(fresh ? "CacheUi.Fresh":"CacheUi.Expired"),localization.Get("CacheDuration."+CacheChoiceKeys[(int)configuration.Cache.Get(area)])));
    }
}
