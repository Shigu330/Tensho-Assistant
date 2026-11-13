using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// I11 (0.594): the Bank as a list beside its grids: one item per line with NQ, HQ, total and its storages, for the same tab and filters.
// Sorted by clicking a column; built again only when the source, the filters or the order change.
internal sealed partial class MainWindow
{
    private readonly record struct BankListKey(BankSourceViewKey Source,string Category,int Quality,string Query,int Sort,bool Descending);
    private BankListKey? bankListKey;
    private BankListRow[] bankListRows=[];
    private int bankListSort;
    private bool bankListDescending;

    private void DrawBankList(BankSourceViewKey source,IReadOnlyList<BankEntry> selected)
    {
        if(!UI.UiComponents.BeginDataTable("BankList",6,sortable:true,scrollHeight:Math.Max(220,ImGui.GetContentRegionAvail().Y)))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableSetupColumn(localization.Get("BankUi.ListItem"),ImGuiTableColumnFlags.DefaultSort|ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("BankUi.Category"),ImGuiTableColumnFlags.WidthStretch,2);
            ImGui.TableSetupColumn(localization.Get("BankUi.Nq"),ImGuiTableColumnFlags.NoSort|ImGuiTableColumnFlags.WidthFixed,50);
            ImGui.TableSetupColumn(localization.Get("BankUi.Hq"),ImGuiTableColumnFlags.NoSort|ImGuiTableColumnFlags.WidthFixed,50);
            ImGui.TableSetupColumn(localization.Get("BankUi.ListTotal"),ImGuiTableColumnFlags.PreferSortDescending|ImGuiTableColumnFlags.WidthFixed,60);
            ImGui.TableSetupColumn(localization.Get("BankUi.ListStorages"),ImGuiTableColumnFlags.NoSort|ImGuiTableColumnFlags.WidthStretch,2);
            ImGui.TableHeadersRow();
            (int Column,bool Descending) sort=(0,false);
            if(ReadSort(ref sort))
            {
                bankListSort=sort.Column switch {1=>(int)BankListSort.Category,4=>(int)BankListSort.Quantity,_=>(int)BankListSort.Name};
                bankListDescending=sort.Descending;
            }
            var key=new BankListKey(source,bankCategoryFilter,bankQualityFilter,bankQuery,bankListSort,bankListDescending);
            if(bankListKey!=key)
            {
                var items=source.Items;
                var quantities=bankSourcePersonal.SelectMany(container=>container.Quantities.Select(quantity=>(localization.Get("BankUi.Character"),quantity.Key.Id.Value,quantity.Key.IsHighQuality,quantity.Value)));
                // X-291 (0.1293): the gil purses are an amount on their own line, not a row of the list.
                if(!source.Personal)quantities=quantities.Concat(selected.SelectMany(entry=>entry.Snapshot.Containers.Values.Where(container=>!InventoryDisplayLayout.IsGil(container.Location.Container.Value))
                    .SelectMany(container=>container.Quantities.Select(quantity=>(BankEntryName(entry),quantity.Key.Id.Value,quantity.Key.IsHighQuality,quantity.Value)))));
                bankListRows=BankList.Sorted(BankList.Merge(quantities).Where(row=>MatchesBankFilters(new(row.ItemId,row.Nq,row.Hq))),(BankListSort)bankListSort,bankListDescending,
                    id=>items?.Find(id)?.Name ?? "",id=>items?.Find(id)?.UiCategoryName ?? "");
                bankListKey=key;
            }
            var rows=bankListRows;
            var clipper=ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(rows.Length);
                while(clipper.Step())
                    for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                    {
                        var row=rows[index];var item=source.Items?.Find(row.ItemId);
                        ImGui.TableNextRow();
                        ImGui.PushID((int)row.ItemId);
                        try
                        {
                            ImGui.TableNextColumn();
                            DrawItemIcon(item,new Vector2(22));
                            if(ImGui.IsItemClicked())SelectItem(row.ItemId);
                            ImGui.SameLine();
                            if(ImGui.Selectable(item?.Name ?? localization.Format("BankUi.Item",row.ItemId),itemId==(int)row.ItemId))SelectItem(row.ItemId);
                            ImGui.TableNextColumn();ImGui.TextDisabled(item?.UiCategoryName ?? "");
                            ImGui.TableNextColumn();ImGui.TextUnformatted(row.Nq.ToString("N0",Numbers));
                            ImGui.TableNextColumn();ImGui.TextUnformatted(row.Hq.ToString("N0",Numbers));
                            ImGui.TableNextColumn();ImGui.TextUnformatted(row.Total.ToString("N0",Numbers));
                            ImGui.TableNextColumn();ImGui.TextDisabled(string.Join(", ",row.Storages));
                            if(ImGui.IsItemHovered())WrappedTooltip(string.Join("\n",row.Storages));
                        }
                        finally{ImGui.PopID();}
                    }
            }
            finally{clipper.End();clipper.Destroy();}
        }
        finally{ImGui.EndTable();}
    }
}
