using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

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
            var metadata=catalog?.Find(8,spec.Kind);
            var name=metadata?.Name ?? spec.Kind.ToString();
            var on=forbidden.Contains(spec.Kind);
            // 0.457 (user request, in-game campaign): the action's icon, as in the simulator.
            DrawGameIcon(metadata?.Icon ?? 0,new Vector2(24));
            ImGui.SameLine();
            if(ImGui.Checkbox($"{name} ({localization.Format("CraftRules.Level",spec.Level)})###Forbid{spec.Kind}",ref on))
            {
                if(on)forbidden=forbidden.Append(spec.Kind).ToHashSet();else forbidden=forbidden.Where(kind=>kind!=spec.Kind).ToHashSet();
                changed=true;
            }
            Tip("CraftRules.ActionTip");
        }
        if(forbidden.Count>0 && ImGui.SmallButton(localization.Get("CraftRules.Clear"))){forbidden=new HashSet<CraftActionKind>();changed=true;}
        if(!changed)return;
        SaveForbiddenCraftActions(search,forbidden.Select(kind=>kind.ToString()).Order(StringComparer.Ordinal).ToList());
    }

    // X-76 H3 (0.914): the previous list comes back when saving fails.
    private void SaveForbiddenCraftActions(CraftSearchSettings search,List<string> next)
    {
        var previous=search.ForbiddenActions;
        search.ForbiddenActions=next;
        if(TrySave("Forbidden crafting actions",()=>search.ForbiddenActions=previous)){craftMacroExport=null;craftMacroError=null;}
    }
}
