namespace TenshoAssistant.Maps;

/// <summary>0.542 (P1): how the map opens: at the last zoom used, at a fixed zoom following the character, or whole.</summary>
public enum MapOpenZoom { Last, Fixed, Whole }

/// <summary>0.542 (P1): the zoom to save once it has rested; never at each frame, as the wheel changes it many times a second.</summary>
public sealed class MapZoomMemory(TimeSpan rest)
{
    private float seen=float.NaN;
    private TimeSpan changed;
    private bool pending;

    public float? Tick(float zoom,float saved,TimeSpan now)
    {
        if(zoom!=seen){seen=zoom;changed=now;pending=Math.Abs(zoom-saved)>0.001f;return null;}
        if(!pending || now-changed<rest)return null;
        pending=false;
        return zoom;
    }
}
