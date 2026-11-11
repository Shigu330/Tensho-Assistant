using Dalamud.Bindings.ImGui;
using System.Numerics;
using System.Globalization;
using TenshoAssistant.Items;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// Quests page (Progression), like QuestTracker: every quest of the game's Quest sheet with the character's observed progress,
// filtered by journal section then genre (completion % in the lists, 0.283–0.284), search, state; ARR main scenario quests of the
// other starting cities and options not taken (other Grand Companies, branches) are left out unless shown (0.285–0.286). Details:
// steps, previous / next quests, quest giver on the Tensho map, rewards (MainWindow.QuestRewards.cs); up to 64 followed quests.
internal sealed partial class MainWindow
{
    private QuestState? questViewState;
    private IReadOnlyList<QuestEntry>? questMetadataEntries;
    private string questViewQuery = "";
    private string questViewCategory = "";
    // 0.283 (user request): main kind = journal section, then its genres as sub-categories.
    private string questSection = "", questViewSection = "";
    private (string Name, byte Id)[] questSections = [];
    private (string Section, string Genre)[] questGenres = [];
    // 0.284 (user request): completion of each kind and category, shown in the drop-down lists (computed with the quest list).
    private Dictionary<string, (int Done, int Total)> questSectionStats = [], questGenreStats = [];
    // 0.285 (user request): ARR main scenario quests of the two other starting cities are left out of lists and percentages
    // (docs/evidence/0.285-quest-start-cities.md); « show » brings them back in the lists.
    private HashSet<uint> questOtherCity = [];
    private string? questPlayerCity;
    private bool questShowOtherCities, questViewShowOtherCities;
    private string QuestStatLabel(string name, Dictionary<string, (int Done, int Total)> stats)
        => stats.TryGetValue(name, out var stat) && stat.Total > 0 ? localization.Format("QuestsBrowser.Completion", name, stat.Done, stat.Total, 100 * stat.Done / stat.Total) : name;
    private int questViewFilter = -1;
    private string[] questCategories = [];
    private QuestEntry[] questMatches = [];
    private (int From, int To) questViewLevels;
    private bool questAvailableNow, questHereOnly;
    private (bool, bool, string) questViewNarrow;
    private (int Column, bool Descending) questSort, questViewSort;
    private int questActiveCount;
    private int questCompleteCount;
    private QuestLinks questLinks = new(Array.Empty<QuestEntry>());
    private uint selectedQuestId;
    private bool questShowDetails;
    private string? questFollowError;
    private readonly Stack<uint> questNavigation = new();

    private void DrawQuests()
    {
        Heading(localization.Get("QuestsBrowser.Title"),localization.Get("QuestsBrowser.Description"));
        var state = quests.State;
        if (!ReferenceEquals(questMetadataEntries,state.Entries))
        {
            questMetadataEntries=state.Entries;
            questLinks = new(state.Entries);
            questCategories = state.Entries.Select(entry => entry.Category).Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            questSections = state.Entries.Where(entry => entry.Section.Length > 0).Select(entry => (entry.Section, entry.SectionId))
                .Distinct().OrderBy(section => section.SectionId).ToArray();
            questGenres = state.Entries.Where(entry => entry.Category.Length > 0).Select(entry => (entry.Section, entry.Category)).Distinct()
                .OrderBy(pair => pair.Category, StringComparer.CurrentCulture).ToArray();
            var cities = QuestStartCities.Of(state.Entries);
            questPlayerCity = QuestStartCities.PlayerCity(state.Entries, cities);
            questOtherCity = questPlayerCity is { } city ? cities.Where(pair => pair.Value != city).Select(pair => pair.Key).ToHashSet() : [];
            // 0.286: plus the options not taken (other Grand Companies, other branches), see QuestAlternatives.
            questOtherCity.UnionWith(QuestAlternatives.NotPlayable(state.Entries));
            questSectionStats = state.Entries.Where(entry => entry.Section.Length > 0 && !questOtherCity.Contains(entry.Id)).GroupBy(entry => entry.Section)
                .ToDictionary(group => group.Key, group => (group.Count(entry => entry.Progress == QuestProgress.Complete), group.Count()));
            questGenreStats = state.Entries.Where(entry => entry.Category.Length > 0 && !questOtherCity.Contains(entry.Id)).GroupBy(entry => entry.Category)
                .ToDictionary(group => group.Key, group => (group.Count(entry => entry.Progress == QuestProgress.Complete), group.Count()));
            questActiveCount = state.Entries.Count(entry => entry.Progress == QuestProgress.Active);
            questCompleteCount = state.Entries.Count(entry => entry.Progress == QuestProgress.Complete);
        }
        if (ImGui.Button(localization.Get("QuestsBrowser.Refresh"))) quests.RequestRefresh();
        ImGui.SameLine();
        ImGui.TextDisabled(state.Loading ? localization.Get("QuestsBrowser.Loading") : state.ObservedAt is { } date ? localization.Format("QuestsBrowser.Observed",date.ToLocalTime().ToString("g")) : localization.Get("QuestsBrowser.Offline"));
        if (state.Error is { } error) DrawDataError(error,"QuestsBrowser.Error");
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Quests,state.ObservedAt,state.FromCache);
        DrawFollowedQuests();
        if (ImGui.SmallButton(localization.Get("QuestsBrowser.Catalogue")+"###QuestCatalogue")) questShowDetails = false;
        if (selectedQuestId != 0)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("QuestsBrowser.Details")+"###QuestDetails")) questShowDetails = true;
        }
        if (questShowDetails && selectedQuestId != 0) { DrawQuestDetails(); return; }
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##QuestSearch", localization.Get("QuestsBrowser.Search"), ref questQuery, 128);
        Tip("QuestsBrowser.SearchTip");
        ImGui.SetNextItemWidth(260);
        if (ImGui.BeginCombo(localization.Get("QuestsBrowser.Section")+"###QuestSection", questSection.Length == 0 ? localization.Get("QuestsBrowser.All") : QuestStatLabel(questSection, questSectionStats)))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("QuestsBrowser.All"), questSection.Length == 0)) { questSection = ""; questCategory = ""; }
                foreach (var (section, _) in questSections)
                    if (ImGui.Selectable(QuestStatLabel(section, questSectionStats) + "##" + section, questSection == section)) { questSection = section; questCategory = ""; }
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("QuestsBrowser.SectionTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(260);
        if (ImGui.BeginCombo(localization.Get("QuestsBrowser.Category")+"###QuestCategory", string.IsNullOrWhiteSpace(questCategory) ? localization.Get("QuestsBrowser.All") : QuestStatLabel(questCategory, questGenreStats)))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("QuestsBrowser.All"), string.IsNullOrWhiteSpace(questCategory))) questCategory = string.Empty;
                // Sub-categories of the chosen kind only (every genre when no kind is chosen).
                foreach (var category in questSection.Length == 0 ? questCategories : questGenres.Where(pair => pair.Section == questSection).Select(pair => pair.Genre).Distinct())
                    if (ImGui.Selectable(QuestStatLabel(category, questGenreStats) + "##" + category, questCategory == category)) questCategory = category;
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("QuestsBrowser.CategoryTip");
        // Plural filter label; QuestsBrowser.Unknown stays the singular state of one quest (QuestProgressLabel).
        var labels = new[] { "All", "Active", "Complete", "Unconfirmed" };
        for (var index = 0; index < labels.Length; index++)
        {
            if (index > 0) ImGui.SameLine();
            if (ImGui.RadioButton(localization.Get("QuestsBrowser."+labels[index])+"###QuestFilter"+index, questFilter == index)) questFilter = index;
            QuestFilterTip(index switch { 1 => QuestSourceProgressFilter.Active, 2 => QuestSourceProgressFilter.Complete, 3 => QuestSourceProgressFilter.Unconfirmed, _ => QuestSourceProgressFilter.All });
        }
        ImGui.TextDisabled(localization.Format("QuestsBrowser.Count",questActiveCount,questCompleteCount));
        if (questOtherCity.Count > 0)
        {
            ImGui.SameLine();
            ImGui.Checkbox(localization.Format("QuestsBrowser.OtherCities", questOtherCity.Count) + "###QuestOtherCities", ref questShowOtherCities);
            if (ImGui.IsItemHovered()) WrappedTooltip(localization.Format("QuestsBrowser.OtherCitiesHelp", questPlayerCity ?? localization.Get("QuestsBrowser.CityUnknown")));
        }
        // 0.550 (T3).
        var levelFrom = configuration.QuestList.LevelFrom; var levelTo = configuration.QuestList.LevelTo;
        ImGui.SetNextItemWidth(90);
        var levelsChanged = ImGui.InputInt(localization.Get("QuestsBrowser.LevelFrom") + "###QuestLevelFrom", ref levelFrom);
        Tip("QuestsBrowser.LevelFromTip");
        ImGui.SameLine(); ImGui.SetNextItemWidth(90);
        levelsChanged |= ImGui.InputInt(localization.Get("QuestsBrowser.LevelTo") + "###QuestLevelTo", ref levelTo);
        Tip("QuestsBrowser.LevelToTip");
        if (levelsChanged)
        {
            configuration.QuestList.LevelFrom = Math.Clamp(levelFrom, 0, 999); configuration.QuestList.LevelTo = Math.Clamp(levelTo, 0, 999);
            try { save(); } catch (Exception saveFailure) { log.Warning(saveFailure, "Could not save the quest levels."); }
        }
        // 0.582 (T11).
        ImGui.Checkbox(localization.Get("QuestsBrowser.AvailableNow")+"###QuestAvailableNow",ref questAvailableNow);
        Tip("QuestsBrowser.AvailableNowTip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("QuestsBrowser.HereOnly")+"###QuestHereOnly",ref questHereOnly);
        Tip("QuestsBrowser.HereOnlyTip");
        var territoryHere=mapLive?.State?.TerritoryId ?? 0;
        var zoneHere=questHereOnly && territoryHere!=0 ? maps.State.Catalog?.Entries.FirstOrDefault(map=>map.TerritoryId==territoryHere)?.Zone ?? "":"";
        ImGui.TextWrapped(localization.Get("QuestsBrowser.Help"));
        if (!ReferenceEquals(questViewState?.Entries, state.Entries) || questViewQuery != questQuery
            || questViewCategory != questCategory || questViewFilter != questFilter || questViewSection != questSection
            || questViewShowOtherCities != questShowOtherCities || questViewLevels != (configuration.QuestList.LevelFrom, configuration.QuestList.LevelTo) || questViewSort != questSort || questViewNarrow != (questAvailableNow,questHereOnly,zoneHere))
        {
            questViewNarrow = (questAvailableNow,questHereOnly,zoneHere);
            questViewLevels = (configuration.QuestList.LevelFrom, configuration.QuestList.LevelTo); questViewSort = questSort;
            questViewSection = questSection; questViewShowOtherCities = questShowOtherCities;
            questMatches = TenshoAssistant.Quests.QuestList.Sorted(state.Entries.Where(entry => (questShowOtherCities || !questOtherCity.Contains(entry.Id))
                && TenshoAssistant.Quests.QuestList.InLevels(entry.Level, questViewLevels.From, questViewLevels.To)
                && (!questAvailableNow || TenshoAssistant.Quests.QuestList.Available(questLinks, entry))
                && (!questHereOnly || entry.Zone == zoneHere)
                && (questSection.Length == 0 || entry.Section == questSection)
                && (string.IsNullOrWhiteSpace(questCategory) || entry.Category == questCategory)
                && (questFilter == 0 || entry.Progress == (questFilter == 1 ? QuestProgress.Active : questFilter == 2 ? QuestProgress.Complete : QuestProgress.Unconfirmed))
                && (string.IsNullOrWhiteSpace(questQuery) || entry.Name.Contains(questQuery.Trim(), StringComparison.OrdinalIgnoreCase)
                    || entry.Id.ToString(CultureInfo.InvariantCulture) == questQuery.Trim())), questSort.Column, questSort.Descending);
            questViewState = state; questViewQuery = questQuery;
            questViewCategory = questCategory; questViewFilter = questFilter;
        }
        var matches = questMatches;
        if (matches.Length == 0) { ImGui.TextDisabled(localization.Get("QuestsBrowser.Empty")); return; }
        if (!ImGui.BeginTable("QuestEntries", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Sortable)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("QuestsBrowser.Quest"), ImGuiTableColumnFlags.WidthStretch, 2);
            ImGui.TableSetupColumn(localization.Get("QuestsBrowser.Level"));
            ImGui.TableSetupColumn(localization.Get("QuestsBrowser.Zone"));
            ImGui.TableSetupColumn(localization.Get("QuestsBrowser.Category"));
            ImGui.TableSetupColumn(localization.Get("QuestsBrowser.Progress"));
            ImGui.TableHeadersRow();
            var specs = ImGui.TableGetSortSpecs();
            if (!specs.IsNull && specs.SpecsDirty && specs.SpecsCount > 0)
            {
                questSort = (specs.Specs.ColumnIndex, specs.Specs.SortDirection == ImGuiSortDirection.Descending);
                specs.SpecsDirty = false;
            }
            var clipper = ImGui.ImGuiListClipper();
            try
            {
            clipper.Begin(matches.Length);
            while (clipper.Step())
            for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
            {
                var entry = matches[index];
                ImGui.TableNextRow(); ImGui.TableNextColumn();
                DrawGameIcon(entry.Icon,new Vector2(24));
                if (ImGui.IsItemClicked()) SelectQuest(entry.Id);
                ImGui.SameLine();
                if (ImGui.Selectable($"{entry.Name}##quest{entry.Id}", selectedQuestId == entry.Id)) SelectQuest(entry.Id);
                // X-89 (0.970): the quest's expansion (game data) in its tooltip.
                if (ImGui.IsItemHovered()) WrappedTooltip($"{entry.Name}\n{entry.Section} › {entry.Category}"
                    +(entry.Expansion is { Length:>0 } questExpansion ? "\n"+localization.Format("QuestDetails.Expansion",questExpansion):"")+(developerMode ? $"\nID: {entry.Id}":""));
                QuestMenu($"QuestMenu{entry.Id}", entry.Id, entry.Name);
                ImGui.TableNextColumn(); if (entry.Level > 0) ImGui.TextUnformatted(entry.Level.ToString(CultureInfo.CurrentCulture)); else ImGui.TextDisabled("?");
                ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.Zone ?? "");
                ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.Category);
                if (entry.Section.Length > 0 && ImGui.IsItemHovered()) WrappedTooltip(entry.Section);
                ImGui.TableNextColumn();
                ImGui.TextColored(entry.Progress == QuestProgress.Complete ? Success : Muted,
                    QuestProgressLabel(entry));
            }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
    }

    private void SelectQuest(uint id)
    {
        questShowDetails = true;
        if (id == selectedQuestId) return;
        if (selectedQuestId != 0)
        {
            if (questNavigation.Count >= 64) questNavigation.Clear();
            questNavigation.Push(selectedQuestId);
        }
        selectedQuestId = id;
    }

    private void DrawQuestDetails()
    {
        if (selectedQuestId == 0) return;
        if (!TenshoAssistant.UI.UiComponents.Header(localization.Get("QuestDetails.Title")+"###QuestDetailsHeader", ImGuiTreeNodeFlags.DefaultOpen)) return;
        if (questNavigation.Count > 0)
        {
            if (ImGui.SmallButton(localization.Get("QuestDetails.Back")+"###QuestBack")) selectedQuestId = questNavigation.Pop();
            ImGui.SameLine();
        }
        if (ImGui.SmallButton(localization.Get("QuestDetails.Close"))) { selectedQuestId = 0; questShowDetails = false; questNavigation.Clear(); return; }
        var entry = questLinks.Find(selectedQuestId);
        if (entry is null) { ImGui.TextDisabled(localization.Format("QuestDetails.Missing",selectedQuestId)); return; }
        DrawGameIcon(entry.Icon,new Vector2(36));
        ImGui.SameLine(); ImGui.TextColored(Accent, entry.Name);
        var followed = configuration.FollowedQuests?.Contains(entry.Id) == true;
        if (ImGui.SmallButton(localization.Get(followed ? "QuestDetails.Unfollow":"QuestDetails.Follow"))) SaveFollowedQuest(entry.Id, !followed);
        ImGui.TextDisabled(entry.Section.Length > 0 ? $"{entry.Section} › {entry.Category}" : entry.Category);
        if(entry.IsRepeatable)ImGui.TextDisabled(localization.Get("QuestDetails.Repeatable"));
        if(developerMode)ImGui.TextDisabled($"ID {entry.Id}");
        ImGui.TextUnformatted(QuestProgressLabel(entry));
        // T13 (0.648): what this quest unlocks.
        foreach(var unlocked in duties.State.Catalog?.UnlockedBy(entry.Id) ?? [])ImGui.TextDisabled(localization.Format("DutyUi.Unlocks",unlocked.Name));
        DrawQuestRewards(entry);
        if (entry.Level != 0)
            ImGui.TextUnformatted(localization.Format("QuestDetails.Level",entry.Level));
        if(entry.AdditionalLevel!=0)ImGui.TextUnformatted(localization.Format("QuestDetails.OtherLevel",entry.AdditionalLevel));
        if (!string.IsNullOrWhiteSpace(entry.Zone)) ImGui.TextUnformatted(localization.Format("QuestDetails.Zone",entry.Zone));
        if (!string.IsNullOrWhiteSpace(entry.Expansion)) ImGui.TextUnformatted(localization.Format("QuestDetails.Expansion",entry.Expansion));
        if (developerMode && entry.IssuerId != 0) ImGui.TextDisabled(localization.Format("DevMode.IssuerId",entry.IssuerId));
        if (entry.IssuerMapId != 0 && entry.IssuerTerritoryId != 0)
        {
            var issuerMap = maps.State.Catalog?.Find(entry.IssuerMapId);
            if (issuerMap is not null && issuerMap.TerritoryId == entry.IssuerTerritoryId)
            {
                // Tensho map (0.118): centred on the quest giver with the flag (cached position index), else the whole map.
                if (ImGui.SmallButton(localization.Get("QuestDetails.Map")+"###QuestMap"))
                {
                    if (mapLocal?.State.Index?.Quest(entry.Id) is { } giver && giver.MapId == issuerMap.MapId) ShowOnTenshoMap(issuerMap, new Vector3(giver.X, 0, giver.Z));
                    else ShowMapOnTenshoMap(issuerMap.MapId);
                }
                Tip("QuestDetails.MapHelp");
            }
            else ImGui.TextDisabled(localization.Get("QuestDetails.NoMap"));
        }
        if (ImGui.TreeNodeEx(localization.Get("QuestDetails.Previous")+"###QuestPrevious", ImGuiTreeNodeFlags.DefaultOpen))
        {
            try
            {
                if (entry.PreviousQuestIds.Count == 0) ImGui.TextDisabled(localization.Get("QuestDetails.NoPrevious"));
                else
                {
                    ImGui.TextUnformatted(localization.Get(entry.PreviousQuestJoin switch {1=>"QuestRewards.AllPrevious",2=>"QuestRewards.AnyPrevious",_=>"QuestRewards.UnknownPrevious"}));
                    foreach (var id in entry.PreviousQuestIds) DrawQuestLink(id, "previous");
                    if (entry.PreviousQuestStep != 0)
                        ImGui.TextWrapped(localization.Get("QuestDetails.Step"));
                    ImGui.TextWrapped(localization.Get(questLinks.CompletionEvidence(entry) ? "QuestDetails.Confirmed":"QuestDetails.Unconfirmed"));
                }
            }
            finally { ImGui.TreePop(); }
        }
        DrawQuestPath(entry);
        var following = questLinks.Following(entry.Id);
        if (ImGui.TreeNode(localization.Format("QuestDetails.Following",following.Count)+"###QuestFollowing"))
        {
            try { foreach (var next in following) DrawQuestLink(next.Id, "next"); }
            finally { ImGui.TreePop(); }
        }
        if (entry.IsRepeatable) ImGui.TextWrapped(localization.Get("QuestDetails.RepeatHelp"));
        ImGui.Separator();
    }

    private void DrawQuestLink(uint id, string direction)
    {
        var linked = questLinks.Find(id);
        if (linked is not null)
        {
            DrawGameIcon(linked.Icon,new Vector2(24));
            if (ImGui.IsItemClicked()) SelectQuest(id);
            ImGui.SameLine();
        }
        if (ImGui.SmallButton($"{linked?.Name ?? localization.Format("QuestDetails.Quest",id)}##{direction}{id}")) SelectQuest(id);
        QuestMenu($"QuestLinkMenu{direction}{id}", id, linked?.Name ?? localization.Format("QuestDetails.Quest",id));
        ImGui.SameLine();
        ImGui.TextDisabled(linked is null ? localization.Get("QuestDetails.MissingLink") : QuestProgressLabel(linked));
    }

    private string QuestProgressLabel(QuestEntry entry) => QuestProgressLabel(entry.Progress, entry.Sequence);

    private string QuestProgressLabel(QuestProgress progress, byte? sequence) => progress switch
    {
        QuestProgress.Complete => localization.Get("QuestsBrowser.Completed"),
        // 255 is shown with its raw value; "final" follows the community convention for the last quest step (not locally verified).
        QuestProgress.Active when sequence == byte.MaxValue => localization.Format("QuestsBrowser.SequenceFinal",byte.MaxValue),
        QuestProgress.Active => localization.Format("QuestsBrowser.Sequence",sequence?.ToString() ?? "—"),
        _ => localization.Get("QuestsBrowser.Unknown")
    };

    // State filters of the Quests page and of an item's quest sources; literal keys, so the tooltip key test sees them.
    // 0.583 (T12): computed once per quest and per published list.
    private (QuestLinks Links,uint Id,TenshoAssistant.Quests.QuestList.QuestPath Path)? questPath;
    private void DrawQuestPath(QuestEntry entry)
    {
        if(entry.PreviousQuestIds.Count==0)return;
        if(questPath is not { } known || !ReferenceEquals(known.Links,questLinks) || known.Id!=entry.Id)
        {
            known=(questLinks,entry.Id,TenshoAssistant.Quests.QuestList.PathTo(questLinks,entry.Id));
            questPath=known;
        }
        var path=known.Path;
        if(path.Todo.Count>0 && ImGui.TreeNodeEx(localization.Format("QuestDetails.PathTitle",path.Todo.Count)+"###QuestPath"))
        {
            try
            {
                ImGui.TextColored(Accent,localization.Format("QuestDetails.PathFirst",path.Todo[0].Name));
                foreach(var step in path.Todo.Take(30))DrawQuestLink(step.Id,"path");
                if(path.Todo.Count>30)ImGui.TextDisabled(localization.Format("QuestDetails.PathMore",path.Todo.Count-30));
            }
            finally{ImGui.TreePop();}
        }
        if(path.Unknown)ImGui.TextDisabled(localization.Get("QuestDetails.PathUnknown"));
        if(path.Cut)ImGui.TextDisabled(localization.Get("QuestDetails.PathCut"));
    }
    private void QuestFilterTip(QuestSourceProgressFilter filter)
    {
        switch (filter)
        {
            case QuestSourceProgressFilter.All: Tip("QuestsBrowser.AllTip"); break;
            case QuestSourceProgressFilter.Active: Tip("QuestsBrowser.ActiveTip"); break;
            case QuestSourceProgressFilter.Complete: Tip("QuestsBrowser.CompleteTip"); break;
            case QuestSourceProgressFilter.Unconfirmed: Tip("QuestsBrowser.UnconfirmedTip"); break;
        }
    }

    private void SaveFollowedQuest(uint id, bool follow)
    {
        var old = configuration.FollowedQuests;
        var updated = (old ?? []).Where(value => value != 0).Distinct().ToList();
        if (follow && !updated.Contains(id))
        {
            if (updated.Count >= 64) { questFollowError = "QuestsBrowser.FollowLimit"; return; }
            updated.Add(id);
        }
        else if (!follow) updated.Remove(id);
        try
        {
            configuration.FollowedQuests = updated; save(); questFollowError = null;
        }
        catch (Exception error)
        {
            configuration.FollowedQuests = old!;
            questFollowError = "QuestsBrowser.FollowError";
            log.Warning(error, "Could not save followed quests.");
        }
    }

    private void DrawFollowedQuests()
    {
        var followed = configuration.FollowedQuests;
        if (TenshoAssistant.UI.UiComponents.Header(localization.Format("QuestsBrowser.Followed",followed?.Count ?? 0)+"###FollowedQuests"))
        {
            ImGui.TextDisabled(localization.Get("QuestsBrowser.FollowHelp"));
            if (followed is not { Count: > 0 }) EmptyState("QuestsBrowser.FollowEmpty");
            else
            {
                uint? remove = null;
                var visible = ImGui.BeginChild("FollowedQuestList", new Vector2(0, 140), true);
                try
                {
                    if (visible)
                    for (var index = 0; index < followed.Count; index++)
                    {
                        var id = followed[index];
                        ImGui.PushID(index);
                        try
                        {
                            if (DeleteButton("CraftPresets.Remove")) remove = id;
                            ImGui.SameLine(); DrawQuestLink(id, "followed");
                        }
                        finally { ImGui.PopID(); }
                    }
                }
                finally { ImGui.EndChild(); }
                if (remove is { } removed) SaveFollowedQuest(removed, false);
            }
        }
        if (questFollowError is { } errorText) ImGui.TextWrapped(localization.Get(errorText));
    }
}
