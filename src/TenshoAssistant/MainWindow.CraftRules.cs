using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// 0.438 (user request): Settings › Crafting › « Forbidden actions » — one box per crafting action (names in the game's language when
// the catalogue is read); a ticked action is never proposed (search, crafting log assistant and its saved sequences, live synthesis),
// is greyed out in the manual simulator, and an imported or recorded sequence that uses it is said so.
internal sealed partial class MainWindow
{
    private void DrawForbiddenActions(CraftSearchSettings search)
    {
        var forbidden=CraftActionRules.Parse(CraftActionRules.Key(search.ForbiddenActions));
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("CraftRules.Title",forbidden.Count)+"###ForbiddenActions"))return;
        ImGui.TextWrapped(localization.Get("CraftRules.Help"));
        var catalog=craftActions.State.Catalog;var changed=false;
        foreach(var spec in CraftActionCatalog.Specs)
        {
            var name=catalog?.Find(8,spec.Kind)?.Name ?? spec.Kind.ToString();
            var on=forbidden.Contains(spec.Kind);
            if(ImGui.Checkbox($"{name} ({localization.Format("CraftRules.Level",spec.Level)})###Forbid{spec.Kind}",ref on))
            {
                if(on)forbidden=forbidden.Append(spec.Kind).ToHashSet();else forbidden=forbidden.Where(kind=>kind!=spec.Kind).ToHashSet();
                changed=true;
            }
            Tip("CraftRules.ActionTip");
        }
        if(forbidden.Count>0 && ImGui.SmallButton(localization.Get("CraftRules.Clear"))){forbidden=new HashSet<CraftActionKind>();changed=true;}
        if(!changed)return;
        var previous=search.ForbiddenActions;
        search.ForbiddenActions=forbidden.Select(kind=>kind.ToString()).Order(StringComparer.Ordinal).ToList();
        try{save();saveError=null;craftMacroExport=null;craftMacroError=null;}
        catch(Exception error){search.ForbiddenActions=previous;saveError=localization.Get("Common.SaveError");log.Warning(error,"Forbidden crafting actions could not be saved.");}
    }
}
