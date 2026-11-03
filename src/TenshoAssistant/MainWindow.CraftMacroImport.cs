using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// A-08 (0.425): « Import a macro » in the simulator — pasted text only (macro lines « /ac … »), names matched to the job's actions in
// the game's language; the sequence replaces the current one. Online rotation links are not read (see G-PROPOSALS).
internal sealed partial class MainWindow
{
    private string craftImportText="";
    private string? craftImportMessage;

    private void DrawCraftMacroImport(CraftActionCatalog catalog,uint job)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftImport.Title")+"###CraftImport"))return;
        ImGui.InputTextMultiline("###CraftImportText",ref craftImportText,CraftMacroImport.MaxText,new System.Numerics.Vector2(-1,120));
        Tip("CraftImport.TextTip");
        if(ImGui.SmallButton(localization.Get("CraftImport.Import")))
        {
            var names=CraftActionCatalog.Specs.Select(spec=>catalog.Find(job,spec.Kind)).OfType<CraftActionMetadata>()
                .GroupBy(action=>action.Name.Trim(),StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>group.First().Spec.Kind,StringComparer.OrdinalIgnoreCase);
            var result=CraftMacroImport.Parse(craftImportText,name=>names.TryGetValue(name,out var kind) ? kind:null);
            if(result.Actions.Count>0){simulationActions.Clear();simulationActions.AddRange(result.Actions);simulationResult=null;simulationError=null;}
            craftImportMessage=localization.Format("CraftImport.Result",result.Actions.Count,result.Ignored)
                +(result.Unknown.Count>0 ? " "+localization.Format("CraftImport.Unknown",string.Join(", ",result.Unknown.Take(6))):"");
        }
        if(craftImportMessage is { } message)ImGui.TextWrapped(message);
    }
}
