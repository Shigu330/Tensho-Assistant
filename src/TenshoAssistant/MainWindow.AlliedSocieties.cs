using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;
using TenshoAssistant.Societies;

namespace TenshoAssistant;

// W-10 (0.713): the allied societies, rank and reputation, read when the page opens and on the player's click. docs/evidence/0.713-allied-societies.md.
internal sealed partial class MainWindow
{
    internal Func<AlliedCatalog?>? AlliedCatalogLoad { get; set; }
    internal Func<AlliedCatalog,Task<AlliedSnapshot>>? ReadAllied { get; set; }
    private AlliedCatalog? alliedCatalog;
    private bool alliedTried,alliedFailed;
    private Task<AlliedSnapshot>? alliedRead;
    private AlliedSnapshot? allied;

    private void DrawAlliedSocieties()
    {
        Heading(localization.Get("Allied.Title"),localization.Get("Allied.Subtitle"));
        if(!alliedTried)
        {
            alliedTried=true;
            alliedCatalog=AlliedCatalogLoad?.Invoke();
            if(alliedCatalog is { Societies.Count:>0 } first && ReadAllied is not null)alliedRead=ReadAllied(first);
        }
        if(alliedCatalog is not { Societies.Count:>0 } catalog){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Allied.SheetsUnavailable"));return;}
        if(alliedRead is { IsCompleted:true } read)
        {
            alliedRead=null;
            alliedFailed=!read.IsCompletedSuccessfully;
            allied=read.IsCompletedSuccessfully ? read.Result:null;
        }
        ImGui.BeginDisabled(ReadAllied is null || alliedRead is not null);
        try{if(ImGui.Button(localization.Get("Allied.Read")))alliedRead=ReadAllied!(catalog);}
        finally{ImGui.EndDisabled();}
        Tip("Allied.ReadTip");
        if(alliedFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Allied.Unavailable"));return;}
        if(allied is not { } current)return;
        switch(current.Status)
        {
            case AlliedReadStatus.NotLoaded:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Allied.NotLoaded"));return;
            case AlliedReadStatus.Unavailable:ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Allied.Unavailable"));return;
        }
        if(current.AllowancesLeft is { } left)ImGui.TextUnformatted(localization.Format("Allied.Allowances",left));
        RefreshRecipeHoldingsCache();
        if(!ImGui.BeginTable("###AlliedTable",6,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnSociety","ColumnExpansion","ColumnPlace","ColumnRank","ColumnReputation","ColumnCurrency"})ImGui.TableSetupColumn(localization.Get("Allied."+column));
            ImGui.TableHeadersRow();
            var items=itemCatalog.State.Catalog;var size=new Vector2(ImGui.GetTextLineHeight());
            foreach(var society in catalog.Societies)
            {
                var standing=current.Standings.GetValueOrDefault(society.Row);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(society.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(society.Expansion);
                ImGui.TableNextColumn();ImGui.TextUnformatted(society.Place);
                if(maps.State.Catalog?.Find(society.MapId) is { } map)PlaceMenu($"AlliedPlace{society.Row}",map,CoordinateOf(map,new Vector3(society.X,0,society.Z)),society.Name);
                ImGui.TableNextColumn();
                if(standing.Rank<=0)ImGui.TextDisabled(localization.Get("Allied.NoRank"));
                else if(AlliedBoard.AtMax(society,standing))ImGui.TextColored(Success,AlliedBoard.RankName(catalog.RankNames,standing.Rank));
                else ImGui.TextUnformatted(AlliedBoard.RankName(catalog.RankNames,standing.Rank));
                ImGui.TableNextColumn();
                if(standing.Rank>0 && standing.Needed>0)ImGui.TextUnformatted($"{standing.Reputation:N0} / {standing.Needed:N0}");
                else if(standing.Rank>0)ImGui.TextDisabled(standing.Reputation.ToString("N0"));
                ImGui.TableNextColumn();
                if(items?.Find(society.Currency) is { } currency)
                {
                    DrawItemIcon(currency,size);
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(currency.Id);
                    ImGui.SameLine();
                    var held=GetRecipeHoldings(currency.Id);
                    ImGui.TextUnformatted(held.HasObservation ? held.Total.ToString("N0"):"?");
                }
            }
        }
        finally{ImGui.EndTable();}
    }
}
