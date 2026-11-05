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
            if(x<window.X+window.Width && window.X<x+width && y<window.Y+window.Height && window.Y<y+height)return true;
        }
        return false;
    }
}
