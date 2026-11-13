using System.Numerics;
using TenshoAssistant.Diagnostics;

namespace TenshoAssistant.UI;

/// <summary>
/// X-264 (0.1269): the geometry of a gauge and of a curve, without drawing: the dial is an arc open at the bottom (from bottom left, over
/// the top, to bottom right), filled up to the value; curves are polylines in a rectangle. Pure, so the layout is tested without ImGui.
/// </summary>
public static class GaugeMath
{
    /// <summary>Width of a gauge at scale 1, in pixels.</summary>
    public const float BaseWidth=150;
    /// <summary>Where the dial starts (bottom left) and how far it turns (three quarters of a circle), in radians; y grows downwards.</summary>
    public const float Start=MathF.PI*0.75f,Sweep=MathF.PI*1.5f;

    /// <summary>How full the dial is, 0 to 1; an unknown value, a value that is not a number or a scale of 0 or less: empty.</summary>
    public static float Fraction(double? value,double max)
        =>value is not { } known || !double.IsFinite(known) || !(max>0) ? 0:(float)Math.Clamp(known/max,0,1);

    /// <summary>The angle of a fraction of the dial.</summary>
    public static float Angle(float fraction)=>Start+Sweep*Math.Clamp(fraction,0,1);

    /// <summary>
    /// The level a value has under Tensho's own rule for it: a problem from <paramref name="problemAt"/>, worth a look from
    /// <paramref name="watchAt"/>, fine under them; an unknown value is fine (nothing to say). No rule (both null): always fine.
    /// </summary>
    public static HealthLevel Zone(double? value,double? watchAt,double? problemAt)
        =>value is not { } known ? HealthLevel.Ok
            :problemAt is { } problem && known>=problem ? HealthLevel.Problem
            :watchAt is { } watch && known>=watch ? HealthLevel.Watch:HealthLevel.Ok;

    /// <summary>A round top for a scale: the first of 1, 2 or 5 times a power of ten at or above the largest value, never under <paramref name="floor"/>.</summary>
    public static double NiceMax(double largest,double floor)
    {
        var need=Math.Max(double.IsFinite(largest) ? largest:0,floor>0 ? floor:1);
        var power=Math.Pow(10,Math.Floor(Math.Log10(need)));
        foreach(var step in new[]{1.0,2.0,5.0,10.0})if(step*power>=need*(1-1e-12))return step*power;
        return 10*power;
    }

    /// <summary>The dial of a gauge drawn from <paramref name="origin"/> (its top left corner) in a box of <paramref name="width"/>: centre, radius, thickness and the box.</summary>
    public static (Vector2 Centre,float Radius,float Thickness,Vector2 Size) Layout(Vector2 origin,float width,float titleHeight)
    {
        var thickness=MathF.Max(3,MathF.Round(width*0.085f));
        var radius=MathF.Max(8,width*0.5f-thickness*0.5f-2);
        var centre=new Vector2(origin.X+width*0.5f,origin.Y+titleHeight+radius+thickness*0.5f+2);
        // The dial ends at 45° under the horizontal: sin(45°) of the radius under the centre, plus half the band.
        var height=titleHeight+2+thickness*0.5f+radius+radius*0.7072f+thickness*0.5f+2;
        return (centre,radius,thickness,new Vector2(width,MathF.Ceiling(height)));
    }

    /// <summary>
    /// The band of the dial between two fractions, as quads (inner then outer points, turning with the dial): each quad is
    /// (inner start, outer start, outer end, inner end). Nothing for an empty span.
    /// </summary>
    public static IReadOnlyList<(Vector2 A,Vector2 B,Vector2 C,Vector2 D)> Band(Vector2 centre,float radius,float thickness,float from,float to)
    {
        from=Math.Clamp(from,0,1);to=Math.Clamp(to,0,1);
        if(!(to>from))return [];
        var steps=Math.Max(1,(int)MathF.Ceiling(48*(to-from)));
        var inner=radius-thickness*0.5f;var outer=radius+thickness*0.5f;
        var quads=new (Vector2,Vector2,Vector2,Vector2)[steps];
        var angle=Angle(from);var before=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
        for(var step=1;step<=steps;step++)
        {
            angle=Angle(from+(to-from)*step/steps);var after=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
            quads[step-1]=(centre+before*inner,centre+before*outer,centre+after*outer,centre+after*inner);
            before=after;
        }
        return quads;
    }

    /// <summary>
    /// A series as polylines inside a rectangle: x by the point's place (the first at the left edge, the last at the right), y by its value
    /// against <paramref name="top"/> (0 at the bottom, clamped). An unknown value breaks the line: no value is invented between two points.
    /// A lone point between two unknown ones is a line of one point (drawn as a dot).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<Vector2>> Curve(IReadOnlyList<double?> values,Vector2 min,Vector2 max,double top)
    {
        var lines=new List<IReadOnlyList<Vector2>>();
        if(values.Count==0 || !(top>0))return lines;
        List<Vector2>? line=null;
        for(var index=0;index<values.Count;index++)
        {
            if(values[index] is not { } value || !double.IsFinite(value)){line=null;continue;}
            var x=values.Count==1 ? max.X:min.X+(max.X-min.X)*index/(values.Count-1);
            var y=max.Y-(max.Y-min.Y)*(float)Math.Clamp(value/top,0,1);
            if(line is null){line=[];lines.Add(line);}
            line.Add(new Vector2(x,y));
        }
        return lines;
    }
}
