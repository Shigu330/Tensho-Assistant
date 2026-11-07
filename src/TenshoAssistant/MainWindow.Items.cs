using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// Items page (R-06 step 3, 0.272 and 0.280): item search (the result list folds, 0.292), manual item ID and the item sheet
// (MainWindow.ItemSheet.cs). Also the views the sheet and the Storage page reuse: the Craft tab (« Made by » / « Used in », crafter
// filters, breadcrumb, ingredients with stocks), the selected item's stocks per storage, the Crystals tab, and SelectItem (the previous
// item goes to the breadcrumb). Item and recipe catalogues come from the game sheets (loaded in the background, cached); stocks from the
// live inventory and the saved Bank, cached per item until either changes.
internal sealed partial class MainWindow
{
    private StorageFilter storageFilter;
    private ItemHoldingsSummary? groupedHoldings;
    private uint holdingsItem;
    private ulong holdingsCharacter;
    private InventorySnapshot? holdingsLive;
    private BankState? holdingsBank;
    private StorageFilter holdingsFilter;
    private object? holdingsCatalog;
    // 0.327 (queue step e): number of item search results (Settings › Interface), bounded on use, and the limit the shown results
    // were searched with: a new value searches again and the « at most » notice follows it.
    private int ItemSearchLimit => Configuration.BoundedItemSearchLimit(configuration.ItemSearchLimit);
    private int searchedLimit;

    private void DrawItems()
    {
        DrawAnnotatedItems();
        Heading(localization.Get("ItemsUi.Title"), localization.Get("ItemsUi.Description"));
        // 0.272 (R-06 step 3): the item sheet holds Stocks, Craft, Usages and Gathering; requests for those tabs (focus flags set
        // by menus and other tools) open the sheet on the matching tab.
        string? sheetTab = focusStocksTab ? "Stocks" : focusCraftTab ? "Craft" : focusUsesTab ? "Uses" : focusGatherTab ? "Gathering" : null;
        focusStocksTab = focusCraftTab = focusUsesTab = focusGatherTab = false;
        if (sheetTab is not null) itemSheetFocus = sheetTab;
        // 0.280 (R-06 step 3): « Objets » is the search and the item sheet; storages, crystals and imports moved to « Rangements ».
        DrawItemSearch();
        DrawManualItemId();
        DrawItemSheet();
    }

    private void DrawCraftTab()
    {
        if (itemId <= 0)
        {
            ImGui.TextWrapped(localization.Get("ItemsUi.SelectCraft"));
            return;
        }
        DrawRecipes();
    }

    private void DrawCrystalsTab()
    {
        ImGui.TextColored(Accent, localization.Get("ItemsUi.CrystalTitle"));
        ImGui.TextDisabled(localization.Get("ItemsUi.CrystalHelp"));
        ImGui.Separator();
        var character = bankBridge.State.CharacterId;
        var snapshot = inventory.Current;
        if (character == 0 || snapshot is null)
        {
            ImGui.TextWrapped(localization.Get("ItemsUi.CrystalOffline"));
            return;
        }
        var location = new InventoryLocation(new(character), new(2001));
        if (!snapshot.Containers.TryGetValue(location, out var container)
            || container.Coverage == InventoryCoverage.Unknown)
        {
            ImGui.TextWrapped(localization.Get("ItemsUi.CrystalLoading"));
            return;
        }
        if (container.Quantities.Count == 0)
        {
            ImGui.TextDisabled(localization.Get("ItemsUi.CrystalEmpty"));
            return;
        }
        if (container.Coverage != InventoryCoverage.Complete)
            ImGui.TextColored(Muted, localization.Get("ItemsUi.Partial"));
        var crystals = container.Quantities.GroupBy(pair => pair.Key.Id.Value)
            .Select(group => new CrystalCell(group.Key,
                group.Where(pair => !pair.Key.IsHighQuality).Sum(pair => pair.Value),
                group.Where(pair => pair.Key.IsHighQuality).Sum(pair => pair.Value)))
            .ToArray();
        DrawCrystalElementTable(crystals);
        var other = crystals.Where(crystal => ClassifyCrystal(crystal.ItemId) is null).ToArray();
        if (other.Length > 0 && TenshoAssistant.UI.UiComponents.Header(localization.Format("RecipeUi.Unclassified",other.Length)))
            foreach (var crystal in other) DrawCrystalCell(crystal);
    }

    private sealed record CrystalCell(uint ItemId, long Nq, long Hq);

    private static readonly string[] CrystalElements = CrystalClassifier.Elements;
    private static readonly string[] CrystalSizes = CrystalClassifier.Sizes;
    private static readonly string[] CrystalElementKeys=["Fire","Ice","Wind","Earth","Lightning","Water"];
    private static readonly string[] CrystalSizeKeys=["Shards","Crystals","Clusters"];

    private void DrawCrystalElementTable(IReadOnlyList<CrystalCell> crystals)
    {
        if (!ImGui.BeginTable("CrystalElementGrid", CrystalElements.Length + 1,
            ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.BordersInnerV)) return;
        try
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextDisabled(localization.Get("ItemsUi.Size"));
            foreach (var element in CrystalElements)
            {
                ImGui.TableNextColumn();
                ImGui.TextColored(Accent, localization.Get("CrystalElements."+CrystalElementKeys[Array.IndexOf(CrystalElements,element)]));
            }
            foreach (var size in CrystalSizes)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextDisabled(localization.Get("CrystalSizes."+CrystalSizeKeys[Array.IndexOf(CrystalSizes,size)]));
                foreach (var element in CrystalElements)
                {
                    ImGui.TableNextColumn();
                    var cell = crystals.FirstOrDefault(crystal =>
                    {
                        var classification = ClassifyCrystal(crystal.ItemId);
                        return classification is { } value
                            && value.Element == element && value.Size == size;
                    });
                    if (cell is not null) DrawCrystalCell(cell);
                    else ImGui.Dummy(new Vector2(58, 74));
                }
            }
        }
        finally { ImGui.EndTable(); }
    }

    private void DrawCrystalCell(CrystalCell crystal)
    {
        var item = itemCatalog.State.Catalog?.Find(crystal.ItemId);
        ImGui.PushID((int)crystal.ItemId);
        try
        {
            var iconSize = new Vector2(42);
            var left = ImGui.GetCursorPosX();
            var available = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(left + Math.Max(0, (available - iconSize.X) / 2));
            DrawItemIcon(item, iconSize);
            if (ImGui.IsItemClicked()) SelectItem(crystal.ItemId);
            var label = crystal.Hq > 0 ? $"{crystal.Nq:N0} / {crystal.Hq:N0}" : $"{crystal.Nq:N0}";
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.SetCursorPosX(left + Math.Max(0, (available - labelWidth) / 2));
            ImGui.TextColored(Accent, label);
        }
        finally { ImGui.PopID(); }
    }

    // 0.322: classified once per item and catalogue (it ran for every cell on every frame), from the English name first.
    private readonly Dictionary<uint,(string Element,string Size)?> crystalClasses=[];
    private object? crystalClassesCatalog;
    private (string Element, string Size)? ClassifyCrystal(uint itemId)
    {
        var catalog = itemCatalog.State.Catalog;
        if (!ReferenceEquals(catalog, crystalClassesCatalog)) { crystalClasses.Clear(); crystalClassesCatalog = catalog; }
        if (crystalClasses.TryGetValue(itemId, out var known)) return known;
        var item = catalog?.Find(itemId);
        var classification = item is null || string.IsNullOrWhiteSpace(item.Name) ? null : CrystalClassifier.Classify(item.Name, item.EnglishName);
        (string, string)? result = classification is null ? null : (classification.Element, classification.Size);
        if (catalog is not null) crystalClasses[itemId] = result;
        return result;
    }

    private void DrawManualItemId()
    {
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("ItemsUi.Manual")))
        {
            ImGui.InputInt(localization.Get("ItemsUi.BaseId"), ref itemId);
            Tip("ItemsUi.BaseIdTip");
        }
    }

    private void DrawSelectedItemHoldings(bool includeRecipes = true)
    {
        if (itemId <= 0)
        {
            ImGui.TextWrapped(localization.Get("HoldingsUi.Select"));
            return;
        }
        DrawItemIcon(SelectedItem, new Vector2(56));
        ImGui.SameLine();
        ImGui.BeginGroup();
        try
        {
            ImGui.TextColored(Accent, SelectedItem?.Name ?? localization.Format("BankUi.Item",itemId));
            if(developerMode)ImGui.TextDisabled($"ID {itemId}");
            ImGui.TextDisabled(localization.Get("HoldingsUi.Quality"));
        }
        finally { ImGui.EndGroup(); }
        ImGui.Spacing();
        if (ImGui.BeginTabBar("StorageScope"))
        {
            try
            {
                StorageTab(localization.Get("HoldingsUi.All"), StorageFilter.All);
                StorageTab(localization.Get("HoldingsUi.Player"), StorageFilter.PlayerBags);
                StorageTab(localization.Get("HoldingsUi.Chocobo"), StorageFilter.Chocobo);
                StorageTab(localization.Get("HoldingsUi.Retainers"), StorageFilter.Retainers);
            }
            finally { ImGui.EndTabBar(); }
        }

        var live = inventory.Current;
        var bankState = bank.State;
        var character = bankBridge.State.CharacterId;
        var catalog = itemCatalog.State.Catalog;
        if (groupedHoldings is null || holdingsItem != (uint)itemId || holdingsCharacter != character
            || !ReferenceEquals(holdingsLive, live) || !ReferenceEquals(holdingsBank, bankState) || holdingsFilter != storageFilter
            || !ReferenceEquals(holdingsCatalog, catalog))
        {
            // 0.329: a crystal also counts the crystal bag (container 2001), as the sheet header and the hover do; it showed 0 here.
            // Only for a crystal (classified from the catalogue), so other items keep their rows.
            groupedHoldings = ItemHoldings.Build((uint)itemId, character, live, bankState.Entries, storageFilter,
                includeCrystals: ClassifyCrystal((uint)itemId) is not null, crystal: ClassifyCrystal((uint)itemId) is not null);
            holdingsItem = (uint)itemId;
            holdingsCharacter = character;
            holdingsLive = live;
            holdingsBank = bankState;
            holdingsFilter = storageFilter;
            holdingsCatalog = catalog;
        }
        var summary = groupedHoldings;
        DrawHoldingsSummary(summary);
        DrawStorageGroups(summary);
        DrawOtherCharacterStocks((uint)itemId,character,bankState);
        if (includeRecipes) DrawRecipes();
        if (bankState.Loading) ImGui.TextDisabled(localization.Get("HoldingsUi.Loading"));
        if (bankState.Error is { } error) DrawDataError(error,"ServiceErrors.Bank");
        // 0.329: the last import message (« saved », or an old « incomplete read ») is no longer repeated on every item's
        // Stocks tab for the whole session; the Imports tab and the in-game panel show it.
    }

    private void DrawRecipes()
    {
        // 0.292: shown inside the sheet's « Craft lié » tab; the header that repeated the tab name is gone.
        ImGui.PushStyleColor(ImGuiCol.ChildBg, TenshoAssistant.UI.FfxivTheme.Panel);
        ImGui.PushStyleColor(ImGuiCol.Border, TenshoAssistant.UI.FfxivTheme.Border);
        var visible = ImGui.BeginChild("RecipePanel", new Vector2(-1, 0), true);
        try
        {
            if (!visible) return;
            ImGui.TextColored(Accent, localization.Get("RecipeUi.Title"));
            ImGui.TextDisabled(localization.Get("RecipeUi.Help"));
            ImGui.Separator();
            if (craftHistory.Count > 0 && ImGui.SmallButton(localization.Get("RecipeUi.Back")))
            {
                var previous = craftHistory.Pop();
                SelectItem(previous, false);
                return;
            }
            if (craftHistory.Count > 0) ImGui.SameLine();
            ImGui.TextDisabled(localization.Format("RecipeUi.Current",SelectedItem?.Name ?? localization.Format("BankUi.Item",itemId)));
            DrawCraftBreadcrumb();
            RefreshRecipeHoldingsCache();
            DrawRecipeTools();
            ImGui.Spacing();
            DrawRecipeResults();
        }
        finally
        {
            ImGui.EndChild();
            ImGui.PopStyleColor(2);
        }
    }

    private void DrawRecipeResults()
    {
        var state = recipeCatalog.State;
        if (state.Catalog is not { } catalog)
        {
            ImGui.TextDisabled(state.Error ?? (state.TotalRows > 0
                ? localization.Format("RecipeUi.CatalogProgress",state.RowsCopied.ToString("N0"),state.TotalRows.ToString("N0"))
                : localization.Get("RecipeUi.Loading")));
            return;
        }
        // 0.292 (user request): the two directions are tabs; a new item opens on the direction that has recipes.
        var producedBy = catalog.ForResult((uint)itemId); var usedIn = catalog.ForIngredient((uint)itemId);
        var openUsedIn = recipeTabsItem != itemId && producedBy.Count == 0 && usedIn.Count > 0;
        var openProduced = recipeTabsItem != itemId && !openUsedIn;
        recipeTabsItem = itemId;
        if (!ImGui.BeginTabBar("RecipeDirections")) return;
        try
        {
            if (ImGui.BeginTabItem($"{localization.Get("RecipeUi.ProducedBy")} ({producedBy.Count})###RecipeProduced", openProduced ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
            { try { DrawRecipeSection(localization.Get("RecipeUi.ProducedBy"), producedBy, catalog.Source, false, false); } finally { ImGui.EndTabItem(); } }
            if (ImGui.BeginTabItem($"{localization.Get("RecipeUi.UsedIn")} ({usedIn.Count})###RecipeUsedIn", openUsedIn ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
            { try { DrawRecipeSection(localization.Get("RecipeUi.UsedIn"), usedIn, catalog.Source, true, false); } finally { ImGui.EndTabItem(); } }
        }
        finally { ImGui.EndTabBar(); }
    }

    // 0.329: the history keeps the last CraftHistoryLimit items (every item click in the plugin lands here); the breadcrumb shows
    // the last BreadcrumbShown places after « … » and wraps to the panel width (one line ran off the panel). « Back » walks it all.
    private const int CraftHistoryLimit = 32, BreadcrumbShown = 10;
    private void DrawCraftBreadcrumb()
    {
        if (craftHistory.Count == 0) return;
        ImGui.Spacing();
        ImGui.TextDisabled(localization.Get("RecipeUi.Path"));
        var path = craftHistory.Reverse().Append((uint)itemId).ToArray();
        var first = Math.Max(0, path.Length - BreadcrumbShown);
        if (first > 0) ImGui.TextDisabled("… ›");
        var style = ImGui.GetStyle();
        for (var index = first; index < path.Length; index++)
        {
            var item = itemCatalog.State.Catalog?.Find(path[index]);
            var label = item?.Name ?? $"ID {path[index]}";
            var last = index == path.Length - 1;
            if (index > 0)
            {
                ImGui.SameLine();
                var width = ImGui.CalcTextSize(label).X + (last ? 0 : style.FramePadding.X * 2 + style.ItemSpacing.X + ImGui.CalcTextSize("›").X);
                if (ImGui.GetContentRegionAvail().X < width) ImGui.NewLine();
            }
            if (last)
            {
                ImGui.TextColored(Accent, label);
                continue;
            }
            if (ImGui.SmallButton($"{label}##Breadcrumb{index}"))
            {
                // 0.329: oldest first, so the newest place left stays on top (it was pushed in reverse: « Back » went to the oldest).
                craftHistory.Clear();
                for (var previous = 0; previous < index; previous++) craftHistory.Push(path[previous]);
                SelectItem(path[index], false);
                return;
            }
            ImGui.SameLine();
            ImGui.TextDisabled("›");
        }
    }

    // The item left goes to the craft history, without its oldest places beyond CraftHistoryLimit.
    private void RememberItem(uint next)
    {
        if (itemId <= 0 || itemId == next) return;
        craftHistory.Push((uint)itemId);
        if (craftHistory.Count <= CraftHistoryLimit) return;
        var kept = craftHistory.Take(CraftHistoryLimit).Reverse().ToArray();
        craftHistory.Clear();
        foreach (var id in kept) craftHistory.Push(id);
    }

    private int recipeTabsItem;
    private void DrawRecipeSection(string title, IReadOnlyList<RecipeDefinition> recipes, string source, bool showOutputs, bool withHeader = true)
    {
        var header = $"{title} ({recipes.Count})###RecipeSection{(showOutputs ? "Outputs" : "Inputs")}";
        if (withHeader && !TenshoAssistant.UI.UiComponents.Header(header, ImGuiTreeNodeFlags.DefaultOpen)) return;
        if (recipes.Count == 0)
        {
            ImGui.TextDisabled(localization.Get("RecipeUi.NoRecipes"));
            return;
        }
        if (showOutputs)
        {
            DrawCraftBook(recipes, source);
            return;
        }
            var visibleRecipes = recipes;
            if (showOutputs)
            {
                DrawCraftBookFilters(recipes);
                visibleRecipes = string.IsNullOrWhiteSpace(craftBookFilter)
                    ? recipes
                    : recipes.Where(recipe => string.Equals(recipe.CraftType, craftBookFilter, StringComparison.Ordinal)).ToArray();
                if (visibleRecipes.Count == 0)
                {
                    ImGui.TextDisabled(localization.Get("RecipeUi.NoJobRecipe"));
                    return;
                }
            }
            ImGui.TextDisabled(localization.Format("RecipeUi.Variants",visibleRecipes.Count));
            foreach (var recipe in visibleRecipes)
        {
            ImGui.PushID((int)recipe.RecipeId);
            try
            {
                ImGui.Spacing();
                if (showOutputs) DrawRecipeOutput(recipe);
                DrawRecipeFacts(recipe);
                ImGui.TextColored(Accent, localization.Format("RecipeUi.JobLevel",recipe.CraftType,recipe.Level));
                ImGui.SameLine();
                ImGui.TextDisabled(localization.Format("RecipeUi.Yield",recipe.ResultAmount));
                DrawRecipeCrafted(recipe);
                ImGui.SameLine();
                var craftable = GetCraftableCount(recipe);
                ImGui.TextColored(craftable.LowerBound ? Muted : Success,
                    craftable.Count is { } count ? (craftable.LowerBound ? "≥ "+localization.Format("RecipeUi.Crafts",count) : localization.Format("RecipeUi.Crafts",count)) : localization.Get("RecipeUi.UnknownCrafts"));
                if (craftable.Count is { } productCount)
                {
                    var produced = checked(productCount * recipe.ResultAmount);
                    ImGui.SameLine();
                    ImGui.TextDisabled(craftable.LowerBound ? localization.Format("RecipeUi.Produced","≥ "+produced.ToString("N0")) : localization.Format("RecipeUi.Produced",produced.ToString("N0")));
                }
                if (!showOutputs && recipe.Ingredients.Count == 0) ImGui.TextDisabled(localization.Get("RecipeUi.NoIngredients"));
                else if (!showOutputs)
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        var item = itemCatalog.State.Catalog?.Find(ingredient.ItemId);
                        DrawRecipeIngredient(item, ingredient);
                    }
                if (!showOutputs) DrawCraftLinks(recipe);
                ImGui.Separator();
            }
            finally { ImGui.PopID(); }
        }
    }

    // 0.329: the craft book's jobs (with the job id of their first recipe, for the icon) and its filtered, sorted recipes, recomputed
    // only when their inputs change: Distinct/OrderBy, one First() per job, then Where and three OrderBy over every recipe ran on
    // every frame (a shard or a crystal is used in thousands of recipes).
    private object? craftBookTypesSource;
    private (string Type, uint TypeId)[] craftBookTypes = [];
    private (object? Recipes, object? Items, string Filter, string Query, object? NeverCrafted) craftBookMatchesKey;
    private RecipeDefinition[] craftBookMatches = [];

    private void DrawCraftBookFilters(IReadOnlyList<RecipeDefinition> recipes)
    {
        if (!ReferenceEquals(craftBookTypesSource, recipes))
        {
            craftBookTypes = recipes.Where(recipe => !string.IsNullOrWhiteSpace(recipe.CraftType))
                .GroupBy(recipe => recipe.CraftType, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => (group.Key, group.First().CraftTypeId))
                .ToArray();
            craftBookTypesSource = recipes;
        }
        var craftTypes = craftBookTypes;
        if (!Array.Exists(craftTypes, type => string.Equals(type.Type, craftBookFilter, StringComparison.Ordinal))) craftBookFilter = string.Empty;

        ImGui.TextDisabled(localization.Get("RecipeUi.Book"));
        ImGui.PushFont(UiBuilder.IconFont);
        try
        {
            if (ImGui.Button($"{FontAwesomeIcon.BookOpen.ToIconString()}##CraftBookAll", new Vector2(38, 38)))
                craftBookFilter = string.Empty;
        }
        finally { ImGui.PopFont(); }
        if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("RecipeUi.AllJobs"));

        foreach (var (craftType, craftTypeId) in craftTypes)
        {
            ImGui.SameLine();
            ImGui.PushID($"CraftBookFilter{craftType}");
            try
            {
                var selected = string.Equals(craftBookFilter, craftType, StringComparison.Ordinal);
                if (selected) ImGui.PushStyleColor(ImGuiCol.Button, Accent);
                var clicked = ImGui.Button("##CraftBookJob", new Vector2(38, 38));
                var buttonPosition = ImGui.GetItemRectMin();
                var buttonHovered = ImGui.IsItemHovered();
                ImGui.SetCursorScreenPos(buttonPosition + new Vector2(3));
                // Native framed class icons: 62100 + ClassJob ID, crafters 8..15.
                if (craftTypeId < 8)
                    DrawItemIcon(new ItemDefinition(0, craftType, false, 62108 + craftTypeId), new Vector2(32));
                // 0.207: the icon was the last item, so the next SameLine started after it (3 px lower and further right each time);
                // an invisible item over the button restores the button's place for the next one.
                ImGui.SetCursorScreenPos(buttonPosition);
                ImGui.Dummy(new Vector2(38, 38));
                if (selected) ImGui.PopStyleColor();
                if (clicked) craftBookFilter = craftType;
                if (buttonHovered) WrappedTooltip(craftType);
            }
            finally { ImGui.PopID(); }
        }
        ImGui.Spacing();
    }

    // A-11 (0.421): the recipes crafted at least once, and « never crafted only » in the craft book.
    internal Craft.RecipeLogReader? RecipeLog { get; set; }
    private bool craftBookNeverCrafted;
    private void DrawRecipeCrafted(RecipeDefinition recipe)
    {
        RecipeLog?.Want();
        if (RecipeLog?.State?.WasCrafted(recipe.RecipeId) is not { } crafted) return;
        ImGui.SameLine();
        if (crafted) ImGui.TextDisabled(localization.Get("RecipeUi.Crafted"));
        else ImGui.TextColored(Accent, localization.Get("RecipeUi.NeverCrafted"));
        if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("RecipeUi.CraftedTip"));
    }
    private void DrawCraftBook(IReadOnlyList<RecipeDefinition> recipes, string source)
    {
        DrawCraftBookFilters(recipes);
        RecipeLog?.Want();
        var recipeLog = RecipeLog?.State;
        if (recipeLog is not null)
        {
            ImGui.Checkbox(localization.Get("RecipeUi.NeverCraftedOnly") + "###CraftBookNever", ref craftBookNeverCrafted);
            Tip("RecipeUi.NeverCraftedOnlyTip");
        }
        ImGui.TextColored(Accent, string.IsNullOrWhiteSpace(craftBookFilter) ? localization.Get("RecipeUi.AllJobs") : craftBookFilter);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##CraftBookQuery", localization.Get("RecipeUi.Query"), ref craftBookQuery, 128);
        Tip("RecipeUi.QueryTip");
        var items = itemCatalog.State.Catalog;
        var neverOnly = craftBookNeverCrafted && recipeLog is not null;
        var matchesKey = ((object?)recipes, (object?)items, craftBookFilter, craftBookQuery, neverOnly ? (object?)recipeLog : null);
        if (matchesKey != craftBookMatchesKey)
        {
            var query = craftBookQuery.Trim();
            craftBookMatches = recipes.Where(recipe => (string.IsNullOrWhiteSpace(craftBookFilter) || recipe.CraftType == craftBookFilter)
                    && (!neverOnly || recipeLog!.WasCrafted(recipe.RecipeId) == false)
                    && (string.IsNullOrWhiteSpace(query)
                        || (items?.Find(recipe.ResultItemId)?.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)))
                .OrderBy(recipe => recipe.CraftTypeId).ThenBy(recipe => recipe.Level).ThenBy(recipe => recipe.RecipeId).ToArray();
            craftBookMatchesKey = matchesKey;
        }
        var matches = craftBookMatches;
        if (matches.Length == 0)
        {
            ImGui.TextDisabled(localization.Get("RecipeUi.NoMatch"));
            return;
        }
        var selectedIndex = Array.FindIndex(matches, recipe => recipe.RecipeId == craftBookRecipe);
        if (selectedIndex < 0) { selectedIndex = 0; craftBookRecipe = matches[0].RecipeId; }
        ImGui.TextDisabled(localization.Format("RecipeUi.Using",matches.Length,SelectedItem?.Name ?? itemId.ToString()));
        if (!ImGui.BeginTable("CraftBookWorkspace", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("RecipeUi.Recipes"), ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableSetupColumn(localization.Get("RecipeUi.Details"), ImGuiTableColumnFlags.WidthStretch, 1.6f);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var height = Math.Clamp(ImGui.GetContentRegionAvail().Y - 10, 220, 460);
            var visible = ImGui.BeginChild("CraftBookList", new Vector2(0, height), false);
            try
            {
                if (visible)
                {
                    // 0.329: only the visible rows are drawn (fixed height: the 30 px selectable next to its 28 px icon).
                    var clipper = ImGui.ImGuiListClipper();
                    try
                    {
                        clipper.Begin(matches.Length, 30 + ImGui.GetStyle().ItemSpacing.Y);
                        while (clipper.Step())
                        for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                        {
                            var recipe = matches[index];
                            ImGui.PushID((int)recipe.RecipeId);
                            try
                            {
                                var result = items?.Find(recipe.ResultItemId);
                                DrawItemIcon(result, new Vector2(28));
                                var clickedIcon = ImGui.IsItemClicked();
                                ImGui.SameLine();
                                if (ImGui.Selectable($"{result?.Name ?? recipe.ResultItemId.ToString()}##Recipe", craftBookRecipe == recipe.RecipeId,
                                        ImGuiSelectableFlags.None, new Vector2(0, 30)) || clickedIcon)
                                    craftBookRecipe = recipe.RecipeId;
                                if (ImGui.IsItemHovered()) WrappedTooltip(localization.Format("RecipeUi.JobLevel",recipe.CraftType,recipe.Level));
                            }
                            finally { ImGui.PopID(); }
                        }
                    }
                    finally { clipper.End(); clipper.Destroy(); }
                }
            }
            finally { ImGui.EndChild(); }
            ImGui.TableNextColumn();
            var details = ImGui.BeginChild("CraftBookDetails", new Vector2(0, height), false);
            try
            {
                if (details)
                {
                    // A click in the list this frame may have chosen another recipe.
                    var selected = matches[selectedIndex].RecipeId == craftBookRecipe ? matches[selectedIndex] : matches.First(recipe => recipe.RecipeId == craftBookRecipe);
                    DrawRecipeOutput(selected);
                    DrawRecipeSection(localization.Get("RecipeUi.SelectedRecipe"), new[] { selected }, source, false);
                }
            }
            finally { ImGui.EndChild(); }
        }
        finally { ImGui.EndTable(); }
    }

    private void DrawRecipeOutput(RecipeDefinition recipe)
    {
        var item = itemCatalog.State.Catalog?.Find(recipe.ResultItemId);
        DrawItemIcon(item, new Vector2(32));
        if (ImGui.IsItemClicked()) SelectItem(recipe.ResultItemId);
        ImGui.SameLine();
        if (ImGui.SmallButton($"{item?.Name ?? localization.Format("BankUi.Item",recipe.ResultItemId)}##RecipeOutput"))
            SelectItem(recipe.ResultItemId);
        ImGui.SameLine();
        ImGui.TextDisabled(localization.Get("RecipeUi.Result"));
    }

    private void DrawRecipeResultHeader()
    {
        var item = SelectedItem;
        DrawItemIcon(item, new Vector2(48));
        ImGui.SameLine();
        ImGui.BeginGroup();
        try
        {
            ImGui.TextColored(Accent, item?.Name ?? localization.Format("BankUi.Item",itemId));
            ImGui.TextDisabled(localization.Get("RecipeUi.Result"));
        }
        finally { ImGui.EndGroup(); }
    }

    private void DrawRecipeIngredient(ItemDefinition? item, RecipeIngredient ingredient)
    {
        var label = item?.Name ?? localization.Format("BankUi.Item",ingredient.ItemId);
        ImGui.PushID($"Ingredient{ingredient.ItemId}");
        try
        {
            DrawItemIcon(item, new Vector2(28));
            if (ImGui.IsItemClicked()) SelectItem(ingredient.ItemId);
            ImGui.SameLine();
            var buttonWidth = Math.Max(90, ImGui.CalcTextSize(label).X + 20);
            if (ImGui.Selectable($"{label}##IngredientButton", false, ImGuiSelectableFlags.None, new Vector2(buttonWidth, 30)))
                SelectItem(ingredient.ItemId);
            if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("RecipeUi.IngredientHint"));
            ImGui.SameLine();
            ImGui.TextDisabled($"× {ingredient.Amount}");
            ImGui.SameLine();
            var holdings = GetRecipeHoldings(ingredient.ItemId);
            ImGui.TextColored(holdings.HasUnknown ? Muted : Success,
                FormatRecipeHoldings(holdings, ingredient.Amount));
        }
        finally { ImGui.PopID(); }
    }

    private void RefreshRecipeHoldingsCache()
    {
        var live = inventory.Current;
        var bankState = bank.State;
        var character = bankBridge.State.CharacterId;
        if (ReferenceEquals(recipeHoldingsLive, live) && ReferenceEquals(recipeHoldingsBank, bankState)
            && recipeHoldingsCharacter == character) return;
        recipeHoldingsCache.Clear();
        recipeHoldingsLive = live;
        recipeHoldingsBank = bankState;
        recipeHoldingsCharacter = character;
    }

    private ItemHoldingsSummary GetRecipeHoldings(uint itemId)
    {
        if (recipeHoldingsCache.TryGetValue(itemId, out var summary)) return summary;
        summary = ItemHoldings.Build(itemId, recipeHoldingsCharacter, recipeHoldingsLive,
            recipeHoldingsBank?.Entries ?? [], StorageFilter.All, includeCrystals: true, crystal: ClassifyCrystal(itemId) is not null);
        recipeHoldingsCache[itemId] = summary;
        return summary;
    }

    private string FormatRecipeHoldings(ItemHoldingsSummary summary, int required)
    {
        if (!summary.HasObservation) return localization.Get("RecipeUi.UnknownStock");
        var value = $"NQ {summary.Nq:N0} · HQ {summary.Hq:N0}";
        var missing = Math.Max(0, required - summary.Total);
        // 0.735 (user report): a lower bound that already covers the need is enough: crystals held by retainers are never read, yet 490 of 5 is OK.
        if (summary.HasUnknown) return localization.Format(missing == 0 ? "RecipeUi.PartialEnough":"RecipeUi.Partial",value);
        return missing == 0 ? $"{value} · OK" : localization.Format("RecipeUi.Missing",value,missing.ToString("N0"));
    }

    private (long? Count, bool LowerBound) GetCraftableCount(RecipeDefinition recipe)
    {
        if (recipe.Ingredients.Count == 0) return (null, false);
        long possible = long.MaxValue;
        var lowerBound = false;
        foreach (var ingredient in recipe.Ingredients)
        {
            var holdings = GetRecipeHoldings(ingredient.ItemId);
            if (!holdings.HasObservation || ingredient.Amount <= 0) return (null, false);
            possible = Math.Min(possible, holdings.Total / ingredient.Amount);
            lowerBound |= holdings.HasUnknown;
        }
        return (possible == long.MaxValue ? null : possible, lowerBound);
    }

    private void DrawRecipeTools()
    {
        ImGui.TextDisabled(ToolText("Related"));
        var tools = PlannedTools.Where(tool => tool.Reference is "Craftimizer" or "Item Use" or "GatherBuddy" or "Price Insight" or "Monster Loot Hunter").ToArray();
        foreach (var tool in tools)
        {
            if (ImGui.GetContentRegionAvail().X < ImGui.CalcTextSize(ToolText(tool.Name)).X + 56) ImGui.NewLine();
            ImGui.PushID($"RecipeTool{tool.Reference}");
            try
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(Accent, tool.Icon.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                if (ImGui.SmallButton(ToolText(tool.Name))) OpenContextTool(tool.Reference);
                if (ImGui.IsItemHovered()) WrappedTooltip(localization.Format("ToolDirectory.OpenContext",ToolText(tool.Name)));
                ImGui.SameLine();
            }
            finally { ImGui.PopID(); }
        }
        ImGui.NewLine();
        ImGui.TextDisabled(ToolText("OtherTools"));
    }

    private void SelectItem(uint id, bool remember = true)
    {
        if (id > int.MaxValue) return;
        if (remember) RememberItem(id);
        itemId = (int)id;
        if (itemCatalog.State.Catalog is not { } catalog) return;
        var item = catalog.Find(id);
        itemQuery = item?.Name ?? id.ToString();
        searchedCatalog = catalog;
        searchedLimit = ItemSearchLimit;
        searchResults = catalog.Search(itemQuery, searchedLimit);
    }

    private void StorageTab(string label, StorageFilter filter)
    {
        if (!ImGui.BeginTabItem(label)) return;
        try { storageFilter = filter; }
        finally { ImGui.EndTabItem(); }
    }

    private void DrawHoldingsSummary(ItemHoldingsSummary summary)
    {
        string Number(long value) => !summary.HasObservation ? localization.Get("HoldingsUi.Unknown")
            : !summary.HasCached && summary.HasUnknown ? $"≥ {value:N0}" : value.ToString("N0");
        if (ImGui.BeginTable("GroupedQuantities", 3, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.BordersInnerV))
        {
            try
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextDisabled("NQ");
                ImGui.TextUnformatted(Number(summary.Nq));
                ImGui.TableNextColumn();
                ImGui.TextDisabled("HQ");
                ImGui.TextUnformatted(Number(summary.Hq));
                ImGui.TableNextColumn();
                ImGui.TextDisabled(summary.IsExact ? localization.Get("HoldingsUi.Total") : localization.Get("HoldingsUi.Recorded"));
                ImGui.TextColored(Accent, Number(summary.Total));
            }
            finally { ImGui.EndTable(); }
        }
        ImGui.Spacing();
        if (summary.HasCached)
        {
            ImGui.TextWrapped(localization.Get("HoldingsUi.CacheHelp"));
            ImGui.TextDisabled(localization.Get("HoldingsUi.Excluded"));
        }
        else if (summary.IsExact)
            ImGui.TextColored(Success, localization.Get("HoldingsUi.Exact"));
        else if (summary.HasUnknown)
            ImGui.TextWrapped(localization.Get("HoldingsUi.Partial"));
        ImGui.Spacing();
    }

    private void DrawItemSearch(float maximumHeight = 340)
    {
        var state = itemCatalog.State;
        ImGui.SetNextItemWidth(-1);
        var changed = ImGui.InputTextWithHint("##ItemQuery", localization.Get("ItemsUi.Query"), ref itemQuery, 128);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) WrappedTooltip(localization.Format("ItemsUi.QueryTip", ItemSearchLimit));
        if (state.Catalog is not { } catalog)
        {
            DrawCatalogStatus();
            return;
        }
        // 0.353 (g, I-12): filters, combined with the name (or alone).
        changed |= DrawItemSearchFilters(catalog);
        // 0.327 (queue step e): the limit is a setting; a new value searches again.
        var limit = ItemSearchLimit;
        if (changed || !ReferenceEquals(searchedCatalog, catalog) || searchedLimit != limit)
        {
            searchResults = catalog.Search(itemQuery, limit, itemSearchFilter);
            searchedCatalog = catalog;
            searchedLimit = limit;
        }
        if (string.IsNullOrWhiteSpace(itemQuery) && itemSearchFilter.IsEmpty)
        {
            ImGui.TextDisabled(localization.Get("ItemsUi.QueryHelp"));
            DrawKeyItemSearch();
            return;
        }
        if (searchResults.Count == 0)
        {
            ImGui.TextUnformatted(localization.Get("ItemsUi.Empty"));
            return;
        }
        // 0.292 (user request): the result list folds, leaving the room to the item sheet.
        if (!TenshoAssistant.UI.UiComponents.Header(localization.Format("ItemsUi.Results", searchResults.Count) + "###ItemResultsHeader", ImGuiTreeNodeFlags.DefaultOpen)) return;
        var height = Math.Clamp(searchResults.Count * 38f + 12, 64, maximumHeight);
        if (ImGui.BeginListBox("##ItemResults", new Vector2(-1, height)))
        {
            try
            {
                foreach (var item in searchResults)
                {
                    if (item.Id > int.MaxValue) continue;
                    ImGui.PushID((int)item.Id);
                    try
                    {
                        // 0.329: a result goes to the craft history like every other item click (it skipped it: the breadcrumb
                        // showed a path not taken), but keeps the typed query and its results (SelectItem would replace them).
                        DrawItemIcon(item, new Vector2(32));
                        if (ImGui.IsItemClicked()) { RememberItem(item.Id); itemId = (int)item.Id; }
                        ImGui.SameLine();
                        if (ImGui.Selectable($"{item.Name}##Result", itemId == (int)item.Id,
                                ImGuiSelectableFlags.None, new Vector2(0, 32))) { RememberItem(item.Id); itemId = (int)item.Id; }
                        if (ImGui.IsItemHovered()) DrawItemInfoTooltip(item.Id);
                        ItemLinksMenu($"SearchResult{item.Id}",item);
                    }
                    finally { ImGui.PopID(); }
                }
            }
            finally { ImGui.EndListBox(); }
        }
        if (searchResults.Count == searchedLimit) ImGui.TextDisabled(localization.Format("ItemsUi.Limit", searchedLimit));
    }
}
