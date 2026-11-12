namespace TenshoAssistant.UI;

/// <summary>
/// X-180 (0.1168, night review N1): a save refused because the window is drawn by the desktop application. The application works on its own
/// copy of the plugin's folder and settings, refreshed from the game: what it saved there would be overwritten without a word. Every save of
/// the window throws this there, so each caller's existing failure path runs (the previous value put back, « not saved » said).
/// </summary>
public sealed class DesktopReadOnlyException():InvalidOperationException("Not saved: the desktop application does not change the plugin's data; make this change in the game.");

/// <summary>X-180 (0.1168): the window's save, refused while it is drawn by the desktop application, the plugin's own save otherwise.</summary>
public static class DesktopReadOnly
{
    public static Action Guard(Func<bool> inDesktopApp,Action save)
    {
        ArgumentNullException.ThrowIfNull(inDesktopApp);
        ArgumentNullException.ThrowIfNull(save);
        return ()=>
        {
            if(inDesktopApp())throw new DesktopReadOnlyException();
            save();
        };
    }
}
