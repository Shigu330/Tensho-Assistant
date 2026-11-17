namespace TenshoAssistant.Beastmaster;

/// <summary>
/// X-551 (0.1534, the user's request of 17/11: « look more like the game's without growing too much »): the team panel's widest size: the
/// width of the game's window it stands beside (the team window when it is open, else the board window), between <see cref="Least"/> and
/// <see cref="Most"/> (before: always 460); its lines wrap inside it, the window's padding taken off. Pure.
/// </summary>
public static class BeastPanelSize
{
    public const float Least = 300, Most = 460, Padding = 24;
    public static float Width(float beside) => float.IsFinite(beside) && beside > 0 ? Math.Clamp(beside, Least, Most) : Most;
    public static float Wrap(float width) => Math.Max(Least, width) - Padding;
}
