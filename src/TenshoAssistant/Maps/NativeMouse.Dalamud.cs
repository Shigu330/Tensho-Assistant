using System.Numerics;
using System.Runtime.InteropServices;

namespace TenshoAssistant.Maps;

/// <summary>
/// X-387 (0.1366): the mouse as Windows sees it, for a right click on the game's own chat window (no ImGui window under the mouse: ImGui's own
/// click state is not what the game window received). Read only: buttons, cursor (screen pixels), and whether the game is the window in front
/// (GetAsyncKeyState is global). The physical right button is the left one when the user swapped them (SM_SWAPBUTTON).
/// </summary>
internal static class NativeMouse
{
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out CursorPoint point);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [StructLayout(LayoutKind.Sequential)] private struct CursorPoint{public int X,Y;}
    private const int SwapButtons=23,LeftButton=0x01,RightButton=0x02;

    public static bool RightDown()=>(GetAsyncKeyState(GetSystemMetrics(SwapButtons)!=0 ? LeftButton:RightButton)&0x8000)!=0;
    public static Vector2? Cursor()=>GetCursorPos(out var point) ? new Vector2(point.X,point.Y):null;
    public static bool GameInFront()
    {
        var window=GetForegroundWindow();
        if(window==0)return false;
        GetWindowThreadProcessId(window,out var process);
        return process==(uint)Environment.ProcessId;
    }
}
