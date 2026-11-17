namespace TenshoAssistant.Maps;

/// <summary>
/// X-535 (17/11, test T-0.546-1 reported by the user: « a click on the map places no flag »): a short left click on an empty spot of a
/// zone map places the flag, as on the game's map. The setting is on by default now (it was off since 0.546). Pure: the frame's facts in,
/// whether this click places the flag out.
/// </summary>
internal static class MapClickFlag
{
    /// <param name="enabled">The map setting « Left click places the flag ».</param>
    /// <param name="hovered">The mouse over the map itself (not over a panel drawn on it).</param>
    /// <param name="overIcon">An icon under the mouse: its own click wins (aetheryte, gathering point, vendor…).</param>
    /// <param name="overLink">A map link (door, region name) under the mouse: the click opens that map.</param>
    /// <param name="overBookmark">A bookmark under the mouse: the click flags the bookmark itself.</param>
    /// <param name="overPoint">A gathering point's tooltip shown.</param>
    /// <param name="overVista">A sightseeing log vista's tooltip shown.</param>
    /// <param name="territory">The shown map's territory; 0 for a region or world map (no flag there).</param>
    /// <param name="released">The left button released this frame.</param>
    /// <param name="dragged">The map was dragged since the button went down: a drag never places the flag.</param>
    public static bool Places(bool enabled,bool hovered,bool overIcon,bool overLink,bool overBookmark,bool overPoint,bool overVista,uint territory,bool released,bool dragged)
        =>enabled && hovered && !overIcon && !overLink && !overBookmark && !overPoint && !overVista && territory!=0 && released && !dragged;
}
