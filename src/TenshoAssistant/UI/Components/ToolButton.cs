using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace TenshoAssistant;

// X-76 step 2b (harmonisation H8, 0.920): the square icon button of a tool bar (map), with its tooltip; greyed when not enabled, the theme's
// Selected colour when active. Square on the frame height, so it follows the font size.
// X-164 (0.1130, user report: icons not centred in their buttons): the glyph is drawn at the centre of its own ink box (Font Awesome glyphs
// are not centred in their advance, which ImGui centres); a text button of the same height for the bars that need a word (« 100% »).
internal sealed partial class MainWindow
{
    private unsafe bool ToolButton(FontAwesomeIcon icon,string tooltipKey,bool enabled,bool active=false)
    {
        bool clicked;
        ImGui.BeginDisabled(!enabled);
        try
        {
            ImGui.PushFont(UiBuilder.IconFont);
            try
            {
                var side=ImGui.GetFrameHeight();
                if(active)ImGui.PushStyleColor(ImGuiCol.Button,UI.FfxivTheme.Selected);
                try{clicked=ImGui.Button($"##{tooltipKey}",new Vector2(side));}
                finally{if(active)ImGui.PopStyleColor();}
                var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
                var font=ImGui.GetFont();var size=ImGui.GetFontSize();var label=icon.ToIconString();
                var glyph=label.Length>0 ? font.FindGlyph(label[0]):null;
                Vector2 at;
                if(glyph!=null && font.FontSize>0)
                {
                    var scale=size/font.FontSize;
                    var ink=new Vector2(glyph->X1-glyph->X0,glyph->Y1-glyph->Y0)*scale;
                    at=(min+max-ink)/2-new Vector2(glyph->X0,glyph->Y0)*scale;
                }
                else at=(min+max-ImGui.CalcTextSize(label))/2;
                ImGui.GetWindowDrawList().AddText(font,size,new Vector2(MathF.Round(at.X),MathF.Round(at.Y)),ImGui.GetColorU32(ImGuiCol.Text),label);
            }
            finally{ImGui.PopFont();}
        }
        finally{ImGui.EndDisabled();}
        Tip(tooltipKey);
        return clicked && enabled;
    }

    private bool TextToolButton(string text,string tooltipKey,bool enabled=true)
    {
        ImGui.BeginDisabled(!enabled);
        bool clicked;
        try{clicked=ImGui.Button($"{text}##{tooltipKey}",new Vector2(Math.Max(ImGui.GetFrameHeight(),ImGui.CalcTextSize(text).X+ImGui.GetStyle().FramePadding.X),ImGui.GetFrameHeight()));}
        finally{ImGui.EndDisabled();}
        Tip(tooltipKey);
        return clicked && enabled;
    }
}
