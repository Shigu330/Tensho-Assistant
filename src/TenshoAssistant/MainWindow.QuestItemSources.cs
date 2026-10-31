using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private string questSourceQuery="";
    private string questSourceCachedQuery="";
    private uint questSourceCachedItem;
    private QuestState? questSourceCachedState;
    private QuestSourceProgressFilter questSourceFilter=QuestSourceProgressFilter.All;
    private QuestSourceProgressFilter questSourceCachedFilter=QuestSourceProgressFilter.All;
    private QuestItemProgressProjection? questSourceProjection;
    private static readonly (QuestSourceProgressFilter Filter,string Key)[] QuestSourceFilters=
    [
        (QuestSourceProgressFilter.All,"QuestsBrowser.All"),(QuestSourceProgressFilter.Active,"QuestsBrowser.Active"),
        (QuestSourceProgressFilter.Complete,"QuestsBrowser.Complete"),(QuestSourceProgressFilter.Unconfirmed,"QuestsBrowser.Unknown")
    ];
    private void DrawQuestItemSources(uint item)
    {
        if(!ImGui.CollapsingHeader(localization.Get("QuestSources.Title")))return;
        var state=quests.State;
        if(ImGui.SmallButton(localization.Get("QuestSources.Refresh")))quests.RequestRefresh();
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Quests,state.ObservedAt,state.FromCache);
        if(state.Loading){ImGui.TextDisabled(localization.Get("QuestSources.Loading"));return;}
        if(state.Error is not null){ImGui.TextWrapped(localization.Get("QuestSources.Error"));return;}
        if(state.RewardIndexLoading){ImGui.TextDisabled(localization.Get("QuestSources.IndexLoading"));return;}
        if(state.RewardIndex is not { } index)
        {
            ImGui.TextWrapped(state.RewardIndexError is null ? localization.Get("QuestSources.IndexMissing"):localization.Get("QuestSources.IndexError"));
            if(ImGui.SmallButton(localization.Get("QuestSources.RefreshIndex")))quests.RequestRefresh();
            return;
        }
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##QuestSourceSearch",localization.Get("QuestSources.Search"),ref questSourceQuery,128);
        if(!string.IsNullOrEmpty(questSourceQuery) && ImGui.SmallButton(localization.Get("QuestSources.Clear")))questSourceQuery="";
        for(var position=0;position<QuestSourceFilters.Length;position++)
        {
            if(position>0)ImGui.SameLine();
            var (filter,key)=QuestSourceFilters[position];
            if(ImGui.RadioButton(localization.Get(key)+"###QuestSourceFilter"+position,questSourceFilter==filter))questSourceFilter=filter;
        }
        // QuestState is replaced on every observation, invalidation or index completion: its reference keys the join cache.
        if(!ReferenceEquals(state,questSourceCachedState) || item!=questSourceCachedItem || questSourceQuery!=questSourceCachedQuery || questSourceFilter!=questSourceCachedFilter)
        {
            questSourceProjection=QuestItemProgress.Project(state,item,questSourceQuery,questSourceFilter);
            questSourceCachedState=state;questSourceCachedItem=item;questSourceCachedQuery=questSourceQuery;questSourceCachedFilter=questSourceFilter;
        }
        if(questSourceProjection is not { } projection)return;
        var coverage=index.Coverage;var rows=projection.Rows;
        ImGui.TextColored(Accent,localization.Format("QuestSourceCounts.Found",projection.Matches,index.ForItem(item).Count));
        ImGui.TextWrapped(localization.Get("QuestSources.Help"));
        if(projection.ProgressConfirmed)
            ImGui.TextDisabled(localization.Format("QuestSourceCounts.Progress",projection.Active,projection.Complete,projection.Unconfirmed));
        else
            ImGui.TextWrapped(localization.Get("QuestSources.ProgressOffline"));
        ImGui.TextDisabled(localization.Format("QuestSourceCounts.Coverage",coverage.CoveredQuests,coverage.TotalQuests));
        if(coverage.UnresolvedReferences>0 || coverage.CoveredQuests<coverage.TotalQuests || coverage.UnknownQuantityLines>0)
            ImGui.TextWrapped(localization.Format("QuestSourceCounts.Partial",coverage.UnresolvedReferences,coverage.UnknownQuantityLines));
        if(projection.Matches==0){ImGui.TextDisabled(index.ForItem(item).Count==0 ? localization.Get("QuestSources.Empty"):localization.Get("QuestSources.Filtered"));return;}
        if(rows.Count==0){ImGui.TextDisabled(localization.Get("QuestSources.ProgressFiltered"));return;}
        if(!ImGui.BeginTable("ItemQuestSources",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("QuestSources.Quest"),ImGuiTableColumnFlags.WidthStretch,4);ImGui.TableSetupColumn(localization.Get("QuestSources.Progress"),ImGuiTableColumnFlags.WidthStretch,2);ImGui.TableSetupColumn(localization.Get("QuestSources.Category"),ImGuiTableColumnFlags.WidthStretch,2);ImGui.TableSetupColumn(localization.Get("QuestSources.Variants"),ImGuiTableColumnFlags.WidthStretch,1);ImGui.TableHeadersRow();
            var clipper=ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(rows.Count);
                while(clipper.Step())for(var position=clipper.DisplayStart;position<clipper.DisplayEnd;position++)
                {
                    var row=rows[position];var source=row.Source;ImGui.PushID((int)source.QuestId);
                    try
                    {
                        ImGui.TableNextRow();ImGui.TableNextColumn();
                        DrawItemIcon(new ItemDefinition(0,source.Name,false,source.Icon),new Vector2(28));var clicked=ImGui.IsItemClicked();ImGui.SameLine();
                        clicked|=ImGui.Selectable(source.Name);
                        if(ImGui.IsItemHovered())WrappedTooltip($"{source.Name}\n{source.Category}"+(developerMode ? $"\nID: {source.QuestId}":""));
                        if(clicked){SelectQuest(source.QuestId);page=Page.Quests;}
                        ImGui.TableNextColumn();
                        ImGui.TextColored(row.Progress==QuestProgress.Complete ? Success:Muted,QuestProgressLabel(row.Progress,row.Sequence));
                        if(row.IsRepeatable && row.Progress==QuestProgress.Complete && ImGui.IsItemHovered())WrappedTooltip(localization.Get("QuestSources.Repeatable"));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(source.Category);
                        ImGui.TableNextColumn();ImGui.TextDisabled(source.Variants.Count==1 ? localization.Get("QuestSourceCounts.VariantOne"):localization.Format("QuestSourceCounts.Variants",source.Variants.Count));
                        if(ImGui.IsItemHovered())WrappedTooltip(string.Join('\n',source.Variants.Take(12).Select(line=>
                            $"{QuestRewardConditionLabel(line)} · {(line.Quantity is { } count ? "× "+count:localization.Get("QuestSources.Quantity"))}{(line.HighQuality==true ? " HQ":"")}"))
                            +(source.Variants.Count>12 ? localization.Get("QuestSources.More"):""));
                    }
                    finally{ImGui.PopID();}
                }
            }
            finally{clipper.End();clipper.Destroy();}
        }
        finally{ImGui.EndTable();}
    }
}
