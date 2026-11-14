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
    // X-332 (0.1327, review of 0.1318 I4, R07): the catalogue (with X-265's walk of the whole Quest sheet) read off the frame, like the FATEs' and lexicon's.
    private Task<AlliedCatalog?>? alliedLoad;
    private AlliedSnapshot? allied;

    private void DrawAlliedSocieties()
    {
        Heading(localization.Get("Allied.Title"),localization.Get("Allied.Subtitle"));
        if(!alliedTried && alliedLoad is null && AlliedCatalogLoad is { } load)alliedLoad=Task.Run(load);
        if(alliedLoad is { IsCompleted:true } loaded)
        {
            alliedLoad=null;
            alliedTried=true;
            alliedCatalog=loaded.IsCompletedSuccessfully ? loaded.Result:null;
            if(alliedCatalog is { Societies.Count:>0 } first && ReadAllied is not null)alliedRead=ReadAllied(first);
        }
        if(alliedLoad is not null){ImGui.TextDisabled(localization.Get("Allied.Loading"));return;}
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
        if(allied is not { } current){ReadInGameNote();return;}
        switch(current.Status)
        {
            case AlliedReadStatus.NotLoaded:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Allied.NotLoaded"));return;
            case AlliedReadStatus.Unavailable:ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Allied.Unavailable"));return;
        }
        if(current.AllowancesLeft is { } left)ImGui.TextUnformatted(localization.Format("Allied.Allowances",left));
        // X-265 (0.1306): one kind of society (combat, crafting, gathering: the game's names, ClassJobCategory), kept; the Namazu under both of theirs.
        var kindsKnown=catalog.KindNames.Count>0 && catalog.Societies.Any(society=>society.Kind!=AlliedKind.None);
        var chosen=kindsKnown ? (AlliedKind)configuration.AlliedKindFilter:AlliedKind.None;
        if(kindsKnown)
            ComboSetting("Allied.Kind","Allied.KindTip",()=>configuration.AlliedKindFilter,value=>configuration.AlliedKindFilter=value,
                [(0,localization.Get("Allied.KindAll")),..AlliedKinds.Shown.Select(kind=>((int)kind,AlliedKindName(catalog,kind)))],0,200);
        RefreshRecipeHoldingsCache();
        if(!ImGui.BeginTable("###AlliedTable",7,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnSociety","ColumnKind","ColumnExpansion","ColumnPlace","ColumnRank","ColumnReputation","ColumnCurrency"})ImGui.TableSetupColumn(localization.Get("Allied."+column));
            ImGui.TableHeadersRow();
            var items=itemCatalog.State.Catalog;var size=new Vector2(ImGui.GetTextLineHeight());
            foreach(var society in catalog.Societies)
            {
                if(!AlliedKinds.Listed(society,chosen))continue;
                var standing=current.Standings.GetValueOrDefault(society.Row);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(society.Name);
                ImGui.TableNextColumn();
                if(AlliedKinds.Names(society.Kind,catalog.KindNames) is { Length:>0 } kindName)ImGui.TextUnformatted(Capitalized(kindName));
                else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();ImGui.TextUnformatted(society.Expansion);
                ImGui.TableNextColumn();ImGui.TextUnformatted(society.Place);
                if(maps.State.Catalog?.Find(society.MapId) is { } map)PlaceMenu($"AlliedPlace{society.Row}",map,CoordinateOf(map,new Vector3(society.X,0,society.Z)),society.Name);
                ImGui.TableNextColumn();
                if(standing.Rank<=0)ImGui.TextDisabled(localization.Get("Allied.NoRank"));
                else if(AlliedBoard.AtMax(society,standing))ImGui.TextColored(Success,AlliedBoard.RankName(catalog.RankNames,standing.Rank));
                else ImGui.TextUnformatted(AlliedBoard.RankName(catalog.RankNames,standing.Rank));
                ImGui.TableNextColumn();
                if(standing.Rank>0 && standing.Needed>0)ImGui.TextUnformatted($"{(standing.Reputation).ToString("N0",Numbers)} / {(standing.Needed).ToString("N0",Numbers)}");
                else if(standing.Rank>0)ImGui.TextDisabled(standing.Reputation.ToString("N0",Numbers));
                ImGui.TableNextColumn();
                if(items?.Find(society.Currency) is { } currency)
                {
                    DrawItemIcon(currency,size);
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(currency.Id);
                    ImGui.SameLine();
                    var held=GetRecipeHoldings(currency.Id);
                    ImGui.TextUnformatted(held.HasObservation ? held.Total.ToString("N0",Numbers):"?");
                }
            }
        }
        finally{ImGui.EndTable();}
    }

    // X-265 (0.1306): a kind's name as the game writes it (« artisans »), with a capital in the list.
    private static string AlliedKindName(AlliedCatalog catalog,AlliedKind kind)=>Capitalized(catalog.KindNames.GetValueOrDefault(kind) ?? kind.ToString());
    private static string Capitalized(string text)=>text.Length==0 ? text:char.ToUpper(text[0],System.Globalization.CultureInfo.InvariantCulture)+text[1..];
}
