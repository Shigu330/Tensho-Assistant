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
    private Task<IslandAgenda>? islandPlanning;
    private IslandAgenda? islandAgenda;
    private IReadOnlyList<IslandMaterialTotal> islandMaterialTotals=[];
    private string[] islandMaterialTexts=[];

    private void DrawIslandSanctuary()
    {
        Heading(localization.Get("Island.Title"),localization.Get("Island.Subtitle"));
        if(!ImGui.BeginTabBar("IslandTabs"))return;
        try
        {
            if(ImGui.BeginTabItem(localization.Get("Island.TabWorkshop")+"###IslandTabWorkshop")){try{DrawIslandWorkshop();}finally{ImGui.EndTabItem();}}
            if(ImGui.BeginTabItem(localization.Get("Island.TabPasture")+"###IslandTabPasture")){try{DrawIslandPasture();}finally{ImGui.EndTabItem();}}
        }
        finally{ImGui.EndTabBar();}
    }

    private void DrawIslandWorkshop()
    {
        if(!islandCatalogTried){islandCatalogTried=true;islandCatalog=IslandCatalogLoad?.Invoke();}
        if(islandCatalog is not { Valid:true } catalog){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Island.SheetsUnavailable"));return;}
        ImGui.TextWrapped(localization.Get("Island.Help"));
        if(islandRead is { IsCompleted:true } read)
        {
            islandRead=null;
            islandReadFailed=!read.IsCompletedSuccessfully;
            island=read.IsCompletedSuccessfully ? read.Result:null;
            islandRows=island is { Status:IslandReadStatus.Loaded } ? IslandValue.Rows(catalog,island):[];
            islandMaterialTexts=islandRows.Select(row=>string.Join(", ",(row.Craft.Materials ?? []).Select(item=>localization.Format("Island.MaterialAmount",item.Name,item.Amount)))).ToArray();
            islandAgenda=null;
        }
        ImGui.BeginDisabled(ReadIsland is null || islandRead is not null);
        try{if(ImGui.Button(localization.Get("Island.Read")))islandRead=ReadIsland!(catalog);}
        finally{ImGui.EndDisabled();}
        Tip("Island.ReadTip");
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
        DrawIslandAgenda(catalog,current);
        DrawIslandTable(catalog);
    }

    // N-10 (0.672): a proposal to copy into the game's agenda by hand; off the framework and off Draw.
    private void DrawIslandAgenda(IslandCatalog catalog,IslandSnapshot current)
    {
        if(islandPlanning is { IsCompleted:true } planning)
        {
            islandPlanning=null;
            islandAgenda=planning.IsCompletedSuccessfully ? planning.Result:null;
            islandMaterialTotals=islandAgenda is { } planned ? IslandMaterials.Totals(planned):[];
        }
        ImGui.BeginDisabled(islandPlanning is not null);
        try{if(ImGui.Button(localization.Get("Island.Propose")))islandPlanning=Task.Run(()=>IslandPlanner.Propose(catalog,current));}
        finally{ImGui.EndDisabled();}
        Tip("Island.ProposeTip");
        if(islandAgenda is not { } agenda)return;
        if(agenda.Chains.Count==0){ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Island.NoAgenda"));return;}
        ImGui.SameLine();
        CopyButton(localization.Get("Island.Copy"),()=>IslandAgendaText(agenda));
        Tip("Island.CopyTip");
        foreach(var line in IslandAgendaText(agenda).Split('\n'))ImGui.TextUnformatted(line);
        ImGui.TextDisabled(localization.Get("Island.AgendaLimits"));
        DrawIslandMaterials(agenda);
    }

    // N-07 (0.673): what this agenda uses, from the game's sheets; the island's stocks are not read.
    private void DrawIslandMaterials(IslandAgenda agenda)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("Island.Materials",islandMaterialTotals.Count)+"###IslandMaterials",ImGuiTreeNodeFlags.None))return;
        if(!IslandMaterials.Complete(agenda))ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Island.MaterialsIncomplete"));
        ImGui.TextDisabled(localization.Get("Island.MaterialsLimits"));
        if(islandMaterialTotals.Count==0)return;
        CopyButton(localization.Get("Island.MaterialsCopy"),()=>string.Join("\n",islandMaterialTotals.Select(item=>localization.Format("Island.MaterialAmount",item.Name,item.Amount))));
        if(!ImGui.BeginTable("###IslandMaterialsTable",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Island.ColumnMaterial"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnCategory"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnAmount"));
            ImGui.TableHeadersRow();
            foreach(var item in islandMaterialTotals)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(item.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(item.Category);
                ImGui.TableNextColumn();ImGui.TextUnformatted(item.Amount.ToString(Numbers));
            }
        }
        finally{ImGui.EndTable();}
    }

    private string IslandAgendaText(IslandAgenda agenda)
    {
        var culture=Numbers;
        var lines=new List<string>();
        foreach(var chain in agenda.Chains)
        {
            lines.Add(localization.Format("Island.Workshop",chain.Workshop+1,chain.Level));
            lines.AddRange(chain.Steps.Select(step=>"  "+localization.Format(step.Efficient ? "Island.StepEfficient":"Island.Step",step.Start,step.Craft.Name,step.Craft.Hours,step.Value.ToString("0",culture))));
        }
        lines.Add(localization.Format("Island.AgendaTotal",agenda.Total.ToString("0",culture)));
        lines.Add(localization.Format("Island.AgendaGroove",agenda.StartGroove,agenda.EndGroove,agenda.GrooveCap));
        return string.Join("\n",lines);
    }

    private void DrawIslandTable(IslandCatalog catalog)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("Island.Crafts",islandRows.Length)+"###IslandCrafts",ImGuiTreeNodeFlags.DefaultOpen))return;
        if(!ImGui.BeginTable("###IslandTable",7,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.ScrollY|ImGuiTableFlags.Resizable,new Vector2(0,Math.Max(ImGui.GetContentRegionAvail().Y,240))))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var key in new[]{"Island.ColumnCraft","Island.ColumnHours","Island.ColumnThemes","Island.ColumnPopularity","Island.ColumnSupply","Island.ColumnValue","Island.ColumnMaterials"})
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
                        ImGui.TableNextColumn();ImGui.TextUnformatted(row.Craft.Hours.ToString(Numbers));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(string.Join(", ",row.Craft.Themes.Select(theme=>catalog.ThemeNames.GetValueOrDefault(theme) ?? "#"+theme)));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(row.Popularity is { } popularity ? localization.Get("Island.Popularity"+popularity):"—");
                        ImGui.TableNextColumn();ImGui.TextUnformatted((row.Supply is { } supply ? localization.Get("Island.Supply"+supply):"—")+(row.Demand is { } demand ? " · "+localization.Get("Island.Demand"+demand):""));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(row.Value is { } value ? "~"+value.ToString("0",Numbers):"—");
                        ImGui.TableNextColumn();ImGui.TextUnformatted(index<islandMaterialTexts.Length ? islandMaterialTexts[index]:"");
                    }
                clipper.End();
            }
            finally{clipper.Destroy();}
        }
        finally{ImGui.EndTable();}
    }
}
