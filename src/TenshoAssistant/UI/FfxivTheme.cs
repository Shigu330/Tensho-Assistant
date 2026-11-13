using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant.UI;

/// <summary>Plugin style following the game's interface theme (see ThemePalettes); colours are read at each push.</summary>
internal static class FfxivTheme
{
    private static ThemeColors current=ThemePalettes.All[PluginTheme.Dark];
    public static PluginTheme Theme{get;private set;}=PluginTheme.Dark;
    public static void Select(PluginTheme theme){Theme=theme;current=ThemePalettes.All[theme];Following=false;}

    /// <summary>X-157 (0.1272): « Follow Dalamud's style »: the standard controls keep the active style (colours, spacing, rounding); only
    /// Tensho's identity is pushed (PushIdentity), and the hand-drawn parts read the style's own neutral colours (ThemePalettes.Follow).</summary>
    public static bool Following{get;private set;}
    public static void Follow(ThemeColors style){current=style;Following=true;}
    // What each Push pushed, so its Pop takes back the same (the mode may change between two frames, never inside a push; draw thread).
    private static readonly Stack<bool> pushed=new();

    public static Vector4 Background=>current.Background;
    public static Vector4 Panel=>current.Panel;
    public static Vector4 Border=>current.Border;
    public static Vector4 Gold=>current.Accent;
    public static Vector4 Text=>current.Text;
    public static Vector4 TextMuted=>current.TextMuted;
    public static Vector4 Success=>current.Success;
    public static Vector4 Selected=>current.Selected;
    public static Vector4 Hovered=>current.Hovered;
    public static Vector4 Warning=>current.Warning;
    public static Vector4 Error=>current.Error;

    public static void Push()
    {
        pushed.Push(Following);
        if(Following){PushIdentity();return;}
        var c=current;
        ImGui.PushStyleColor(ImGuiCol.WindowBg, c.Background);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, c.Panel);
        ImGui.PushStyleColor(ImGuiCol.Border, c.Border);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, c.Hovered);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, Vector4.Lerp(c.Selected,c.Accent,0.25f));
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, c.TextMuted);
        ImGui.PushStyleColor(ImGuiCol.Header, c.Selected);
        ImGui.PushStyleColor(ImGuiCol.Button, c.Panel);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, c.Hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, c.Selected);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, c.Background);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, c.Hovered);
        ImGui.PushStyleColor(ImGuiCol.Tab, c.Panel);
        ImGui.PushStyleColor(ImGuiCol.TabActive, c.Selected);
        ImGui.PushStyleColor(ImGuiCol.TabHovered, c.Hovered);
        ImGui.PushStyleColor(ImGuiCol.Separator, c.Border);
        ImGui.PushStyleColor(ImGuiCol.Text, c.Text);
        ImGui.PushStyleColor(ImGuiCol.PopupBg, c.Panel);
        ImGui.PushStyleColor(ImGuiCol.TitleBg, c.Background);
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, c.Selected);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, c.Selected);
        ImGui.PushStyleColor(ImGuiCol.CheckMark, c.Accent);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, c.Accent);
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, c.Warning);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, c.Background);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, c.Border);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, c.Hovered);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, c.Accent);
        ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, c.Selected);
        ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, c.Border);
        ImGui.PushStyleColor(ImGuiCol.TableBorderLight, c.Panel);
        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(c.Accent.X,c.Accent.Y,c.Accent.Z,0.035f));
        // X-154 (0.1252): the keyboard and gamepad focus frame in the theme's accent (it kept Dalamud's colour).
        ImGui.PushStyleColor(ImGuiCol.NavHighlight, c.Accent);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 2);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize,1);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize,1);
    }

    public static void Pop()
    {
        if(pushed.Count>0 && pushed.Pop()){PopIdentity();return;}
        ImGui.PopStyleVar(6);
        ImGui.PopStyleColor(33);
    }

    // X-157 (0.1272): Tensho's identity over Dalamud's style: the accent on the check mark, the slider and the focus frame; nothing else.
    private static void PushIdentity()
    {
        ImGui.PushStyleColor(ImGuiCol.CheckMark, current.Accent);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, current.Accent);
        ImGui.PushStyleColor(ImGuiCol.NavHighlight, current.Accent);
    }
    private static void PopIdentity()=>ImGui.PopStyleColor(3);

    public static Scope Apply() { Push();return new Scope(); }
    public readonly struct Scope : IDisposable
    {
        public void Dispose()=>Pop();
    }
}
