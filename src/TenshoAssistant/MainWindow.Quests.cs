using Dalamud.Bindings.ImGui;
using System.Numerics;
using System.Globalization;
using TenshoAssistant.Items;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private QuestState? questViewState;
    private IReadOnlyList<QuestEntry>? questMetadataEntries;
    private string questViewQuery = "";
    private string questViewCategory = "";
    private int questViewFilter = -1;
    private string[] questCategories = [];
    private QuestEntry[] questMatches = [];
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
            questActiveCount = state.Entries.Count(entry => entry.Progress == QuestProgress.Active);
            questCompleteCount = state.Entries.Count(entry => entry.Progress == QuestProgress.Complete);
        }
        if (ImGui.Button(localization.Get("QuestsBrowser.Refresh"))) quests.RequestRefresh();
        ImGui.SameLine();
        ImGui.TextDisabled(state.Loading ? localization.Get("QuestsBrowser.Loading") : state.ObservedAt is { } date ? localization.Format("QuestsBrowser.Observed",date.ToLocalTime().ToString("g")) : localization.Get("QuestsBrowser.Offline"));
        if (state.Error is { } error) {ImGui.TextWrapped(localization.Get("QuestsBrowser.Error"));if(developerMode)ImGui.TextWrapped(error);}
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
        ImGui.SetNextItemWidth(260);
        if (ImGui.BeginCombo(localization.Get("QuestsBrowser.Category")+"###QuestCategory", string.IsNullOrWhiteSpace(questCategory) ? localization.Get("QuestsBrowser.All") : questCategory))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("QuestsBrowser.All"), string.IsNullOrWhiteSpace(questCategory))) questCategory = string.Empty;
                foreach (var category in questCategories)
                    if (ImGui.Selectable(category, questCategory == category)) questCategory = category;
            }
            finally { ImGui.EndCombo(); }
        }
        var labels = new[] { "All", "Active", "Complete", "Unknown" };
        for (var index = 0; index < labels.Length; index++)
        {
            if (index > 0) ImGui.SameLine();
            if (ImGui.RadioButton(localization.Get("QuestsBrowser."+labels[index])+"###QuestFilter"+index, questFilter == index)) questFilter = index;
        }
        ImGui.TextDisabled(localization.Format("QuestsBrowser.Count",questActiveCount,questCompleteCount));
        ImGui.TextWrapped(localization.Get("QuestsBrowser.Help"));
        if (!ReferenceEquals(questViewState?.Entries, state.Entries) || questViewQuery != questQuery
            || questViewCategory != questCategory || questViewFilter != questFilter)
        {
            questMatches = state.Entries.Where(entry => (string.IsNullOrWhiteSpace(questCategory) || entry.Category == questCategory)
                && (questFilter == 0 || entry.Progress == (questFilter == 1 ? QuestProgress.Active : questFilter == 2 ? QuestProgress.Complete : QuestProgress.Unconfirmed))
                && (string.IsNullOrWhiteSpace(questQuery) || entry.Name.Contains(questQuery.Trim(), StringComparison.OrdinalIgnoreCase)
                    || entry.Id.ToString(CultureInfo.InvariantCulture) == questQuery.Trim())).ToArray();
            questViewState = state; questViewQuery = questQuery;
            questViewCategory = questCategory; questViewFilter = questFilter;
        }
        var matches = questMatches;
        if (matches.Length == 0) { ImGui.TextDisabled(localization.Get("QuestsBrowser.Empty")); return; }
        if (!ImGui.BeginTable("QuestEntries", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("QuestsBrowser.Quest"), ImGuiTableColumnFlags.WidthStretch, 2);
            ImGui.TableSetupColumn(localization.Get("QuestsBrowser.Category"));
            ImGui.TableSetupColumn(localization.Get("QuestsBrowser.Progress"));
            ImGui.TableHeadersRow();
            var clipper = ImGui.ImGuiListClipper();
            try
            {
            clipper.Begin(matches.Length);
            while (clipper.Step())
            for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
            {
                var entry = matches[index];
                ImGui.TableNextRow(); ImGui.TableNextColumn();
                DrawItemIcon(new ItemDefinition(0, entry.Name, false, entry.Icon), new Vector2(24));
                if (ImGui.IsItemClicked()) SelectQuest(entry.Id);
                ImGui.SameLine();
                if (ImGui.Selectable($"{entry.Name}##quest{entry.Id}", selectedQuestId == entry.Id)) SelectQuest(entry.Id);
                if (ImGui.IsItemHovered()) WrappedTooltip(developerMode ? $"{entry.Name}\n{entry.Category}\nID: {entry.Id}":$"{entry.Name}\n{entry.Category}");
                ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.Category);
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
        if (!ImGui.CollapsingHeader(localization.Get("QuestDetails.Title")+"###QuestDetailsHeader", ImGuiTreeNodeFlags.DefaultOpen)) return;
        if (questNavigation.Count > 0)
        {
            if (ImGui.SmallButton(localization.Get("QuestDetails.Back")+"###QuestBack")) selectedQuestId = questNavigation.Pop();
            ImGui.SameLine();
        }
        if (ImGui.SmallButton(localization.Get("QuestDetails.Close"))) { selectedQuestId = 0; questShowDetails = false; questNavigation.Clear(); return; }
        var entry = questLinks.Find(selectedQuestId);
        if (entry is null) { ImGui.TextDisabled(localization.Format("QuestDetails.Missing",selectedQuestId)); return; }
        DrawItemIcon(new ItemDefinition(0, entry.Name, false, entry.Icon), new Vector2(36));
        ImGui.SameLine(); ImGui.TextColored(Accent, entry.Name);
        var followed = configuration.FollowedQuests?.Contains(entry.Id) == true;
        if (ImGui.SmallButton(localization.Get(followed ? "QuestDetails.Unfollow":"QuestDetails.Follow"))) SaveFollowedQuest(entry.Id, !followed);
        ImGui.TextDisabled(entry.Category);
        if(entry.IsRepeatable)ImGui.TextDisabled(localization.Get("QuestDetails.Repeatable"));
        if(developerMode)ImGui.TextDisabled($"ID {entry.Id}");
        ImGui.TextUnformatted(QuestProgressLabel(entry));
        DrawQuestRewards(entry);
        if (entry.Level != 0)
            ImGui.TextUnformatted(localization.Format("QuestDetails.Level",entry.Level));
        if(entry.AdditionalLevel!=0)ImGui.TextUnformatted(localization.Format("QuestDetails.OtherLevel",entry.AdditionalLevel));
        if (!string.IsNullOrWhiteSpace(entry.Zone)) ImGui.TextUnformatted(localization.Format("QuestDetails.Zone",entry.Zone));
        if (developerMode && entry.IssuerId != 0) ImGui.TextDisabled($"Issuer ID: {entry.IssuerId}");
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
                if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("QuestDetails.MapHelp"));
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
            DrawItemIcon(new ItemDefinition(0, linked.Name, false, linked.Icon), new Vector2(24));
            if (ImGui.IsItemClicked()) SelectQuest(id);
            ImGui.SameLine();
        }
        if (ImGui.SmallButton($"{linked?.Name ?? localization.Format("QuestDetails.Quest",id)}##{direction}{id}")) SelectQuest(id);
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
        if (ImGui.CollapsingHeader(localization.Format("QuestsBrowser.Followed",followed?.Count ?? 0)+"###FollowedQuests"))
        {
            ImGui.TextDisabled(localization.Get("QuestsBrowser.FollowHelp"));
            if (followed is not { Count: > 0 }) ImGui.TextWrapped(localization.Get("QuestsBrowser.FollowEmpty"));
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
                            if (ImGui.SmallButton(localization.Get("CraftPresets.Remove"))) remove = id;
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
