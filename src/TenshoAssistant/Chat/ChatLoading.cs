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

    /// <summary>
    /// X-558 (0.1775): after the history is read back (ChatStore.Restore makes every tab's view again) each tab takes all the messages again;
    /// all tabs in one frame was one long frame (« Chat 50 ms » in the minute after the load of 22/11 03:56, « Chat refresh 97 ms » on 21/11
    /// 16:21). The shown tab first, always; then the others, from <paramref name="next"/> on and round, while the frame's refresh stays
    /// under <see cref="RefreshBudgetMs"/> (<see cref="RefreshMore"/>): a tab left for the next frame takes its messages then (nothing lost,
    /// each view keeps its place in the store). The places in <paramref name="count"/> tabs; <paramref name="shown"/> -1 when none.
    /// </summary>
    public static IReadOnlyList<int> RefreshOrder(int count,int shown,int next)
    {
        var order=new List<int>(count);
        if(shown>=0 && shown<count)order.Add(shown);
        if(count==0)return order;
        var start=((next%count)+count)%count;
        for(var step=0;step<count;step++)
        {
            var index=(start+step)%count;
            if(index!=shown)order.Add(index);
        }
        return order;
    }

    public const double RefreshBudgetMs=3,FrameBudgetMs=16;

    /// <summary>X-558 (0.1775): another tab may take its new messages in this frame.</summary>
    public static bool RefreshMore(double elapsedMs)=>elapsedMs<RefreshBudgetMs;
}

/// <summary>
/// X-558 (0.1775, the definition's « no frame over 16 ms due to the chat at the load, measured in the log »: the « Slow frame » line starts
/// at 100 ms): the chat window's frames in the first minute after the load. Each frame of <see cref="ChatLoading.FrameBudgetMs"/> or more is
/// said (at most <see cref="MaxSaid"/>, with its refresh part), then one line at the end of the minute: frames, the longest, how many over.
/// Pure: the window gives the times, the log writes the lines.
/// X-558 (0.1779): the slow frames of 22/11 (35 to 49.5 ms, 9.5 to 10.3 s after the load, the tabs' refresh 0.0 to 0.1 ms) each held a pause
/// of the runtime for a collection (« Runtime suspended every managed thread 26 to 34 ms », the same frame in dalamud.log): every managed
/// thread stopped, whatever code it ran. Each frame now also takes the runtime's pauses (GC.GetTotalPauseDuration) and the compilations on
/// the game's thread (JitInfo.GetCompilationTime) during it: the chat's own time is the rest, and the minute's line counts the frames over the
/// budget by that time apart.
/// </summary>
public sealed class ChatLoadFrames
{
    public const double MinuteMs=60_000;
    public const int MaxSaid=8;
    private int frames,over,overOwn,said;
    private double longest;
    private bool done;

    /// <summary>X-558 (0.1779): the minute is over and its line written: the window no longer times its frames.</summary>
    public bool Done=>done;

    /// <summary>One frame of the chat window, <paramref name="sinceLoadMs"/> after the load; the lines to write (none, one, or two at the end).</summary>
    /// <param name="pauseMs">X-558 (0.1779): the runtime's pauses for a collection during the frame (every managed thread stopped).</param>
    /// <param name="compileMs">X-558 (0.1779): the methods compiled on the game's thread during the frame.</param>
    public IReadOnlyList<string> Add(double sinceLoadMs,double frameMs,double refreshMs,double pauseMs=0,double compileMs=0)
    {
        if(done)return [];
        var lines=new List<string>();
        if(sinceLoadMs>=MinuteMs)
        {
            done=true;
            lines.Add(FormattableString.Invariant($"Chat window in the first minute after the load (X-558): {frames} frames, the longest {longest:F1} ms, {over} of {ChatLoading.FrameBudgetMs:F0} ms or more, {overOwn} by the chat's own time (the runtime's pauses and the compilations left out)."));
            return lines;
        }
        frames++;longest=Math.Max(longest,frameMs);
        if(frameMs<ChatLoading.FrameBudgetMs)return lines;
        over++;
        var own=Own(frameMs,pauseMs,compileMs);
        if(own>=ChatLoading.FrameBudgetMs)overOwn++;
        if(said++<MaxSaid)lines.Add(FormattableString.Invariant($"Chat window frame of {frameMs:F1} ms {sinceLoadMs/1000:F1} s after the load (X-558, budget {ChatLoading.FrameBudgetMs:F0} ms): its tabs' refresh {refreshMs:F1} ms; the runtime's pauses {pauseMs:F1} ms, compilation on the game's thread {compileMs:F1} ms: the chat's own {own:F1} ms."));
        return lines;
    }

    /// <summary>X-558 (0.1779): the chat's own part of a frame: what the runtime's pauses and the compilations leave (never below 0).</summary>
    public static double Own(double frameMs,double pauseMs,double compileMs)=>Math.Max(0,frameMs-Math.Max(0,pauseMs)-Math.Max(0,compileMs));
}
