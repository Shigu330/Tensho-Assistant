using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void DrawGatheringObservation()
    {
        if(!ImGui.CollapsingHeader(localization.Get("GatheringObservation.Title")+"###GatheringObservation"))return;
        var state=gatheringObservation.State;var enabled=state.Enabled;
        DrawToolSettingsLink("GatheringObservation");
        if(!enabled)ImGui.TextDisabled(localization.Get("GatheringObservation.Disabled"));
        if(!enabled)return;
        var now=DateTimeOffset.UtcNow;
        if(!state.Fresh(now))
        {
            ImGui.TextWrapped(localization.Get(state.Status switch
            {
                GatheringObservationStatus.Offline=>"GatheringObservation.Offline",
                GatheringObservationStatus.AddonAbsent=>"GatheringObservation.Absent",
                GatheringObservationStatus.NotReady=>"GatheringObservation.NotReady",
                GatheringObservationStatus.Error=>"GatheringObservation.Error",
                _=>"GatheringObservation.Waiting"
            }));
            if(developerMode && state.Error is { } error)ImGui.TextWrapped(error);
            return;
        }
        var frame=state.Frame!;
        ImGui.TextDisabled(localization.Format("GatheringObservation.Summary",localization.Get(frame.JobId==16 ? "GatheringObservation.Miner":"GatheringObservation.Botanist"),frame.Level,frame.Gp,frame.MaxGp,state.ObservedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "—"));
        ImGui.TextWrapped(localization.Format("GatheringObservation.Attempts",frame.Attempts?.ToString() ?? localization.Get("GatheringObservation.Unknown"),frame.TotalAttempts?.ToString() ?? localization.Get("GatheringObservation.Unknown")));
        if(frame.NativeStatus==2)ImGui.TextDisabled(localization.Get("GatheringObservation.Quick"));
        ImGui.TextWrapped(localization.Get("GatheringObservation.Help"));
        if(frame.Slots.Count==0)ImGui.TextDisabled(localization.Get("GatheringObservation.Empty"));
        if(frame.Slots.Count>0 && ImGui.BeginTable("ObservedGatheringRows",4,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("GatheringObservation.Item"),ImGuiTableColumnFlags.WidthStretch,2);
                ImGui.TableSetupColumn(localization.Get("GatheringObservation.Success"));ImGui.TableSetupColumn(localization.Get("GatheringObservation.Boon"));ImGui.TableSetupColumn(localization.Get("GatheringObservation.Quantity"));ImGui.TableHeadersRow();
                foreach(var slot in frame.Slots)
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.PushID(slot.Index);
                    try
                    {
                        var item=itemCatalog.State.Catalog?.Find(slot.ItemId);DrawItemIcon(item,new Vector2(28));var click=ImGui.IsItemClicked();ImGui.SameLine();
                        ImGui.BeginDisabled(slot.ItemId==0);
                        try{click|=ImGui.SmallButton(item?.Name ?? (slot.ItemId==0 ? localization.Get("GatheringObservation.Special"):localization.Format("CraftPresetMessages.Item",slot.ItemId)));}
                        finally{ImGui.EndDisabled();}
                        if(click && slot.ItemId!=0)SelectItem(slot.ItemId);
                        ImGui.TableNextColumn();ImGui.TextUnformatted(slot.SuccessPercent is { } success ? $"{success} %":localization.Get("GatheringObservation.Unknown"));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(slot.BoonPercent is { } boon ? $"{boon} %":localization.Get("GatheringObservation.Unknown"));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(slot.DisplayedQuantity?.ToString() ?? localization.Get("GatheringObservation.Unknown"));
                    }
                    finally{ImGui.PopID();}
                }
            }
            finally{ImGui.EndTable();}
        }
        var resources=state.CopyResources(now);ImGui.BeginDisabled(resources is null);
        try
        {
            if(ImGui.SmallButton(localization.Get("GatheringObservation.Prepare")) && resources is { } copied)
            {
                gpJob=copied.JobId;gpLevel=copied.Level;gpBudget=copied.Budget;gpAttempts=copied.Attempts;
                gpBaseYield=0;gpSuccess=-1;gpBoon=-1;gpBountiful=0;
                gpYieldActive=gpGift1Active=gpGift2Active=gpTidingsActive=gpOtherEffects=false;gpFirstAttemptBonus=0;
                gpCopiedOwner=copied.Owner;gpCopiedAt=copied.ObservedAt;gpResult=null;
                gpError=localization.Get("GpUi.NodeCopied");
                page=Page.GatheringGp;
            }
        }
        finally{ImGui.EndDisabled();}
        if(frame.NativeStatus==2)ImGui.TextDisabled(localization.Get("GatheringObservation.WaitQuick"));
    }
}
