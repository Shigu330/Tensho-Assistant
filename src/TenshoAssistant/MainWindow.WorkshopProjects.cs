using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// C22 (0.664): the materials of the workshop's projects, part by part and phase by phase, against the stocks read. What the workshop has already received is not read.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<WorkshopProject>>? WorkshopProjectsLoad { get; set; }
    private Task<IReadOnlyList<WorkshopProject>>? workshopProjects;
    private bool workshopFailureLogged;
    private string workshopSearch="";
    private string? workshopSearched;
    private WorkshopProject[] workshopShown=[];
    private uint workshopSelected;

    private string WorkshopName(WorkshopProject project)=>itemCatalog.State.Catalog?.Find(project.ResultItem)?.Name ?? localization.Format("BankUi.Item",project.ResultItem);

    private void DrawWorkshopProjects()
    {
        ImGui.TextWrapped(localization.Get("FreeCompany.WorkshopHelp"));
        if(WorkshopProjectsLoad is not { } load)return;
        workshopProjects??=Task.Run(load);
        if(!workshopProjects.IsCompleted){ImGui.TextDisabled(localization.Get("FreeCompany.WorkshopLoading"));return;}
        if(!workshopProjects.IsCompletedSuccessfully)
        {
            if(!workshopFailureLogged){workshopFailureLogged=true;log.Warning(workshopProjects.Exception,"The workshop projects could not be read.");}
            ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("FreeCompany.WorkshopFailed"));
            return;
        }
        if(itemCatalog.State.Catalog is null){ImGui.TextDisabled(localization.Get("FreeCompany.WorkshopLoading"));return;}
        // 0.676 (user report): without this the stock column only knew what another page had read, so it showed a dash for everything.
        RefreshRecipeHoldingsCache();
        var projects=workshopProjects.Result;
        if(workshopSelected==0)workshopSelected=configuration.WorkshopPanelProject;
        ImGui.SetNextItemWidth(260);
        ImGui.InputTextWithHint("##WorkshopSearch",localization.Get("FreeCompany.WorkshopSearch"),ref workshopSearch,100);
        Tip("FreeCompany.WorkshopSearchTip");
        if(workshopSearched!=workshopSearch)
        {
            workshopSearched=workshopSearch;
            workshopShown=projects.Where(project=>workshopSearch.Length==0 || WorkshopName(project).Contains(workshopSearch,StringComparison.CurrentCultureIgnoreCase) || project.Type.Contains(workshopSearch,StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(project=>project.Type,StringComparer.CurrentCulture).ThenBy(WorkshopName,StringComparer.CurrentCulture).ToArray();
        }
        if(ImGui.BeginChild("WorkshopList",new Vector2(0,170),true))
            foreach(var project in workshopShown)
                if(ImGui.Selectable($"{WorkshopName(project)}  ·  {project.Type}##Workshop{project.Id}",workshopSelected==project.Id))workshopSelected=project.Id;
        ImGui.EndChild();
        if(projects.FirstOrDefault(project=>project.Id==workshopSelected) is not { } selected){ImGui.TextDisabled(localization.Get("FreeCompany.WorkshopPick"));return;}
        ImGui.TextColored(Accent,WorkshopName(selected));
        if(configuration.WorkshopPanelProject!=selected.Id){configuration.WorkshopPanelProject=selected.Id;SaveReminders();}
        BoolSetting("FreeCompany.WorkshopPanel","FreeCompany.WorkshopPanelTip",()=>configuration.WorkshopPanel,value=>configuration.WorkshopPanel=value,c=>c.WorkshopPanel);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("FreeCompany.WorkshopAll")+"###WorkshopAll"))
            DrawWorkshopMaterials("WorkshopTotals",selected.Totals().Select(total=>(total.Item,(string?)null,total.Total)));
        var partNumber=0;
        foreach(var part in selected.Parts)
        {
            partNumber++;
            if(!TenshoAssistant.UI.UiComponents.Header($"{(part.Name.Length>0 ? part.Name:partNumber.ToString())}###WorkshopPart{partNumber}"))continue;
            foreach(var phase in part.Phases)
            {
                ImGui.TextDisabled(localization.Format("FreeCompany.WorkshopPhase",phase.Number));
                DrawWorkshopMaterials($"WorkshopPhase{partNumber}_{phase.Number}",phase.Materials.Select(material=>(material.Item,(string?)localization.Format("FreeCompany.WorkshopSets",material.PerSet,material.Sets),material.Total)));
            }
        }
    }

    private void DrawWorkshopMaterials(string id,IEnumerable<(uint Item,string? Sets,int Total)> materials)
    {
        if(!ImGui.BeginTable(id,4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("FreeCompany.WorkshopMaterial"));
            ImGui.TableSetupColumn(localization.Get("FreeCompany.WorkshopSetsColumn"));
            ImGui.TableSetupColumn(localization.Get("FreeCompany.WorkshopNeed"));
            ImGui.TableSetupColumn(localization.Get("FreeCompany.WorkshopHave"));
            ImGui.TableHeadersRow();
            foreach(var (item,sets,total) in materials)
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();
                ItemCell(itemCatalog.State.Catalog?.Find(item),item);
                ImGui.TableNextColumn();ImGui.TextDisabled(sets ?? "");
                ImGui.TableNextColumn();ImGui.TextUnformatted(total.ToString("N0"));
                ImGui.TableNextColumn();
                var stock=GetRecipeHoldings(item);
                if(!stock.HasObservation)ImGui.TextDisabled("—");
                else if(WorkshopStock.Missing(total,stock.Total)>0)ImGui.TextColored(UI.FfxivTheme.Warning,stock.Total.ToString("N0"));
                else ImGui.TextUnformatted(stock.Total.ToString("N0"));
            }
        }
        finally{ImGui.EndTable();}
    }
}
