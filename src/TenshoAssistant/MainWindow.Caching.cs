using Dalamud.Bindings.ImGui;
using TenshoAssistant.Caching;

namespace TenshoAssistant;

// Settings › Data › cache: one duration per cache area (prices, quests, collections, completion, sources, Fashion Report, imported
// stocks, craft calculations), from five minutes to « Never (on request only) » (0.281), saved in the configuration; and the date stamp
// (latest or from disk, fresh or expired) other pages show. The disk store (Caching/DataCache.cs) does its I/O off the framework thread.
internal sealed partial class MainWindow
{
    private DataCache? dataCacheStore;
    internal void ConfigureDataCache(DataCache store){dataCacheStore=store;craftLogSession.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.CraftCalculations),store);RaphaelSequences.Configure(()=>configuration.Cache.Duration(CacheArea.CraftCalculations),store);}
    private static readonly string[] CacheChoiceKeys=["FiveMinutes","OneHour","OneDay","SevenDays","Never"];
    private void DrawCacheSettings()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("CacheUi.Title"),ImGuiTreeNodeFlags.DefaultOpen))return;
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
                        SaveCache(configuration.Cache.With(area,(CacheRetention)index));
                    }
                }
                finally{ImGui.EndCombo();}
            }
            Tip("SettingTips.Cache"+area);
            // 0.324: « ↺ » to the default duration of this area.
            var byDefault=DefaultConfiguration.Cache.Get(area);
            if(DefaultButton("Cache"+area,configuration.Cache.Get(area)!=byDefault))
            {
                SaveCache(configuration.Cache.With(area,byDefault));
            }
        }
        ImGui.TextWrapped(localization.Get("CacheUi.QuestHelp"));
        ImGui.TextWrapped(localization.Get("CacheUi.StockHelp"));
        ImGui.TextWrapped(localization.Get("CacheUi.NativeHelp"));
        ImGui.TextWrapped(localization.Get("CacheUi.LiveHelp"));
        if(dataCacheStore?.Error is { } errorText)DrawDataError(errorText,"ServiceErrors.Cache");
    }
    // X-76 H3 (0.914): one cache duration chosen or reset; the previous durations come back when saving fails.
    private void SaveCache(CachePreferences next)
    {
        var previous=configuration.Cache;configuration.Cache=next;
        TrySave("Cache durations",()=>configuration.Cache=previous);
    }
    private void DrawCacheStamp(CacheArea area,DateTimeOffset? date,bool restored=false)
    {
        if(date is not { } observed){ImGui.TextDisabled(localization.Get("CacheUi.NoDate"));return;}
        var duration=configuration.Cache.Duration(area);var fresh=CacheAge.Fresh(observed,DateTimeOffset.UtcNow,duration);
        ImGui.TextColored(fresh ? Muted:Accent,localization.Format("CacheUi.Stamp",localization.Get(restored ? "CacheUi.Disk":"CacheUi.Latest"),observed.ToLocalTime().ToString("g"),localization.Get(fresh ? "CacheUi.Fresh":"CacheUi.Expired"),localization.Get("CacheDuration."+CacheChoiceKeys[(int)configuration.Cache.Get(area)])));
    }
}
