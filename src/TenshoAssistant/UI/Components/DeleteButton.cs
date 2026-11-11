using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-122, decision D-112 (user, 2026-11-10; 0.1043): the one way to draw a button that deletes saved data (alarm, deck, sort template,
// craft sequence, bookmark…): two clicks, the second within a few seconds (UI.DeleteConfirm). The armed button says « Click again to
// delete » in the error colour. The ImGui id keeps the label key, so the caller's PushID scope tells rows apart; idSuffix covers a row
// drawn without one. Returns true only on the confirming click.
internal sealed partial class MainWindow
{
    private readonly UI.DeleteConfirm deleteConfirm=new();

    private bool DeleteButton(string labelKey,string idSuffix="",bool small=true)
    {
        var imguiId="###Delete"+labelKey+idSuffix;
        var id=ImGui.GetID(imguiId);
        var now=Environment.TickCount64;
        var armed=deleteConfirm.IsArmed(id,now);
        var label=localization.Get(armed ? "Common.DeleteConfirm":labelKey)+imguiId;
        if(armed)ImGui.PushStyleColor(ImGuiCol.Text,UI.FfxivTheme.Error);
        bool clicked;
        try{clicked=small ? ImGui.SmallButton(label):ImGui.Button(label);}
        finally{if(armed)ImGui.PopStyleColor();}
        return clicked && deleteConfirm.Click(id,now);
    }
}
