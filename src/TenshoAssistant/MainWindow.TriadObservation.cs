using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void DrawTriadObservation()
    {
        ImGui.TextColored(Accent,localization.Get("TriadObserved.Title"));
        var state = triadObservation.State;
        DrawToolSettingsLink("TriadObservation");
        ImGui.TextWrapped(localization.Get("TriadObserved.Help"));
        var status = state.Status switch
        {
            TriadObservationStatus.Disabled => localization.Get("TriadObserved.Disabled"),
            TriadObservationStatus.Waiting => localization.Get("TriadObserved.Waiting"),
            TriadObservationStatus.Offline => localization.Get("TriadObserved.Offline"),
            TriadObservationStatus.AddonAbsent => localization.Get("TriadObserved.Absent"),
            TriadObservationStatus.NotReady => localization.Get("TriadObserved.NotReady"),
            TriadObservationStatus.Error => localization.Get("GatheringObservation.Error"),
            _ => localization.Get("TriadObserved.Ready"),
        };
        ImGui.TextWrapped(status);
        if (state.Frame is not { } frame) return;
        if (state.CharacterId != bankBridge.State.CharacterId)
        { ImGui.TextDisabled(localization.Get("TriadObserved.Changed")); return; }
        var phase = frame.NativePhase switch {0 => localization.Get("TriadObserved.WaitingPhase"),1 => localization.Get("TriadObserved.FreePhase"),2 => localization.Get("TriadObserved.ForcedPhase"),_ => localization.Get("TriadObserved.UnknownPhase")};
        ImGui.TextDisabled(localization.Format("TriadObservedCounts.Phase",phase,state.ObservedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "—"));
        DrawTriadAdviceText();
        if (ImGui.GetContentRegionAvail().X >= 620 && ImGui.BeginTable("ObservedTriadLayout",2,ImGuiTableFlags.SizingFixedFit))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("TriadObserved.Board"),ImGuiTableColumnFlags.WidthFixed,260);
                ImGui.TableSetupColumn(localization.Get("TriadObserved.Hands"),ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow(); ImGui.TableNextColumn();
                DrawObservedTriadGroup(localization.Get("TriadObserved.ObservedBoard"),frame.Board,3);
                ImGui.TableNextColumn();
                DrawObservedTriadGroup(localization.Get("TriadObserved.Blue"),frame.BlueHand,5);
                DrawObservedTriadGroup(localization.Get("TriadObserved.Red"),frame.RedHand,5);
            }
            finally { ImGui.EndTable(); }
        }
        else
        {
            DrawObservedTriadGroup(localization.Get("TriadObserved.ObservedBoard"),frame.Board,3);
            DrawObservedTriadGroup(localization.Get("TriadObserved.Blue"),frame.BlueHand,5);
            DrawObservedTriadGroup(localization.Get("TriadObserved.Red"),frame.RedHand,5);
        }
        ImGui.TextWrapped(localization.Get("TriadObserved.Disclaimer"));
        if (ImGui.SmallButton(localization.Get("TriadObserved.Copy")))
        {
            var text = new StringBuilder().AppendLine(localization.Format("UiFragments.ObservedCopy",frame.Source,state.ObservedAt?.ToLocalTime().ToString("G") ?? "—",frame.NativePhase));
            foreach (var group in new[] {(localization.Get("TriadObserved.Board"),frame.Board),(localization.Get("TriadObserved.Blue"),frame.BlueHand),(localization.Get("TriadObserved.Red"),frame.RedHand)})
                for (var index=0;index<group.Item2.Count;index++) text.AppendLine($"{group.Item1} {index+1}: {ObservedTriadDescription(group.Item2[index])}");
            ImGui.SetClipboardText(text.ToString());
        }
    }
    private void DrawObservedTriadGroup(string label,IReadOnlyList<TriadObservedCard> cards,int columns)
    {
        ImGui.TextColored(Accent,label);
        if (!ImGui.BeginTable(label,columns,ImGuiTableFlags.SizingFixedFit)) return;
        try
        {
            for (var column=0;column<columns;column++) ImGui.TableSetupColumn($"Slot{column}",ImGuiTableColumnFlags.WidthFixed,columns==3?76:54);
            for (var index=0;index<cards.Count;index++)
            {
                ImGui.TableNextColumn();
                var card=cards[index];
                var size=new Vector2(columns==3?68:48,72);
                var start=ImGui.GetCursorScreenPos();
                ImGui.Dummy(size);
                var color=card.Owner switch {TriadPlayer.Blue=>TriadBlue,TriadPlayer.Red=>TriadRed,_=>Muted};
                var draw=ImGui.GetWindowDrawList();
                draw.AddRectFilled(start,start+size,ImGui.GetColorU32(new Vector4(color.X*0.18f,color.Y*0.18f,color.Z*0.18f,0.95f)),4);
                draw.AddRect(start,start+size,ImGui.GetColorU32(color),4);
                void Center(string text,float y)
                    => draw.AddText(start+new Vector2(Math.Max(0,(size.X-ImGui.CalcTextSize(text).X)/2),y),ImGui.GetColorU32(color),text);
                if (card.Presence==TriadObservedPresence.Known)
                {
                    Center(TriadCardDefinition.Value(card.Top),5);
                    draw.AddText(start+new Vector2(5,25),ImGui.GetColorU32(color),TriadCardDefinition.Value(card.Left));
                    var right=TriadCardDefinition.Value(card.Right);
                    draw.AddText(start+new Vector2(size.X-5-ImGui.CalcTextSize(right).X,25),ImGui.GetColorU32(color),right);
                    Center(TriadCardDefinition.Value(card.Bottom),45);
                }
                else Center(card.Presence switch {TriadObservedPresence.Hidden=>"?",TriadObservedPresence.Empty=>"—",_=>"…"},25);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(localization.Format("TriadObservedCounts.Slot",index+1,ObservedTriadDescription(card)));
            }
        }
        finally { ImGui.EndTable(); }
    }
    private string ObservedTriadDescription(TriadObservedCard card)
    {
        var owner=card.Owner switch {TriadPlayer.Blue=>localization.Get("TriadObserved.OwnerBlue"),TriadPlayer.Red=>localization.Get("TriadObserved.OwnerRed"),_=>localization.Get("TriadObserved.UnknownColor")};
        return card.Presence switch
        {
            TriadObservedPresence.Empty=>localization.Get("TriadObserved.Empty"),
            TriadObservedPresence.Hidden=>localization.Format("TriadObservedCounts.Hidden",owner),
            TriadObservedPresence.Incomplete=>localization.Format("TriadObservedCounts.Incomplete",owner),
            _=>localization.Format("UiFragments.ObservedCard",owner,card.Stars,card.TypeId,TriadCardDefinition.Value(card.Top),TriadCardDefinition.Value(card.Right),TriadCardDefinition.Value(card.Bottom),TriadCardDefinition.Value(card.Left)),
        };
    }
}
