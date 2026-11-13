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
    /// <summary>X-154 (0.1254): the square button itself, shared by ToolButton and IconDeleteButton: frame height, glyph centred, no tooltip.</summary>
    private bool GlyphButton(string id,FontAwesomeIcon icon,bool active,Vector4? colour)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        try
        {
            var side=ImGui.GetFrameHeight();
            if(active)ImGui.PushStyleColor(ImGuiCol.Button,UI.FfxivTheme.Selected);
            bool clicked;
            try{clicked=ImGui.Button(id,new Vector2(side));}
            finally{if(active)ImGui.PopStyleColor();}
            DrawCentredGlyph(icon,colour);
            return clicked;
        }
        finally{ImGui.PopFont();}
    }

    /// <summary>
    /// X-164 (0.1130), shared since X-154 (0.1254): the glyph at the centre of the last item, by its own ink box; the icon font is pushed by
    /// the caller. The colour of the text unless one is given (an armed delete button: the error colour).
    /// </summary>
    private unsafe void DrawCentredGlyph(FontAwesomeIcon icon,Vector4? colour)
    {
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
        ImGui.GetWindowDrawList().AddText(font,size,new Vector2(MathF.Round(at.X),MathF.Round(at.Y)),colour is { } given ? ImGui.GetColorU32(given):ImGui.GetColorU32(ImGuiCol.Text),label);
    }

    private bool ToolButton(FontAwesomeIcon icon,string tooltipKey,bool enabled,bool active=false)
    {
        bool clicked;
        ImGui.BeginDisabled(!enabled);
        try{clicked=GlyphButton($"##{tooltipKey}",icon,active,null);}
        finally{ImGui.EndDisabled();}
        Tip(tooltipKey);
        return clicked && enabled;
    }

    /// <summary>
    /// X-154 (0.1254): the main action of a block (one per block): the selection background and an accent border, the label in the text
    /// colour; greyed when not enabled; its tooltip is required.
    /// </summary>
    private bool PrimaryButton(string labelKey,string tipKey,bool enabled=true,bool small=false)
    {
        bool clicked;
        ImGui.BeginDisabled(!enabled);
        try
        {
            ImGui.PushStyleColor(ImGuiCol.Button,UI.FfxivTheme.Selected);
            ImGui.PushStyleColor(ImGuiCol.Border,UI.FfxivTheme.Gold);
            try
            {
                var label=localization.Get(labelKey)+"###Primary"+labelKey;
                clicked=small ? ImGui.SmallButton(label):ImGui.Button(label);
            }
            finally{ImGui.PopStyleColor(2);}
        }
        finally{ImGui.EndDisabled();}
        Tip(tipKey);
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
