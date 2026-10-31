using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void DrawDiagnostics()
    {
        Heading(localization.Get("DiagnosticsUi.Title"), localization.Get("DiagnosticsUi.Help"));
        ImGui.TextUnformatted(localization.Format("DiagnosticsUi.Build",BuildVersion,configuration.Version));
        DrawContributorHelp();
        DrawMacroObservation();
        ImGui.Spacing();
        ImGui.TextColored(Accent, localization.Get("DiagnosticsUi.Items"));
        DrawCatalogStatus();
        if (itemCatalog.State.Catalog is { } catalog)
        {
            ImGui.TextUnformatted(localization.Format("DiagnosticsUi.Generation",catalog.Metadata.Generation));
            ImGui.TextWrapped(localization.Format("DiagnosticsUi.Source",catalog.Metadata.Source));
            ImGui.TextWrapped(localization.Format("DiagnosticsUi.DataVersion",catalog.Metadata.DataVersion));
        }
        ImGui.Spacing();
        ImGui.TextColored(Accent, localization.Get("DiagnosticsUi.Recipes"));
        var recipeState = recipeCatalog.State;
        if (recipeState.Catalog is { } recipes)
            ImGui.TextColored(Success, localization.Format("DiagnosticsUi.RecipesReady",recipes.Count.ToString("N0"),recipes.Source));
        else if (recipeState.Error is { } recipeError)
            ImGui.TextWrapped(localization.Format("DiagnosticsUi.Error",recipeError));
        else if (recipeState.TotalRows > 0)
            ImGui.TextWrapped(localization.Format("DiagnosticsUi.Copied",recipeState.RowsCopied.ToString("N0"),recipeState.TotalRows.ToString("N0")));
        else
            ImGui.TextDisabled(localization.Get("DiagnosticsUi.RecipeLoading"));
        ImGui.Spacing();
        ImGui.Separator();
        var gathering = gatheringCatalog.State;
        ImGui.TextColored(Accent, localization.Get("DiagnosticsUi.Gathering"));
        var mapState = maps.State;
        ImGui.TextDisabled(mapState.Catalog is { } mapCatalog ? localization.Format("DiagnosticsUi.Maps",mapCatalog.Entries.Count.ToString("N0"))
            : mapState.Error ?? localization.Format("DiagnosticsUi.MapLoading",mapState.Copied.ToString("N0"),mapState.Total.ToString("N0")));
        if (gathering.Catalog is { } gathered)
        {
            ImGui.TextColored(Success, localization.Format("DiagnosticsUi.GatheringReady",gathered.Count.ToString("N0")));
            ImGui.TextWrapped(localization.Format("DiagnosticsUi.Source","GatheringItem → GatheringPointBase → GatheringPoint + FishingSpot ; "+gathered.Source));
        }
        else ImGui.TextWrapped(gathering.Error ?? localization.Format("DiagnosticsUi.GatheringLoading",gathering.Copied.ToString("N0"),gathering.Total.ToString("N0")));
        ImGui.Spacing();
        ImGui.TextColored(Accent, localization.Get("DiagnosticsUi.Services"));
        ImGui.TextDisabled(localization.Format("DiagnosticsUi.CollectionState",collections.State.Entries.Count.ToString("N0"),collections.State.CharacterId,collections.State.Loading));
        if (collections.State.Error is { } collectionError) ImGui.TextWrapped(collectionError);
        var questState = quests.State;
        ImGui.TextDisabled(localization.Format("DiagnosticsUi.QuestState",questState.Entries.Count.ToString("N0"),questState.CharacterId,questState.Loading));
        if (questState.Error is { } questError) ImGui.TextWrapped(questError);
        if (questState.RewardIndexLoading) ImGui.TextDisabled(localization.Get("DiagnosticsUi.RewardsLoading"));
        else if (questState.RewardIndex is { } rewardIndex)
        {
            var coverage = rewardIndex.Coverage;
            ImGui.TextDisabled(localization.Format("DiagnosticsUi.Rewards",rewardIndex.ItemCount.ToString("N0"),coverage.CoveredQuests.ToString("N0"),coverage.TotalQuests.ToString("N0")));
            ImGui.TextDisabled(localization.Format("DiagnosticsUi.Unresolved",coverage.UnresolvedReferences.ToString("N0"),coverage.UnknownQuantityLines.ToString("N0"),coverage.UnversionedQuests.ToString("N0")));
            ImGui.TextDisabled(localization.Get("DiagnosticsUi.RewardHelp"));
        }
        else ImGui.TextDisabled(localization.Get("DiagnosticsUi.NoRewardIndex"));
        if (questState.RewardIndexError is { } rewardIndexError) ImGui.TextWrapped(localization.Format("DiagnosticsUi.RewardError",rewardIndexError));
        ImGui.TextDisabled(localization.Format("DiagnosticsUi.Market",market.State.WorldName,localization.Get(configuration.MarketPricesOnHover ? "DiagnosticsUi.Enabled":"DiagnosticsUi.Disabled")));
        ImGui.TextDisabled(localization.Format("DiagnosticsUi.Hover",localization.Get(configuration.ItemUsagesOnHover ? "DiagnosticsUi.Enabled":"DiagnosticsUi.Disabled"),itemHover.State.Item is { } hovered ? hovered.ItemId.ToString():localization.Get("DiagnosticsUi.Absent")));
        if (market.State.Error is { } marketError) ImGui.TextWrapped(marketError);
        var stats = characterStats.State;
        ImGui.TextDisabled(stats.CharacterId == 0 ? localization.Get("DiagnosticsUi.NoStats") : localization.Format("DiagnosticsUi.Stats",stats.Values.Count,stats.ObservedAt?.ToLocalTime().ToString("T") ?? "—"));
        ImGui.TextDisabled(cactpotSolve.State.Loading ? localization.Get("DiagnosticsUi.CactpotLoading") : localization.Format("DiagnosticsUi.Cactpot",cactpotObservation.State.Enabled ? cactpotObservation.State.Status.ToString():localization.Get("DiagnosticsUi.ManualObservation")));
        ImGui.TextDisabled(localization.Format("DiagnosticsUi.Node",gatheringObservation.State.Enabled ? gatheringObservation.State.Status.ToString():localization.Get("DiagnosticsUi.ObservationDisabled")));
        if(itemUses.State.Catalog is { } uses)ImGui.TextDisabled(localization.Format("DiagnosticsUi.Uses",uses.LeveCount,uses.SupplyItemCount,uses.AquariumItemCount,uses.Language));
        else ImGui.TextDisabled(localization.Format("DiagnosticsUi.UsesLoading",itemUses.State.Copied,itemUses.State.Total));
        if(itemUses.State.Error is { } usageError)ImGui.TextWrapped(usageError);
        var chain = craftChain.State;
        ImGui.TextDisabled(chain.Loading ? localization.Get("DiagnosticsUi.ChainLoading") : chain.Result is { } chainResult
            ? localization.Format("DiagnosticsUi.Chain",chainResult.Steps.Count,chain.CalculatedAt?.ToLocalTime().ToString("T") ?? "—")
            : localization.Get("DiagnosticsUi.ChainManual"));
        if (chain.Error is { } chainError) ImGui.TextWrapped(chainError);
        ImGui.Spacing();
        ImGui.TextColored(Accent, localization.Get("DiagnosticsUi.Bank"));
        var bankState = bank.State;
        ImGui.TextWrapped(bankState.Loading ? localization.Get("DiagnosticsUi.FileLoading")
            : bankState.Error ?? localization.Format("DiagnosticsUi.Saved",bankState.Entries.Count));
        ImGui.TextUnformatted(bankState.Importing ? localization.Get("DiagnosticsUi.ImportWriting") : localization.Get("DiagnosticsUi.ManualRefresh"));
        var menus = bankBridge.State;
        ImGui.TextWrapped(localization.Format("DiagnosticsUi.RetainerWindow",menus.Retainer?.AddonName ?? localization.Get("DiagnosticsUi.None")));
        ImGui.TextWrapped(localization.Format("DiagnosticsUi.ChocoboWindow",menus.Saddlebag?.AddonName ?? localization.Get("DiagnosticsUi.None")));
        if (menus.Message is { } message) ImGui.TextWrapped(message);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(Accent, localization.Get("DiagnosticsUi.Inventory"));
        var snapshot = inventory.Current;
        if (snapshot is null)
        {
            ImGui.TextWrapped(localization.Get("DiagnosticsUi.NoObservation"));
            return;
        }
        ImGui.TextUnformatted(localization.Format("DiagnosticsUi.Observed",snapshot.ObservedAt.ToLocalTime().ToString("G")));
        ImGui.TextWrapped(localization.Format("DiagnosticsUi.Source",snapshot.Source));
        foreach (var container in snapshot.Containers.Values.OrderBy(c => c.Location.Container.Value))
        {
            var state = container.Coverage switch
            {
                InventoryCoverage.Complete => localization.Get("DiagnosticsUi.Complete"),
                InventoryCoverage.Partial => localization.Get("DiagnosticsUi.Partial"),
                _ => localization.Get("DiagnosticsUi.Unknown"),
            };
            var name = container.Location.Container.Value == 2001
                ? localization.Get("DiagnosticsUi.Crystals")
                : localization.Format("DiagnosticsUi.Bag",container.Location.Container.Value+1);
            ImGui.TextUnformatted($"{name} : {state}");
        }
        ImGui.TextWrapped(localization.Get("DiagnosticsUi.Coverage"));
    }
}
