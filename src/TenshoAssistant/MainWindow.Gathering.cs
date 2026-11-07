using TenshoAssistant.Maps;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Gathering details of the selected item (item sheet › Gathering, right side of the Gathering page): item, Eorzea clock and its tools; for
// a fish, GatherBuddy's « How to catch it » first (MainWindow.GatheringFish.cs); every gathering point by type with level, zone, place, ET
// window and countdown (GatherBuddy's window for fish, 0.230), alarm (fish: GatherBuddy weather conditions, 0.253–0.254), Tensho map
// centred on the point with the flag, zone weather; then the fishing log, Garland sources and the node observation. Points come from the
// game sheets (GatheringPoint, GatheringItem, FishingSpot…) through GatheringCatalog, loaded in the background and cached.
internal sealed partial class MainWindow
{
    private void OpenGathering(uint id)
    {
        SelectItem(id);
        page = Page.Items;
        gatheringTypeFilter = null;
        focusGatherTab = true;
    }

    private void DrawGathering()
    {
        Heading(localization.Get("GatheringDetails.Title"),localization.Get("GatheringDetails.Description"));
        if (itemId <= 0)
        {
            DrawGatheringClock();
            DrawFishingPanelButton();
            ImGui.TextWrapped(localization.Get("GatheringDetails.Select"));
            DrawGatheringObservation();
            return;
        }
        // 0.230: item, clock and the other modules for it (same useful tools as the context item panel) first.
        DrawItemIcon(SelectedItem, new Vector2(40));
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.TextColored(Accent, SelectedItem?.Name ?? $"ID {itemId}");
        DrawGatheringClock();
        ImGui.EndGroup();
        DrawContextTools();
        DrawItemCollectable((uint)itemId);
        var state = gatheringCatalog.State;
        if (state.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(state.Error is null ? localization.Format("GatheringBrowser.Loading",state.Copied,state.Total):localization.Get("GatheringBrowser.Error"));
            if(developerMode && state.Error is { } error)ImGui.TextWrapped(RawMessage(error));
            return;
        }
        // Fish: GatherBuddy first, then the places; the game's log and Garland stay below as other sources.
        var gatherBuddyShown = DrawGatherBuddyFish(catalog);
        if (!gatherBuddyShown) DrawFishingSources(catalog);
        var sources = catalog.ForItem((uint)itemId);
        if (sources.Count == 0)
        {
            ImGui.TextWrapped(localization.Get("GatheringDetails.Empty"));
            ImGui.TextDisabled(localization.Get("GatheringDetails.Coverage"));
            return;
        }
        if (gatheringTypeFilter is { } filter && !sources.Any(source => source.TypeId == filter)) gatheringTypeFilter = null;
        if (ImGui.SmallButton(localization.Get("GatheringDetails.All")+"###AllGatheringTypes")) gatheringTypeFilter = null;
        Tip("GatheringDetails.AllTip");
        foreach (var type in sources.DistinctBy(source => source.TypeId))
        {
            ImGui.SameLine();
            ImGui.PushID((int)type.TypeId);
            try
            {
                DrawItemIcon(new ItemDefinition(0, type.TypeName, false, type.IconId), new Vector2(30));
                if (ImGui.IsItemClicked()) gatheringTypeFilter = type.TypeId;
                if (ImGui.IsItemHovered()) WrappedTooltip(type.TypeName);
            }
            finally { ImGui.PopID(); }
        }
        ImGui.Spacing();
        ImGui.TextColored(Accent, gatheringTypeFilter is { } selected ? sources.First(source => source.TypeId == selected).TypeName : localization.Get("GatheringDetails.All"));
        if (ImGui.BeginTable("GatheringSources", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("GatheringDetails.Type"));
                ImGui.TableSetupColumn(localization.Get("GatheringDetails.Level"));
                ImGui.TableSetupColumn(localization.Get("GatheringDetails.Zone"));
                ImGui.TableSetupColumn(localization.Get("GatheringDetails.Place"));
                ImGui.TableSetupColumn(localization.Get("GatheringDetails.Times"));
                ImGui.TableSetupColumn(localization.Get("GatheringDetails.Map"));
                ImGui.TableHeadersRow();
                foreach (var source in sources.Where(source => gatheringTypeFilter is null || source.TypeId == gatheringTypeFilter))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.TextWrapped(source.IsHidden ? localization.Format("GatheringDetails.Hidden",source.TypeName) : source.TypeName);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(source.Level.ToString());
                    ImGui.TableNextColumn(); ImGui.TextWrapped(source.Zone);
                    ImGui.TableNextColumn(); ImGui.TextWrapped(string.IsNullOrWhiteSpace(source.Place) ? localization.Get("GatheringDetails.UnknownPlace") : source.Place);
                    ImGui.TableNextColumn();
                    // Fishing rows: GatherBuddy's ET window (or « any time ») instead of the unknown schedule of the game data (0.230).
                    // 0.253–0.254: fish rows take the alarm of GatherBuddy's conditions (ET window and weather), like « How to fish it ».
                    if (source.IsFishing && source.Timing.Kind != Gathering.GatheringScheduleKind.Limited && gatherBuddyFish?.State.Data?.Find(source.ItemId) is { } gbFish)
                    {
                        if (gbFish.StartMinute is { } gbStart && gbFish.EndMinute is { } gbEnd)
                            DrawGatheringTime(Gathering.GatheringSchedule.FromEphemeral((ushort)(gbStart/60*100+gbStart%60),(ushort)(gbEnd/60%24*100+gbEnd%60)));
                        else ImGui.TextDisabled(localization.Get("GatheringFish.AnyTime"));
                        GatherBuddyFishAlarmButton($"Gather{source.PointId}/{source.ItemId}",gbFish,source.TerritoryId,$"{SelectedItem?.Name ?? itemId.ToString()} · {(string.IsNullOrWhiteSpace(source.Place) ? source.Zone:source.Place)}");
                    }
                    else
                    {
                        DrawGatheringTime(source.Timing);
                        DrawGatheringAlarmButton($"Gather{source.IsFishing}/{source.PointId}/{source.ItemId}",source);
                    }
                    ImGui.TableNextColumn();
                    ImGui.PushID($"{source.IsFishing}/{source.PointId}/{source.ItemId}");
                    try
                    {
                        ImGui.BeginDisabled(source.MapId == 0);
                        try
                        {
                            // Tensho map (0.118): centred on the point with the flag when its position is known, else the whole map.
                            if (ImGui.SmallButton(localization.Get("GatheringDetails.ZoneMap")))
                            {
                                if (!source.IsFishing && mapLocal?.State.Index?.Gathering(source.BaseId) is { } spot && maps.State.Catalog?.Find(source.MapId) is { } spotMap)
                                    ShowOnTenshoMap(spotMap, new Vector3(spot.X, 0, spot.Z));
                                else ShowMapOnTenshoMap(source.MapId);
                            }
                        }
                        finally { ImGui.EndDisabled(); }
                        if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("GatheringDetails.MapHelp"));
                        // 0.397 (g, R-03): « Go there » — flag on the point, Tensho map on it, nearest aetheryte's teleport question (click only).
                        var goTo=GatheringPointPixel(source);
                        ImGui.BeginDisabled(goTo is null);
                        try
                        {
                            if(ImGui.SmallButton(localization.Get("GatheringDetails.GoThere")) && goTo is { } target)
                            {
                                // 0.412 (g, R-10): the job's gear set too, when the setting asks for it.
                                if(configuration.GoThereEquip)EquipGatheringGearset(source);
                                AskNearestAetheryteAt(target.Map,target.Pixel,string.IsNullOrWhiteSpace(source.Place) ? source.Zone:source.Place,flag:true,direct:configuration.GoThereTeleport);
                            }
                        }
                        finally { ImGui.EndDisabled(); }
                        if (goTo is { } goSpot) PlaceMenu("GoTherePlace", goSpot.Map, MapProjection.CoordinateFromPixel(goSpot.Pixel, goSpot.Map), string.IsNullOrWhiteSpace(source.Place) ? source.Zone : source.Place);
                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) WrappedTooltip(localization.Get(goTo is null ? "GatheringDetails.GoThereUnknown":configuration.GoThereTeleport ? "GatheringDetails.GoThereDirectTip":"GatheringDetails.GoThereTip"));
                        DrawGatheringGearset(source);
                        if(ImGui.SmallButton(localization.Get("GatheringDetails.ZoneWeather"))) OpenWeather(source.TerritoryId,source.Zone);
                        // 0.405 (user request): the game's gathering log, opened on this item (not for fishing: no game function found).
                        if(!source.IsFishing)
                        {
                            if(ImGui.SmallButton(localization.Get("GameLogs.Gathering"))) OpenGameGatherable(source.ItemId);
                            if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("GameLogs.GatheringTip"));
                        }
                    }
                    finally { ImGui.PopID(); }
                }
            }
            finally { ImGui.EndTable(); }
        }
        ImGui.Spacing();
        ImGui.TextWrapped(localization.Get("GatheringDetails.Disclaimer"));
        if (ImGui.Button(localization.Get("GatheringDetails.Copy")))
            ImGui.SetClipboardText(string.Join(Environment.NewLine,
                sources.Where(source => gatheringTypeFilter is null || source.TypeId == gatheringTypeFilter)
                    .Select(source => $"{SelectedItem?.Name ?? itemId.ToString()} — {source.TypeName}, {localization.Format("GatheringPanel.Level",source.Level)} — {source.Zone} / {source.Place} — {ScheduleText(source.Timing)}")));
        DrawFishingNotebook(catalog);
        if (gatherBuddyShown) DrawFishingSources(catalog);
        DrawGatheringObservation();
    }
}
