using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

// Triple Triad page (Gold Saucer), like TriadBuddy. Tabs: Notebook (game cards with owned state from the collection observation,
// values, filters and sorts by side, total, weakest side or rule score; « Cards » and « Cards Ex. » tabs, 0.277–0.279), Deck (saved
// Tensho decks + the 5-card deck editor), Simulation, NPCs and cards, Observation (MainWindow.Triad*.cs). Other tools reach a card
// or a tab through ShowTriadCard / OpenTriadTab.
internal sealed partial class MainWindow
{
    private CollectionState? triadViewState;
    private CollectionState? triadCatalogState;
    private CollectionEntry[] triadCards = [];
    private CollectionEntry[] triadMatches = [];
    private string triadQuery = "";
    private string triadPreviousQuery = "";
    private int triadStars;
    private int triadPreviousStars = -1;
    private int triadAcquisition;
    private int triadPreviousAcquisition = -1;
    private uint? triadType;
    private uint? triadPreviousType;
    private uint selectedTriad;
    private bool focusTriadNotebook,triadScrollToSelected;
    // 0.277 (user request): card values in the notebook list, sort by a side, the total, the weakest side or the rule score.
    private int triadSort, triadPreviousSort = -1;
    // 0.279 (user request): two tabs, ordinary cards and the Ex. series (numbered apart).
    private bool triadExSeries, triadPreviousExSeries, focusTriadSeries;
    private bool triadSortDescending, triadPreviousDescending;
    private MiniGames.TriadRules triadScoreRules, triadPreviousScoreRules;
    private string TriadSides(MiniGames.TriadCardDefinition card)
        => localization.Format("TriadSort.Sides", MiniGames.TriadCardDefinition.Value(card.Top), MiniGames.TriadCardDefinition.Value(card.Right),
            MiniGames.TriadCardDefinition.Value(card.Bottom), MiniGames.TriadCardDefinition.Value(card.Left), card.Top + card.Right + card.Bottom + card.Left,
            MiniGames.TriadCardScore.Score(card, triadScoreRules));
    private void DrawTriadSortControls()
    {
        var names = Enum.GetNames<MiniGames.TriadCardSort>().Select(name => localization.Get("TriadSort." + name)).ToArray();
        ImGui.SetNextItemWidth(170);
        ImGui.Combo(localization.Get("TriadSort.Title") + "##TriadSort", ref triadSort, names, names.Length); Tip("TriadSort.TitleTip");
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get(triadSortDescending ? "TriadSort.Descending" : "TriadSort.Ascending") + "##TriadSortOrder")) triadSortDescending = !triadSortDescending;
        Tip("TriadSort.OrderTip");
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("TriadSort.Rules") + "##TriadScoreRules")) ImGui.OpenPopup("TriadScoreRules");
        Tip("TriadSort.ScoreHelp");
        if (!ImGui.BeginPopup("TriadScoreRules")) return;
        try
        {
            ImGui.TextDisabled(localization.Get("TriadSort.ScoreHelp"));
            foreach (var (rule, key) in new[] { (MiniGames.TriadRules.Reverse, "TriadSim.Reverse"), (MiniGames.TriadRules.FallenAce, "TriadSim.FallenAce"), (MiniGames.TriadRules.Same, "TriadSim.Same") })
            {
                var on = (triadScoreRules & rule) != 0;
                if (ImGui.Checkbox(localization.Get(key) + "##Score" + rule, ref on)) triadScoreRules = on ? triadScoreRules | rule : triadScoreRules & ~rule;
            }
            var family = (triadScoreRules & MiniGames.TriadRules.Ascension) != 0 ? 1 : (triadScoreRules & MiniGames.TriadRules.Descension) != 0 ? 2 : 0;
            var families = new[] { localization.Get("TriadSim.NoEffect"), localization.Get("TriadSim.Ascension"), localization.Get("TriadSim.Descension") };
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(localization.Get("TriadSim.Families") + "##ScoreFamily", ref family, families, families.Length))
                triadScoreRules = (triadScoreRules & ~(MiniGames.TriadRules.Ascension | MiniGames.TriadRules.Descension))
                    | (family == 1 ? MiniGames.TriadRules.Ascension : family == 2 ? MiniGames.TriadRules.Descension : MiniGames.TriadRules.Normal);
        }
        finally { ImGui.EndPopup(); }
    }
    // 0.273: a link can ask for the Deck or « NPCs and cards » tab (an NPC's « Triple Triad tool », the Deck button of the settings).
    private string? triadFocusTab;
    private void OpenTriadTab(string tab){page=Page.TripleTriad;triadFocusTab=tab;}
    private ImGuiTabItemFlags TriadTabFlags(string tab)=>triadFocusTab==tab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None;
    // 0.267: « Show in Triple Triad » from any menu — the notebook tab, filters cleared so the card cannot be hidden, card selected.
    private void ShowTriadCard(uint cardId)
    {
        page=Page.TripleTriad;selectedTriad=cardId;focusTriadNotebook=true;triadScrollToSelected=true;
        // 0.331 (audit): the series comes from the live collection; triadCards stays empty until the page was drawn once.
        triadExSeries=TriadCard(cardId)?.Extra==true;focusTriadSeries=true;
        triadQuery="";triadStars=0;triadAcquisition=0;triadType=null;
    }
    private List<uint>? triadDeck;
    private string? triadDeckError;
    private bool triadDeckSaved;

    private void DrawTripleTriad()
    {
        Heading(localization.Get("TriadUi.Title"), localization.Get("TriadUi.Description"));
        var state = collections.State;
        if (ImGui.SmallButton(localization.Get("TriadUi.Refresh"))) collections.RequestRefresh();
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Collections,collections.State.ObservedAt,collections.State.FromCache);
        ImGui.SameLine();
        ImGui.TextDisabled(state.Loading ? localization.Get("TriadUi.Loading") : state.ObservedAt is { } date ? localization.Format("QuestsBrowser.Observed",date.ToLocalTime().ToString("g")) : localization.Get("TriadUi.Offline"));
        if (state.Error is { } error) DrawDataError(error,"ServiceErrors.Collection");
        foreach (var issue in state.CategoryIssues ?? []) if (issue.Kind == Collections.CollectionKind.TripleTriad) ImGui.TextWrapped(issue.Reason);
        triadDeck ??= (configuration.TripleTriadDeck ?? []).ToList();
        if (!ReferenceEquals(triadCatalogState, state))
        {
            triadCatalogState = state;
            triadCards = state.Entries.Where(entry => entry.Kind == CollectionKind.TripleTriad && entry.Card is { IsValid: true })
                .OrderBy(entry => entry.Card!.Extra).ThenBy(entry => entry.Card!.Number).ThenBy(entry => entry.Id).ToArray();
        }
        if (ImGui.BeginTabBar("TriadNotebook"))
        {
            try
            {
                var notebookFlags = focusTriadNotebook ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None; focusTriadNotebook = false;
                if (ImGui.BeginTabItem(localization.Get("TriadUi.Notebook"), notebookFlags))
                {
                    try { DrawTriadNotebook(state); }
                    finally { ImGui.EndTabItem(); }
                }
                if (ImGui.BeginTabItem(localization.Get("TriadUi.Deck"), TriadTabFlags("Deck")))
                {
                    try { DrawTriadSavedDecks(); DrawTriadDeck(); }
                    finally { ImGui.EndTabItem(); }
                }
                if (ImGui.BeginTabItem(localization.Get("TriadUi.Simulation")))
                {
                    try { DrawTriadSimulation(); }
                    finally { ImGui.EndTabItem(); }
                }
                if (ImGui.BeginTabItem(localization.Get("TriadNpcs.Tab"), TriadTabFlags("Npcs")))
                {
                    try { DrawTriadNpcs(); }
                    finally { ImGui.EndTabItem(); }
                }
                triadFocusTab = null;
                if (ImGui.BeginTabItem(localization.Get("TriadUi.Observation")))
                {
                    try { DrawTriadObservation(); }
                    finally { ImGui.EndTabItem(); }
                }
            }
            finally { ImGui.EndTabBar(); }
        }
        if (state.CharacterId == 0) ImGui.TextDisabled(localization.Get("TriadUi.Login"));
        ImGui.TextWrapped(localization.Get("TriadUi.Help"));
    }

    private void DrawTriadNotebook(CollectionState state)
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##TriadSearch", localization.Get("TriadUi.Query"), ref triadQuery, 128); Tip("TriadUi.QueryTip");
        ImGui.SetNextItemWidth(150);
        ImGui.Combo(localization.Get("TriadUi.Rarity"), ref triadStars, new[] { localization.Get("TriadUi.All"), localization.Get("TriadUi.One"), localization.Get("TriadUi.Two"), localization.Get("TriadUi.Three"), localization.Get("TriadUi.Four"), localization.Get("TriadUi.Five") }, 6); Tip("TriadUi.RarityTip");
        ImGui.SameLine(); ImGui.SetNextItemWidth(170);
        ImGui.Combo(localization.Get("TriadUi.Acquisition"), ref triadAcquisition, new[] { localization.Get("TriadUi.All"), localization.Get("TriadUi.Confirmed"), localization.Get("TriadUi.Unconfirmed") }, 3); Tip("TriadUi.AcquisitionTip");
        ImGui.SetNextItemWidth(180);
        var typeName = triadType is { } type ? triadCards.FirstOrDefault(entry => entry.Card!.TypeId == type)?.Card?.TypeName : null;
        if (ImGui.BeginCombo(localization.Get("TriadUi.Type"), triadType is null ? localization.Get("TriadUi.AllTypes") : string.IsNullOrWhiteSpace(typeName) ? localization.Get("TriadUi.NoType") : typeName))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("TriadUi.AllTypes"), triadType is null)) triadType = null;
                foreach (var card in triadCards.Select(entry => entry.Card!).DistinctBy(card => card.TypeId).OrderBy(card => card.TypeId))
                    if (ImGui.Selectable($"{(string.IsNullOrWhiteSpace(card.TypeName) ? localization.Get("TriadUi.NoType") : card.TypeName)}##{card.TypeId}", triadType == card.TypeId)) triadType = card.TypeId;
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("TriadUi.TypeTip");
        DrawTriadSortControls();
        if (ImGui.BeginTabBar("TriadSeries"))
        {
            try
            {
                var focus = focusTriadSeries; focusTriadSeries = false;
                if (ImGui.BeginTabItem(localization.Get("TriadSort.SeriesNormal"), focus && !triadExSeries ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None)) { if (!focus) triadExSeries = false; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("TriadSort.SeriesEx"), focus && triadExSeries ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None)) { if (!focus) triadExSeries = true; ImGui.EndTabItem(); }
            }
            finally { ImGui.EndTabBar(); }
        }
        // 0.331 (audit): « Ex. 12 » typed on the ordinary tab moves to the Ex. tab (the series filter runs first). Only when the query
        // changes, so the ordinary tab stays reachable with the same query.
        if (!triadExSeries && triadQuery != triadPreviousQuery && triadQuery.Trim().StartsWith("Ex.", StringComparison.OrdinalIgnoreCase)) { triadExSeries = true; focusTriadSeries = true; }
        if (!ReferenceEquals(triadViewState, state) || triadQuery != triadPreviousQuery || triadStars != triadPreviousStars
            || triadAcquisition != triadPreviousAcquisition || triadType != triadPreviousType
            || triadSort != triadPreviousSort || triadSortDescending != triadPreviousDescending || triadScoreRules != triadPreviousScoreRules
            || triadExSeries != triadPreviousExSeries)
        {
            triadPreviousExSeries = triadExSeries;
            triadPreviousSort = triadSort; triadPreviousDescending = triadSortDescending; triadPreviousScoreRules = triadScoreRules;
            triadViewState = state;
            triadPreviousQuery = triadQuery; triadPreviousStars = triadStars;
            triadPreviousAcquisition = triadAcquisition; triadPreviousType = triadType;
            var query = triadQuery.Trim();
            triadMatches = triadCards.Where(entry => entry.Card!.Extra == triadExSeries && (triadStars == 0 || entry.Card!.Stars == triadStars)
                && (triadType is null || entry.Card!.TypeId == triadType)
                && (triadAcquisition == 0 || (triadAcquisition == 1 ? entry.ConfirmedUnlocked : !entry.ConfirmedUnlocked))
                && (query.Length == 0 || entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || entry.Card!.Number.ToString() == query || entry.Card!.Label.Equals(query, StringComparison.OrdinalIgnoreCase))).ToArray();
            // Sort key, then the card number so equal keys stay in notebook order.
            var sortBy = (MiniGames.TriadCardSort)Math.Clamp(triadSort, 0, Enum.GetValues<MiniGames.TriadCardSort>().Length - 1);
            var keyed = triadMatches.Select(entry => (Entry: entry, Key: MiniGames.TriadCardScore.Key(entry.Card!, sortBy, triadScoreRules)));
            triadMatches = (triadSortDescending ? keyed.OrderByDescending(row => row.Key) : keyed.OrderBy(row => row.Key))
                .ThenBy(row => row.Entry.Card!.Extra).ThenBy(row => row.Entry.Card!.Number).Select(row => row.Entry).ToArray();
            if (!triadMatches.Any(entry => entry.Id == selectedTriad)) selectedTriad = triadMatches.FirstOrDefault()?.Id ?? 0;
        }
        ImGui.TextColored(Accent,localization.Format("TriadCounts.Cards",triadMatches.Length,triadCards.Count(entry=>entry.ConfirmedUnlocked),triadCards.Length));
        if (!ImGui.BeginTable("TriadCards", 2, ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("TriadUi.List"), ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableSetupColumn(localization.Get("TriadUi.Details"), ImGuiTableColumnFlags.WidthStretch, 1.4f);
            ImGui.TableNextRow(); ImGui.TableNextColumn();
            var height = Math.Max(160, ImGui.GetContentRegionAvail().Y - 90);
            var visible = ImGui.BeginChild("TriadCardList", new Vector2(0, height), true);
            try
            {
                if (visible)
                {
                    var clipper = ImGui.ImGuiListClipper();
                    try
                    {
                        var rowHeight = 36 + ImGui.GetStyle().ItemSpacing.Y;
                        // A card opened from a menu is scrolled into view (the clipped list does not draw it otherwise).
                        if (triadScrollToSelected)
                        {
                            triadScrollToSelected = false;
                            var at = Array.FindIndex(triadMatches, entry => entry.Id == selectedTriad);
                            if (at >= 0) ImGui.SetScrollY(Math.Max(0, at * rowHeight - ImGui.GetWindowHeight() / 3));
                        }
                        clipper.Begin(triadMatches.Length, rowHeight);
                        while (clipper.Step())
                        for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                        {
                            var entry = triadMatches[index];
                            ImGui.PushID((int)entry.Id);
                            try
                            {
                                DrawGameIcon(entry.IconId,new Vector2(36));
                                var clicked = ImGui.IsItemClicked();
                                DrawEntryMark(entry);
                                TriadCardTooltip(entry.Id);
                                ImGui.SameLine();
                                if (ImGui.Selectable($"{entry.Card!.Label} · {entry.Name}\n{TriadSides(entry.Card!)}", selectedTriad == entry.Id, ImGuiSelectableFlags.None, new Vector2(0, 36)) || clicked)
                                    selectedTriad = entry.Id;
                                TriadCardTooltip(entry.Id);
                            }
                            finally { ImGui.PopID(); }
                        }
                    }
                    finally { clipper.End(); clipper.Destroy(); }
                    if (triadMatches.Length == 0) EmptyState("TriadUi.Empty");
                }
            }
            finally { ImGui.EndChild(); }
            ImGui.TableNextColumn();
            var detailsVisible = ImGui.BeginChild("TriadCardDetail", new Vector2(0, height), true);
            try
            {
                if (detailsVisible && triadCards.FirstOrDefault(entry => entry.Id == selectedTriad) is { } selected)
                    DrawTriadCardDetails(selected);
            }
            finally { ImGui.EndChild(); }
        }
        finally { ImGui.EndTable(); }
    }

    private void DrawTriadCardDetails(CollectionEntry entry)
    {
        var card = entry.Card!;
        DrawGameIcon(entry.IconId,new Vector2(80));
        TriadCardTooltip(entry.Id);
        ImGui.SameLine(); ImGui.TextColored(Accent, $"{card.Label} · {entry.Name}");
        ImGui.TextUnformatted(new string('★', card.Stars));
        ImGui.TextUnformatted(string.IsNullOrWhiteSpace(card.TypeName) ? localization.Get("TriadUi.NoType") : card.TypeName);
        ImGui.TextColored(entry.ConfirmedUnlocked ? Success : Muted, entry.ConfirmedUnlocked ? localization.Get("TriadUi.Acquired") : localization.Get("TriadUi.Unknown"));
        ImGui.Separator();
        DrawTriadValues(card);
        ImGui.TextWrapped(card.Description);
        ImGui.Spacing();
        var canAdd = triadDeck!.Count < 5 && !triadDeck.Contains(entry.Id);
        ImGui.BeginDisabled(!canAdd);
        try
        {
            if (ImGui.Button(localization.Get("TriadUi.Add"))) { triadDeck.Add(entry.Id); triadDeckSaved = false; }
        }
        finally { ImGui.EndDisabled(); }
        if (ImGui.SmallButton(localization.Get("TriadUi.Blue"))) AddTriadSimulationCard(entry.Id, false);
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("TriadUi.Red"))) AddTriadSimulationCard(entry.Id, true);
        foreach (var item in itemCatalog.State.Catalog?.ForTriadCard(card.Id) ?? [])
        {
            if (ImGui.Button(localization.Format("CompletionDetails.Item",item.Name)+$"###TriadItem{item.Id}"))
            { SelectItem(item.Id); page = Page.Items; focusStocksTab = true; }
        }
        if (ImGui.SmallButton(localization.Get("TriadUi.Copy")))
            ImGui.SetClipboardText(localization.Format("UiFragments.CardCopy",card.Label,entry.Name,card.Stars,TriadCardDefinition.Value(card.Top),TriadCardDefinition.Value(card.Right),TriadCardDefinition.Value(card.Bottom),TriadCardDefinition.Value(card.Left)));
    }

    private static void DrawTriadValues(TriadCardDefinition card)
    {
        if (!ImGui.BeginTable("CardSides", 3, ImGuiTableFlags.SizingFixedFit)) return;
        try
        {
            for (var column = 0; column < 3; column++) ImGui.TableSetupColumn($"Side{column}", ImGuiTableColumnFlags.WidthFixed, 38);
            for (var index = 0; index < 9; index++)
            {
                ImGui.TableNextColumn();
                var number = index switch { 1 => card.Top, 3 => card.Left, 5 => card.Right, 7 => card.Bottom, _ => 0 };
                if (number == 0) continue;
                var text = TriadCardDefinition.Value(number);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2));
                ImGui.TextColored(Accent, text);
            }
        }
        finally { ImGui.EndTable(); }
    }

    private void DrawTriadDeck()
    {
        ImGui.TextWrapped(localization.Get("TriadUi.DeckHelp"));
        var analysis = TriadDeck.Analyze(triadDeck!, id => triadCards.FirstOrDefault(entry => entry.Id == id)?.Card);
        ImGui.TextColored(analysis.Valid ? Success : Accent, analysis.Valid ? localization.Get("TriadUi.Valid") : localization.Format("TriadCounts.Invalid",triadDeck!.Count));
        if (!analysis.RarityAllowed) ImGui.TextWrapped(localization.Get("TriadUi.Rare"));
        if (analysis.HasDuplicates) ImGui.TextWrapped(localization.Get("TriadUi.Duplicate"));
        if (analysis.UnknownIds.Count > 0) ImGui.TextWrapped(localization.Get("TriadUi.Missing"));
        var remove = -1;
        for (var index = 0; index < triadDeck!.Count; index++)
        {
            var id = triadDeck[index];
            var entry = triadCards.FirstOrDefault(entry => entry.Id == id);
            ImGui.PushID(index);
            try
            {
                DrawGameIcon(entry?.IconId ?? 0,new Vector2(44));
                TriadCardTooltip(id);
                ImGui.SameLine();
                ImGui.TextUnformatted(entry is null ? localization.Format("UiFragments.CardUnavailable",id) : $"{entry.Card!.Label} · {entry.Name} · {new string('★', entry.Card.Stars)}");
                ImGui.SameLine();
                if (DeleteButton("TriadUi.Remove")) remove = index;
                ImGui.TextColored(entry?.ConfirmedUnlocked == true ? Success : Muted, entry?.ConfirmedUnlocked == true ? localization.Get("TriadUi.Acquired") : localization.Get("TriadUi.Unknown"));
            }
            finally { ImGui.PopID(); }
        }
        if (remove >= 0) { triadDeck.RemoveAt(remove); triadDeckSaved = false; }
        if (ImGui.Button(localization.Get("TriadUi.Save")))
        {
            var previous = configuration.TripleTriadDeck;
            configuration.TripleTriadDeck = triadDeck.ToList();
            try { save(); triadDeckError = null; triadDeckSaved = true; }
            catch (Exception error)
            {
                configuration.TripleTriadDeck = previous;
                triadDeckSaved = false; triadDeckError = "Common.SaveError";
                log.Warning(error, "Tensho Triad deck preparation could not be saved.");
            }
        }
        if (triadDeckSaved) ImGui.TextColored(Success, localization.Get("TriadUi.Saved"));
        if (triadDeckError is { } errorText) ImGui.TextWrapped(localization.Get(errorText));
    }
}
