using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-11 (0.413): the collectables table on the Gathering page (folded): every item the game takes as a collectable, its turn-in level,
/// low / mid / high collectability and the scrips of each tier (scrip shops only; item rewards are said, not counted), filtered by item
/// or group name; a click opens the item. The item's own thresholds also show on its Gathering view. Data: the game's sheets
/// (docs/evidence/0.409-collectables.md), copied once off the framework thread.
/// </summary>
internal sealed partial class MainWindow
{
    private string collectableQuery="";
    private (CollectableCatalog Catalog,string Query)? collectableViewKey;
    private CollectableRule[] collectableView=[];

    private void DrawCollectableTable()
    {
        if(Collectables is not { } reader)return;
        var catalog=reader.Catalog;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("CollectableTable.Title",catalog?.Count ?? 0)+"###CollectableTable"))return;
        if(catalog is null){ImGui.TextDisabled(localization.Get("CollectablePanel.Loading"));return;}
        ImGui.TextWrapped(localization.Get("CollectableTable.Help"));
        ImGui.SetNextItemWidth(280);
        ImGui.InputTextWithHint("###CollectableQuery",localization.Get("CollectableTable.Search"),ref collectableQuery,80);
        Tip("CollectableTable.SearchTip");
        if(collectableViewKey is not { } key || !ReferenceEquals(key.Catalog,catalog) || key.Query!=collectableQuery)
        {
            collectableView=Gathering.Collectables.Filter(catalog.All,collectableQuery);
            collectableViewKey=(catalog,collectableQuery);
        }
        ImGui.TextDisabled(localization.Format("CollectableTable.Count",collectableView.Length));
        if(collectableView.Length==0)return;
        if(!ImGui.BeginTable("CollectableRows",5,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.ScrollY,new Vector2(0,320)))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableSetupColumn(localization.Get("CollectableTable.Item"),ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("CollectableTable.Group"),ImGuiTableColumnFlags.WidthStretch,2);
            ImGui.TableSetupColumn(localization.Get("CollectableTable.Level"));
            ImGui.TableSetupColumn(localization.Get("CollectableTable.Thresholds"));
            ImGui.TableSetupColumn(localization.Get("CollectableTable.Reward"));
            ImGui.TableHeadersRow();
            var clipper=ImGui.ImGuiListClipper();
            clipper.Begin(collectableView.Length);
            try
            {
            while(clipper.Step())
                for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                {
                    var rule=collectableView[index];
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    if(ImGui.Selectable($"{rule.Name}##Collectable{index}")){SelectItem(rule.ItemId);OpenItemSheet();}
                    ImGui.TableNextColumn();ImGui.TextUnformatted(rule.Group);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(rule.LevelMin==rule.LevelMax ? $"{rule.LevelMin}":$"{rule.LevelMin}–{rule.LevelMax}");
                    ImGui.TableNextColumn();ImGui.TextUnformatted($"{rule.Low} / {rule.Mid} / {rule.High}");
                    ImGui.TableNextColumn();
                    if(rule.Scrips && rule.RewardHigh>0)ImGui.TextUnformatted($"{rule.RewardLow} / {rule.RewardMid} / {rule.RewardHigh}");
                    else ImGui.TextDisabled(localization.Get("CollectableTable.ItemReward"));
                }
            }
            finally{clipper.End();clipper.Destroy();}
        }
        finally{ImGui.EndTable();}
    }

    // The item's own thresholds, on its Gathering view (nothing when the game takes no collectable of it).
    private void DrawItemCollectable(uint id)
    {
        if(Collectables?.Catalog?.For(id) is not { Count:>0 } rules)return;
        foreach(var rule in rules.DistinctBy(rule=>(rule.Low,rule.Mid,rule.High,rule.RewardLow,rule.RewardMid,rule.RewardHigh,rule.Scrips)).Take(3))
        {
            ImGui.TextColored(Accent,localization.Format("CollectablePanel.Thresholds",rule.Low,rule.Mid,rule.High));
            if(rule.Scrips && rule.RewardHigh>0){ImGui.SameLine();ImGui.TextDisabled(localization.Format("CollectablePanel.Scrips",rule.RewardLow,rule.RewardMid,rule.RewardHigh));}
        }
    }
}
