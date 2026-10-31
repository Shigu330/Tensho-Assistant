using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private Guid selectedSortTemplate;
    private string sortTemplateName=string.Empty;
    private InventorySortProfile DrawSortTemplates(InventorySortScope scope,InventorySortProfile draft)
    {
        if(!ImGui.CollapsingHeader(localization.Get("SortUi.Templates")+"###SortTemplates"))return draft;
        ImGui.TextWrapped(localization.Get("SortUi.TemplatesHelp"));
        var templates=(configuration.InventorySortTemplates ?? []).Where(value=>value is not null).ToArray();
        var selected=templates.FirstOrDefault(value=>value.Id==selectedSortTemplate);
        ImGui.SetNextItemWidth(300);
        if(ImGui.BeginCombo(localization.Get("SortUi.Template"),selected?.Name ?? localization.Get("SortUi.ChooseTemplate")))
        {
            try
            {
                foreach(var template in templates)
                    if(ImGui.Selectable($"{template.Name} · {template.ContainerCount} × {template.SlotsPerContainer}##{template.Id}",selectedSortTemplate==template.Id))
                    {selectedSortTemplate=template.Id;sortTemplateName=template.Name ?? string.Empty;selected=template;}
            }
            finally{ImGui.EndCombo();}
        }
        ImGui.BeginDisabled(selected is null || !InventorySortTemplates.Compatible(selected,scope));
        try
        {
            if(ImGui.Button(localization.Get("SortUi.LoadDraft")))
            {
                try{draft=InventorySortTemplates.Load(selected!,scope);sortRulesError=null;}
                catch(Exception error){sortRulesError=error.Message;}
            }
        }
        finally{ImGui.EndDisabled();}
        if(selected is not null && !InventorySortTemplates.Compatible(selected,scope))ImGui.TextWrapped(localization.Get("SortUi.Incompatible"));
        ImGui.SetNextItemWidth(300);ImGui.InputText(localization.Get("SortUi.TemplateName"),ref sortTemplateName,80);
        if(ImGui.Button(localization.Get("SortUi.CreateTemplate")))SaveSortTemplate(scope,draft,null);
        ImGui.BeginDisabled(selected is null);
        try
        {
            ImGui.BeginDisabled(selected is null || !InventorySortTemplates.Compatible(selected,scope));
            try{if(ImGui.Button(localization.Get("SortUi.UpdateTemplate")))SaveSortTemplate(scope,draft,selected!.Id);}
            finally{ImGui.EndDisabled();}
            if(ImGui.Button(localization.Get("SortUi.DeleteTemplate")))
            {
                var previous=configuration.InventorySortTemplates;
                try{configuration.InventorySortTemplates=InventorySortTemplates.Delete(previous,selected!.Id);save();selectedSortTemplate=Guid.Empty;sortRulesError=null;}
                catch(Exception error){configuration.InventorySortTemplates=previous;sortRulesError="SortUi.TemplateDeleteError";log.Warning(error,"Could not delete inventory sort template.");}
            }
        }
        finally{ImGui.EndDisabled();}
        return draft;
    }
    private void SaveSortTemplate(InventorySortScope scope,InventorySortProfile draft,Guid? id)
    {
        var previous=configuration.InventorySortTemplates;
        try
        {
            var template=InventorySortTemplates.Create(draft,scope,sortTemplateName,id);
            configuration.InventorySortTemplates=InventorySortTemplates.Save(previous,template);
            save();selectedSortTemplate=template.Id;sortTemplateName=template.Name;sortRulesError=null;
        }
        catch(Exception error){configuration.InventorySortTemplates=previous;sortRulesError=TenshoAssistant.UI.UiMessages.Key(error.Message) is not null ? error.Message:"SortUi.TemplateSaveError";log.Warning(error,"Could not save inventory sort template.");}
    }
}
