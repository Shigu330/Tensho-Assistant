using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private StorageFilter storageFilter;
    private ItemHoldingsSummary? groupedHoldings;
    private uint holdingsItem;
    private ulong holdingsCharacter;
    private InventorySnapshot? holdingsLive;
    private BankState? holdingsBank;
    private StorageFilter holdingsFilter;

    private void DrawItems()
    {
        Heading(localization.Get("ItemsUi.Title"), localization.Get("ItemsUi.Description"));
        // New tab definition: reset persisted ImGui order once so Usages is inserted at its declared position.
        if (ImGui.BeginTabBar("ItemWorkspaceTabs_Usages051"))
        {
            try
            {
                if (ImGui.BeginTabItem(localization.Get("ItemsUi.Search")))
                {
                    try
                    {
                        DrawItemSearch();
                        DrawManualItemId();
                        if (itemId > 0)
                        {
                            ImGui.Spacing();
                            ImGui.TextDisabled(localization.Get("ItemsUi.Selected"));
                            DrawRecipeResultHeader();
                            ImGui.TextWrapped(localization.Get("ItemsUi.SelectedHelp"));
                        }
                    }
                    finally { ImGui.EndTabItem(); }
                }
                if (ImGui.BeginTabItem(localization.Get("ItemsUi.Stocks"), focusStocksTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                {
                    try { focusStocksTab = false; DrawSelectedItemHoldings(false); }
                    finally { ImGui.EndTabItem(); }
                }
                var craftFlags = focusCraftTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                if (ImGui.BeginTabItem(localization.Get("ItemsUi.Craft"), craftFlags))
                {
                    try
                    {
                        focusCraftTab = false;
                        DrawCraftTab();
                    }
                    finally { ImGui.EndTabItem(); }
                }
                if(ImGui.BeginTabItem(localization.Get("ItemsUi.Uses"),focusUsesTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None))
                {
                    try{focusUsesTab=false;DrawItemUses();}
                    finally{ImGui.EndTabItem();}
                }
                if (ImGui.BeginTabItem(localization.Get("ItemsUi.Crystals")))
                {
                    try { DrawCrystalsTab(); }
                    finally { ImGui.EndTabItem(); }
                }
                var bankFlags = focusBankTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                if (ImGui.BeginTabItem(localization.Get("ItemsUi.Bank"), bankFlags))
                {
                    try
                    {
                        focusBankTab = false;
                        DrawBankBrowser();
                    }
                    finally { ImGui.EndTabItem(); }
                }
                if (ImGui.BeginTabItem(localization.Get("ItemsUi.Gathering"), focusGatherTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                {
                    try { focusGatherTab = false; DrawGathering(); }
                    finally { ImGui.EndTabItem(); }
                }
                if (ImGui.BeginTabItem(localization.Get("ItemsUi.Imports")))
                {
                    try { DrawImportManager(); }
                    finally { ImGui.EndTabItem(); }
                }
            }
            finally { ImGui.EndTabBar(); }
        }
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
        if (other.Length > 0 && ImGui.CollapsingHeader(localization.Format("RecipeUi.Unclassified",other.Length)))
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
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(item?.Name ?? localization.Format("BankUi.Item",crystal.ItemId));
            var label = crystal.Hq > 0 ? $"{crystal.Nq:N0} / {crystal.Hq:N0}" : $"{crystal.Nq:N0}";
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.SetCursorPosX(left + Math.Max(0, (available - labelWidth) / 2));
            ImGui.TextColored(Accent, label);
        }
        finally { ImGui.PopID(); }
    }

    private (string Element, string Size)? ClassifyCrystal(uint itemId)
    {
        var name = itemCatalog.State.Catalog?.Find(itemId)?.Name;
        var classification = string.IsNullOrWhiteSpace(name) ? null : CrystalClassifier.Classify(name);
        return classification is null ? null : (classification.Element, classification.Size);
    }

    private void DrawManualItemId()
    {
        if (ImGui.CollapsingHeader(localization.Get("ItemsUi.Manual")))
            ImGui.InputInt(localization.Get("ItemsUi.BaseId"), ref itemId);
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
        if (groupedHoldings is null || holdingsItem != (uint)itemId || holdingsCharacter != character
            || !ReferenceEquals(holdingsLive, live) || !ReferenceEquals(holdingsBank, bankState) || holdingsFilter != storageFilter)
        {
            groupedHoldings = ItemHoldings.Build((uint)itemId, character, live, bankState.Entries, storageFilter);
            holdingsItem = (uint)itemId;
            holdingsCharacter = character;
            holdingsLive = live;
            holdingsBank = bankState;
            holdingsFilter = storageFilter;
        }
        var summary = groupedHoldings;
        DrawHoldingsSummary(summary);
        DrawStorageGroups(summary);
        if (includeRecipes) DrawRecipes();
        if (bankState.Loading) ImGui.TextDisabled(localization.Get("HoldingsUi.Loading"));
        if (bankState.Error is { } error) DrawDataError(error,"ServiceErrors.Bank");
        if (bankBridge.State.Message is { } message) DrawServiceMessage(message,"ServiceErrors.Bank");
    }

    private void DrawRecipes()
    {
        ImGui.Spacing();
        if (!ImGui.CollapsingHeader(localization.Get("ItemsUi.Craft"), ImGuiTreeNodeFlags.DefaultOpen)) return;
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
        DrawRecipeSection(localization.Get("RecipeUi.ProducedBy"), catalog.ForResult((uint)itemId), catalog.Source, false);
        DrawRecipeSection(localization.Get("RecipeUi.UsedIn"), catalog.ForIngredient((uint)itemId), catalog.Source, true);
    }

    private void DrawCraftBreadcrumb()
    {
        if (craftHistory.Count == 0) return;
        ImGui.Spacing();
        ImGui.TextDisabled(localization.Get("RecipeUi.Path"));
        var path = craftHistory.Reverse().Append((uint)itemId).ToArray();
        for (var index = 0; index < path.Length; index++)
        {
            if (index > 0) ImGui.SameLine();
            var item = itemCatalog.State.Catalog?.Find(path[index]);
            var label = item?.Name ?? $"ID {path[index]}";
            if (index == path.Length - 1)
            {
                ImGui.SameLine();
                ImGui.TextColored(Accent, label);
                continue;
            }
            if (ImGui.SmallButton($"{label}##Breadcrumb{index}"))
            {
                craftHistory.Clear();
                for (var previous = index - 1; previous >= 0; previous--) craftHistory.Push(path[previous]);
                SelectItem(path[index], false);
                return;
            }
            ImGui.SameLine();
            ImGui.TextDisabled("›");
        }
    }

    private void DrawRecipeSection(string title, IReadOnlyList<RecipeDefinition> recipes, string source, bool showOutputs)
    {
        var header = $"{title} ({recipes.Count})###RecipeSection{(showOutputs ? "Outputs" : "Inputs")}";
        if (!ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen)) return;
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
                ImGui.TextColored(Accent, localization.Format("RecipeUi.JobLevel",recipe.CraftType,recipe.Level));
                ImGui.SameLine();
                ImGui.TextDisabled(localization.Format("RecipeUi.Yield",recipe.ResultAmount));
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
                if (!showOutputs) DrawCraftPlan(recipe);
                ImGui.Separator();
            }
            finally { ImGui.PopID(); }
        }
    }

    private void DrawCraftBookFilters(IReadOnlyList<RecipeDefinition> recipes)
    {
        var craftTypes = recipes.Select(recipe => recipe.CraftType)
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToArray();
        if (!craftTypes.Contains(craftBookFilter, StringComparer.Ordinal)) craftBookFilter = string.Empty;

        ImGui.TextDisabled(localization.Get("RecipeUi.Book"));
        ImGui.PushFont(UiBuilder.IconFont);
        try
        {
            if (ImGui.Button($"{FontAwesomeIcon.BookOpen.ToIconString()}##CraftBookAll", new Vector2(34, 30)))
                craftBookFilter = string.Empty;
        }
        finally { ImGui.PopFont(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(localization.Get("RecipeUi.AllJobs"));

        foreach (var craftType in craftTypes)
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
                var cursor = ImGui.GetCursorScreenPos();
                var job = recipes.First(recipe => recipe.CraftType == craftType);
                ImGui.SetCursorScreenPos(buttonPosition + new Vector2(3));
                // Native framed class icons: 62100 + ClassJob ID, crafters 8..15.
                if (job.CraftTypeId < 8)
                    DrawItemIcon(new ItemDefinition(0, craftType, false, 62108 + job.CraftTypeId), new Vector2(32));
                ImGui.SetCursorScreenPos(cursor);
                if (selected) ImGui.PopStyleColor();
                if (clicked) craftBookFilter = craftType;
                if (buttonHovered) ImGui.SetTooltip(craftType);
            }
            finally { ImGui.PopID(); }
        }
        ImGui.Spacing();
    }

    private void DrawCraftBook(IReadOnlyList<RecipeDefinition> recipes, string source)
    {
        DrawCraftBookFilters(recipes);
        ImGui.TextColored(Accent, string.IsNullOrWhiteSpace(craftBookFilter) ? localization.Get("RecipeUi.AllJobs") : craftBookFilter);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##CraftBookQuery", localization.Get("RecipeUi.Query"), ref craftBookQuery, 128);
        var matches = recipes.Where(recipe => (string.IsNullOrWhiteSpace(craftBookFilter) || recipe.CraftType == craftBookFilter)
                && (string.IsNullOrWhiteSpace(craftBookQuery)
                    || (itemCatalog.State.Catalog?.Find(recipe.ResultItemId)?.Name.Contains(craftBookQuery.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)))
            .OrderBy(recipe => recipe.CraftTypeId).ThenBy(recipe => recipe.Level).ThenBy(recipe => recipe.RecipeId).ToArray();
        if (matches.Length == 0)
        {
            ImGui.TextDisabled(localization.Get("RecipeUi.NoMatch"));
            return;
        }
        if (!matches.Any(recipe => recipe.RecipeId == craftBookRecipe)) craftBookRecipe = matches[0].RecipeId;
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
                    foreach (var recipe in matches)
                    {
                        ImGui.PushID((int)recipe.RecipeId);
                        try
                        {
                            var result = itemCatalog.State.Catalog?.Find(recipe.ResultItemId);
                            DrawItemIcon(result, new Vector2(28));
                            var clickedIcon = ImGui.IsItemClicked();
                            ImGui.SameLine();
                            if (ImGui.Selectable($"{result?.Name ?? recipe.ResultItemId.ToString()}##Recipe", craftBookRecipe == recipe.RecipeId,
                                    ImGuiSelectableFlags.None, new Vector2(0, 30)) || clickedIcon)
                                craftBookRecipe = recipe.RecipeId;
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip(localization.Format("RecipeUi.JobLevel",recipe.CraftType,recipe.Level));
                        }
                        finally { ImGui.PopID(); }
                    }
            }
            finally { ImGui.EndChild(); }
            ImGui.TableNextColumn();
            var details = ImGui.BeginChild("CraftBookDetails", new Vector2(0, height), false);
            try
            {
                if (details)
                {
                    var selected = matches.First(recipe => recipe.RecipeId == craftBookRecipe);
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
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(localization.Get("RecipeUi.IngredientHint"));
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
            recipeHoldingsBank?.Entries ?? [], StorageFilter.All, includeCrystals: true);
        recipeHoldingsCache[itemId] = summary;
        return summary;
    }

    private string FormatRecipeHoldings(ItemHoldingsSummary summary, int required)
    {
        if (!summary.HasObservation) return localization.Get("RecipeUi.UnknownStock");
        var value = $"NQ {summary.Nq:N0} · HQ {summary.Hq:N0}";
        if (summary.HasUnknown) return localization.Format("RecipeUi.Partial",value);
        var missing = Math.Max(0, required - summary.Total);
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
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(localization.Format("ToolDirectory.OpenContext",ToolText(tool.Name)));
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
        if (remember && itemId > 0 && itemId != id) craftHistory.Push((uint)itemId);
        itemId = (int)id;
        if (itemCatalog.State.Catalog is not { } catalog) return;
        var item = catalog.Find(id);
        itemQuery = item?.Name ?? id.ToString();
        searchedCatalog = catalog;
        searchResults = catalog.Search(itemQuery);
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
        if (state.Catalog is not { } catalog)
        {
            DrawCatalogStatus();
            return;
        }
        if (changed || !ReferenceEquals(searchedCatalog, catalog))
        {
            searchResults = catalog.Search(itemQuery);
            searchedCatalog = catalog;
        }
        if (string.IsNullOrWhiteSpace(itemQuery))
        {
            ImGui.TextDisabled(localization.Get("ItemsUi.QueryHelp"));
            return;
        }
        if (searchResults.Count == 0)
        {
            ImGui.TextUnformatted(localization.Get("ItemsUi.Empty"));
            return;
        }
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
                        DrawItemIcon(item, new Vector2(32));
                        if (ImGui.IsItemClicked()) itemId = (int)item.Id;
                        ImGui.SameLine();
                        if (ImGui.Selectable($"{item.Name}##Result", itemId == (int)item.Id,
                                ImGuiSelectableFlags.None, new Vector2(0, 32))) itemId = (int)item.Id;
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(developerMode ? $"{item.Name}\nID: {item.Id}":item.Name);
                    }
                    finally { ImGui.PopID(); }
                }
            }
            finally { ImGui.EndListBox(); }
        }
        if (searchResults.Count == 30) ImGui.TextDisabled(localization.Get("ItemsUi.Limit"));
    }

    private void DrawItemIcon(ItemDefinition? item, Vector2 size)
    {
        if (item is not null && item.IconId != 0
            && textures.TryGetFromGameIcon(new GameIconLookup(item.IconId, false, true), out var shared)
            && shared.TryGetWrap(out var texture, out _) && texture is not null)
        {
            // Borrowed for this UI frame only; ownership remains with Dalamud.
            ImGui.Image(texture.Handle, size);
        }
        else ImGui.Dummy(size);
    }
}
