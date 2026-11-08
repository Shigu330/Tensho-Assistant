using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Beastmaster;

namespace TenshoAssistant;

// Progression › Beastmaster (0.500, user request): the beasts, with their attributes at a chosen rank, and the boards of the Beast Labyrinth with
// their enemies and bonuses. Reads only. No team advice yet: what makes a beast better against an enemy is not established
// (docs/evidence/0.500-xbm-columns.md).
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

    private void DrawBeastmaster()
    {
        Heading(localization.Get("BeastUi.Title"), localization.Get("BeastUi.Help"));
        var state = Beasts?.State;
        if (state?.Error is not null) { ImGui.TextWrapped(localization.Get("BeastUi.Error")); return; }
        if (state?.Catalog is not { } catalog) { ImGui.TextDisabled(localization.Get("BeastUi.Loading")); return; }
        DetailTabs("BeastTabs",
        [
            ("Beasts", localization.Get("BeastUi.Beasts"), () => DrawBeastList(state, catalog)),
            ("Boards", localization.Get("BeastUi.Boards"), () => DrawBeastBoards(catalog)),
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
                if (SourceButton(localization.Get(wiki.Book is null ? "BeastUi.WikiFetch" : "BeastUi.WikiRefresh"), configuration.CommunityWiki, true)) wiki.Fetch();
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
        ImGui.SetNextItemWidth(200); ImGui.InputTextWithHint("##BeastQuery", localization.Get("BeastUi.Search"), ref beastQuery, 64); Tip("BeastUi.SearchTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        if (ImGui.BeginCombo("##BeastFamily", beastFamily ?? localization.Get("BeastUi.AllFamilies")))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("BeastUi.AllFamilies"), beastFamily is null)) beastFamily = null;
                foreach (var family in catalog.Beasts.Select(beast => beast.Family).Where(name => name.Length > 0).Distinct().Order())
                    if (ImGui.Selectable(family, beastFamily == family)) beastFamily = family;
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("BeastUi.FamilyTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(90); if (ImGui.InputInt(localization.Get("BeastUi.Rank") + "##BeastRank", ref beastRank)) beastRank = Math.Clamp(beastRank, 1, Math.Max(1, catalog.Beasts.Max(beast => beast.Ranks.Count))); Tip("BeastUi.RankTip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("BeastUi.OnlyObtained"), ref beastOnlyObtained); Tip("BeastUi.OnlyObtainedTip");
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
                ImGui.TableNextColumn(); ImGui.TextUnformatted(beast.Family);
                ImGui.TableNextColumn(); ImGui.TextUnformatted(beast.AutoAttack.Length > 0 ? beast.AutoAttack : localization.Get("BeastUi.Magical"));
                var stats = beast.At(beastRank);
                foreach (var value in new[] { stats?.Strength, stats?.Intelligence, stats?.PhysicalResistance, stats?.MagicalResistance, stats?.Vitality })
                { ImGui.TableNextColumn(); ImGui.TextUnformatted(value?.ToString() ?? "?"); }
                ImGui.TableNextColumn(); ImGui.TextUnformatted(beast.CatchPlace);
            }
        }
        finally { ImGui.EndTable(); }
    }

    private void DrawBeastBoards(BeastCatalog catalog)
    {
        if (catalog.Boards.Count == 0) { ImGui.TextDisabled(localization.Get("BeastUi.NoBoard")); return; }
        beastBoard = Math.Clamp(beastBoard, 0, catalog.Boards.Count - 1);
        var board = catalog.Boards[beastBoard];
        ImGui.SetNextItemWidth(320);
        if (ImGui.BeginCombo("##BeastBoard", board.Name))
        {
            try { for (var index = 0; index < catalog.Boards.Count; index++) if (ImGui.Selectable(catalog.Boards[index].Name + $"##Board{index}", index == beastBoard)) beastBoard = index; }
            finally { ImGui.EndCombo(); }
        }
        Tip("BeastUi.BoardTip");
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
                    foreach (var action in enemy.Actions)
                        ImGui.TextDisabled("    " + string.Join(" · ", new[] { action.Name, action.Shape, action.Target }.Where(part => part.Length > 0)));
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
