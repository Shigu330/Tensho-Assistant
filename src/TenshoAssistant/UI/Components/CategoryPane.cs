using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-449: a list of categories on the left and the chosen category on the right, as the game's system configuration shows its pages. The choice
// is kept by key by the caller; a key no longer in the list falls back to the first category.
internal sealed partial class MainWindow
{
    private void CategoryPane(string id,IReadOnlyList<(string Key,string Label,Action Draw)> categories,ref string? selected)
    {
        var wanted=selected;
        var current=categories.FirstOrDefault(category=>category.Key==wanted);
        if(current.Draw is null)current=categories[0];
        if(ImGui.BeginChild(id+"List",new Vector2(190*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale,0),true))
        {
            try{foreach(var category in categories)if(TenshoAssistant.UI.UiComponents.SelectableRow(category.Label+"##"+id+category.Key,category.Key==current.Key))current=category;}
            finally{ImGui.EndChild();}
        }
        else ImGui.EndChild();
        selected=current.Key;
        ImGui.SameLine();
        if(ImGui.BeginChild(id+"Body",Vector2.Zero,true))
        {
            try
            {
                TenshoAssistant.UI.UiComponents.SubTitle(current.Label);
                ImGui.Separator();
                current.Draw();
            }
            finally{ImGui.EndChild();}
        }
        else ImGui.EndChild();
    }
}
