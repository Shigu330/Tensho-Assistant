using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-607 (0.1946, user request of 18/11): one form for a text filter where empty means « all »: its preview is the choice or the translated
// « All … », its first line « All … », then each choice. The same calls as the combos written by hand before it, so the pages are unchanged;
// the choices are read only while the list is open. Its width (SetNextItemWidth) and its tooltip (Tip) stay at the caller, as before.
internal sealed partial class MainWindow
{
    /// <summary>A combo filter on <paramref name="value"/> (empty: all); true when the choice changed.</summary>
    private bool FilterCombo(string id,string allKey,ref string value,IEnumerable<string> options)
    {
        var all=localization.Get(allKey);
        if(!ImGui.BeginCombo(id,value.Length==0 ? all:value))return false;
        var changed=false;
        try
        {
            if(ImGui.Selectable(all,value.Length==0)){value="";changed=true;}
            foreach(var option in options)
                if(ImGui.Selectable(option,value==option)){value=option;changed=true;}
        }
        finally{ImGui.EndCombo();}
        return changed;
    }
}
