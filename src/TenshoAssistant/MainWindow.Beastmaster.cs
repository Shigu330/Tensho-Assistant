using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Beastmaster;

namespace TenshoAssistant;

// Progression › Beastmaster (0.500, user request): the beasts, with their attributes at a chosen rank, and the boards of the Beast Labyrinth with
// their enemies and bonuses. Reads only. 0.980 (X-109, DEC-046): a team tab, prepared out of combat, following plain rules said on the page
// (docs/evidence/0.980-beast-teams.md).
internal sealed partial class MainWindow
{
    internal BeastService? Beasts { get; set; }
    private string beastQuery = string.Empty;
    private string? beastFamily;
    private string? beastTabFocus;
    private bool beastOnlyObtained;
    private int beastRank = 1;
    private int beastBoard;
    private (BeastSort Sort, bool Descending) beastSort = (BeastSort.Name, false);
    private (object? Catalog, object? Obtained, string Query, string? Family, bool Only, int Rank, BeastSort Sort, bool Descending) beastViewKey;
    private IReadOnlyList<Beast> beastView = [];
    // X-745 (0.1767): the « To capture » tab: only the beasts not captured yet (on by default), its rows worked out when something changes.
    private bool beastCaptureMissing = true;
    private (object? Catalog, object? Obtained, bool Missing) beastCaptureKey;
    private IReadOnlyList<Beast> beastCaptureRows = [];

    private void DrawBeastmaster()
    {
        var state = Beasts?.State;
        if (state?.Error is not null) { ImGui.TextWrapped(localization.Get("BeastUi.Error")); return; }
        if (state?.Catalog is not { } catalog) { ImGui.TextDisabled(LoadingText("BeastUi.Loading")); return; }
        DetailTabs("BeastTabs",
        [
            ("Beasts", localization.Get("BeastUi.Beasts"), () => DrawBeastList(state, catalog)),
            ("Capture", localization.Get("BeastUi.Capture"), () => DrawBeastCapture(state, catalog)),
            ("Boards", localization.Get("BeastUi.Boards"), () => DrawBeastBoards(catalog)),
            ("Team", localization.Get("BeastUi.Team"), () => DrawBeastTeam(state, catalog)),
        ], ref beastTabFocus);
    }

    // 0.804 (X-39, user request): where to tame what is missing, nearby; the wiki's bestiary read on a click only.
    private void DrawBeastNearby()
    {
        if (BeastWiki is not { } wiki) return;
        wiki.LoadCached();
        BoolSetting("MapView.ShowBeastMonsters", "BeastUi.NearbyTip", () => configuration.MapView.ShowBeastMonsters, value => configuration.MapView.ShowBeastMonsters = value, c => c.MapView.ShowBeastMonsters);
        if (!configuration.MapView.ShowBeastMonsters) return;
        ImGui.Indent();
        try
        {
            // X-76 step 3 (0.953): every overhead icon needs « Icons above nearby monsters » (DrawMonsterOverheadIcons): greyed while it is off.
            ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);
            try { BoolSetting("MapView.AboveBeast", "BeastUi.AboveTip", () => configuration.MapView.AboveBeast, value => configuration.MapView.AboveBeast = value, c => c.MapView.AboveBeast); }
            finally { ImGui.EndDisabled(); }
            BoolSetting("MapView.BeastMonstersAlways", "BeastUi.AlwaysTip", () => configuration.MapView.BeastMonstersAlways, value => configuration.MapView.BeastMonstersAlways = value, c => c.MapView.BeastMonstersAlways);
            if (wiki.Running) ImGui.TextDisabled(localization.Get("BeastUi.WikiReading"));
            else
            {
                ImGui.TextDisabled(wiki.Book is { } book ? localization.Format("BeastUi.WikiKnown", book.Rows.Count, book.FetchedAt.ToLocalTime().ToString("g")) : localization.Get("BeastUi.WikiNone"));
                ImGui.SameLine();
                if (SourceButton(localization.Get(wiki.Book is null ? "BeastUi.WikiFetch" : "BeastUi.WikiRefresh"), SourceOn(SourceUse.WikiBeastmaster), true)) wiki.Fetch();
                Tip("BeastUi.WikiFetchTip");
                if (wiki.Error == "disabled") ImGui.TextDisabled(localization.Get("Advanced.SourceOffTip"));
                else if (wiki.Error is not null) ImGui.TextColored(UI.FfxivTheme.Warning, localization.Get("BeastUi.WikiFailed"));
            }
        }
        finally { ImGui.Unindent(); }
    }

    private void DrawBeastList(BeastState state, BeastCatalog catalog)
    {
        DrawBeastNearby();
        var obtained = state.Obtained;
        if (obtained is null) ImGui.TextDisabled(localization.Get(state.ObtainedUnavailable ? "BeastUi.ObtainedFailed" : "BeastUi.ObtainedUnknown"));
        else ImGui.TextDisabled(localization.Format("BeastUi.ObtainedCount", obtained.Count, catalog.Beasts.Count));
        // X-764 M3 (review of 0.1751): the effects' columns no longer hold flags (a game update): said once here, no beast shows « none ».
        if (catalog.EffectsUnavailable) ImGui.TextDisabled(localization.Get("BeastUi.EffectsUnavailable"));
        ImGui.SetNextItemWidth(200); ImGui.InputTextWithHint("##BeastQuery", localization.Get("BeastUi.Search"), ref beastQuery, 64); Tip("BeastUi.SearchTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        if (ImGui.BeginCombo("##BeastFamily", beastFamily ?? localization.Get("BeastUi.AllFamilies")))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("BeastUi.AllFamilies"), beastFamily is null)) beastFamily = null;
                foreach (var family in catalog.Beasts.Select(beast => beast.Family).Where(name => name.Length > 0).Distinct().Order())
                {
                    // X-746 (0.1766): each family with the action Borrow gives with it (the game's name).
                    var borrowed = catalog.Beasts.FirstOrDefault(beast => beast.Family == family) is { } member ? catalog.BorrowOf(member) : null;
                    if (ImGui.Selectable(borrowed is null ? family : $"{family} · {borrowed.Name}##{family}", beastFamily == family)) beastFamily = family;
                }
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("BeastUi.FamilyTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(90); if (ImGui.InputInt(localization.Get("BeastUi.Rank") + "##BeastRank", ref beastRank)) beastRank = Math.Clamp(beastRank, 1, Math.Max(1, catalog.Beasts.Max(beast => beast.Ranks.Count))); Tip("BeastUi.RankTip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("BeastUi.OnlyObtained"), ref beastOnlyObtained); Tip("BeastUi.OnlyObtainedTip");
        // X-746 (0.1766): the family chosen: the action Borrow gives with its beasts, its icon, name and effect (the game's texts).
        if (beastFamily is not null && catalog.Beasts.FirstOrDefault(beast => beast.Family == beastFamily) is { } chosen && catalog.BorrowOf(chosen) is { } borrow)
            DrawBeastBorrowLine(catalog, borrow);
        var key = ((object?)catalog, (object?)obtained, beastQuery, beastFamily, beastOnlyObtained, beastRank, beastSort.Sort, beastSort.Descending);
        if (key != beastViewKey) { beastViewKey = key; beastView = catalog.View(beastQuery, beastFamily, beastOnlyObtained, obtained, beastRank, beastSort.Sort, beastSort.Descending); }
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable("BeastTable", 9, flags, new Vector2(0, Math.Max(200, ImGui.GetContentRegionAvail().Y)))) return;
        try
        {
            foreach (var title in new[] { "BeastUi.Name", "BeastUi.Family", "BeastUi.Auto", "BeastUi.Strength", "BeastUi.Intelligence", "BeastUi.PhysicalResistance", "BeastUi.MagicalResistance", "BeastUi.Vitality" })
                ImGui.TableSetupColumn(localization.Get(title), title == "BeastUi.Name" ? ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch : ImGuiTableColumnFlags.None);
            ImGui.TableSetupColumn(localization.Get("BeastUi.CatchPlace"), ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();
            var specs = ImGui.TableGetSortSpecs();
            if (!specs.IsNull && specs.SpecsDirty && specs.SpecsCount > 0)
            {
                beastSort = ((BeastSort)specs.Specs.ColumnIndex, specs.Specs.SortDirection == ImGuiSortDirection.Descending);
                specs.SpecsDirty = false;
            }
            foreach (var beast in beastView)
            {
                ImGui.TableNextRow(); ImGui.TableNextColumn();
                DrawSmallGameIcon(beast.Icon); ImGui.SameLine();
                var known = obtained?.Contains(beast.Id);
                DrawStateMark(known); ImGui.SameLine();
                ImGui.TextColored(known == true ? Success : known == false ? Muted : Accent, beast.Name);
                if (ImGui.IsItemHovered()) WrappedTooltip(localization.Format("BeastUi.Tooltip", beast.Id, beast.Assault, beast.Volley));
                // X-135 (0.1163): the beast's rank when read (team window or board result).
                if (BeastRanks?.TryGetValue(beast.Id, out var rankKnown) == true) { ImGui.SameLine(); ImGui.TextDisabled(localization.Format("BeastUi.KnownRank", rankKnown)); }
                ImGui.TableNextColumn(); ImGui.TextUnformatted(beast.Family);
                DrawBeastBorrowIcon(catalog, beast);
                ImGui.TableNextColumn(); ImGui.TextUnformatted(beast.AutoAttack.Length > 0 ? beast.AutoAttack : localization.Get("BeastUi.Magical"));
                DrawBeastEffects(catalog, beast);
                var stats = beast.At(beastRank);
                foreach (var value in new[] { stats?.Strength, stats?.Intelligence, stats?.PhysicalResistance, stats?.MagicalResistance, stats?.Vitality })
                { ImGui.TableNextColumn(); ImGui.TextUnformatted(value?.ToString() ?? "?"); }
                ImGui.TableNextColumn(); ImGui.TextUnformatted(beast.CatchPlace);
                // X-110 (0.984, user request: where to tame each beast, exact place): places seen in game, the common place menu on them (X-111).
                if (BeastPlacesOf(beast.Id) is { Count: > 0 } places)
                {
                    ImGui.SameLine(); ImGui.TextColored(Accent, localization.Format("BeastUi.PlacesSeen", places.Count));
                    Tip("BeastUi.PlacesSeenTip");
                    NpcMenu($"BeastPlaces{beast.Id}", beast.Name, 0, places);
                }
            }
        }
        finally { ImGui.EndTable(); }
    }

    private Board DrawBeastBoardChoice(BeastCatalog catalog)
    {
        beastBoard = Math.Clamp(beastBoard, 0, catalog.Boards.Count - 1);
        var board = catalog.Boards[beastBoard];
        ImGui.SetNextItemWidth(320);
        if (ImGui.BeginCombo("##BeastBoard", board.Name))
        {
            try { for (var index = 0; index < catalog.Boards.Count; index++) if (ImGui.Selectable(catalog.Boards[index].Name + $"##Board{index}", index == beastBoard)) beastBoard = index; }
            finally { ImGui.EndCombo(); }
        }
        Tip("BeastUi.BoardTip");
        return board;
    }

    // X-109 (0.980, DEC-046): the 10 beasts for a board and the 3 of each fight, prepared out of combat, kept on disk (BeastTeams.json).
    internal Func<string>? BeastTeamsFile { get; set; }
    private BeastTeams.SavedPlans? beastPlans;
    private bool beastPlansLoaded;
    // X-587 (external audit A03 of 18/11): the file read before any write; one that could not be read is never written over.
    private readonly BeastPlansFile beastFile = new();
    private bool beastBlockedSaid;
    private Task<BoardPlan>? beastPlanTask;
    private (uint Board, string Key)? beastPlanFor;
    /// <summary>X-135 (0.1163): read once, before the first draw or the first ranks read (before, ranks read before the page was opened were lost).</summary>
    private BeastTeams.SavedPlans LoadBeastPlans()
    {
        // X-587 (external audit A03 of 18/11): held by another program, the file is read again (every 10 s) and nothing is saved over it
        // meanwhile; damaged, it is set aside under a dated name first (BeastPlansFile).
        if (!beastPlansLoaded || beastFile.Blocked)
        {
            beastPlansLoaded = true;
            try
            {
                // X-617 (review of block 0.1551 to 0.1554, I1): what was read while the file was held is joined to it once read, then written
                // (before, the file replaced it: a board's result read meanwhile was lost without a word).
                if (BeastTeamsFile?.Invoke() is { } file)
                {
                    var (plans, save) = beastFile.Load(file, beastPlans, DateTimeOffset.UtcNow,
                        (aside, error) => log.Warning(error, "Beast team plans unreadable; the file is set aside as {0}.", Path.GetFileName(aside)));
                    beastPlans = plans;
                    if (save) SaveBeastPlans();
                }
                if (beastFile.Blocked && !beastBlockedSaid)
                {
                    beastBlockedSaid = true;
                    log.Warning("Beast team plans could not be read (held by another program?): nothing is saved over them; read again every 10 s.");
                }
            }
            catch (Exception error) { log.Warning(error, "Beast team plans unreadable."); }
            beastPlans ??= new([], []);
        }
        return beastPlans!;
    }
    /// <summary>X-284 M1: the plans' writes, off the game's thread (BeastTeams.SaveLater); flushed when the window is disposed.</summary>
    private BackgroundWriter? beastWriter;
    private void SaveBeastPlans()
    {
        // X-180 (0.1168): never in the desktop application (its copy is overwritten from the game; the plans stay in memory there).
        if (inDesktopApp) return;
        // X-587 (A03): never before the file was read, nor over a file that could not be read (BeastPlansFile.MayWrite).
        if (!beastFile.MayWrite) return;
        try
        {
            if (beastPlans is not null && BeastTeamsFile?.Invoke() is { } file)
                BeastTeams.SaveLater(beastWriter ??= new BackgroundWriter((error, message) => { log.Warning(error, message); NoteWriteFailed(message); }), file, beastPlans);
        }
        catch (Exception error) { log.Warning(error, "Beast team plans could not be saved."); }
    }
    /// <summary>X-284 M1: at unload, the plans' last write waited for (a few seconds at most), said when it did not end.</summary>
    private void FlushBeastPlans()
    {
        if (beastWriter is { } writer && !writer.Flush(TimeSpan.FromSeconds(3))) log.Warning("The beast team plans were still being written 3 s after the unload began.");
    }
    internal BoardPlan? BeastPlanFor(Board board, BeastState state)
    {
        LoadBeastPlans();
        var floors = LoadBeastPlans().Floors;
        // X-575 (0.1543): the key holds the mark of the rules and of the game's data: a plan kept from older rules is made again.
        var key = BeastTeams.Key(state.Obtained, BeastRanks, floors, state.Catalog is { } marked ? BeastMarkOf(marked) : "");
        if (beastPlanTask is { IsCompleted: true } done && beastPlanFor is { } wanted)
        {
            beastPlanTask = null;
            if (done.IsCompletedSuccessfully)
            {
                beastPlans!.Plans[wanted.Board] = done.Result; beastPlans.Keys[wanted.Board] = wanted.Key;
                SaveBeastPlans();
            }
        }
        if (beastPlans!.Keys.GetValueOrDefault(board.Id) == key && beastPlans.Plans.TryGetValue(board.Id, out var plan)) return plan;
        if (beastPlanTask is null && state.Catalog is { } catalog)
        {
            var held = state.Obtained; var ranks = BeastRanks;
            beastPlanFor = (board.Id, key);
            beastPlanTask = Task.Run(() => BeastTeams.Plan(board, catalog.Beasts, held, ranks, floors));
        }
        return beastPlans.Plans.GetValueOrDefault(board.Id);
    }
    private (BeastCatalog Catalog, string Mark)? beastMark;
    /// <summary>X-575 (0.1543): BeastTeams.Mark of the catalog, worked out once per catalog (R07).</summary>
    private string BeastMarkOf(BeastCatalog catalog)
    {
        if (beastMark is not { } kept || !ReferenceEquals(kept.Catalog, catalog)) beastMark = kept = (catalog, BeastTeams.Mark(catalog));
        return kept.Mark;
    }
    /// <summary>X-109 (0.981): each beast's rank read in the game's team window, kept on disk with the plans; null: unknown, the board's limit is assumed.</summary>
    internal IReadOnlyDictionary<uint, int>? BeastRanks => LoadBeastPlans().Ranks;
    /// <summary>X-109 (0.981): ranks read in the game's team window: merged with those known, saved when they change. X-549 (0.1522): a rank
    /// read at the limit of the board the game is on is only a floor (BeastTeams.MergeRanks).</summary>
    internal void BeastRanksRead(Dictionary<uint, int> ranks)
    {
        var plans = LoadBeastPlans();
        var limit = Beasts?.State.Catalog is { } catalog && CurrentBeastBoard(catalog).Board is { } board ? board.RankLimit : 0;
        var (merged, floors) = BeastTeams.MergeRanks(plans.Ranks, plans.Floors, ranks, limit);
        if (plans.Ranks is { } known && known.Count == merged.Count && merged.All(pair => known.TryGetValue(pair.Key, out var rank) && rank == pair.Value)
            && (plans.Floors ?? []).SetEquals(floors)) return;
        beastPlans = plans with { Ranks = merged, Floors = floors };
        SaveBeastPlans();
    }
    /// <summary>X-578 (0.1548): a page of the game's bestiary read: the beast's own rank, saved when something changes (BeastTeams.WithBook).</summary>
    internal void BeastBookRead(BeastTeams.BookRead read)
    {
        if (BeastTeams.WithBook(LoadBeastPlans(), read) is not { } plans) return;
        beastPlans = plans;
        SaveBeastPlans();
    }
    /// <summary>X-549 (0.1522): the board the game's board or team window is about (0: not read).</summary>
    internal Func<uint>? BeastBoardContent { get; set; }
    /// <summary>X-549 (0.1522): the board the game is on: the labyrinth's territory, else the game's board or team window (BeastTeams.Current).</summary>
    private (Board? Board, BoardSource Source) CurrentBeastBoard(BeastCatalog catalog)
        => BeastTeams.Current(catalog.Boards, BeastBoardOfTerritory?.Invoke(CurrentTerritory?.Invoke() ?? 0) ?? 0, BeastBoardContent?.Invoke() ?? 0);
    private uint beastBoardFollowed;
    /// <summary>X-549: the page's list follows the board the game is on, once each time that board changes (it stays free to choose another).</summary>
    private void FollowBeastBoard(BeastCatalog catalog)
    {
        var read = CurrentBeastBoard(catalog).Board?.Id ?? 0;
        if (read == beastBoardFollowed) return;
        beastBoardFollowed = read;
        for (var index = 0; index < catalog.Boards.Count; index++) if (catalog.Boards[index].Id == read) beastBoard = index;
    }
    /// <summary>X-135 (0.1163): the board's result read in the game's result window: kept as the last result, its « after » ranks over those known.</summary>
    internal void BeastResultRead(BoardResult result)
    {
        var plans = LoadBeastPlans();
        // X-590 A06 (0.1610): the same board read again with fuller numbers (bonus, total) replaces the first reading; the same numbers, or fewer, change nothing.
        if (BeastResults.Keep(plans.Result, result) is not { } kept) return;
        result = kept;
        // X-549 (0.1522): the result's ranks are the beasts' own (not brought down to the board's limit): no longer floors.
        var floors = plans.Floors is { } before ? before.Where(beast => result.Beasts.All(gain => gain.Beast != beast)).ToHashSet() : null;
        beastPlans = plans with { Result = result, Ranks = BeastResults.Merge(plans.Ranks, result), Floors = floors };
        SaveBeastPlans();
    }
    /// <summary>X-135 (0.1163): the last board's result: score, then each beast's rank and experience before and after.</summary>
    private void DrawBeastResult(BeastCatalog catalog)
    {
        if (LoadBeastPlans().Result is not { } result) { ImGui.TextDisabled(localization.Get("BeastUi.ResultNone")); return; }
        // X-481 (0.1470): the window's bonus and total, when they were read.
        var totals = result.Total is { } total ? " · " + localization.Format("BeastUi.ResultTotal", result.Bonus ?? 0, result.Bonuses ?? 0, total) : "";
        if (!TenshoAssistant.UI.UiComponents.Header(localization.Format("BeastUi.ResultTitle", result.At.ToLocalTime().ToString("g"), result.Score) + totals + "###BeastResult", ImGuiTreeNodeFlags.None)) return;
        DrawBeastResultLines(result);
        foreach (var gain in result.Beasts)
        {
            var beast = catalog.Beasts.FirstOrDefault(row => row.Id == gain.Beast);
            if (beast is not null) { DrawSmallGameIcon(beast.Icon); ImGui.SameLine(); }
            ImGui.TextUnformatted(localization.Format("BeastUi.ResultBeast", beast?.Name ?? $"#{gain.Beast}", gain.RankBefore, gain.ExpBefore, gain.RankAfter, gain.ExpAfter));
            if (gain.RankAfter > gain.RankBefore) { ImGui.SameLine(); ImGui.TextColored(Success, localization.Format("BeastUi.ResultRanks", gain.RankAfter - gain.RankBefore)); }
        }
    }
    /// <summary>X-550 (0.1526): the team the game's team window shows (null: not read).</summary>
    internal Func<IReadOnlyList<BeastTeams.TeamMember>?>? BeastTeamRead { get; set; }
    private (string Key, IReadOnlyList<BeastTeams.TeamFight> Fights)? beastTeamFights;
    /// <summary>X-550 (0.1526): the team taken in the game: beasts down or low on HP, then each fight's three among them, with the advised beasts
    /// missing and those taking their place. Worked out again only when the board, the plan or the team (HP included) changes.</summary>
    private void DrawBeastTeamRead(Board board, BoardPlan plan, IReadOnlyList<BeastTeams.TeamMember> team, BeastCatalog catalog)
    {
        Beast? Of(uint id) => catalog.Beasts.FirstOrDefault(beast => beast.Id == id);
        string Names(IEnumerable<uint> ids) => string.Join(", ", BeastTeams.BookOrder(ids).Select(id => Of(id)?.Name ?? $"#{id}"));
        var fights = BeastTeamFightsFor(board, plan, team, catalog);
        ImGui.TextColored(Accent, localization.Format("BeastUi.TeamRead", team.Count));
        // X-585 M7: in the bestiary's order, as the 10 of the plan (X-580).
        foreach (var member in BeastTeams.BookOrder(team))
        {
            if (member.Down) ImGui.TextColored(Error, localization.Format("BeastUi.TeamReadDown", Of(member.Beast)?.Name ?? $"#{member.Beast}"));
            else if (member.Low) ImGui.TextColored(Warning, localization.Format("BeastUi.TeamReadLow", Of(member.Beast)?.Name ?? $"#{member.Beast}", member.Hp, member.MaxHp));
            // X-575 part 5 (0.1545): the rank the team window shows, below the board's recommended range.
            if (BeastTeams.RankLow(board, member.Rank))
                ImGui.TextColored(Warning, localization.Format(BeastTeams.RankLowKey(board, true), Of(member.Beast)?.Name ?? $"#{member.Beast}", member.Rank, board.RankRecommended, board.RankLimit));
        }
        foreach (var fight in fights)
        {
            var battle = board.Battles[fight.Battle];
            ImGui.TextColored(Accent, battle.Guardian ? localization.Get("BeastUi.Guardian") : localization.Format("BeastUi.TeamFight", fight.Battle));
            ImGui.SameLine(); ImGui.TextDisabled(localization.Format(BeastTeams.HitKey(fight.Enemies), fight.Hit, fight.Enemies));
            ImGui.Indent();
            try
            {
                DrawBeastCallOrder(battle, fight.Trio, catalog);
                if (fight.Lost.Count > 0) ImGui.TextColored(Warning, localization.Format("BeastUi.TeamReadLost", Names(fight.Lost), fight.Instead.Count > 0 ? Names(fight.Instead) : "—"));
            }
            finally { ImGui.Unindent(); }
        }
    }

    /// <summary>X-550 (0.1526): each fight's three among the team taken, worked out again only when the board, the plan or the team (HP included)
    /// changes (X-486: shared by the fight shown and the whole board).</summary>
    private IReadOnlyList<BeastTeams.TeamFight> BeastTeamFightsFor(Board board, BoardPlan plan, IReadOnlyList<BeastTeams.TeamMember> team, BeastCatalog catalog)
    {
        var key = $"{board.Id}|{string.Join(",", plan.Battles.SelectMany(battle => battle.Team).Select(pick => pick.Beast))}|{string.Join(",", team.Select(member => $"{member.Beast}:{member.Rank}:{member.Hp}/{member.MaxHp}"))}";
        if (beastTeamFights?.Key != key) beastTeamFights = (key, BeastTeams.FromTeam(board, plan, team, catalog.Beasts));
        return beastTeamFights.Value.Fights;
    }

    /// <summary>X-481 (0.1525): the result's lines as the game's window shows them: the score's parts (a label, the value when it has one, the
    /// points), the score, each bonus's points, the bonus and the total. Labels from the game's own words (docs/evidence/0.1470-beast-result-window.md).</summary>
    private void DrawBeastResultLines(BoardResult result)
    {
        if (result.Parts is not { } parts) return;
        if (!TenshoAssistant.UI.UiComponents.BeginDataTable("BeastResultLines", 3)) return;
        try
        {
            void Line(string key, string value, int points, bool strong = false)
            {
                ImGui.TableNextRow(); ImGui.TableNextColumn();
                if (strong) ImGui.TextColored(Accent, localization.Get(key)); else ImGui.TextUnformatted(localization.Get(key));
                ImGui.TableNextColumn(); ImGui.TextDisabled(value);
                ImGui.TableNextColumn(); ImGui.TextUnformatted(points.ToString("N0", System.Globalization.CultureInfo.CurrentCulture));
            }
            Line("BeastUi.ResultPath", "", parts.Path);
            Line("BeastUi.ResultEnemies", "", parts.Enemies);
            Line("BeastUi.ResultElite", "", parts.Elite);
            Line("BeastUi.ResultBoss", "", parts.Boss);
            Line("BeastUi.ResultHp", parts.HpLeft?.ToString(System.Globalization.CultureInfo.CurrentCulture) ?? "", parts.HpPoints);
            Line("BeastUi.ResultScore", "", result.Score, true);
            if (result.BonusPoints is { } points)
                for (var index = 0; index < points.Count; index++) Line("BeastUi.ResultBonusOne", (index + 1).ToString(System.Globalization.CultureInfo.CurrentCulture), points[index]);
            if (result.Bonus is { } bonus) Line("BeastUi.ResultBonusLine", "", bonus, true);
            if (result.Total is { } total) Line("BeastUi.ResultTotalLine", "", total, true);
        }
        finally { ImGui.EndTable(); }
    }

    /// <summary>X-109 (0.981): the board of a labyrinth territory (its ContentFinderCondition); null outside.</summary>
    internal Func<uint, uint>? BeastBoardOfTerritory { get; set; }

    /// <summary>X-109 (0.981, DEC-047): the panel beside the game's board: in the labyrinth its own board, before entering the board chosen here.</summary>
    internal void DrawBeastBoardPanel()
    {
        var state = Beasts?.State;
        if (state?.Catalog is not { } catalog || catalog.Boards.Count == 0) { EmptyState(inDesktopApp ? "Desktop.ReadInGame":"BeastUi.Loading"); return; }
        // X-549 (0.1522): the board the game is on (the labyrinth's territory, else its board or team window), the list only when it tells
        // nothing. Before, the territory was read only during a cut, and the labyrinth is no longer cut (X-137): the list was always used.
        Board board;
        var (read, source) = CurrentBeastBoard(catalog);
        if (read is not null)
        {
            board = read;
            ImGui.TextColored(Accent, board.Name);
            // X-562 M6 (review of 0.1526): the windows' number (AgentXBM…ContentId) is not yet confirmed in game (R06): said so until then.
            ImGui.SameLine(); ImGui.TextDisabled(localization.Get(source == BoardSource.Window ? "BeastUi.BoardReadWindow" : "BeastUi.BoardRead"));
        }
        else if (TenshoAssistant.Competitive.CompetitiveZones.IsLabyrinth(CurrentTerritory?.Invoke() ?? 0)) { EmptyState("BeastUi.TeamBoardUnknown"); return; }
        else
        {
            FollowBeastBoard(catalog);
            board = DrawBeastBoardChoice(catalog);
            ImGui.TextDisabled(localization.Get("BeastUi.BoardChosen"));
        }
        if (BeastPlanFor(board, state) is not { } plan) { EmptyState("BeastUi.TeamComputing"); return; }
        // X-366 (0.1373): the panel sizes itself to its content, up to BeastBoardWindow.MaxWidth: every line wraps inside that width, never cut at the edge.
        var wrapAt = ImGui.GetCursorPosX() + BeastPanelWrap;
        var team = BeastTeamRead?.Invoke();
        // X-486 (0.1529, the user's request of 16/11): the fights the game's board window shows come first, one block each; the whole board
        // (the team taken, the advised plan) folded under « whole board ». The panel follows the window: it is read four times a second.
        var now = BeastTeams.FightsShown(board, BeastFightsShown?.Invoke());
        if (now.Count > 0)
        {
            var taken = team is { Count: > 0 } ? BeastTeamFightsFor(board, plan, team, catalog) : null;
            ImGui.PushTextWrapPos(wrapAt);
            try { foreach (var index in now) DrawBeastFightNow(board, index, plan, taken, catalog); }
            finally { ImGui.PopTextWrapPos(); }
            ImGui.Separator();
            if (!TenshoAssistant.UI.UiComponents.TreeNode(localization.Get("BeastUi.WholeBoard") + "###BeastWholeBoard")) return;
            try { DrawBeastBoardWhole(board, plan, team, catalog, state, wrapAt); }
            finally { ImGui.TreePop(); }
            return;
        }
        DrawBeastBoardWhole(board, plan, team, catalog, state, wrapAt);
    }

    /// <summary>X-136 (0.1530): an action's name in the game's language (empty: not known, the desktop application).</summary>
    internal Func<uint, string>? BeastActionName { get; set; }

    /// <summary>
    /// X-136 (0.1530): a beast's Trick affinity and its two intentional combos (« combo instinctif II »): the beastmaster's skill before the
    /// Trick, or after it, with the kind (BeastInstinct, from the game's help). Shown before the fight only; nothing is sent to the game (R03.3).
    /// </summary>
    private void DrawBeastCombo(uint beast)
    {
        var combos = BeastInstinct.Combos(beast);
        if (combos.Count < 2) return;
        string Skill(BeastInstinct.Combo combo) => BeastActionName?.Invoke(combo.Skill) is { Length: > 0 } name ? name : localization.Get("BeastUi.Affinity" + combo.SkillAffinity);
        var trick = BeastActionName?.Invoke(BeastInstinct.Trick(beast)) ?? "";
        ImGui.Indent();
        try
        {
            ImGui.TextDisabled(localization.Format("BeastUi.ComboLine", localization.Get("BeastUi.Affinity" + BeastInstinct.Of(beast)), trick.Length > 0 ? trick : "—",
                Skill(combos[0]), localization.Get("BeastUi.Combo" + combos[0].Kind), Skill(combos[1]), localization.Get("BeastUi.Combo" + combos[1].Kind)));
        }
        finally { ImGui.Unindent(); }
    }

    /// <summary>X-136 (0.1550): the hotbars read for the combo highlight (its own locator, apart from the gathering panel's).</summary>
    internal Gathering.ActionBarLocator? BeastBarLocator { get; set; }
    /// <summary>
    /// X-136 (0.1550): the beast called. X-655 (0.1599): read on the field first (the player's familiar), then in the game's « Cornes d'appel »
    /// window, which is closed during the fights (BeastCalled, BeastFamiliarReader).
    /// </summary>
    internal Func<Beastmaster.BeastCalledRead>? BeastCalledNow { get; set; }
    private string beastComboSaid = "";
    private (bool, bool, Beastmaster.BeastCalledRead, bool, object?) beastComboKey;

    /// <summary>
    /// X-136 (0.1550, the user's request of 11/11): with the option on, the beastmaster's two skills that make an intentional combo with the
    /// beast called (BeastInstinct.Combos: the one before its Trick and the one after) are highlighted on the hotbars, as the gathering panel's
    /// next action. X-655 (0.1599, R03.1 changed on 20/11, journal J174): everywhere in PvE, no longer in the labyrinth only (the trainer's
    /// skills work everywhere; PvP and ranked contents stay cut by CompetitiveGate); where it stops, or which bar and slot carry « 1 » and
    /// « 2 », is said in the log at each change (BeastComboTrace). Shows only: nothing is sent to the game (R03.3).
    /// </summary>
    internal void DrawBeastComboHighlight()
    {
        var option = configuration.HighlightBeastCombos;
        var located = BeastBarLocator;
        var called = option && located is not null ? BeastCalledNow?.Invoke() ?? Beastmaster.BeastCalledRead.None : Beastmaster.BeastCalledRead.None;
        var combos = called.Beast > 0 ? BeastInstinct.Combos(called.Beast) : [];
        if (combos is [var wantBefore, var wantAfter] && located is not null) located.Want(wantBefore.Skill, wantAfter.Skill);
        var read = combos.Count == 2 && located is not null && located.ReadFor == combos[0].Skill;
        // The line is made again only when what it says may have changed (the rectangles are a new list at each reading of the bars).
        var key = (option, located is not null, called, read, read ? (object?)located!.Rects : null);
        if (!key.Equals(beastComboKey))
        {
            beastComboKey = key;
            var trace = Beastmaster.BeastComboTrace.Line(option, located is not null, called, combos.Count == 2 ? combos[0].Skill : 0, combos.Count == 2 ? combos[1].Skill : 0, read,
                read ? located!.Rects.Select(rect => new Beastmaster.BeastComboSlot(rect.Bar, rect.Slot, rect.Second)).ToList() : []);
            if (trace != beastComboSaid) { beastComboSaid = trace; log.Information(trace); }
        }
        if (!read || located is null) return;
        DrawGatheringBarHighlight(located);
        // X-585 M6 (review of 0.1550): the order the combo needs (BeastInstinct.Combos: AfterTrick false, then true): « 1 » on the skill used
        // before the beast's Trick, « 2 » on the one after, in the slot's top-left corner (before, both were highlighted alike).
        var draw = ImGui.GetBackgroundDrawList();
        foreach (var rect in located.Rects)
        {
            var at = new Vector2(rect.X + 3, rect.Y + 1);
            var mark = BeastInstinct.Mark(rect.Second);
            draw.AddText(at + Vector2.One, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), mark);
            draw.AddText(at, ImGui.GetColorU32(UI.FfxivTheme.Gold), mark);
        }
    }


    private readonly Dictionary<uint, byte[]> beastFontIcons = [];
    private bool beastFontIconFailed;
    /// <summary>
    /// X-551 (0.1574): the game's own icon of a damage type or weakness (an icon of the game's font, named by the XBMElement sheet's text:
    /// BeastCatalog.ElementIcons), drawn by Dalamud's text renderer at the height of a line, the word following on the same line. Nothing is
    /// drawn when the element has no icon, in the desktop application (no game font) or once the renderer failed (said once in the log):
    /// the word alone stays.
    /// </summary>
    // X-755 (0.1750, the user's reading R-X-553): the effects a beast inflicts, after the text on its line: the game's icons in the game's order
    // (BeastEffects, as the game's « Formation de l'équipe »), each named on hover in the client's words; nothing for a beast that inflicts none.
    private void DrawBeastEffects(BeastCatalog catalog, Beast beast)
    {
        if (beast.Effects.Count == 0) return;
        var side = new Vector2(ImGui.GetTextLineHeight());
        foreach (var at in beast.Effects)
        {
            ImGui.SameLine();
            DrawGameIcon(BeastEffects.Order[at].Icon, side);
            if (at < catalog.EffectNames.Count && catalog.EffectNames[at].Length > 0) TipText(catalog.EffectNames[at]);
        }
    }

    // X-745 (0.1767, the user's idea I-027): the beasts to capture, those of the open world first, then those of duties: where the game says
    // they are caught, the places seen in game (a click shows the first on Tensho's map, a right click gives every place's flag, aetheryte
    // and map), their two techniques. A list only, nothing in combat.
    private void DrawBeastCapture(BeastState state, BeastCatalog catalog)
    {
        var obtained = state.Obtained;
        if (BeastCapture.Missing(catalog.Beasts, obtained) is { } missing) ImGui.TextDisabled(localization.Format("BeastUi.CaptureCount", missing));
        else ImGui.TextDisabled(localization.Get(state.ObtainedUnavailable ? "BeastUi.ObtainedFailed" : "BeastUi.ObtainedUnknown"));
        ImGui.Checkbox(localization.Get("BeastUi.CaptureMissing"), ref beastCaptureMissing); Tip("BeastUi.CaptureMissingTip");
        var key = ((object?)catalog, (object?)obtained, beastCaptureMissing);
        if (key != beastCaptureKey) { beastCaptureKey = key; beastCaptureRows = BeastCapture.Rows(catalog.Beasts, obtained, beastCaptureMissing); }
        if (beastCaptureRows.Count == 0) { EmptyState("BeastUi.CaptureNone"); return; }
        if (!TenshoAssistant.UI.UiComponents.BeginDataTable("BeastCaptureTable", 4, scrollHeight: Math.Max(200, ImGui.GetContentRegionAvail().Y))) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("BeastUi.Name"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(localization.Get("BeastUi.CatchPlace"));
            ImGui.TableSetupColumn(localization.Get("BeastUi.CaptureWhere"));
            ImGui.TableSetupColumn(localization.Get("BeastUi.CaptureTechniques"));
            ImGui.TableSetupScrollFreeze(0, 1);
            TenshoAssistant.UI.UiComponents.HeadersRow([localization.Get("BeastUi.Name"), localization.Get("BeastUi.CatchPlace"), localization.Get("BeastUi.CaptureWhere"), localization.Get("BeastUi.CaptureTechniques")], _ => false);
            var kind = -1;
            foreach (var beast in beastCaptureRows)
            {
                if (beast.CatchKind != kind)
                {
                    kind = beast.CatchKind;
                    ImGui.TableNextRow(); ImGui.TableNextColumn();
                    TenshoAssistant.UI.UiComponents.SubTitle(localization.Get(kind == BeastCapture.InZone ? "BeastUi.CaptureInZone" : "BeastUi.CaptureInDuty"));
                }
                ImGui.TableNextRow(); ImGui.TableNextColumn();
                ImGui.PushID((int)beast.Id);
                try
                {
                    DrawSmallGameIcon(beast.Icon);
                    var known = obtained?.Contains(beast.Id);
                    DrawStateMark(known); ImGui.SameLine();
                    ImGui.TextColored(known == true ? Success : known == false ? Muted : Accent, beast.Name);
                    ImGui.SameLine(); ImGui.TextDisabled(beast.Family);
                    DrawBeastBorrowIcon(catalog, beast);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(beast.CatchPlace);
                    ImGui.TableNextColumn(); DrawBeastCapturePlaces(beast);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(localization.Format("BeastUi.CaptureTechniquesLine", beast.Assault, beast.Volley));
                }
                finally { ImGui.PopID(); }
            }
        }
        finally { ImGui.EndTable(); }
    }

    // X-745 (0.1767): the first place seen (its zone and coordinates) as a link to Tensho's map, the others in the right-click menu.
    private void DrawBeastCapturePlaces(Beast beast)
    {
        if (beast.CatchKind != BeastCapture.InZone) { ImGui.TextDisabled(localization.Get("BeastUi.CaptureInDutyPlace")); return; }
        var places = BeastPlacesOf(beast.Id);
        if (places.Count == 0) { ImGui.TextDisabled(localization.Get("BeastUi.CaptureNotSeen")); Tip("BeastUi.CaptureNotSeenTip"); return; }
        var first = places[0];
        var text = NpcPlace(first) + (places.Count > 1 ? localization.Format("BeastUi.CaptureMore", places.Count - 1) : "");
        if (ImGui.Selectable(text) && maps.State.Catalog?.Find(first.MapId) is { } map)
        { ShowMapOnTenshoMap(map.MapId); mapCenterRequest = Maps.MapProjection.PixelFromCoordinate(new Vector2(first.X, first.Y), map); mapViewFollow = false; }
        Tip("BeastUi.CapturePlaceTip");
        NpcMenu($"BeastCapture{beast.Id}", beast.Name, 0, places);
    }

    // X-746 (0.1766): after a beast's family, the icon of the action Borrow gives with it, its name and effect on hover.
    private void DrawBeastBorrowIcon(BeastCatalog catalog, Beast beast)
    {
        if (catalog.BorrowOf(beast) is not { } borrow) return;
        ImGui.SameLine();
        DrawGameIcon(borrow.Icon, new Vector2(ImGui.GetTextLineHeight()));
        TipText(() => localization.Format("BeastUi.BorrowTip", BorrowLabel(catalog), borrow.Name, borrow.Effect));
    }

    // X-746 (0.1766): under the filter, the action Borrow gives with the family chosen.
    private void DrawBeastBorrowLine(BeastCatalog catalog, BeastBorrow borrow)
    {
        DrawGameIcon(borrow.Icon, new Vector2(ImGui.GetTextLineHeight() * 1.5f));
        ImGui.SameLine();
        ImGui.BeginGroup();
        try
        {
            ImGui.TextUnformatted(localization.Format("BeastUi.BorrowLine", BorrowLabel(catalog), borrow.Name));
            if (borrow.Effect.Length > 0) { ImGui.PushStyleColor(ImGuiCol.Text, Muted); try { ImGui.TextWrapped(borrow.Effect); } finally { ImGui.PopStyleColor(); } }
        }
        finally { ImGui.EndGroup(); }
    }

    private string BorrowLabel(BeastCatalog catalog) => catalog.BorrowLabel.Length > 0 ? catalog.BorrowLabel : localization.Get("BeastUi.Borrow");

    private void DrawBeastElementIcon(BeastCatalog catalog, string element)
    {
        if (inDesktopApp || beastFontIconFailed || element.Length == 0 || !catalog.ElementIcons.TryGetValue(element, out var icon)) return;
        var side = ImGui.GetTextLineHeight();
        var width = side;
        try
        {
            if (!beastFontIcons.TryGetValue(icon, out var text))
                beastFontIcons[icon] = text = new Dalamud.Game.Text.SeStringHandling.SeStringBuilder().AddIcon((Dalamud.Game.Text.SeStringHandling.BitmapFontIcon)icon).Build().Encode();
            // Drawn on the window's list at the cursor, without an item of its own (Dalamud's rule for a given draw list: the list, the place,
            // the font and its size are all given): the place is kept by the Dummy below.
            var how = new Dalamud.Interface.ImGuiSeStringRenderer.SeStringDrawParams { TargetDrawList = ImGui.GetWindowDrawList(), ScreenOffset = ImGui.GetCursorScreenPos(), Font = ImGui.GetFont(), FontSize = ImGui.GetFontSize(), WrapWidth = float.MaxValue };
            var drawn = Dalamud.Interface.Utility.ImGuiHelpers.SeStringWrapped(text, how);
            if (float.IsFinite(drawn.Size.X) && drawn.Size.X > 0) width = Math.Min(drawn.Size.X, side * 2);
        }
        catch (Exception error)
        {
            beastFontIconFailed = true;
            log.Warning(error, "Beast element icons unavailable; the words alone are shown.");
            return;
        }
        ImGui.Dummy(new Vector2(width, side));
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
    }

    /// <summary>X-551 (0.1534): the width the panel's lines wrap at (BeastBoardWindow sets it from the game's window beside it).</summary>
    internal float BeastPanelWrap { get; set; } = BeastBoardWindow.WrapWidth;

    /// <summary>X-486 (0.1529): the fights the game's board window shows (null: not read).</summary>
    internal Func<IReadOnlySet<uint>?>? BeastFightsShown { get; set; }

    /// <summary>X-486 (0.1529): the whole board as the panel showed it before: the team taken first (X-550), then the advised plan.</summary>
    private void DrawBeastBoardWhole(Board board, BoardPlan plan, IReadOnlyList<BeastTeams.TeamMember>? team, BeastCatalog catalog, BeastState state, float wrapAt)
    {
        // X-550 (0.1526): the team the player took (the game's team window) comes first: its three per fight, then the advised plan.
        if (team is { Count: > 0 })
        {
            ImGui.PushTextWrapPos(wrapAt);
            try { DrawBeastTeamRead(board, plan, team, catalog); }
            finally { ImGui.PopTextWrapPos(); }
            ImGui.Separator();
        }
        DrawBeastPlan(plan, board, catalog, state.Obtained is null, wrapAt);
        ImGui.PushTextWrapPos(wrapAt);
        try { ImGui.TextDisabled(localization.Get("BeastUi.TeamPanelNote")); }
        finally { ImGui.PopTextWrapPos(); }
    }

    /// <summary>
    /// X-486 (0.1529): one fight the game's board window shows: its enemies (icon, weakness, the game's stars) and their actions (damage, shape,
    /// target, from the sheets), then the three beasts for it, from the team taken when it is read (X-550), else the advised plan, each with why
    /// (the enemies whose weakness it hits). Nothing is chosen nor sent to the game (R03.3).
    /// </summary>
    private void DrawBeastFightNow(Board board, int index, BoardPlan plan, IReadOnlyList<BeastTeams.TeamFight>? taken, BeastCatalog catalog)
    {
        if (index >= board.Battles.Count) return;
        var fight = board.Battles[index];
        ImGui.TextColored(Accent, (fight.Guardian ? localization.Get("BeastUi.Guardian") : localization.Format("BeastUi.TeamFight", index)) + " · " + localization.Get("BeastUi.FightNow"));
        ImGui.Separator();
        // X-551 (0.1574): one block per enemy as the game's board window: its icon and name, then « Faiblesse » with the game's own icon of
        // the element before the word; its stars and actions under it, each action after the icon of its type of damage; a line between enemies.
        for (var number = 0; number < fight.Enemies.Count; number++)
        {
            var enemy = fight.Enemies[number];
            if (number > 0) ImGui.Separator();
            DrawSmallGameIcon(enemy.Icon); ImGui.SameLine();
            ImGui.TextUnformatted(enemy.Name);
            if (enemy.Weakness.Length > 0)
            {
                ImGui.Indent();
                try
                {
                    ImGui.TextDisabled(localization.Get("BeastUi.WeaknessLabel")); ImGui.SameLine();
                    DrawBeastElementIcon(catalog, enemy.Weakness);
                    ImGui.TextColored(Success, enemy.Weakness);
                }
                finally { ImGui.Unindent(); }
            }
            ImGui.Indent();
            try
            {
                if (enemy.Stars is { } stars) ImGui.TextDisabled(localization.Format("BeastUi.Stars", stars.Strength, stars.Intelligence, stars.PhysicalResistance, stars.MagicalResistance, stars.Vitality));
                foreach (var action in enemy.Actions)
                {
                    DrawBeastElementIcon(catalog, action.Damage);
                    ImGui.TextDisabled(BeastActionText(action));
                }
            }
            finally { ImGui.Unindent(); }
        }
        ImGui.Separator();
        var fromTeam = taken?.FirstOrDefault(row => row.Battle == index);
        var trio = fromTeam?.Trio ?? plan.Battles.FirstOrDefault(battle => battle.Battle == index)?.Team.Select(pick => pick.Beast).ToArray() ?? [];
        if (trio.Count == 0) return;
        ImGui.TextColored(Accent, localization.Get(fromTeam is not null ? "BeastUi.FightBeastsTeam" : "BeastUi.FightBeasts"));
        ImGui.Indent();
        try
        {
            for (var place = 0; place < trio.Count; place++)
            {
                var id = trio[place];
                var beast = catalog.Beasts.FirstOrDefault(row => row.Id == id);
                if (beast is not null) { DrawSmallGameIcon(beast.Icon); ImGui.SameLine(); DrawBeastElementIcon(catalog, beast.AutoAttack); }
                ImGui.TextUnformatted(BeastCallText(fight, place, id, catalog));
                if (beast is not null) DrawBeastEffects(catalog, beast);
                DrawBeastCombo(id);
            }
            if (fromTeam is { Lost.Count: > 0 } lost)
                ImGui.TextColored(Warning, localization.Format("BeastUi.TeamReadLost", string.Join(", ", BeastTeams.BookOrder(lost.Lost).Select(id => catalog.Beasts.FirstOrDefault(row => row.Id == id)?.Name ?? $"#{id}")),
                    lost.Instead.Count > 0 ? string.Join(", ", BeastTeams.BookOrder(lost.Instead).Select(id => catalog.Beasts.FirstOrDefault(row => row.Id == id)?.Name ?? $"#{id}")) : "—"));
        }
        finally { ImGui.Unindent(); }
    }

    /// <summary>X-575 part 4 (0.1544): a beast called for a fight: its place in the calling order, its name and why (the enemies whose weakness it
    /// hits, else its attack and defence). One text for the plan, the team taken and the fight shown. X-579 (0.1547): the strongest enemy
    /// it aims at is named first and said the strongest.</summary>
    private string BeastCallText(BoardBattle fight, int place, uint id, BeastCatalog catalog)
    {
        var beast = catalog.Beasts.FirstOrDefault(row => row.Id == id);
        var strongest = BeastTeams.Strongest(fight)?.Name;
        var hits = BeastTeams.Hits(fight, beast).Select(name => name == strongest ? localization.Format("BeastUi.Strongest", name) : name).ToArray();
        var name = beast?.Name ?? $"#{id}";
        return hits.Length > 0 ? localization.Format("BeastUi.FightReasonHits", place + 1, name, beast!.AutoAttack, string.Join(", ", hits))
            : localization.Format("BeastUi.FightReasonNone", place + 1, name);
    }

    /// <summary>X-486 (0.1559): an enemy's action as one line: name, type of damage, shape, target, then the effect it inflicts. X-622 M1 (review
    /// of 0.1560): « inflicts » only for a harmful status; a helpful one (a bonus, a barrier, Couvert) is « grants », one not known « effect ».</summary>
    private string BeastActionText(BoardAction action)
        => string.Join(" · ", new[] { action.Name, action.Damage, action.Shape, action.Target, action.Effect.Length == 0 ? ""
            : action.EffectCategory == BoardAction.Harmful ? localization.Format("BeastUi.ActionEffect", action.Effect)
            : localization.Format(action.EffectCategory == BoardAction.Helpful ? "BeastUi.ActionEffectGrants" : "BeastUi.ActionEffectOther", action.Effect) }.Where(part => part.Length > 0));

    /// <summary>X-575 part 4 (0.1544): the three of a fight in calling order, the best first (BeastTeams.Best orders them).</summary>
    private void DrawBeastCallOrder(BoardBattle fight, IReadOnlyList<uint> trio, BeastCatalog catalog)
    {
        if (trio.Count == 0) return;
        ImGui.TextDisabled(localization.Get("BeastUi.CallOrder"));
        for (var place = 0; place < trio.Count; place++) ImGui.TextUnformatted(BeastCallText(fight, place, trio[place], catalog));
    }

    private void DrawBeastTeam(BeastState state, BeastCatalog catalog)
    {
        if (catalog.Boards.Count == 0) { ImGui.TextDisabled(localization.Get("BeastUi.NoBoard")); return; }
        FollowBeastBoard(catalog);
        var board = DrawBeastBoardChoice(catalog);
        BoolSetting("BeastUi.BoardPanel", "BeastUi.BoardPanelTip", () => configuration.ShowBeastBoardPanel, value => configuration.ShowBeastBoardPanel = value, c => c.ShowBeastBoardPanel);
        BoolSetting("BeastUi.ComboHighlight", "BeastUi.ComboHighlightTip", () => configuration.HighlightBeastCombos, value => configuration.HighlightBeastCombos = value, c => c.HighlightBeastCombos);
        ImGui.TextWrapped(localization.Get("BeastUi.TeamRules"));
        DrawBeastResult(catalog);
        if (BeastPlanFor(board, state) is not { } plan) { EmptyState("BeastUi.TeamComputing"); return; }
        DrawBeastPlan(plan, board, catalog, state.Obtained is null);
    }

    /// <summary>X-366 (0.1373): every line wraps at wrapAt (a position in the window; 0: its right edge), the fights' lines indented as a block.</summary>
    internal void DrawBeastPlan(BoardPlan plan, Board board, BeastCatalog catalog, bool heldUnknown, float wrapAt = 0f)
    {
        ImGui.PushTextWrapPos(wrapAt);
        try { DrawBeastPlanLines(plan, board, catalog, heldUnknown); }
        finally { ImGui.PopTextWrapPos(); }
    }

    private void DrawBeastPlanLines(BoardPlan plan, Board board, BeastCatalog catalog, bool heldUnknown)
    {
        Beast? Of(uint id) => catalog.Beasts.FirstOrDefault(beast => beast.Id == id);
        if (heldUnknown) ImGui.TextColored(Warning, localization.Get("BeastUi.TeamHeldUnknown"));
        // X-549 (0.1522): said only for the team's beasts whose rank is not known (the others' do not change the plan).
        // X-562 M2: a plan kept before 0.1522 counts its beasts whose rank is assumed.
        if (BeastTeams.RanksUnknownOf(plan) is > 0 and var unknown) ImGui.TextDisabled(localization.Format("BeastUi.TeamRanksAssumed", unknown, board.RankLimit));
        ImGui.TextColored(Accent, localization.Format("BeastUi.TeamTen", plan.Team.Count));
        // X-551 (0.1534): one row per beast as the game's « Formation de l'équipe »: icon and name, rank, automatic attack.
        // X-580 (0.1549): in the order of the game's bestiary, its number before the name, to find each beast in the game's window.
        if (TenshoAssistant.UI.UiComponents.BeginDataTable("###BeastTeamRows", 3))
        {
            try
            {
                foreach (var pick in BeastTeams.BookOrder(plan.Team))
                {
                    if (Of(pick.Beast) is not { } beast) continue;
                    ImGui.TableNextRow(); ImGui.TableNextColumn();
                    DrawSmallGameIcon(beast.Icon); ImGui.SameLine(); ImGui.TextUnformatted(localization.Format("BeastUi.BookNumber", beast.Id, beast.Name));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(localization.Format("BeastUi.TeamRank", pick.Rank));
                    // X-575 part 5 (0.1545): a rank read below the board's recommended range is said; it never changes the choice.
                    if (BeastTeams.RankLow(board, pick.Rank, pick.RankKnown)) { ImGui.SameLine(); ImGui.TextColored(Warning, localization.Format(BeastTeams.RankLowKey(board, false), board.RankRecommended, board.RankLimit)); }
                    // X-551 (0.1574): the type of damage after the game's own icon of it (none for an unaspected magical attack).
                    ImGui.TableNextColumn(); DrawBeastElementIcon(catalog, beast.AutoAttack); ImGui.TextDisabled(beast.AutoAttack.Length > 0 ? beast.AutoAttack : localization.Get("BeastUi.Magical"));
                    DrawBeastEffects(catalog, beast);
                }
            }
            finally { ImGui.EndTable(); }
        }
        // X-575 (0.1543): a weakness of the board no fight's three hits, and why: no beast held hits it, or the places went to weaknesses of more enemies.
        foreach (var left in plan.Left ?? [])
            ImGui.TextColored(Warning, localization.Format(left.Reason == LeftReason.NoneHeld ? "BeastUi.LeftNoneHeld" : "BeastUi.LeftNoRoom", left.Name, plan.Team.Count));
        ImGui.Separator();
        foreach (var battle in plan.Battles)
        {
            if (battle.Battle >= board.Battles.Count) continue;
            var fight = board.Battles[battle.Battle];
            ImGui.TextColored(Accent, fight.Guardian ? localization.Get("BeastUi.Guardian") : localization.Format("BeastUi.TeamFight", battle.Battle));
            ImGui.SameLine(); ImGui.TextDisabled(localization.Format(BeastTeams.HitKey(battle.Enemies), battle.WeaknessesHit, battle.Enemies));
            ImGui.Indent();
            try
            {
                ImGui.TextDisabled(string.Join(", ", fight.Enemies.Select(enemy => enemy.Weakness.Length > 0 ? $"{enemy.Name} ({enemy.Weakness})" : enemy.Name)));
                DrawBeastCallOrder(fight, battle.Team.Select(pick => pick.Beast).ToArray(), catalog);
                // X-553 (0.1523): a weakness none of the three hits is said, with the beasts that would hit it. X-562 M3: worked out once per
                // plan and beasts held, not at each frame (R07).
                foreach (var miss in BeastMissed(plan, battle, fight, catalog))
                {
                    string Names(IEnumerable<uint> ids) => string.Join(", ", BeastTeams.BookOrder(ids).Take(5).Select(id => Of(id)?.Name ?? $"#{id}"));
                    // X-575 (0.1543): orange only when no beast brought hits the weakness; one of the team that could (its three places went
                    // to weaknesses of more enemies) is said in grey.
                    if (miss.InTeam.Count > 0) ImGui.TextDisabled(localization.Format("BeastUi.MissInTeam", miss.Name, Names(miss.InTeam)));
                    else ImGui.TextColored(Warning, miss.Held.Count > 0 ? localization.Format("BeastUi.MissNone", miss.Name, Names(miss.Held))
                        : localization.Format("BeastUi.MissNoneHeld", miss.Name));
                }
            }
            finally { ImGui.Unindent(); }
        }
    }

    private (BoardPlan Plan, IReadOnlySet<uint>? Held, Dictionary<int, IReadOnlyList<BeastTeams.WeaknessMissed>> Fights)? beastMissed;
    /// <summary>X-562 M3 (review of 0.1526): BeastTeams.Missed of a fight, kept until the plan or the beasts held change.</summary>
    private IReadOnlyList<BeastTeams.WeaknessMissed> BeastMissed(BoardPlan plan, BattlePlan battle, BoardBattle fight, BeastCatalog catalog)
    {
        var held = Beasts?.State.Obtained;
        if (beastMissed is not { } kept || !ReferenceEquals(kept.Plan, plan) || !ReferenceEquals(kept.Held, held)) beastMissed = kept = (plan, held, []);
        if (kept.Fights.TryGetValue(battle.Battle, out var known)) return known;
        var missed = BeastTeams.Missed(fight, battle.Team, plan.Team, catalog.Beasts, held);
        kept.Fights[battle.Battle] = missed;
        return missed;
    }

    /// <summary>
    /// X-110 (0.984): where a beast was seen tamed (BeastTamed with a place: yours or shared), then where the monster it is tamed from was seen
    /// (MonsterPlace; the monster from those tames, or from the wiki's « tamed from » enemy). Six places at most; worked out again when the base changes.
    /// </summary>
    private (int Wiki, object? Seen, int Revision)? beastPlacesFor;
    private Dictionary<uint, IReadOnlyList<Npcs.NpcLocation>> beastPlaces = [];
    private IReadOnlyList<Npcs.NpcLocation> BeastPlacesOf(uint beast)
    {
        if (DiscoveryStore is not { } store || !DiscoveriesLoaded || maps.State.Catalog is not { } catalog) return [];
        var key = (BeastWiki?.Revision ?? -1, (object?)Seen(), store.Revision);
        if (beastPlacesFor != key) { beastPlacesFor = key; beastPlaces = []; }
        if (beastPlaces.TryGetValue(beast, out var known)) return known;
        var places = new List<Npcs.NpcLocation>();
        void Add(Discoveries.DiscoveryFact fact)
        {
            if (fact.Map == 0 || places.Count >= 6 || catalog.Find(fact.Map) is not { } map) return;
            var at = Maps.MapProjection.CoordinateFromWorld(new Vector3(fact.X, 0, fact.Z), map);
            if (places.Any(place => place.MapId == fact.Map && Vector2.Distance(new Vector2(place.X, place.Y), at) < 1)) return;
            places.Add(new(fact.Territory, fact.Map, map.Zone, at.X, at.Y, fact.X, 0, fact.Z));
        }
        var tames = store.Find(Discoveries.DiscoveryKind.BeastTamed, beast, 8);
        foreach (var fact in tames) Add(fact);
        var monsters = new HashSet<uint>(tames.Select(fact => fact.Detail).Where(id => id != 0));
        if (BeastWiki?.Book is { } book && EnglishMonsterIds is { } english)
            foreach (var row in book.Rows.Where(row => row.Number == beast)) foreach (var id in english(row.Enemy.Trim())) monsters.Add(id);
        foreach (var monster in monsters.Take(4)) foreach (var fact in store.Find(Discoveries.DiscoveryKind.MonsterPlace, monster, 3)) Add(fact);
        if (beastPlaces.Count < 512) beastPlaces[beast] = places;
        return places;
    }

    private void DrawBeastBoards(BeastCatalog catalog)
    {
        if (catalog.Boards.Count == 0) { ImGui.TextDisabled(localization.Get("BeastUi.NoBoard")); return; }
        var board = DrawBeastBoardChoice(catalog);
        ImGui.TextUnformatted(localization.Format("BeastUi.BoardLine", board.Level, board.ItemLevel, board.TeamSize, board.RankLimit));
        ImGui.TextDisabled(localization.Get("BeastUi.WeaknessNote"));
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("BeastUi.Enemies"), ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach (var battle in board.Battles.OrderByDescending(battle => battle.Guardian))
            {
                if (battle.Guardian) ImGui.TextColored(Accent, localization.Get("BeastUi.Guardian"));
                else ImGui.Spacing();
                foreach (var enemy in battle.Enemies)
                {
                    DrawSmallGameIcon(enemy.Icon); ImGui.SameLine();
                    ImGui.TextUnformatted(enemy.Name);
                    if (enemy.Weakness.Length > 0) { ImGui.SameLine(); ImGui.TextColored(Success, localization.Format("BeastUi.Weakness", enemy.Weakness)); }
                    // X-109 (0.980): the game's stars of the enemy (same order as its board window).
                    if (enemy.Stars is { } stars) { ImGui.SameLine(); ImGui.TextDisabled(localization.Format("BeastUi.Stars", stars.Strength, stars.Intelligence, stars.PhysicalResistance, stars.MagicalResistance, stars.Vitality)); }
                    foreach (var action in enemy.Actions)
                        ImGui.TextDisabled("    " + BeastActionText(action));
                }
            }
        }
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("BeastUi.Bonuses")))
        {
            if (ImGui.BeginTable("BeastBonuses", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                try
                {
                    foreach (var bonus in board.Bonuses)
                    {
                        ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.TextUnformatted(bonus.Name);
                        ImGui.TableNextColumn(); ImGui.TextWrapped(bonus.Condition);
                        ImGui.TableNextColumn(); ImGui.TextUnformatted(bonus.Points.ToString());
                    }
                }
                finally { ImGui.EndTable(); }
            }
        }
    }
}
