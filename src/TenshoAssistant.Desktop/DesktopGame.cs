using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TenshoAssistant.Desktop;

/// <summary>
/// X-163 (0.1132): whether the game is the window in front (the desktop application then lets the game ring its alarms), and the Windows
/// sound of the application's bell. Read-only: the game's window is only looked at, never touched.
/// </summary>
internal static class DesktopGame
{
    private static readonly string[] GameProcesses=["ffxiv_dx11","ffxiv"];

    public static bool InFront()
    {
        try
        {
            var window=GetForegroundWindow();
            if(window==IntPtr.Zero || GetWindowThreadProcessId(window,out var id)==0)return false;
            using var process=Process.GetProcessById((int)id);
            return GameProcesses.Contains(process.ProcessName,StringComparer.OrdinalIgnoreCase);
        }
        catch(Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){return false;}
    }

    /// <summary>Windows' « exclamation » sound (the user's sound scheme).</summary>
    public static void Beep()=>MessageBeep(0x30);

    [DllImport("user32")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32")] private static extern bool MessageBeep(uint type);
}
