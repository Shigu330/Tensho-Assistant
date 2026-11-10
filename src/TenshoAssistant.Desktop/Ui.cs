using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

/// <summary>
/// The ImGui side: one context, the fonts (Dalamud's own free fonts from its installed assets: Noto Sans CJK JP Medium, OFL, with Font
/// Awesome Free Solid, OFL, merged in so the plugin's FontAwesomeIcon glyphs draw), and one frame of the plugin's main window as Dalamud's
/// WindowSystem draws it (PreDraw, Begin, Draw, End, PostDraw), with Dalamud's error recovery so an exception in a page does not break the frame.
/// </summary>
internal static unsafe class Ui
{
    public static readonly string Assets=Program.FontFolder;
    private static ushort* textRanges,iconRanges;

    public static (int W,int H,IntPtr Pixels) Start(float size)
    {
        ImGui.CreateContext();
        var io=ImGui.GetIO();
        io.IniFilename=null;
        io.DisplaySize=new Vector2(1280,800);
        io.DeltaTime=1/60f;
        io.BackendFlags|=ImGuiBackendFlags.HasMouseCursors;
        var text=Path.Combine(Assets,"NotoSansCJKjp-Medium.otf");
        var icons=Path.Combine(Assets,"FontAwesomeFreeSolid.otf");
        // Latin with extensions (French: œ, «, …), general punctuation, arrows and symbols; Font Awesome's private use area.
        textRanges=Ranges([0x0020,0x024F,0x2000,0x206F,0x20A0,0x20CF,0x2100,0x21FF,0x2200,0x22FF,0x2460,0x24FF,0x25A0,0x26FF,0x2700,0x27BF,0x3000,0x30FF,0xFF00,0xFFEF]);
        iconRanges=Ranges([0xE000,0xF8FF]);
        DesktopFonts.Main=File.Exists(text) ? io.Fonts.AddFontFromFileTTF(text.AsSpan(),size,default,textRanges) : io.Fonts.AddFontDefault();
        if(File.Exists(icons))
        {
            var config=ImGui.ImFontConfig();
            config.MergeMode=true;config.GlyphMinAdvanceX=size;
            io.Fonts.AddFontFromFileTTF(icons.AsSpan(),size*0.85f,config,iconRanges);
        }
        io.Fonts.Build();
        byte* pixels;int w,h;
        io.Fonts.GetTexDataAsRGBA32(0,&pixels,&w,&h);
        return (w,h,(IntPtr)pixels);
    }

    private static ushort* Ranges(int[] values)
    {
        var p=(ushort*)NativeMemory.Alloc((nuint)(values.Length+1),sizeof(ushort));
        for(var i=0;i<values.Length;i++)p[i]=(ushort)values[i];
        p[values.Length]=0;
        return p;
    }

    private static readonly bool Tracing=Environment.GetEnvironmentVariable("PROBE_TRACE") is not null;
    private static void Trace(string step){if(Tracing)Console.WriteLine("    frame: "+step);}

    public static void SetFontTexture(ulong id)=>ImGui.GetIO().Fonts.SetTexID(0,id);

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
