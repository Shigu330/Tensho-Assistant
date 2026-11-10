using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

/// <summary>
/// The ImGui side: one frame of the plugin's main window as Dalamud's WindowSystem draws it (PreDraw, Begin, Draw, End, PostDraw), with
/// Dalamud's error recovery so an exception in a page does not break the frame. The context and its fonts (Dalamud's own free fonts from its
/// installed assets) are PluginUiTest.Host's ImGuiHeadless (moved there in X-120).
/// </summary>
internal static unsafe class Ui
{
    private static readonly bool Tracing=Environment.GetEnvironmentVariable("PROBE_TRACE") is not null;
    private static void Trace(string step){if(Tracing)Console.WriteLine("    frame: "+step);}

    /// <summary>One frame of the plugin's main window filling the desktop window above the status bar; the exception of the page, if any.</summary>
    public static Exception? Frame(Host host,StatusBar status,Vector2 display,float delta)
    {
        var io=ImGui.GetIO();
        io.DisplaySize=display;io.DeltaTime=Math.Max(delta,1/1000f);
        Trace("NewFrame");ImGui.NewFrame();
        Exception? failure=null;
        var window=(Dalamud.Interface.Windowing.Window)host.Window;
        var lines=status.Lines();
        try
        {
            Trace("PreDraw");window.PreDraw();
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new Vector2(display.X,Math.Max(100,display.Y-status.Height(lines.Count))));
            var shown=ImGui.Begin(window.WindowName,ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoSavedSettings);
            Trace("Draw");try{if(shown)window.Draw();}
            finally{ImGui.End();}
        }
        catch(Exception e){failure=e is System.Reflection.TargetInvocationException { InnerException:{ } inner } ? inner : e;}
        finally
        {
            try{window.PostDraw();}catch(Exception e){failure??=e;}
        }
        Trace("Recover");ImGuiP.ErrorCheckEndFrameRecover(null!);
        Trace("Status");
        try{status.Draw(display,lines);}catch(Exception e){failure??=e;}
        Trace("Render");
        ImGui.Render();
        return failure;
    }
}
