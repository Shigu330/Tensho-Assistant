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
    /// X-290 (0.1294): the one way to draw a piece's stats or their differences (the hover comparator, the gear list's tooltip).
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
