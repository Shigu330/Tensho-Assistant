using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>
    /// X-684 (0.1694, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of a gathering point or a
    /// fishing spot (the Tensho map's list of gathering points, with or without a position): its game icon, the one body (DrawGatheringBody),
    /// then what the caller adds, else what a click does (« flag on the map ») or that the game gives no position.
    /// </summary>
    private void DrawGatheringTooltip(MapLocalGatheringPoint point,string? after=null)
    {
        using(TooltipScope.Begin())
        {
            if(point.Icon!=0){DrawGameIcon(point.Icon,new Vector2(32));ImGui.SameLine();}
            DrawGatheringBody(point,GatheringTips.MaxItems);
            ImGui.Separator();
            ImGui.TextDisabled(after ?? localization.Get(point.Position is null ? "MapLocal.NoPosition":"MapLocal.ClickFlag"));
        }
    }

    /// <summary>
    /// X-684 (0.1694): the body of a gathering point's tooltip, also drawn for each point among the icons under the cursor on the Tensho map
    /// (DrawIconListTooltip, with its own icon beside it; before, a copy by hand without the job, the kind or how many items were left out):
    /// title (kind of gathering, level, hidden), job and kind of point, place, whether it is open now and for how long, the folklore tome or
    /// the level it asks for (DrawPointAvailability, read live), the items it gives with their icons (at most <paramref name="max"/>, then how
    /// many more).
    /// </summary>
    private void DrawGatheringBody(MapLocalGatheringPoint point,int max)
    {
        var (facts,defs)=GatheringTipOf(point);
        ImGui.BeginGroup();
        TooltipTitle(facts.Title);
        if(facts.Detail is { } detail)ImGui.TextDisabled(detail);
        if(facts.Place is { } place)ImGui.TextDisabled(place);
        ImGui.EndGroup();
        DrawPointAvailability(point);
        var side=new Vector2(ImGui.GetTextLineHeight());
        for(var index=0;index<facts.Items.Count && index<max;index++)
        {
            DrawItemIcon(defs[index],side);ImGui.SameLine();
            ImGui.TextUnformatted(facts.Items[index].Name);
        }
        if(GatheringTips.More(facts,max) is > 0 and var more)ImGui.TextDisabled(localization.Format("MapView.HoverMore",more));
    }

    // X-684 (0.1694): the facts of each point drawn in a tooltip, built once per point (the icons under the cursor may hold several), language
    // and item catalogue, not every frame; at most 64 kept.
    private readonly Dictionary<MapLocalGatheringPoint,(GatheringTipFacts Facts,Items.ItemDefinition?[] Items)> gatheringTips=new(ReferenceEqualityComparer.Instance);
    private (int Revision,object? Items) gatheringTipsFor=(-1,null);
    private (GatheringTipFacts Facts,Items.ItemDefinition?[] Items) GatheringTipOf(MapLocalGatheringPoint point)
    {
        var catalog=itemCatalog.State.Catalog;
        if(gatheringTipsFor.Revision!=localization.Revision || !ReferenceEquals(gatheringTipsFor.Items,catalog) || gatheringTips.Count>=64)
        {gatheringTips.Clear();gatheringTipsFor=(localization.Revision,catalog);}
        if(gatheringTips.TryGetValue(point,out var known))return known;
        var title=$"{point.Type} · {localization.Format("MapLocal.Level",point.Level)}"+(point.Hidden ? " "+localization.Get("MapLocal.Hidden"):"");
        var job=jobName?.Invoke(point.Job) is { Length:>0 } ? JobName(point.Job):null;
        var facts=GatheringTips.Build(title,[job,PointKindText(point)],point.Place,point.Items.Select(id=>(id,catalog?.Find(id)?.Name ?? $"#{id}")));
        var tip=(facts,facts.Items.Select(item=>catalog?.Find(item.Id)).ToArray());
        gatheringTips[point]=tip;
        return tip;
    }
}
