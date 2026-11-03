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
            ImGui.SameLine();
            var level=comparison.ItemLevelDelta;
            ImGui.TextColored(level>0 ? Success:level<0 ? UI.FfxivTheme.Error:Muted,localization.Format("HoverUi.CompareLevel",(level>0 ? "+":"")+level));
            if(comparison.Deltas.Count==0){ImGui.TextDisabled(localization.Get("HoverUi.CompareSame"));continue;}
            var first=true;
            foreach(var (param,delta) in comparison.Deltas.Take(8))
            {
                if(!first)ImGui.SameLine();
                first=false;
                ImGui.TextColored(delta>0 ? Success:UI.FfxivTheme.Error,$"{localization.Get("Gear.Param"+param)} {(delta>0 ? "+":"")}{delta:N0}");
            }
        }
        ImGui.TextDisabled(localization.Get("HoverUi.CompareNote"));
    }
}
