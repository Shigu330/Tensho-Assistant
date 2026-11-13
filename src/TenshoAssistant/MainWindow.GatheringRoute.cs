using TenshoAssistant.Maps;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-14 (0.416): Crafting › Preparation › chain: « Gathering list by zone » (folded) — the chain's raw materials grouped by the zone
/// that gathers the most of them (GatheringRoutes), with the place, the ET time and « Go there »; the others listed apart. Built once
/// per chain result and catalogue.
/// </summary>
internal sealed partial class MainWindow
{
    private (CraftChainResult Result,GatheringCatalog Catalog)? chainRouteKey;
    private GatheringRoute? chainRoute;

    private void DrawChainGatheringRoute(CraftChainResult result)
    {
        if(result.Materials.Count==0 || gatheringCatalog.State.Catalog is not { } catalog)return;
        if(chainRouteKey is not { } key || !ReferenceEquals(key.Result,result) || !ReferenceEquals(key.Catalog,catalog))
        {
            chainRoute=GatheringRoutes.Build(result.Materials.Where(material=>material.Reason==CraftChainReason.RawMaterial)
                .Select(material=>(material.ItemId,material.Amount)),catalog.ForItem);
            chainRouteKey=(result,catalog);
        }
        if(chainRoute is not { } route || !TenshoAssistant.UI.UiComponents.Header(localization.Format("ChainUi.Route",route.Zones.Count)+"##ChainRoute"))return;
        ImGui.TextWrapped(localization.Get("ChainUi.RouteHelp"));
        foreach(var zone in route.Zones)
        {
            ImGui.Spacing();
            ImGui.TextColored(Accent,localization.Format("ChainUi.RouteZone",zone.Zone,zone.Rows.Count));
            foreach(var row in zone.Rows)
            {
                ImGui.PushID($"Route{zone.TerritoryId}/{row.ItemId}");
                try
                {
                    DrawChainItem(row.ItemId);
                    ImGui.SameLine();
                    ImGui.TextDisabled($"× {(row.Amount).ToString("N0",Numbers)} · {(string.IsNullOrWhiteSpace(row.Source.Place) ? row.Source.Zone:row.Source.Place)} · {ScheduleText(row.Source.Timing)}");
                    if(GatheringPointPixel(row.Source) is { } target)
                    {
                        ImGui.SameLine();
                        if(ImGui.SmallButton(localization.Get("GatheringDetails.GoThere")))
                        {
                            if(configuration.GoThereEquip)EquipGatheringGearset(row.Source);
                            AskNearestAetheryteAt(target.Map,target.Pixel,string.IsNullOrWhiteSpace(row.Source.Place) ? row.Source.Zone:row.Source.Place,flag:true,direct:configuration.GoThereTeleport);
                        }
                        PlaceMenu("RoutePlace",target.Map,MapProjection.CoordinateFromPixel(target.Pixel,target.Map),string.IsNullOrWhiteSpace(row.Source.Place) ? row.Source.Zone:row.Source.Place);
                    }
                }
                finally{ImGui.PopID();}
            }
        }
        if(route.Elsewhere.Count>0)
            ImGui.TextWrapped(localization.Format("ChainUi.RouteElsewhere",string.Join(", ",route.Elsewhere.Take(12)
                .Select(need=>$"{itemCatalog.State.Catalog?.Find(need.ItemId)?.Name ?? $"ID {need.ItemId}"} × {need.Amount:N0}"))
                +(route.Elsewhere.Count>12 ? " …":"")));
    }
}
