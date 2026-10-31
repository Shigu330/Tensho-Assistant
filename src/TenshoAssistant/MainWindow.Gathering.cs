using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using TenshoAssistant.Items;

namespace TenshoAssistant;

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
        DrawGatheringObservation();
        DrawGatheringClock();
        if (itemId <= 0)
        {
            ImGui.TextWrapped(localization.Get("GatheringDetails.Select"));
            return;
        }
        DrawItemIcon(SelectedItem, new Vector2(40));
        ImGui.SameLine();
        ImGui.TextColored(Accent, SelectedItem?.Name ?? $"ID {itemId}");
        var state = gatheringCatalog.State;
        if (state.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(state.Error is null ? localization.Format("GatheringBrowser.Loading",state.Copied,state.Total):localization.Get("GatheringBrowser.Error"));
            if(developerMode && state.Error is { } error)ImGui.TextWrapped(error);
            return;
        }
        DrawFishingNotebook(catalog);
        DrawFishingSources(catalog);
        var sources = catalog.ForItem((uint)itemId);
        if (sources.Count == 0)
        {
            ImGui.TextWrapped(localization.Get("GatheringDetails.Empty"));
            ImGui.TextDisabled(localization.Get("GatheringDetails.Coverage"));
            return;
        }
        if (gatheringTypeFilter is { } filter && !sources.Any(source => source.TypeId == filter)) gatheringTypeFilter = null;
        if (ImGui.SmallButton(localization.Get("GatheringDetails.All")+"###AllGatheringTypes")) gatheringTypeFilter = null;
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
                    ImGui.TableNextColumn(); DrawGatheringTime(source.Timing);
                    DrawGatheringAlarmButton($"Gather{source.IsFishing}/{source.PointId}/{source.ItemId}",source);
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
                        if(ImGui.SmallButton(localization.Get("GatheringDetails.ZoneWeather"))) OpenWeather(source.TerritoryId,source.Zone);
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
                    .Select(source => $"{SelectedItem?.Name ?? itemId.ToString()} — {source.TypeName}, niv. {source.Level} — {source.Zone} / {source.Place} — {source.Timing.Label}")));
    }
}
