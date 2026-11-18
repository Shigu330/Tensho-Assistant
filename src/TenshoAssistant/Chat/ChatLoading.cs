namespace TenshoAssistant.Chat;

/// <summary>
/// X-558 (0.1532, the user's request of 17/11: « load the new lines first and the old ones little by little, and maybe not more lines unless
/// one scrolls back to a certain point »; dalamud.log of 17/11: « Slow frame … Chat 55 ms » at a reload, up to 117 ms): how much of a tab is
/// laid out in a frame. The newest lines first (ChatTabView.NextToLayOut), within <see cref="BudgetMs"/> and <see cref="MaxPerFrame"/>; only the
/// <see cref="Eager"/> newest ahead of time; the older ones by packets of <see cref="Packet"/> just above the view when it is not at the bottom
/// (scrolling back), the rows they add kept above the view (no jump). Pure.
/// </summary>
public static class ChatLoading
{
    public const int Eager=300,Packet=40,MaxPerFrame=100;
    public const double BudgetMs=3;

    /// <summary>One more line may be laid out ahead in this frame: under the count and under the time.</summary>
    public static bool More(int done,double elapsedMs)=>done<MaxPerFrame && elapsedMs<BudgetMs;

    /// <summary>The scroll that keeps the same message at the top of the view when the rows above it changed by <paramref name="added"/>.</summary>
    public static float Kept(float scroll,int added,float lineHeight)=>Math.Max(0,scroll+added*lineHeight);

    /// <summary>
    /// X-563 M1 (review of 0.1534): ImGui.SetScrollY takes effect at the next frame only; in the frame a packet was laid out, the lines are
    /// drawn this much lower in the content (at the scroll <see cref="Kept"/> gives, minus the one ImGui still has), so the same message stays
    /// at the same place on the screen in that frame too.
    /// </summary>
    public static float Shift(float scroll,int added,float lineHeight)=>Kept(scroll,added,lineHeight)-scroll;
}
