using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.UI;

/// <summary>X-76 (step 2): the check a reader makes after copying a game window, written once (framework thread).</summary>
internal static class GameWindowCheck
{
    /// <summary>The window found before the copy is still the same one, ready and shown, and the game's interface is not hidden: the copy can be published.</summary>
    public static bool StillShown(IGameGui gui,string name,AtkUnitBasePtr before)
    {
        var after=gui.GetAddonByName(name);
        return !after.IsNull && after.Address==before.Address && after.IsReady && after.IsVisible && !gui.GameUiHidden;
    }
}
