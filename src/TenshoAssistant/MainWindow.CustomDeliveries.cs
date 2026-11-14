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
        if(!ImGui.BeginTabBar("###CustomDeliveriesTabs"))return;
        try
        {
            if(ImGui.BeginTabItem(localization.Get("CustomDeliveries.TabClients")+"###DeliveryClients"))
            {
                try{DrawDeliveryStatus(clients);}
                finally{ImGui.EndTabItem();}
            }
            if(ImGui.BeginTabItem(localization.Get("CustomDeliveries.TabRewards")+"###DeliveryRewards"))
            {
                try{DrawDeliveryRewards(clients);}
                finally{ImGui.EndTabItem();}
            }
        }
        finally{ImGui.EndTabBar();}
    }

    // W-01 (0.730): what a client gives, from the sheets alone: docs/evidence/0.730-delivery-rewards.md.
    private int deliveryRewardClient;
    private void DrawDeliveryRewards(IReadOnlyList<DeliveryClient> clients)
    {
        ImGui.TextWrapped(localization.Get("CustomDeliveries.RewardsHelp"));
        deliveryRewardClient=Math.Clamp(deliveryRewardClient,0,clients.Count-1);
        ImGui.SetNextItemWidth(260*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
        var names=clients.Select(row=>row.Name).ToArray();
        ImGui.Combo(localization.Get("CustomDeliveries.ColumnClient")+"###DeliveryRewardClient",ref deliveryRewardClient,names,names.Length);
        Tip("CustomDeliveries.RewardClientTip");
        var client=clients[deliveryRewardClient];
        if(client.Rewards is not { Count:>0 } rewards){ImGui.TextDisabled(localization.Get("CustomDeliveries.RewardsNone"));return;}
        static string Tiers(DeliveryTiers tiers)=>$"{(tiers.Low).ToString("N0",Numbers)} / {(tiers.Mid).ToString("N0",Numbers)} / {(tiers.High).ToString("N0",Numbers)}";
        if(ImGui.BeginTable("###DeliveryRewardsTable",5,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))
        {
            try
            {
                foreach(var column in new[]{"ColumnRank","ColumnKind","ColumnPoints","ColumnGil","ColumnScrips"})ImGui.TableSetupColumn(localization.Get("CustomDeliveries."+column));
                ImGui.TableHeadersRow();
                foreach(var reward in rewards)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();ImGui.TextUnformatted(reward.Bonus ? localization.Format("CustomDeliveries.RankBonus",reward.Rank,reward.Percent):reward.Rank.ToString());
                    ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get(reward.Crafting ? "CustomDeliveries.KindCraft":"CustomDeliveries.KindGather"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(Tiers(reward.Points));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(Tiers(reward.Gil));
                    ImGui.TableNextColumn();
                    foreach(var currency in reward.Currencies)ImGui.TextUnformatted($"{Tiers(currency.Amount)}  {itemCatalog.State.Catalog?.Find(currency.ItemId)?.Name ?? localization.Format("BankUi.Item",currency.ItemId)}");
                }
            }
            finally{ImGui.EndTable();}
        }
        if(client.Gifts is not { Count:>0 } gifts)return;
        ImGui.Spacing();
        ImGui.TextUnformatted(localization.Get("CustomDeliveries.GiftsTitle"));
        Tip("CustomDeliveries.GiftsTip");
        foreach(var rank in gifts.GroupBy(gift=>gift.Rank).OrderBy(group=>group.Key))
            ImGui.TextWrapped(localization.Format("CustomDeliveries.GiftsRank",rank.Key,string.Join(", ",rank.Select(gift=>localization.Format(gift.Hq ? "CustomDeliveries.GiftHq":"CustomDeliveries.Gift",gift.Count,gift.Name)))));
    }

    private void DrawDeliveryStatus(IReadOnlyList<DeliveryClient> clients)
    {
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
        Tip("CustomDeliveries.ReadTip");
        if(deliveryReadFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("CustomDeliveries.Unavailable"));return;}
        if(deliverySnapshot is not { } current){ReadInGameNote();return;}
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
