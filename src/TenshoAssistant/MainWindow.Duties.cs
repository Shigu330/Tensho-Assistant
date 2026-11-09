using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Duties;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Duty loot page (Duty Loot Preview-like): duties from the game sheets (InstanceContent → ContentFinderCondition), searched by name and
// category; the selected duty's loot fetched from Garland Tools only on « Fetch » (Duties/DutyLootService: one request at a time,
// cached) and merged with LuminaSupplemental (MainWindow.Lumina.cs); items filtered by origin (rewards, fight coffers, zone coffers), by
// kind and « not obtained only » (MainWindow.LootFilters.cs), each an ItemCell with the item tooltip and tools menu (0.252).
internal sealed partial class MainWindow
{
    private string dutyQuery = "";
    private string dutyCategory = "";
    private string dutyItemQuery = "";
    private int dutySourceFilter;
    private bool showDutyDetails;
    private DutyCatalog? dutyViewCatalog;
    private string dutyViewQuery = "";
    private string dutyViewCategory = "";
    private DutyDefinition[] dutyMatches = [];
    private string[] dutyCategories = [];
    private DutyLootDocument? dutyViewDocument;
    private ItemCatalog? dutyViewItems;
    private string dutyViewItemQuery = "";
    private int dutyViewSourceFilter = -1;
    private int dutyViewLanguageRevision=-1;
    // 0.331: collections state the list was filtered with (« not obtained only » depends on it, as in the Duty Finder panel since 0.326).
    private object? dutyViewCollections;
    private (uint Id, string Name, DutyLootEntry[] Sources)[] dutyItemMatches = [];
    // g, C-01 (0.355): unlocked / completed status of each duty (Duties/DutyProgressReader), « not completed » filters.
    internal Func<DutyProgressState?>? DutyProgressSource { get; set; }
    internal Func<bool>? DutyProgressUnavailable { get; set; }
    private DutyProgressFilter dutyProgressFilter;
    private object? dutyViewProgress;
    private DutyProgressFilter dutyViewProgressFilter;
    private (int Completed,int Known) dutyProgressCount;

    private void DrawDutyLoot()
    {
        Heading(localization.Get("DutyUi.Title"), localization.Get("DutyUi.Description"));
        var catalogState = duties.State;
        if (catalogState.Catalog is not { } catalog)
        { if(catalogState.Error is { } error)DrawDataError(error,"ServiceErrors.Duty");else ImGui.TextWrapped(localization.Format("ServiceErrors.DutyLoading",catalogState.Copied,catalogState.Total)); return; }
        if (showDutyDetails && dutyLoot.State.Duty is not null)
        {
            if (ImGui.SmallButton(localization.Get("DutyUi.Back"))) showDutyDetails = false;
            DrawDutyLootDetails(); return;
        }
        if (dutyLoot.State.Duty is not null && ImGui.SmallButton(localization.Get("DutyUi.Selected"))) showDutyDetails = true;
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##DutySearch",localization.Get("DutyUi.Search"),ref dutyQuery,128);
        Tip("DutyUi.SearchTip");
        if (!ReferenceEquals(dutyViewCatalog,catalog))
            dutyCategories = catalog.Entries.Select(duty => duty.Category).Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal).OrderBy(name => name,StringComparer.CurrentCultureIgnoreCase).ToArray();
        ImGui.SetNextItemWidth(240);
        if (ImGui.BeginCombo(localization.Get("DutyUi.Category"),dutyCategory.Length == 0 ? localization.Get("DutyUi.All") : dutyCategory))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("DutyUi.All"),dutyCategory.Length == 0)) dutyCategory = "";
                foreach (var category in dutyCategories)
                    if (ImGui.Selectable(category,dutyCategory == category)) dutyCategory = category;
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("DutyUi.CategoryTip");
        var progress = DutyProgressSource?.Invoke();
        // Without a status read the filter does nothing (unknown is never « not completed »).
        var progressFilter = progress is null ? DutyProgressFilter.All : dutyProgressFilter;
        ImGui.SameLine(); ImGui.SetNextItemWidth(240);
        ImGui.BeginDisabled(progress is null);
        try
        {
            if (ImGui.BeginCombo(localization.Get("DutyProgress.Filter"),localization.Get("DutyProgress.Filter" + dutyProgressFilter)))
            {
                try
                {
                    foreach (var filter in Enum.GetValues<DutyProgressFilter>())
                        if (ImGui.Selectable(localization.Get("DutyProgress.Filter" + filter),dutyProgressFilter == filter)) dutyProgressFilter = filter;
                }
                finally { ImGui.EndCombo(); }
            }
            Tip("DutyProgress.FilterTip");
        }
        finally { ImGui.EndDisabled(); }
        if (!ReferenceEquals(dutyViewCatalog,catalog) || dutyViewQuery != dutyQuery || dutyViewCategory != dutyCategory
            || !ReferenceEquals(dutyViewProgress,progress) || dutyViewProgressFilter != progressFilter)
        {
            var query = dutyQuery.Trim();
            dutyMatches = catalog.Entries.Where(duty => (dutyCategory.Length == 0 || duty.Category == dutyCategory)
                && (query.Length == 0 || duty.Name.Contains(query,StringComparison.CurrentCultureIgnoreCase)
                    || duty.DutyId.ToString(CultureInfo.InvariantCulture) == query)
                && DutyProgress.Matches(progressFilter,progress?.Of(duty.InstanceId))).ToArray();
            dutyProgressCount = progress is null ? default : DutyProgress.Count(catalog.Entries.Where(duty => dutyCategory.Length == 0 || duty.Category == dutyCategory),progress);
            dutyViewCatalog = catalog; dutyViewQuery = dutyQuery; dutyViewCategory = dutyCategory; dutyViewProgress = progress; dutyViewProgressFilter = progressFilter;
        }
        ImGui.TextDisabled(localization.Format("DutyCounts.Duties",dutyMatches.Length));
        ImGui.SameLine();
        if (progress is not null) ImGui.TextDisabled(localization.Format("DutyProgress.Count",dutyProgressCount.Completed,dutyProgressCount.Known));
        // X-106 (0.990): logged out, the last character's saved state, said as such.
        if (progress is { FromDisk: true }) ImGui.TextDisabled(localization.Format("DutyProgress.Saved",progress.ReadAt.ToLocalTime().ToString("g")));
        else ImGui.TextDisabled(localization.Get(DutyProgressUnavailable?.Invoke() == true ? "DutyProgress.Unavailable" : "DutyProgress.Unknown"));
        if (!ImGui.BeginTable("DutyCatalog",4,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("DutyUi.Duty"),ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("DutyUi.Type")); ImGui.TableSetupColumn(localization.Get("DutyUi.Level"));
            ImGui.TableSetupColumn(localization.Get("DutyProgress.Column")); ImGui.TableHeadersRow();
            var clipper = ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(dutyMatches.Length);
                while (clipper.Step())
                for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                {
                    var duty = dutyMatches[index];
                    ImGui.TableNextRow(); ImGui.TableNextColumn();
                    DrawGameIcon(duty.Icon,new Vector2(24));
                    var clicked = ImGui.IsItemClicked(); ImGui.SameLine();
                    if (ImGui.Selectable($"{duty.Name}##Duty{duty.DutyId}",dutyLoot.State.Duty?.DutyId == duty.DutyId,ImGuiSelectableFlags.None,new Vector2(0,24)) || clicked)
                    { dutyLoot.Select(duty); showDutyDetails = true; dutyItemQuery = ""; }
                    if (developerMode && ImGui.IsItemHovered()) WrappedTooltip($"Duty {duty.DutyId} · instance {duty.InstanceId} · territory {duty.TerritoryId}");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(duty.Category);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(duty.Level.ToString(CultureInfo.InvariantCulture));
                    ImGui.TableNextColumn(); DrawDutyStatus(progress?.Of(duty.InstanceId));
                }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
    }

    // T13 (0.648): the quest that unlocks the duty, where the game says so.
    private void DrawDutyUnlock(DutyDefinition duty)
    {
        if(duty.UnlockQuest==0 || QuestOf(duty.UnlockQuest) is not { Name.Length:>0 } quest)return;
        ImGui.AlignTextToFramePadding();ImGui.TextDisabled(localization.Get("DutyUi.UnlockedBy"));ImGui.SameLine();
        if(ImGui.SmallButton($"{quest.Name}##DutyUnlock{duty.DutyId}")){SelectQuest(quest.Id);page=Page.Quests;}
        ImGui.SameLine();ImGui.TextDisabled(QuestProgressLabel(quest));
    }
    // X-90 (0.994, DEC-045 and DEC-041): the duty's mechanics and strategy from the wiki (GFDL), on a click, out of combat only (hidden in combat).
    internal WikiDutyGuides? DutyGuides { get; set; }
    internal Func<bool>? InCombatNow { get; set; }
    private void DrawDutyGuide(DutyDefinition duty)
    {
        if(DutyGuides is not { } guides)return;
        guides.Select(duty.DutyId);
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("DutyGuide.Title")+"###DutyGuide"))return;
        if(InCombatNow?.Invoke()==true){EmptyState("DutyGuide.InCombat");return;}
        var view=guides.State;
        ImGui.BeginDisabled(view.Loading);
        try{if(SourceButton(localization.Get(view.Document is null ? "DutyGuide.Fetch":"DutyGuide.Refresh"),configuration.CommunityWiki,true))_=guides.RequestAsync(view.Document is not null);}
        finally{ImGui.EndDisabled();}
        Tip("DutyGuide.FetchTip");
        if(view.Loading)ImGui.TextDisabled(localization.Get("DutyGuide.Loading"));
        if(view.Error is { } error)DrawDataError(error,"DutyGuide.Error");
        if(view.Document is not { } document)return;
        ImGui.TextDisabled(localization.Format("DutyGuide.Source",document.Title,document.FetchedAt.ToLocalTime().ToString("g")));
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("DutyGuide.Open")))Dalamud.Utility.Util.OpenLink(document.Url);
        if(document.Sections.Count==0){EmptyState("DutyGuide.Empty");return;}
        foreach(var section in document.Sections)
        {
            if(!ImGui.TreeNode(section.Title+$"###DutyGuide{section.Title}"))continue;
            try{ImGui.TextWrapped(section.Text);}
            finally{ImGui.TreePop();}
        }
    }

    // X-90 (0.995, DEC-045): the player's note and arena drawing for the duty, in Tensho's window, hidden in combat; never drawn over the game.
    private int arenaDragged=-1;
    private string arenaLabel="";
    private string? arenaMessage;
    private void DrawDutyNotes(DutyDefinition duty)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("DutyNotes.Title")+"###DutyNotes"))return;
        if(InCombatNow?.Invoke()==true){EmptyState("DutyGuide.InCombat");return;}
        var notes=configuration.DutyNotes ??= [];
        var note=notes.GetValueOrDefault(duty.DutyId) ?? new DutyNote();
        var changed=false;
        var text=note.Text;
        ImGui.InputTextMultiline("##DutyNoteText",ref text,DutyArena.MaxText,new Vector2(-1,90));
        Tip("DutyNotes.TextTip");
        if(text!=note.Text)note.Text=text;
        if(ImGui.IsItemDeactivatedAfterEdit())changed=true;
        var round=note.Shape==ArenaShape.Round;
        if(ImGui.RadioButton(localization.Get("DutyNotes.Round"),round)){note.Shape=ArenaShape.Round;changed=true;}
        Tip("DutyNotes.ShapeTip");
        ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("DutyNotes.Square"),!round)){note.Shape=ArenaShape.Square;changed=true;}
        Tip("DutyNotes.ShapeTip");
        // The drawing: north up, marks dragged with the mouse, a right click removes one.
        var side=Math.Min(260f,ImGui.GetContentRegionAvail().X);
        var origin=ImGui.GetCursorScreenPos();var centre=origin+new Vector2(side/2);var radius=side/2-14;
        ImGui.InvisibleButton("##ArenaCanvas",new Vector2(side));
        var draw=ImGui.GetWindowDrawList();
        var line=ImGui.GetColorU32(TenshoAssistant.UI.FfxivTheme.TextMuted);
        if(note.Shape==ArenaShape.Round)draw.AddCircle(centre,radius,line,64,2);
        else draw.AddRect(centre-new Vector2(radius),centre+new Vector2(radius),line,0,ImDrawFlags.None,2);
        draw.AddText(centre-new Vector2(4,radius+13),line,localization.Get("DutyNotes.North"));
        Vector2 At(ArenaToken token)=>centre+new Vector2(DutyArena.Clamp(token.X),DutyArena.Clamp(token.Y))*radius;
        var mouse=ImGui.GetIO().MousePos;var hovered=ImGui.IsItemHovered();
        if(hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))arenaDragged=note.Tokens.FindIndex(token=>Vector2.Distance(At(token),mouse)<=12);
        if(arenaDragged>=0 && arenaDragged<note.Tokens.Count && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {var moved=(mouse-centre)/radius;note.Tokens[arenaDragged].X=DutyArena.Clamp(moved.X);note.Tokens[arenaDragged].Y=DutyArena.Clamp(moved.Y);}
        if(arenaDragged>=0 && ImGui.IsMouseReleased(ImGuiMouseButton.Left)){arenaDragged=-1;changed=true;}
        if(hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && note.Tokens.FindIndex(token=>Vector2.Distance(At(token),mouse)<=12) is var removed and >=0){note.Tokens.RemoveAt(removed);changed=true;}
        foreach(var token in note.Tokens)
        {
            var at=At(token);
            draw.AddCircleFilled(at,11,ImGui.GetColorU32(Accent));
            var size=ImGui.CalcTextSize(token.Label);
            draw.AddText(at-size/2,ImGui.GetColorU32(new Vector4(0,0,0,1)),token.Label);
        }
        if(hovered)WrappedTooltip(localization.Get("DutyNotes.CanvasTip"));
        // Marks in one click (waymark names and roles), or a short label typed.
        ImGui.BeginDisabled(note.Tokens.Count>=DutyArena.MaxTokens);
        try
        {
            foreach(var label in DutyArena.QuickLabels)
            {
                if(ImGui.SmallButton(label+$"##ArenaQuick{label}")){note.Tokens.Add(new ArenaToken{Label=label});changed=true;}
                ImGui.SameLine();
            }
            ImGui.NewLine();
            ImGui.SetNextItemWidth(60);ImGui.InputText("##ArenaLabel",ref arenaLabel,DutyArena.MaxLabel);Tip("DutyNotes.LabelTip");
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("DutyNotes.Add")) && DutyArena.CleanLabel(arenaLabel) is { Length:>0 } typed){note.Tokens.Add(new ArenaToken{Label=typed});arenaLabel="";changed=true;}
            Tip("DutyNotes.LabelTip");
        }
        finally{ImGui.EndDisabled();}
        if(CopyButton(localization.Get("DutyNotes.Copy"),()=>DutyArena.Share(duty.DutyId,note)))arenaMessage=null;
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("DutyNotes.Paste")))
        {
            if(DutyArena.Parse(ImGui.GetClipboardText()) is { } pasted){note=pasted.Note;changed=true;arenaMessage=pasted.Duty==duty.DutyId ? null:localization.Get("DutyNotes.OtherDuty");}
            else arenaMessage=localization.Get("DutyNotes.NotACode");
        }
        Tip("DutyNotes.PasteTip");
        if(arenaMessage is { } said)ImGui.TextDisabled(said);
        if(!changed)
        {
            if(!notes.ContainsKey(duty.DutyId) && (note.Text.Length>0 || note.Tokens.Count>0))notes[duty.DutyId]=note;
            return;
        }
        if(note.Text.Length==0 && note.Tokens.Count==0)notes.Remove(duty.DutyId);
        else if(notes.ContainsKey(duty.DutyId) || notes.Count<DutyArena.MaxNotes)notes[duty.DutyId]=note;
        TrySave("DutyNotes.Title");
    }

    private void DrawDutyLootDetails()
    {
        var state = dutyLoot.State;
        if (state.Duty is not { } duty) return;
        DrawGameIcon(duty.Icon,new Vector2(32)); ImGui.SameLine();
        ImGui.TextColored(Accent,duty.Name);
        ImGui.TextDisabled(localization.Format("DutyCounts.Level",duty.Category,duty.Level));
        ImGui.SameLine(); DrawDutyStatus(DutyProgressSource?.Invoke()?.Of(duty.InstanceId));
        DrawDutyUnlock(duty);
        if(developerMode)ImGui.TextDisabled(localization.Format("DevMode.DutyInstance",duty.DutyId,duty.InstanceId));
        DrawDutyGuide(duty);
        DrawDutyNotes(duty);
        if (state.Loading)
        { if (ImGui.Button(localization.Get("DutyUi.Cancel"))) dutyLoot.Cancel(); ImGui.SameLine(); ImGui.TextDisabled(localization.Get("DutyUi.Loading")); }
        else
        {
            if (ImGui.Button(localization.Get("DutyUi.Fetch"))) _ = dutyLoot.RequestAsync();
            if (state.Document is not null)
            { ImGui.SameLine(); if (SourceButton(localization.Get("DutyUi.Refresh"),configuration.CommunityGarland)) _ = dutyLoot.RequestAsync(true); }
        }
        if (state.Error is { } error) DrawDataError(error,"ServiceErrors.Duty");
        ImGui.TextWrapped(localization.Get("DutyUi.Source"));
        if (DutyDocument(state) is not { } document)
        { ImGui.TextDisabled(localization.Get("DutyUi.NeedFetch")); return; }
        // 0.331: date and cache stamp of the Garland document only (a LuminaSupplemental-only merge was dated « now », nothing being fetched).
        if (state.Document is { } fetched)
        { ImGui.TextDisabled(localization.Format("DutyCounts.Fetched",fetched.FetchedAt.ToLocalTime().ToString("g"))); DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Sources,fetched.FetchedAt); }
        else ImGui.TextDisabled(localization.Get("DutyFinder.Lumina"));
        if (!document.HasRewards || !document.HasFights || !document.HasCoffers)
            ImGui.TextWrapped(localization.Get("DutyUi.Partial"));
        if (document.HasUnparsedFightDrops)
            ImGui.TextColored(Accent,localization.Get("DutyUi.Unparsed"));
        if (itemId > 0)
        {
            ImGui.TextDisabled(localization.Format("UiFragments.Context",SelectedItem?.Name ?? localization.Format("BankUi.Item",itemId)));
            if (ImGui.SmallButton(localization.Get("DutyUi.Stocks"))) { page = Page.Items; focusStocksTab = true; }
            ImGui.SameLine(); if (ImGui.SmallButton(localization.Get("DutyUi.Recipes"))) OpenContextTool("Craftimizer");
            ImGui.SameLine(); if (ImGui.SmallButton(localization.Get("DutyUi.Prices"))) OpenContextTool("Price Insight");
        }
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##DutyItemSearch",localization.Get("DutyUi.ItemQuery"),ref dutyItemQuery,128);
        Tip("DutyUi.ItemQueryTip");
        var labels = new[] { localization.Get("DutyUi.AllSources"), localization.Get("DutyUi.Rewards"), localization.Get("DutyUi.FightCoffers"), localization.Get("DutyUi.ZoneCoffers") };
        for (var index = 0; index < labels.Length; index++)
        {
            if (index > 0) ImGui.SameLine();
            if (ImGui.RadioButton(labels[index],dutySourceFilter == index)) dutySourceFilter = index;
            Tip("DutyUi.SourceFilterTip");
        }
        DrawLootKindFilter("DutyLoot");
        var items = itemCatalog.State.Catalog;
        if (dutyViewLootKind != lootKind || dutyViewLootMissing != lootMissingOnly) { dutyViewDocument = null; dutyViewLootKind = lootKind; dutyViewLootMissing = lootMissingOnly; }
        if (!ReferenceEquals(dutyViewDocument,document) || !ReferenceEquals(dutyViewItems,items)
            || dutyViewItemQuery != dutyItemQuery || dutyViewSourceFilter != dutySourceFilter || dutyViewLanguageRevision!=localization.Revision
            || (lootMissingOnly && !ReferenceEquals(dutyViewCollections,collections.State)))
        {
            var query = dutyItemQuery.Trim();
            dutyItemMatches = document.Entries.Where(entry => dutySourceFilter == 0 || (int)entry.Kind == dutySourceFilter-1)
                .GroupBy(entry => entry.ItemId).Select(group => (Id:group.Key,Name:items?.Find(group.Key)?.Name ?? localization.Format("BankUi.Item",group.Key),Sources:group.ToArray()))
                .Where(item => LootKindMatches(items?.Find(item.Id)))
                .Where(item => query.Length == 0 || item.Name.Contains(query,StringComparison.CurrentCultureIgnoreCase)
                    || item.Id.ToString(CultureInfo.InvariantCulture) == query)
                .OrderBy(item => item.Name,StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.Id).ToArray();
            dutyViewDocument = document; dutyViewItems = items; dutyViewItemQuery = dutyItemQuery; dutyViewSourceFilter = dutySourceFilter;
            dutyViewLanguageRevision=localization.Revision; dutyViewCollections=collections.State;
        }
        ImGui.TextDisabled(localization.Format("DutyCounts.Items",dutyItemMatches.Length));
        if (dutyItemMatches.Length == 0) { EmptyState("DutyUi.Empty"); return; }
        if (!ImGui.BeginTable("DutyLootEntries",2,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("DutyUi.Item"),ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("DutyUi.Sources")); ImGui.TableHeadersRow();
            var clipper = ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(dutyItemMatches.Length);
                while (clipper.Step())
                for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                {
                    var item = dutyItemMatches[index];
                    var local = items?.Find(item.Id);
                    ImGui.TableNextRow(); ImGui.TableNextColumn();
                    // 0.186: full item tooltip and right click = links to the other tools (ItemCell since 0.252).
                    if (local is not null && item.Id <= int.MaxValue) ItemCell(local,item.Id,28,item.Name);
                    else
                    {
                        DrawItemIcon(local,new Vector2(28)); ImGui.SameLine(); ImGui.TextDisabled(item.Name);
                        Tip("UiFragments.MissingItem");
                    }
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(item.Sources.Length == 1 ? DutySourceLabel(item.Sources[0]) : $"{item.Sources.Length} sources");
                    if (ImGui.IsItemHovered())
                        WrappedTooltip(string.Join('\n',item.Sources.Select(DutySourceLabel)));
                }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
    }
    private string DutySourceLabel(DutyLootEntry entry) => entry.Kind switch
    {
        DutyLootSourceKind.Reward => localization.Get("DutyUi.Reward"),
        DutyLootSourceKind.Seen => localization.Get("DutyUi.SeenChest"),
        DutyLootSourceKind.FightCoffer => localization.Format("DutyCounts.Fight",entry.SourceIndex+1) + (string.IsNullOrWhiteSpace(entry.SourceName) ? "" : $" · {entry.SourceName}"),
        _ => localization.Format("DutyCounts.Coffer",entry.SourceIndex+1)
    };

    private void DrawDutyStatus(DutyStatus? status)
    {
        switch (status)
        {
            case DutyStatus.Completed: ImGui.TextColored(Success,localization.Get("DutyProgress.Completed")); break;
            case DutyStatus.Unlocked: ImGui.TextColored(Accent,localization.Get("DutyProgress.Unlocked")); break;
            case DutyStatus.Locked: ImGui.TextDisabled(localization.Get("DutyProgress.Locked")); break;
            default: ImGui.TextDisabled("—"); break;
        }
    }
}
