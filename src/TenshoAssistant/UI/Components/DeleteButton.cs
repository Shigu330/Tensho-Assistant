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

    /// <summary>
    /// X-154 (0.1254): the same two-click deletion as a square icon button (a row of icons). Armed: the glyph in the error colour and the
    /// tooltip « Click again to delete »; else the tooltip of the action. Same id rule as DeleteButton.
    /// </summary>
    private bool IconDeleteButton(Dalamud.Interface.FontAwesomeIcon icon,string labelKey,string tipKey,string idSuffix="")
    {
        var imguiId="###Delete"+labelKey+idSuffix;
        var id=ImGui.GetID(imguiId);
        var now=Environment.TickCount64;
        var armed=deleteConfirm.IsArmed(id,now);
        var clicked=GlyphButton(imguiId,icon,false,armed ? UI.FfxivTheme.Error:null);
        Tip(armed ? "Common.DeleteConfirm":tipKey);
        return clicked && deleteConfirm.Click(id,now);
    }
}
