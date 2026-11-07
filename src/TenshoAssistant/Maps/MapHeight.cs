namespace TenshoAssistant.Maps;

/// <summary>
/// 0.844 (X-67, user request: the game map's up and down arrows on an icon above or below). The game keeps no arrow on its markers (developer capture 2026-11-08:
/// MapMarkerData.Flags 40 as well on a quest at the player's height as on one 38 yalms below): its map compares heights when it draws. Tensho compares the marker's
/// height with the player's; the threshold is Tensho's own setting, not a figure read from the game.
/// </summary>
public static class MapHeight
{
    /// <summary>1 above, -1 below, 0 within the threshold or unknown.</summary>
    public static int Arrow(float target,float player,float threshold)
    {
        if(!float.IsFinite(target) || !float.IsFinite(player) || threshold<=0)return 0;
        var difference=target-player;
        return difference>threshold ? 1:difference< -threshold ? -1:0;
    }
}
