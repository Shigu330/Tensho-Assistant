namespace TenshoAssistant;

// X-153 (0.1249, fourth slice, GPT review of 11/11, accepted by the user): the panel family out of Plugin.cs, after the desktop (0.1225),
// persistence (0.1239) and server bar (0.1240) families. The panels attached to a game window (AttachedPanels, 0.516) and the free-standing
// panels (PanelWindow, 0.514: tracker, tasks, gear wear, GP tick, lists, Occult Crescent, Tensho's state), each created in one install path
// and released in one release path of the plugin's registrations (RegistrationScope: released in the reverse order, so before the main
// window they draw from, which is registered earlier; a failure does not stop the others), in the order of before. Plugin.cs only calls the
// family, after the main window is added; it keeps their frame (Follow before the draw, Apply and EndFrame around it). PanelFamily1249Tests.
public sealed partial class Plugin
{
    private PanelWindow[] panelWindows = [];
    private AttachedPanels? attachedPanels;

    /// <summary>The panel family: the attached panels (shared with the main window and the bank's import overlay), then the panel windows.</summary>
    private void RegisterPanels()
    {
        registrations.Register(() => { attachedPanels = new AttachedPanels(windows, mainWindow); mainWindow.Panels = attachedPanels; bankOverlay.Panels = attachedPanels; }, () => { mainWindow.Panels = null; bankOverlay.Panels = null; attachedPanels?.Dispose(); attachedPanels = null; });
        registrations.Register(() => { panelWindows = mainWindow.CreatePanelWindows(); foreach (var panel in panelWindows) windows.AddWindow(panel); }, () => { foreach (var panel in panelWindows) windows.RemoveWindow(panel); panelWindows = []; });
    }
}
