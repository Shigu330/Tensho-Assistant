using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Deliveries;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// W-01 (0.675): the custom delivery clients, read when the page opens and on the player's click. docs/evidence/0.675-custom-deliveries.md.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<DeliveryClient>?>? DeliveryClientsLoad { get; set; }
    internal Func<IReadOnlyList<DeliveryClient>,Task<DeliverySnapshot>>? ReadDeliveries { get; set; }
    private IReadOnlyList<DeliveryClient>? deliveryClients;
    private bool deliveryClientsTried,deliveryReadFailed;
    private Task<DeliverySnapshot>? deliveryRead;
    private DeliverySnapshot? deliverySnapshot;
    private IReadOnlyList<DeliveryRow> deliveryRows=[];

    private void DrawCustomDeliveries()
    {
        Heading(localization.Get("CustomDeliveries.Title"),localization.Get("CustomDeliveries.Subtitle"));
        if(!deliveryClientsTried)
        {
            deliveryClientsTried=true;
            deliveryClients=DeliveryClientsLoad?.Invoke();
            if(deliveryClients is { Count:>0 } && ReadDeliveries is not null)deliveryRead=ReadDeliveries(deliveryClients);
        }
        if(deliveryClients is not { Count:>0 } clients){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("CustomDeliveries.SheetsUnavailable"));return;}
        ImGui.TextWrapped(localization.Get("CustomDeliveries.Help"));
        if(deliveryRead is { IsCompleted:true } read)
        {
            deliveryRead=null;
            deliveryReadFailed=!read.IsCompletedSuccessfully;
            deliverySnapshot=read.IsCompletedSuccessfully ? read.Result:null;
            deliveryRows=deliverySnapshot is { } snapshot ? DeliveryBoard.Rows(clients,snapshot):[];
        }
        ImGui.BeginDisabled(ReadDeliveries is null || deliveryRead is not null);
        try{if(ImGui.Button(localization.Get("CustomDeliveries.Read")))deliveryRead=ReadDeliveries!(clients);}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("CustomDeliveries.ReadTip"));
        if(deliveryReadFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("CustomDeliveries.Unavailable"));return;}
        if(deliverySnapshot is not { } current)return;
        switch(current.Status)
        {
            case DeliveryReadStatus.NotLoaded:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("CustomDeliveries.NotLoaded"));return;
            case DeliveryReadStatus.Unavailable:ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("CustomDeliveries.Unavailable"));return;
        }
        var reset=ResetSchedule.NextWeekly(current.ReadAt,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour);
        ImGui.TextUnformatted(localization.Format("CustomDeliveries.Week",current.Remaining,reset.ToLocalTime().ToString("g")));
        DrawDeliveryTable();
    }

    private void DrawDeliveryTable()
    {
        if(!ImGui.BeginTable("###CustomDeliveriesTable",6,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("CustomDeliveries.ColumnClient"));
            ImGui.TableSetupColumn(localization.Get("CustomDeliveries.ColumnPlace"));
            ImGui.TableSetupColumn(localization.Get("CustomDeliveries.ColumnLevel"));
            ImGui.TableSetupColumn(localization.Get("CustomDeliveries.ColumnRank"));
            ImGui.TableSetupColumn(localization.Get("CustomDeliveries.ColumnPoints"));
            ImGui.TableSetupColumn(localization.Get("CustomDeliveries.ColumnWeek"));
            ImGui.TableHeadersRow();
            foreach(var row in deliveryRows)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Client.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Client.Place);
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Client.LevelUnlock.ToString());
                ImGui.TableNextColumn();
                if(!row.Unlocked){ImGui.TextDisabled(localization.Get("CustomDeliveries.Locked"));ImGui.TableNextColumn();ImGui.TableNextColumn();continue;}
                ImGui.TextUnformatted(row.Rank.ToString());
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Needed is { } needed ? localization.Format("CustomDeliveries.PointsOf",row.Points,needed):row.Points.ToString());
                ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Format("CustomDeliveries.WeekOf",row.Used,row.Client.PerWeek));
            }
        }
        finally{ImGui.EndTable();}
    }
}
