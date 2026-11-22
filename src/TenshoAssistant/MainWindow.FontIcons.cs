using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-801 (0.1809): an icon of the game's font (BitmapFontIcon: the type of damage in a combat line) drawn by Dalamud's SeString renderer, as
// the beast panel's element icons are (X-551).
internal sealed partial class MainWindow
{
    private readonly Dictionary<uint,byte[]> fontIcons=[];
    private bool fontIconsFailed;

    /// <summary>Draws an icon of the game's font at the cursor, on the window's list, without moving the cursor; false when it cannot be
    /// drawn (the desktop application, or Dalamud's renderer failed once: the caller then writes its text).</summary>
    private bool DrawFontIcon(uint icon)
    {
        if(inDesktopApp || fontIconsFailed)return false;
        try
        {
            if(!fontIcons.TryGetValue(icon,out var text))
                fontIcons[icon]=text=new Dalamud.Game.Text.SeStringHandling.SeStringBuilder().AddIcon((Dalamud.Game.Text.SeStringHandling.BitmapFontIcon)icon).Build().Encode();
            var how=new Dalamud.Interface.ImGuiSeStringRenderer.SeStringDrawParams{TargetDrawList=ImGui.GetWindowDrawList(),ScreenOffset=ImGui.GetCursorScreenPos(),Font=ImGui.GetFont(),FontSize=ImGui.GetFontSize(),WrapWidth=float.MaxValue};
            Dalamud.Interface.Utility.ImGuiHelpers.SeStringWrapped(text,how);
            return true;
        }
        catch(Exception error)
        {
            fontIconsFailed=true;
            log.Warning(error,"Icons of the game's font unavailable in the chat window; « · » is written instead.");
            return false;
        }
    }
}
