using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// DEC-023, point 3 (0.336): the airship sectors of the Sea of Clouds (game data) with what unlocks them and the items that may drop there
// (LuminaSupplemental, GPL-3.0, embedded with its notice). No quantity and no rate are published for airships: the loot is a list,
// the value of a voyage stays unknown. Filtering is rebuilt only when the filter, the rank or the catalog changes, not every frame.
// 0.337: the route planner (MainWindow.AirshipPlanner.cs) above the table; a « Choose » column ticks the sectors of a route.
internal sealed partial class MainWindow
{
    internal Func<AirshipCatalog?>? AirshipCatalog { get; set; }
    internal Func<bool>? AirshipCatalogFailed { get; set; }
    private string airshipFilter="";
    private int airshipRank;
    private (AirshipCatalog Catalog,ItemCatalog? Items,string Filter,int Rank)? airshipViewKey;
    private AirshipSector[] airshipView=[];

    private void DrawAirshipSectors()
    {
        var catalog=AirshipCatalog?.Invoke();
        if(catalog is null){ImGui.TextDisabled(localization.Get(AirshipCatalogFailed?.Invoke()==true ? "AirshipSectors.Unavailable":"AirshipSectors.Loading"));return;}
        ImGui.TextWrapped(localization.Get("AirshipSectors.Help"));
        DrawAirshipPlanner(catalog);
        ImGui.Separator();
        ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("##AirshipFilter",localization.Get("AirshipSectors.Filter"),ref airshipFilter,64);
        Tip("AirshipSectors.FilterTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);
        ImGui.InputInt(localization.Get("AirshipSectors.Rank")+"##AirshipRank",ref airshipRank);
        Tip("AirshipSectors.RankTip");
        airshipRank=Math.Clamp(airshipRank,0,200);
        var items=itemCatalog.State.Catalog;
        var key=(catalog,items,airshipFilter,airshipRank);
        if(airshipViewKey!=key)
        {
            var filter=airshipFilter.Trim();
            airshipView=catalog.Sectors.Where(sector=>airshipRank==0 || sector.RankReq<=airshipRank)
                .Where(sector=>filter.Length==0 || sector.Name.Contains(filter,StringComparison.CurrentCultureIgnoreCase)
                    || catalog.Drops.GetValueOrDefault(sector.Id)?.Any(id=>items?.Find(id)?.Name.Contains(filter,StringComparison.CurrentCultureIgnoreCase)==true)==true)
                .ToArray();
            airshipViewKey=key;
        }
        // 0.337 (review): at least ten rows high, even when the planner above takes most of the pane (the pane then scrolls).
        var tableHeight=Math.Max(ImGui.GetTextLineHeightWithSpacing()*10,ImGui.GetContentRegionAvail().Y-ImGui.GetTextLineHeightWithSpacing()*2);
        if(!ImGui.BeginTable("AirshipSectors",8,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.ScrollY,new Vector2(0,tableHeight)))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableSetupColumn(localization.Get("AirshipSectors.ColumnChoose"),ImGuiTableColumnFlags.WidthFixed);
            foreach(var column in new[]{"Sector","Rank","Tanks","Survey","Exp","Unlock","Loot"})ImGui.TableSetupColumn(localization.Get("AirshipSectors.Column"+column));
            ImGui.TableHeadersRow();
            foreach(var sector in airshipView)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawAirshipChoice(sector);
                ImGui.TableNextColumn();ImGui.TextUnformatted(sector.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(sector.RankReq.ToString());
                ImGui.TableNextColumn();ImGui.TextUnformatted(sector.CeruleumTanks.ToString());
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{sector.SurveyMinutes/60} h {sector.SurveyMinutes%60:00}");
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("AirshipSectors.SurveyTip"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(sector.Exp>0 ? sector.Exp.ToString("N0"):"—");
                ImGui.TableNextColumn();
                var unlock=sector.Unlock;
                var unlockText=unlock is null ? localization.Get("AirshipSectors.UnlockUnknown")
                    :unlock.UnlockedBy==sector.Id ? localization.Get("AirshipSectors.StartSector")
                    :catalog.Find(unlock.UnlockedBy) is { } by
                        ? unlock.Surveillance>0 ? localization.Format("AirshipSectors.UnlockSurveillance",by.ShortName,unlock.Surveillance):by.ShortName
                        :localization.Get("AirshipSectors.UnlockUnknown");
                ImGui.TextUnformatted(unlockText);
                if(unlock is not null && ImGui.IsItemHovered())
                    WrappedTooltip(localization.Format("AirshipSectors.UnlockTip",string.Join(" › ",AirshipSectors.UnlockPath(catalog,sector.Id).Select(id=>catalog.Find(id)?.ShortName ?? id.ToString()))));
                ImGui.TableNextColumn();
                var drops=catalog.Drops.GetValueOrDefault(sector.Id);
                if(drops is not { Count:>0 }){ImGui.TextDisabled(localization.Get("AirshipSectors.LootNone"));continue;}
                ImGui.TextUnformatted(localization.Format("AirshipSectors.LootCount",drops.Count));
                if(ImGui.IsItemHovered())DrawAirshipLootTooltip(drops,items);
            }
        }
        finally{ImGui.EndTable();}
        ImGui.TextDisabled(localization.Get("AirshipSectors.Source"));
    }

    private void DrawAirshipLootTooltip(IReadOnlyList<uint> drops,ItemCatalog? items)
    {
        ImGui.BeginTooltip();
        try
        {
            ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("AirshipSectors.LootTitle"));
            foreach(var id in drops.Take(30))
            {
                var definition=items?.Find(id);
                DrawItemIcon(definition,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();
                ImGui.TextUnformatted(definition?.Name ?? $"#{id}");
            }
        }
        finally{ImGui.EndTooltip();}
    }
}
