using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Island;

namespace TenshoAssistant;

// N-10 (0.671): the island's workshops, on the player's click, once the agenda has been opened in the game. Estimates: docs/evidence/0.671-island-workshop.md.
internal sealed partial class MainWindow
{
    internal Func<IslandCatalog?>? IslandCatalogLoad { get; set; }
    internal Func<IslandCatalog,Task<IslandSnapshot>>? ReadIsland { get; set; }
    private IslandCatalog? islandCatalog;
    private bool islandCatalogTried,islandReadFailed;
    private Task<IslandSnapshot>? islandRead;
    private IslandSnapshot? island;
    private IslandRow[] islandRows=[];

    private void DrawIslandSanctuary()
    {
        Heading(localization.Get("Island.Title"),localization.Get("Island.Subtitle"));
        if(!islandCatalogTried){islandCatalogTried=true;islandCatalog=IslandCatalogLoad?.Invoke();}
        if(islandCatalog is not { Valid:true } catalog){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Island.SheetsUnavailable"));return;}
        ImGui.TextWrapped(localization.Get("Island.Help"));
        if(islandRead is { IsCompleted:true } read)
        {
            islandRead=null;
            islandReadFailed=!read.IsCompletedSuccessfully;
            island=read.IsCompletedSuccessfully ? read.Result:null;
            islandRows=island is { Status:IslandReadStatus.Loaded } ? IslandValue.Rows(catalog,island):[];
        }
        ImGui.BeginDisabled(ReadIsland is null || islandRead is not null);
        try{if(ImGui.Button(localization.Get("Island.Read")))islandRead=ReadIsland!(catalog);}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Island.ReadTip"));
        if(islandReadFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Island.Unavailable"));return;}
        if(island is not { } current)return;
        switch(current.Status)
        {
            case IslandReadStatus.NotOnIsland:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Island.NotOnIsland"));return;
            case IslandReadStatus.NotLoaded:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Island.NotLoaded"));return;
            case IslandReadStatus.Unavailable:ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Island.Unavailable"));return;
        }
        if(IslandValue.Stale(current.ReadAt,DateTimeOffset.UtcNow))ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Island.Stale"));
        ImGui.TextUnformatted(localization.Format("Island.State",current.Rank,current.CycleDay+1,current.Groove));
        if(current.GrooveAgenda is { } agenda && current.GrooveManager is { } manager && agenda!=manager)
            ImGui.TextDisabled(localization.Format("Island.GrooveDiffers",agenda,manager));
        ImGui.TextUnformatted(localization.Format("Island.Workshops",current.Workshops.Count==0 ? localization.Get("Island.NoWorkshop")
            :string.Join(", ",current.Workshops.Select(item=>localization.Format(item.UnderConstruction ? "Island.WorkshopBuilding":"Island.Workshop",item.Index+1,item.Level)))));
        ImGui.TextDisabled(localization.Get("Island.Estimate"));
        DrawIslandTable(catalog);
    }

    private void DrawIslandTable(IslandCatalog catalog)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("Island.Crafts",islandRows.Length)+"###IslandCrafts",ImGuiTreeNodeFlags.DefaultOpen))return;
        if(!ImGui.BeginTable("###IslandTable",6,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.ScrollY|ImGuiTableFlags.Resizable,new Vector2(0,Math.Max(ImGui.GetContentRegionAvail().Y,240))))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var key in new[]{"Island.ColumnCraft","Island.ColumnHours","Island.ColumnThemes","Island.ColumnPopularity","Island.ColumnSupply","Island.ColumnValue"})
                ImGui.TableSetupColumn(localization.Get(key));
            ImGui.TableHeadersRow();
            var clipper=ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(islandRows.Length);
                while(clipper.Step())
                    for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                    {
                        var row=islandRows[index];
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();ImGui.TextUnformatted(row.Craft.Name);
                        ImGui.TableNextColumn();ImGui.TextUnformatted(row.Craft.Hours.ToString(System.Globalization.CultureInfo.CurrentCulture));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(string.Join(", ",row.Craft.Themes.Select(theme=>catalog.ThemeNames.GetValueOrDefault(theme) ?? "#"+theme)));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(row.Popularity is { } popularity ? localization.Get("Island.Popularity"+popularity):"—");
                        ImGui.TableNextColumn();ImGui.TextUnformatted((row.Supply is { } supply ? localization.Get("Island.Supply"+supply):"—")+(row.Demand is { } demand ? " · "+localization.Get("Island.Demand"+demand):""));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(row.Value is { } value ? "~"+value.ToString("0",System.Globalization.CultureInfo.CurrentCulture):"—");
                    }
                clipper.End();
            }
            finally{clipper.Destroy();}
        }
        finally{ImGui.EndTable();}
    }
}
