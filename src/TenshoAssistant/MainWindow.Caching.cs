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
        DrawPatchChanges();
        DrawCacheClear();
    }

    // X-117 part 2 (0.1047): what the last game patch changed in the cached catalogues, for every player; summaries made again only
    // when the store hands a new list (read off the framework thread by DataCache).
    private IReadOnlyList<PatchCatalogReport>? patchReportsShown;private IReadOnlyList<PatchFamilySummary> patchSummaries=[];
    private void DrawPatchChanges()
    {
        if(dataCacheStore is not { } store)return;
        var reports=store.PatchReports;
        if(!ReferenceEquals(reports,patchReportsShown)){patchReportsShown=reports;patchSummaries=PatchCatalogDiff.Summaries(reports);}
        ImGui.TextWrapped(localization.Get("CacheUi.PatchTitle"));
        Tip("CacheUi.PatchTip");
        if(patchSummaries.Count==0){EmptyState("CacheUi.PatchNone");return;}
        foreach(var summary in patchSummaries)
        {
            var label=summary.Key is { } key ? localization.Get("CacheUi.Patch"+key):summary.Family;
            ImGui.TextWrapped(summary.Compared
                ? localization.Format("CacheUi.PatchLine",label,summary.OldGame ?? "?",summary.NewGame ?? "?",summary.Added,summary.Removed,summary.Changed)
                : localization.Format("CacheUi.PatchSkipped",label,summary.OldGame ?? "?",summary.NewGame ?? "?"));
        }
        DrawPatchRecheck(reports);
    }

    // X-117 part 2b (0.1048): the discoveries to recheck after the patch, counted off the frame (a sliced copy of the facts, then
    // PatchRecheck.Find) once per new report list and once the discoveries are loaded; the line shows the last count.
    private IReadOnlyList<PatchCatalogReport>? patchRecheckFor;private volatile PatchRecheckResult? patchRecheck;private int patchRecheckRunning;
    private void DrawPatchRecheck(IReadOnlyList<PatchCatalogReport> reports)
    {
        if(DiscoveryStore is not { } discoveries || !DiscoveriesLoaded)return;
        if(!ReferenceEquals(reports,patchRecheckFor) && Interlocked.CompareExchange(ref patchRecheckRunning,1,0)==0)
        {
            patchRecheckFor=reports;
            _=Task.Run(()=>
            {
                try{patchRecheck=PatchRecheck.Find(reports,discoveries.Snapshot(DateTimeOffset.UtcNow,saving:false).Facts);}
                catch(Exception error){log.Warning(error,"The discoveries to recheck after the game patch could not be counted.");}
                finally{Volatile.Write(ref patchRecheckRunning,0);}
            });
        }
        if(patchRecheck is not { } result)return;
        ImGui.TextWrapped(localization.Format("CacheUi.PatchRecheck",result.Facts,result.Places,result.Vendors));
        Tip("CacheUi.PatchRecheckTip");
    }

    // X-76 step 3 (0.945, user request): empty the cache, a zip copy taken just before (Backups folder).
    private bool cacheClearAsked;private string? cacheClearMessage;
    private void DrawCacheClear()
    {
        if(dataCacheStore is not { } store)return;
        if(!cacheClearAsked){if(ImGui.SmallButton(localization.Get("CacheUi.Clear")))cacheClearAsked=true;Tip("CacheUi.ClearTip");}
        else
        {
            ImGui.TextColored(Warning,localization.Get("CacheUi.ClearSure"));
            if(ImGui.SmallButton(localization.Get("CacheUi.ClearYes")))
            {
                cacheClearAsked=false;cacheClearMessage=localization.Get("CacheUi.Clearing");
                var root=store.Root;var backups=Path.Combine(Path.GetDirectoryName(root) ?? root,"Backups");
                _=Task.Run(async()=>
                {
                    try
                    {
                        // X-142 part 2 (0.1122): the copy and the purge as one operation of the cache's queue.
                        var done=await store.ClearAsync(()=>BackupZip.Create(root,backups,"cache",DateTimeOffset.UtcNow)).ConfigureAwait(false);
                        cacheClearMessage=done.Failed>0 ? localization.Format("CacheUi.ClearedPartial",done.Removed,done.Failed,done.Backup ?? "—")
                            : localization.Format("CacheUi.Cleared",done.Removed,done.Backup ?? "—");
                        if(done.Failed>0)log.Warning("Cache emptied in part: {0} file(s) removed, {1} could not be (in use).",done.Removed,done.Failed);
                    }
                    catch(Exception error){log.Warning(error,"The cache could not be emptied.");cacheClearMessage=localization.Get("CacheUi.ClearFailed");}
                });
            }
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("CacheUi.ClearNo")))cacheClearAsked=false;
        }
        if(cacheClearMessage is { } message)ImGui.TextDisabled(message);
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
