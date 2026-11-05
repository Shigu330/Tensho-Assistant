namespace TenshoAssistant.UI;

/// <summary>A game window as last read: where it is, and its place in the game's order (a higher rank is further in front).</summary>
public readonly record struct GameWindow(string Name,float X,float Y,float Width,float Height,int Rank);

/// <summary>0.537 (user request): whether a game window stands in front of one of Tensho's interfaces. Only the whole interface can step aside: the game cannot draw over it.</summary>
public static class GameWindows
{
    // What the game keeps on screen without being a window in front of anything (log of 2026-11-05, docs/evidence/0.537-game-windows.md): HUD parts start with « _ ». 0.538 (user capture): its tooltips count, like its windows.
    private static readonly string[] NotWindows=["_","NamePlate","CastBar","ChatLog","ScenarioTree","CursorAddon","AreaMap"];
    public static bool Ignored(string name)=>NotWindows.Any(start=>name.StartsWith(start,StringComparison.Ordinal));

    /// <param name="anchors">The game window the interface belongs to, by the start of its name: it and its parts never cover it, and only what is in front of it counts. None: whatever touches it.</param>
    public static bool Covers(IReadOnlyList<GameWindow> windows,float x,float y,float width,float height,float screenWidth,float screenHeight,IReadOnlyList<string>? anchors=null)
    {
        int? home=null;
        if(anchors is {Count:>0})
            foreach(var window in windows)
                if(anchors.Any(anchor=>window.Name.StartsWith(anchor,StringComparison.Ordinal)) && (home is null || window.Rank>home))home=window.Rank;
        foreach(var window in windows)
        {
            if(window.Width<=0 || window.Height<=0 || window.Width>=screenWidth*0.9f && window.Height>=screenHeight*0.9f)continue;
            if(anchors?.Any(anchor=>window.Name.StartsWith(anchor,StringComparison.Ordinal))==true)continue;
            if(home is { } rank && window.Rank<=rank)continue;
            if(Touch(x,y,width,height,window.X,window.Y,window.Width,window.Height))return true;
        }
        return false;
    }

    public static bool Touch(float x,float y,float width,float height,float otherX,float otherY,float otherWidth,float otherHeight)=>
        x<otherX+otherWidth && otherX<x+width && y<otherY+otherHeight && otherY<y+height;
}

/// <summary>
/// 0.618 (user report): the map enlarged is a view of the whole map: it stays, whatever window it touches. At its usual size it stays in a corner, and steps aside.
/// Judged on its size too, so that it holds after a reload, which forgets the button's state.
/// </summary>
public static class MapEnlarged
{
    public const float Share=0.85f;
    public static bool Is(bool enlargeButton,float width,float height,float screenWidth,float screenHeight)
        =>enlargeButton || width>=screenWidth*Share && height>=screenHeight*Share;
}

/// <summary>0.539 (user request): an interface leaves at once and comes back only after a moment with nothing in front, so that going from one tooltip to the next does not make it blink.</summary>
public sealed class StepAside(TimeSpan hold)
{
    private TimeSpan? lastCovered;

    public bool Hidden(bool covered,TimeSpan now)
    {
        if(covered){lastCovered=now;return true;}
        return lastCovered is { } last && now-last<hold;
    }
}
