using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// g, M-10 (0.379): Free company › History: the voyages read in the workshop (VoyageHistoryStore, 180 days), the real yield of each
// route (average per voyage, apart from the community figures of the Routes tab), the last voyages, and a CSV copy to the clipboard.
internal sealed partial class MainWindow
{
    internal VoyageHistoryStore? VoyageHistorySource { get; set; }
    private string? voyageHistoryMessage;

    private void DrawVoyageHistory()
    {
        ImGui.TextWrapped(localization.Get("VoyageHistory.Help"));
        if(VoyageHistorySource is not { } source)return;
        if(source.Unavailable){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("VoyageHistory.Unavailable"));return;}
        var records=source.Records;
        if(records.Count==0){ImGui.TextDisabled(localization.Get("VoyageHistory.Empty"));return;}
        var items=itemCatalog.State.Catalog;
        string Name(uint id)=>items?.Find(id)?.Name ?? $"#{id}";
        if(ImGui.SmallButton(localization.Get("VoyageHistory.Copy")))
        {
            ImGui.SetClipboardText(VoyageHistory.Csv(records,Name));
            voyageHistoryMessage=localization.Format("VoyageHistory.Copied",records.Count);
        }
        Tip("VoyageHistory.CopyTip");
        if(voyageHistoryMessage is { } message){ImGui.SameLine();ImGui.TextDisabled(message);}
        if(TenshoAssistant.UI.UiComponents.Header(localization.Format("VoyageHistory.Routes",records.Count)+"###VoyageRoutes",ImGuiTreeNodeFlags.DefaultOpen)
           && ImGui.BeginTable("VoyageRoutes",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))
        {
            try
            {
                foreach(var key in new[]{"Route","Voyages","Items"})ImGui.TableSetupColumn(localization.Get("VoyageHistory.Column"+key));
                ImGui.TableHeadersRow();
                foreach(var route in VoyageHistory.ByRoute(records).Take(40))
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    ImGui.TextWrapped(route.Route.Length>0 ? route.Route:localization.Get("FreeCompany."+route.Kind));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(route.Voyages.ToString("N0",Numbers));
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(string.Join(" · ",route.Items.Take(5).Select(item=>$"{Name(item.ItemId)} ×{(item.PerVoyage).ToString("0.#",Numbers)}"))
                        +(route.Items.Count>5 ? localization.Format("HoverUi.More",route.Items.Count-5):""));
                }
            }
            finally{ImGui.EndTable();}
        }
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("VoyageHistory.Last")+"###VoyageLast")
           && ImGui.BeginTable("VoyageLast",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))
        {
            try
            {
                foreach(var key in new[]{"Returned","Vessel","Route","Items"})ImGui.TableSetupColumn(localization.Get("VoyageHistory.Column"+key));
                ImGui.TableHeadersRow();
                foreach(var record in records.OrderByDescending(record=>record.RegisteredAt).Take(50))
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    ImGui.TextUnformatted((record.ReturnAt ?? record.RegisteredAt).ToLocalTime().ToString("g"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted($"{record.Vessel} ({record.Rank})");
                    ImGui.TableNextColumn();ImGui.TextWrapped(string.Join(" → ",record.Route));
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(string.Join(" · ",record.Loot.GroupBy(loot=>loot.ItemId).Select(group=>$"{Name(group.Key)} ×{(group.Sum(loot=>loot.Count)).ToString("N0",Numbers)}")));
                }
            }
            finally{ImGui.EndTable();}
        }
    }
}
