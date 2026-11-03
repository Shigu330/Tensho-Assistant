using Dalamud.Bindings.ImGui;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// N-01 (0.440): under « Hunt bills » in Reminders, each held bill with its targets (name, place, kills / needed) and « Map » (the
// Tensho map on the target's zone; the game data gives the zone and place, not a position, so no flag).
internal sealed partial class MainWindow
{
    internal Func<uint,IReadOnlyList<HuntOrderTarget>>? HuntOrderTargets { get; set; }
    internal Func<byte,string>? HuntBillName { get; set; }
    // N-02 (0.443): a Wondrous Tails task's name (WeeklyBingoOrderData: the duty for type 0, else its text), game's language.
    internal Func<uint,string>? BingoTaskName { get; set; }

    private void DrawHuntBills(IReadOnlyList<HuntBillProgress>? held)
    {
        if(held is null || HuntOrderTargets is not { } order || HuntBillName is not { } bill)return;
        foreach(var view in HuntBills.Views(held,order,bill))
        {
            ImGui.TextColored(view.Done ? UI.FfxivTheme.Success:Accent,view.Done ? localization.Format("Reminders.HuntBillDone",view.Bill):view.Bill);
            foreach(var target in view.Targets)
            {
                ImGui.PushID($"Hunt{view.MarkIndex}/{target.Target.Slot}");
                try
                {
                    ImGui.TextColored(target.Done ? UI.FfxivTheme.Success:Muted,localization.Format("Reminders.HuntTarget",target.Target.Name,target.Target.Place,target.Killed,target.Target.Needed)
                        +(target.Target.FateId!=0 ? " · "+localization.Get("Reminders.HuntFate"):""));
                    if(!target.Done && target.Target.MapId!=0){ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Reminders.HuntMap")))ShowMapOnTenshoMap(target.Target.MapId);}
                }
                finally{ImGui.PopID();}
            }
        }
    }
}
