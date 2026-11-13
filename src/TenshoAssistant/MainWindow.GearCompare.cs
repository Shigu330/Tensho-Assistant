using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// g, M-07 (0.377): under the game's item tooltip, the hovered gear against the worn piece(s) of its slot: item level and base stats
// (no materia on either side), the gains in green and the losses in red.
internal sealed partial class MainWindow
{
    private IReadOnlyList<GearComparison> HoverComparisons(uint id,bool hq)
        =>configuration.HoverShowCompare && GearCatalog?.Invoke() is { } catalog && catalog.Find(id) is { } piece
            ? GearCompare.Compare(piece,hq,EquippedGear?.Invoke() ?? [],catalog.Find,GearStatOrder):[];

    /// <summary>
    /// X-294 (0.1295, user request of 14/11 about 0:20, « the stats must show everywhere »): the gear part of the common item tooltip, for
    /// every page (worn piece, storage, items, market, loot, NPC vendors…): the piece's base stats (HQ as known, else as « assume HQ »), then
    /// the comparison with the worn piece(s) of its slot, drawn as under the game's tooltip (DrawStatPairs, DrawHoverComparisons). Nothing for
    /// an item that is not gear or while the gear catalogue is not loaded. X-290 (0.1294) drew it in the Gear page's list only.
    /// </summary>
    private void DrawItemGearStats(uint id,bool glamourOnly,bool? knownHq)
    {
        if(GearCatalog?.Invoke() is not { } catalog || catalog.Find(id) is not { } piece)return;
        if(glamourOnly){ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("Gear.GlamourOnlyTip"));return;}
        var hq=(knownHq ?? configuration.GearAssumeHq) && piece.CanBeHq;
        var stats=GearCompare.BaseStats(piece,hq,GearStatOrder);
        if(stats.Count>0)
        {
            TenshoAssistant.UI.UiComponents.SubTitle(localization.Get(hq ? "Gear.TipStatsHq":"Gear.TipStats"));
            DrawStatPairs("GearTipStats"+id,stats.Select(entry=>(entry.Param,entry.Value.ToString("N0",Numbers),Text)));
        }
        var comparisons=GearCompare.Compare(piece,hq,EquippedGear?.Invoke() ?? [],catalog.Find,GearStatOrder);
        if(comparisons.Count>0)DrawHoverComparisons(comparisons);
        else if(stats.Count>0)ImGui.TextDisabled(localization.Get("HoverUi.CompareNote"));
    }

    private void DrawHoverComparisons(IReadOnlyList<GearComparison> comparisons)
    {
        ImGui.TextColored(Accent,localization.Get("HoverUi.Compare"));
        foreach(var comparison in comparisons)
        {
            ImGui.TextDisabled(localization.Format("HoverUi.CompareWith",comparison.Worn.Name+(comparison.WornHq ? " HQ":"")));
            var level=comparison.ItemLevelDelta;
            ImGui.TextColored(level>0 ? Success:level<0 ? UI.FfxivTheme.Error:Muted,localization.Format("HoverUi.CompareLevel",(level>0 ? "+":"")+level));
            if(comparison.Deltas.Count==0){ImGui.TextDisabled(localization.Get("HoverUi.CompareSame"));continue;}
            DrawStatPairs("HoverCompare"+comparison.Worn.Id,comparison.Deltas.Take(8)
                .Select(entry=>(entry.Param,$"{(entry.Delta>0 ? "+":"")}{entry.Delta.ToString("N0",Numbers)}",entry.Delta>0 ? Success:UI.FfxivTheme.Error)));
        }
        ImGui.TextDisabled(localization.Get("HoverUi.CompareNote"));
    }

    /// <summary>
    /// 0.462 (user request, in-game campaign): stats two by two, as in the game's tooltip; side by side on one line they wrapped letter by letter.
    /// X-290 (0.1294): the one way to draw a piece's stats or their differences (the hover comparator, the item tooltip's gear part).
    /// </summary>
    private void DrawStatPairs(string id,IEnumerable<(int Param,string Value,System.Numerics.Vector4 Color)> stats)
    {
        if(!ImGui.BeginTable(id,4,ImGuiTableFlags.SizingFixedFit))return;
        try
        {
            foreach(var (param,value,color) in stats)
            {
                ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("Gear.Param"+param));
                ImGui.TableNextColumn();ImGui.TextColored(color,value);
            }
        }
        finally{ImGui.EndTable();}
    }
}
