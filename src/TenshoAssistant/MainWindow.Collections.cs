using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private CollectionState? collectionViewState;
    private CollectionKind collectionViewKind;
    private string collectionViewQuery = "";
    private int collectionViewFilter = -1;
    private CollectionEntry[] collectionViewEntries = [];
    private CollectionEntry[] collectionViewMatches = [];
    private uint? collectionViewMusicCategory;
    private uint? collectionViewAchievementCategory;
    private void DrawCollections(bool blueMagic = false)
    {
        Heading(localization.Get(blueMagic ? "Category.BlueMagic":"CollectionsBrowser.Title"),localization.Get(blueMagic ? "CollectionsBrowser.BlueDescription":"CollectionsBrowser.Description"));
        var state = collections.State;
        if (ImGui.Button(localization.Get("CollectionsBrowser.Refresh"))) collections.RequestRefresh();
        ImGui.SameLine();
        ImGui.TextDisabled(state.Loading ? localization.Get("CollectionsBrowser.Loading") : state.ObservedAt is { } date ? localization.Format("CollectionsBrowser.Observed",date.ToLocalTime().ToString("g")) : localization.Get("CollectionsBrowser.Offline"));
        if (state.Error is { } error) { ImGui.TextWrapped(localization.Get("CollectionsBrowser.Error"));if(developerMode)ImGui.TextWrapped(error); }
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Collections,state.ObservedAt,state.FromCache);
        if (!blueMagic && ImGui.BeginTabBar("CollectionKinds"))
        {
            try
            {
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Mounts")+"###CollectionMounts")) { collectionKind = CollectionKind.Mount; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Companions")+"###CollectionMinions")) { collectionKind = CollectionKind.Companion; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Emotes")+"###CollectionEmotes")) { collectionKind = CollectionKind.Emote; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Orchestrion")+"###CollectionMusic",focusOrchestrionTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { focusOrchestrionTab=false;collectionKind = CollectionKind.Orchestrion; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Titles")+"###CollectionTitles",focusTitleTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { focusTitleTab=false;collectionKind=CollectionKind.Title;ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Achievements")+"###CollectionAchievements",focusAchievementTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { focusAchievementTab=false;collectionKind=CollectionKind.Achievement;ImGui.EndTabItem(); }
                if (ImGui.TabItemButton(localization.Get("Category.Triad"))) page = Page.TripleTriad;
            }
            finally { ImGui.EndTabBar(); }
        }
        var kind = blueMagic ? CollectionKind.BlueMagic : collectionKind;
        if(kind==CollectionKind.Orchestrion)DrawOrchestrionCategories(state);
        if(kind==CollectionKind.Achievement)DrawAchievementCategories(state);
        foreach(var issue in state.CategoryIssues ?? [])if(issue.Kind==kind)ImGui.TextWrapped(issue.Reason);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##CollectionSearch", localization.Get("CollectionsBrowser.Search"), ref collectionQuery, 128);
        var completion=kind is CollectionKind.Title or CollectionKind.Achievement;
        if(!completion && collectionFilter==3)collectionFilter=0;
        var filters = completion ? new[]{"All",kind==CollectionKind.Achievement ? "Completed":"Acquired","Unknown",kind==CollectionKind.Achievement ? "NotCompleted":"NotAcquired"}:new[] { "All", "Acquired", "Unconfirmed" };
        for (var index = 0; index < filters.Length; index++)
        {
            if (index > 0) ImGui.SameLine();
            if (ImGui.RadioButton(localization.Get("CollectionsBrowser."+filters[index])+"###CollectionFilter"+index, collectionFilter == index)) collectionFilter = index;
        }
        var musicCategory=kind==CollectionKind.Orchestrion ? orchestrionCategoryFilter:null;
        var achievementCategory=kind==CollectionKind.Achievement ? achievementCategoryFilter:null;
        if (!ReferenceEquals(collectionViewState, state) || collectionViewKind != kind
            || collectionViewFilter != collectionFilter || collectionViewQuery != collectionQuery || collectionViewMusicCategory!=musicCategory || collectionViewAchievementCategory!=achievementCategory)
        {
            collectionViewState = state; collectionViewKind = kind;
            collectionViewFilter = collectionFilter; collectionViewQuery = collectionQuery;
            collectionViewMusicCategory=musicCategory;
            collectionViewAchievementCategory=achievementCategory;
            collectionViewEntries = state.Entries.Where(entry => entry.Kind == kind)
                .Where(entry=>musicCategory is null || entry.Music?.CategoryId==musicCategory)
                .Where(entry=>achievementCategory is null || entry.Achievement?.CategoryId==achievementCategory)
                .OrderBy(entry=>kind==CollectionKind.Orchestrion ? entry.Music?.CategoryOrder ?? int.MaxValue:kind==CollectionKind.Achievement ? entry.Achievement?.KindOrder ?? int.MaxValue:0)
                .ThenBy(entry=>kind==CollectionKind.Achievement ? entry.Achievement?.CategoryOrder ?? int.MaxValue:0)
                .ThenBy(entry => blueMagic ? entry.Number:kind==CollectionKind.Orchestrion ? entry.Music?.Order ?? int.MaxValue:kind==CollectionKind.Title ? entry.Title?.Order ?? int.MaxValue:kind==CollectionKind.Achievement ? entry.Achievement?.Order ?? int.MaxValue:0)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            collectionViewMatches = collectionViewEntries.Where(entry => (collectionFilter == 0 || (collectionFilter == 1 ? entry.Acquisition==CollectionAcquisition.Acquired:collectionFilter==2 ? entry.Acquisition==CollectionAcquisition.Unknown:entry.Acquisition==CollectionAcquisition.NotAcquired))
                && (string.IsNullOrWhiteSpace(collectionQuery) || entry.Name.Contains(collectionQuery.Trim(), StringComparison.OrdinalIgnoreCase))).ToArray();
        }
        var entries = collectionViewEntries;
        ImGui.TextColored(Accent,localization.Format(kind==CollectionKind.Achievement ? "CollectionSummary.Completed":"CollectionSummary.Acquired",entries.Count(entry=>entry.ConfirmedUnlocked),entries.Length));
        if(completion)ImGui.TextDisabled(localization.Format(kind==CollectionKind.Achievement ? "CollectionSummary.Incomplete":"CollectionSummary.Missing",entries.Count(entry=>entry.Acquisition==CollectionAcquisition.NotAcquired),entries.Count(entry=>entry.Acquisition==CollectionAcquisition.Unknown)));
        ImGui.TextWrapped(localization.Get("CollectionSummary.Help"));
        if (state.CharacterId == 0) ImGui.TextDisabled(localization.Get("CollectionSummary.Login"));
        var openDetails = false;
        var openMusicDetails=kind==CollectionKind.Orchestrion && openOrchestrionDetailsRequested;
        if(openMusicDetails)openOrchestrionDetailsRequested=false;
        var openCompletionDetails=completion && completionDetailsRequested;
        if(openCompletionDetails)completionDetailsRequested=false;
        var visibleEntries = collectionViewMatches;
        if (!ImGui.BeginTable("CollectionEntries", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("CollectionsBrowser.Collection"), ImGuiTableColumnFlags.WidthStretch, 3);
            ImGui.TableSetupColumn(localization.Get("CollectionsBrowser.State"), ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableHeadersRow();
            var clipper = ImGui.ImGuiListClipper();
            try
            {
            clipper.Begin(visibleEntries.Length);
            while (clipper.Step())
            for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
            {
                var entry = visibleEntries[index];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var musicItem=kind==CollectionKind.Orchestrion ? itemCatalog.State.Catalog?.ForOrchestrion(entry.Id).FirstOrDefault():null;
                if(kind==CollectionKind.Title)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    try{if(ImGui.Button(FontAwesomeIcon.Crown.ToIconString()+$"##TitleIcon{entry.Id}",new Vector2(32))){selectedCompletionKind=kind;selectedCompletionId=entry.Id;openCompletionDetails=true;}}
                    finally{ImGui.PopFont();}
                }
                else DrawItemIcon(musicItem ?? new ItemDefinition(0, entry.Name, false, entry.IconId), new Vector2(32));
                if(kind==CollectionKind.Orchestrion && ImGui.IsItemClicked()){selectedOrchestrion=entry.Id;openMusicDetails=true;}
                if(kind==CollectionKind.Achievement && ImGui.IsItemClicked()){selectedCompletionKind=kind;selectedCompletionId=entry.Id;openCompletionDetails=true;}
                ImGui.SameLine();
                if (blueMagic)
                {
                    if (ImGui.Selectable($"{entry.Number} · {entry.Name}##{entry.Id}"))
                    { selectedBlueSpell = entry.Id; openDetails = true; }
                }
                else if(kind==CollectionKind.Orchestrion)
                {
                    if(ImGui.Selectable($"{entry.Name}##Music{entry.Id}")){selectedOrchestrion=entry.Id;openMusicDetails=true;}
                }
                else if(completion)
                {
                    if(ImGui.Selectable($"{entry.Name}##Completion{kind}{entry.Id}")){selectedCompletionKind=kind;selectedCompletionId=entry.Id;openCompletionDetails=true;}
                }
                else ImGui.TextUnformatted(entry.Name);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(developerMode ? $"{entry.Name}\n{entry.Kind} : {entry.Id}":entry.Name);
                ImGui.TableNextColumn();
                ImGui.TextColored(entry.ConfirmedUnlocked ? Success : Muted,CollectionAcquisitionLabel(entry));
            }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
        if(openMusicDetails)ImGui.OpenPopup(localization.Get("OrchestrionDetails.Title")+"###OrchestrionDetails");
        if(kind==CollectionKind.Orchestrion)DrawOrchestrionDetails(state);
        if(openCompletionDetails)ImGui.OpenPopup(localization.Get("CompletionDetails.Title")+"###CompletionDetails");
        DrawCompletionDetails(state);
        var bluePopup=localization.Get("BlueSpell.Title")+"###BlueSpellDetails";
        if (openDetails) ImGui.OpenPopup(bluePopup);
        ImGui.SetNextWindowSize(new Vector2(520, 0), ImGuiCond.Appearing);
        var popupOpen = true;
        if (ImGui.BeginPopupModal(bluePopup, ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            try
            {
                var spell = state.Entries.FirstOrDefault(entry => entry.Kind == CollectionKind.BlueMagic && entry.Id == selectedBlueSpell);
                if (spell is not null)
                {
                    DrawItemIcon(new ItemDefinition(0, spell.Name, false, spell.IconId), new Vector2(40));
                    ImGui.SameLine(); ImGui.TextColored(Accent, $"{spell.Number} · {spell.Name}");
                    ImGui.TextUnformatted(localization.Format("BlueSpell.Rank",spell.Rank));
                    ImGui.TextWrapped(spell.Description);
                    ImGui.TextWrapped(string.IsNullOrWhiteSpace(spell.Location) ? localization.Get("BlueSpell.MissingLocation") : localization.Format("BlueSpell.Location",spell.Location));
                    ImGui.TextColored(spell.ConfirmedUnlocked ? Success : Muted,localization.Get(spell.ConfirmedUnlocked ? "BlueSpell.Learned":"CollectionState.Unknown"));
                    if (ImGui.Button(localization.Get("BlueSpell.Copy"))) ImGui.SetClipboardText(spell.Name);
                }
                if (ImGui.Button(localization.Get("BlueSpell.Close"))) ImGui.CloseCurrentPopup();
            }
            finally { ImGui.EndPopup(); }
        }
    }
}
