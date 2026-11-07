namespace TenshoAssistant.Maps;

/// <summary>
/// 0.844 (X-67, user request: the game map's up and down arrows on an icon above or below). The game keeps no arrow on its markers (developer capture 2026-11-08:
/// MapMarkerData.Flags 40 as well on a quest at the player's height as on one 38 yalms below): its map compares heights when it draws. Tensho compares the marker's
/// height with the player's; the threshold is Tensho's own setting, not a figure read from the game.
/// </summary>
public static class MapHeight
{
    /// <summary>
    /// 0.849 (X-71, user request: « the game's icons », seen at Mor Dhona): the game's orange chevron, pointing up, from its map textures (tools/UldProbe --parts
    /// NaviMap NaviMap: part U 352, V 96, 24 × 24 of the 448 × 212 texture; tools/MapMaskProbe --tex shows it, docs/evidence/0.849-height-chevron.md).
    /// </summary>
    public const string ChevronTexture="ui/uld/NaviMap.tex";
    /// <summary>The chevron's texture coordinates: as the game cuts it for « above », upside down for « below ».</summary>
    public static (System.Numerics.Vector2 From,System.Numerics.Vector2 To) ChevronUv(int arrow)
    {
        var top=new System.Numerics.Vector2(352f/448,96f/212);var bottom=new System.Numerics.Vector2(376f/448,120f/212);
        return arrow<0 ? (new(top.X,bottom.Y),new(bottom.X,top.Y)):(top,bottom);
    }

    /// <summary>1 above, -1 below, 0 within the threshold or unknown.</summary>
    public static int Arrow(float target,float player,float threshold)
    {
        if(!float.IsFinite(target) || !float.IsFinite(player) || threshold<=0)return 0;
        var difference=target-player;
        return difference>threshold ? 1:difference< -threshold ? -1:0;
    }
}
