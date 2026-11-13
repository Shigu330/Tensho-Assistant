using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Diagnostics;
using TenshoAssistant.UI;

namespace TenshoAssistant;

// X-264 (0.1269): the common gauge and counter tile (the dashboard first; any page may use them). Colours come from the theme: a value
// with a rule of Tensho takes the colour of its level (and its dial carries the zones of the rule on its rim), a value without a rule
// takes the accent: it only shows the value, it does not judge it. The number and its unit are always written: never the colour alone.
internal sealed partial class MainWindow
{
    private Vector4 LevelColour(HealthLevel level)=>level switch { HealthLevel.Problem=>Error,HealthLevel.Watch=>Warning,_=>Success };

    /// <summary>
    /// One gauge: its title, its dial filled to <paramref name="amount"/> out of <paramref name="max"/>, its value as text in the middle.
    /// <paramref name="watchAt"/> and <paramref name="problemAt"/>: Tensho's rule for this value, null when it has none. Its help on hover
    /// or focus; true when it was clicked.
    /// </summary>
    private bool Gauge(string id,string title,string value,double? amount,double max,double? watchAt,double? problemAt,string tip,bool chosen=false)
    {
        var width=GaugeMath.BaseWidth*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale;
        var origin=ImGui.GetCursorScreenPos();
        var (centre,radius,thickness,size)=GaugeMath.Layout(origin,width,ImGui.GetTextLineHeight());
        var clicked=ImGui.InvisibleButton("##Gauge"+id,size);
        TipText(tip);
        var draw=ImGui.GetWindowDrawList();
        var ruled=watchAt is not null || problemAt is not null;
        var colour=ruled ? LevelColour(GaugeMath.Zone(amount,watchAt,problemAt)):Accent;
        var border=FfxivTheme.Border;
        void Fill(float from,float to,float at,float thick,Vector4 fill)
        {
            var packed=ImGui.GetColorU32(fill);
            foreach(var (a,b,c,d) in GaugeMath.Band(centre,at,thick,from,to))draw.AddQuadFilled(a,b,c,d,packed);
        }
        Fill(0,1,radius,thickness,border with{W=border.W*0.6f});
        Fill(0,GaugeMath.Fraction(amount,max),radius,thickness,colour);
        if(ruled)
        {
            // The rule's zones on a thin rim outside the dial: fine, worth a look, a problem.
            var rim=MathF.Max(2,thickness*0.22f);var at=radius+thickness*0.5f+rim*0.5f+1;
            var watch=watchAt is { } w ? GaugeMath.Fraction(w,max):problemAt is { } only ? GaugeMath.Fraction(only,max):1;
            var problem=problemAt is { } p ? GaugeMath.Fraction(p,max):1;
            Fill(0,watch,at,rim,Success);
            Fill(watch,problem,at,rim,Warning);
            Fill(problem,1,at,rim,Error);
        }
        draw.AddText(origin,ImGui.GetColorU32(chosen ? Accent:Muted),title);
        using(UiComponents.PushSectionFont())
        {
            var text=ImGui.CalcTextSize(value);
            draw.AddText(new Vector2(centre.X-text.X*0.5f,centre.Y-text.Y*0.5f),ImGui.GetColorU32(amount is null ? Muted:colour),value);
        }
        if(chosen)
        {
            var (markMin,markMax)=SelectionMark.UnderMark(origin,origin+size,Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
            draw.AddRectFilled(markMin,markMax,ImGui.GetColorU32(Accent));
        }
        return clicked;
    }

    /// <summary>A counter beside the gauges: its title and its number, in the colour of its level; its help on hover.</summary>
    private void StatTile(string title,string value,HealthLevel level,string tip)
    {
        ImGui.BeginGroup();
        try
        {
            ImGui.TextColored(Muted,title);
            using(UiComponents.PushSectionFont())ImGui.TextColored(LevelColour(level),value);
        }
        finally{ImGui.EndGroup();}
        TipText(tip);
    }

    /// <summary>
    /// X-264 (0.1270): one curve over the width left: its title, a scale of three lines (0, half, top) with their values, the points joined
    /// (an unknown value breaks the line, GaugeMath.Curve), what the left and right edges stand for; lowest, highest and last on hover.
    /// </summary>
    private void CurvePlot(string title,IReadOnlyList<double?> values,double top,Func<double,string> text,string left,string right,Vector4 colour)
    {
        var scale=Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale;
        ImGui.TextColored(Muted,title);
        var line=ImGui.GetTextLineHeight();
        var width=MathF.Max(160*scale,ImGui.GetContentRegionAvail().X);var height=110*scale;
        var origin=ImGui.GetCursorScreenPos();
        var label=ImGui.CalcTextSize(text(top)).X+ImGui.GetStyle().ItemSpacing.X;
        ImGui.Dummy(new Vector2(width,height+line*1.5f));
        var known=values.Where(value=>value is not null).Select(value=>value!.Value).ToArray();
        if(known.Length>0)TipText(localization.Format("Dashboard.CurveSummary",text(known.Min()),text(known.Max()),text(known[^1])));
        var draw=ImGui.GetWindowDrawList();
        var min=origin+new Vector2(label,line*0.5f);var max=origin+new Vector2(width-2,height);
        var border=FfxivTheme.Border;var grid=ImGui.GetColorU32(border with{W=border.W*0.6f});var muted=ImGui.GetColorU32(Muted);
        foreach(var part in new[]{0f,0.5f,1f})
        {
            var y=max.Y-(max.Y-min.Y)*part;
            draw.AddLine(new Vector2(min.X,y),new Vector2(max.X,y),grid,1);
            draw.AddText(new Vector2(origin.X,y-line*0.5f),muted,text(top*part));
        }
        var packed=ImGui.GetColorU32(colour);var thick=MathF.Max(1.5f,1.5f*scale);
        foreach(var points in GaugeMath.Curve(values,min,max,top))
        {
            if(points.Count==1){draw.AddCircleFilled(points[0],thick*1.5f,packed,12);continue;}
            for(var index=1;index<points.Count;index++)draw.AddLine(points[index-1],points[index],packed,thick);
        }
        draw.AddText(new Vector2(min.X,max.Y+2),muted,left);
        draw.AddText(new Vector2(max.X-ImGui.CalcTextSize(right).X,max.Y+2),muted,right);
    }

    /// <summary>How many gauges fit on a line of the available width (one at least).</summary>
    private static int GaugesPerLine()
    {
        var width=GaugeMath.BaseWidth*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale+ImGui.GetStyle().ItemSpacing.X;
        return Math.Max(1,(int)((ImGui.GetContentRegionAvail().X+ImGui.GetStyle().ItemSpacing.X)/width));
    }
}
