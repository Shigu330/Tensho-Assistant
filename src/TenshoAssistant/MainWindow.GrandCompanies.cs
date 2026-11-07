using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.GrandCompanies;

namespace TenshoAssistant;

// W-11 (0.714): the Grand Companies: grades, seals, the next promotion and what the seals buy. Today's supply missions are not here: the game gives them in its window. docs/evidence/0.714-grand-company.md.
internal sealed partial class MainWindow
{
    internal Func<Task<bool?>>? GcFeminine { get; set; }
    internal Func<bool,GcCatalog?>? GcCatalogLoad { get; set; }
    internal Func<int,Task<GcSnapshot>>? ReadGc { get; set; }
    private GcCatalog? gcCatalog;
    private Task<bool?>? gcGender;
    private Task<GcSnapshot>? gcRead;
    private GcSnapshot? gcSnapshot;
    private bool gcTried,gcFailed,gcMyRank=true;
    private int gcShopCompany;
    private string gcQuery="";

    private void DrawGrandCompanies()
    {
        Heading(localization.Get("GrandCompany.Title"),localization.Get("GrandCompany.Subtitle"));
        // The grades' names depend on the character: read that first, then the sheets, once.
        if(!gcTried && GcFeminine is not null){gcTried=true;gcGender=GcFeminine();}
        if(gcGender is { IsCompleted:true } gender && gcCatalog is null)
        {
            var feminine=gender.IsCompletedSuccessfully && gender.Result==true;
            gcGender=null;
            gcCatalog=GcCatalogLoad?.Invoke(feminine);
            if(gcCatalog is { } loaded && ReadGc is not null)gcRead=ReadGc(loaded.Ranks.Count+1);
        }
        if(gcGender is not null){ImGui.TextDisabled(localization.Get("GrandCompany.Loading"));return;}
        if(gcCatalog is not { Ranks.Count:>0 } catalog){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("GrandCompany.SheetsUnavailable"));return;}
        if(gcRead is { IsCompleted:true } read)
        {
            gcRead=null;
            gcFailed=!read.IsCompletedSuccessfully;
            gcSnapshot=read.IsCompletedSuccessfully ? read.Result:null;
        }
        ImGui.BeginDisabled(ReadGc is null || gcRead is not null);
        try{if(ImGui.Button(localization.Get("GrandCompany.Read")))gcRead=ReadGc!(catalog.Ranks.Count+1);}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("GrandCompany.ReadTip"));
        if(gcFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("GrandCompany.Unavailable"));return;}
        if(gcSnapshot is not { } current)return;
        switch(current.Status)
        {
            case GcReadStatus.NotLoaded:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("GrandCompany.NotLoaded"));return;
            case GcReadStatus.Unavailable:ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("GrandCompany.Unavailable"));return;
        }
        DetailTabs("GrandCompanyTabs",
        [
            ("Grades",localization.Get("GrandCompany.TabGrades"),()=>DrawGcGrades(catalog,current)),
            ("Shop",localization.Get("GrandCompany.TabShop"),()=>DrawGcShop(catalog,current)),
        ],ref gcTabFocus);
    }
    private string? gcTabFocus;

    private void DrawGcGrades(GcCatalog catalog,GcSnapshot current)
    {
        if(current.Company==0)ImGui.TextDisabled(localization.Get("GrandCompany.None"));
        else ImGui.TextColored(Accent,localization.Format("GrandCompany.Current",catalog.Companies.ElementAtOrDefault(current.Company) ?? ""));
        if(!ImGui.BeginTable("###GcCompanies",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            foreach(var column in new[]{"ColumnCompany","ColumnGrade","ColumnSeals","ColumnNext"})ImGui.TableSetupColumn(localization.Get("GrandCompany."+column));
            ImGui.TableHeadersRow();
            for(var company=1;company<=3 && company<=current.Ranks.Count;company++)
            {
                var rank=current.Ranks[company-1];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var name=catalog.Companies.ElementAtOrDefault(company) ?? "";
                if(company==current.Company)ImGui.TextColored(Success,name);else ImGui.TextUnformatted(name);
                ImGui.TableNextColumn();
                var own=GcBoard.Rank(catalog,rank);
                ImGui.TextUnformatted(rank==0 ? localization.Get("GrandCompany.NoGrade"):own?.Names.ElementAtOrDefault(company-1) ?? rank.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(rank==0 ? "":$"{current.Seals[company-1]:N0} / {current.MaxSeals[company-1]:N0}");
                ImGui.TableNextColumn();
                if(rank==0)continue;
                if(GcBoard.Next(catalog,company,rank) is not { } next){ImGui.TextDisabled(localization.Get("GrandCompany.Last"));continue;}
                var quest=next.PromotionQuests.ElementAtOrDefault(company-1) ?? "";
                var text=localization.Format("GrandCompany.Next",next.Names[company-1],(own?.RequiredSeals ?? 0).ToString("N0"));
                ImGui.TextWrapped(quest.Length>0 ? text+" · "+quest:text);
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawGcShop(GcCatalog catalog,GcSnapshot current)
    {
        if(gcShopCompany==0)gcShopCompany=current.Company is >= 1 and <= 3 ? current.Company:1;
        string[] companies=[catalog.Companies.ElementAtOrDefault(1) ?? "",catalog.Companies.ElementAtOrDefault(2) ?? "",catalog.Companies.ElementAtOrDefault(3) ?? ""];
        var picked=gcShopCompany-1;
        ImGui.SetNextItemWidth(240);
        if(ImGui.Combo("###GcShopCompany",ref picked,companies,companies.Length))gcShopCompany=picked+1;
        Tip("GrandCompany.ShopCompanyTip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("GrandCompany.MyRank")+"###GcMyRank",ref gcMyRank);
        Tip("GrandCompany.MyRankTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("###GcQuery",localization.Get("GrandCompany.Search"),ref gcQuery,100);
        Tip("GrandCompany.SearchTip");
        var items=itemCatalog.State.Catalog;
        var rank=gcMyRank && current.Ranks.Count>=gcShopCompany ? current.Ranks[gcShopCompany-1]:(int?)null;
        var rows=GcBoard.Shop(catalog,gcShopCompany,rank).Where(row=>gcQuery.Length==0 || items?.Find(row.Item)?.Name.Contains(gcQuery,StringComparison.CurrentCultureIgnoreCase)==true).ToArray();
        var held=current.Seals.Count>=gcShopCompany ? current.Seals[gcShopCompany-1]:0;
        ImGui.TextDisabled(localization.Format("GrandCompany.ShopCount",rows.Length,held.ToString("N0")));
        if(rows.Length==0 || !ImGui.BeginTable("###GcShop",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.ScrollY,new Vector2(0,Math.Max(160,ImGui.GetContentRegionAvail().Y))))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var column in new[]{"ColumnItem","ColumnCost","ColumnGrade"})ImGui.TableSetupColumn(localization.Get("GrandCompany."+column));
            ImGui.TableHeadersRow();
            var size=new Vector2(ImGui.GetTextLineHeight());
            foreach(var row in rows.Take(400))
            {
                var item=items?.Find(row.Item);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawItemIcon(item,size);
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(row.Item);
                ImGui.SameLine();ImGui.TextUnformatted(item?.Name ?? $"#{row.Item}");
                ImGui.TableNextColumn();
                if(row.Cost<=held)ImGui.TextUnformatted(row.Cost.ToString("N0"));else ImGui.TextColored(UI.FfxivTheme.Warning,row.Cost.ToString("N0"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(GcBoard.Rank(catalog,row.RequiredRank)?.Names.ElementAtOrDefault(gcShopCompany-1) ?? row.RequiredRank.ToString());
            }
        }
        finally{ImGui.EndTable();}
    }
}
