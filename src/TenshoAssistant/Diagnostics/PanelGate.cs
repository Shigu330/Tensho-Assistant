namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-01 (0.1279, user's decision of 13/11 around 11:10, X-177 C4.2): the free panels, the bars and the other parts of the overlay pass were
/// all run on the plugin's first frame, before any of their code was compiled: 152 to 338 ms in one frame at each load (dalamud.log of
/// 13/11, 38 loads), each panel 2 to 66 ms of first-run compiling. They now wait until the warm-up has compiled their code and what it calls
/// (<see cref="JitWarmup.FirstPartDone"/>), <see cref="Bound"/> at most after the load, and are drawn from then on: the panels come back 1 to
/// 6 s after a load instead of freezing the game. Asked from the draw only; once open it stays open, whatever the warm-up does next.
/// </summary>
public sealed class PanelGate
{
    /// <summary>
    /// How long the panels wait at most after the load. X-653 (user decision D-194 b of 20/11, follow-up of X-352): 6 s, was 3 s (decision of
    /// 13/11). Measured on 20/11: after a reload during play their code is ready 3.8 to 4.9 s after the load, so at 3 s they opened before it
    /// and their first frame took 130 to 190 ms; at game start it is ready at 1.8 s, unchanged.
    /// </summary>
    public static readonly TimeSpan Bound=TimeSpan.FromSeconds(6);
    private readonly Func<bool> compiled;
    private readonly Func<long> now;
    private readonly long started;
    private readonly long bound;
    private bool open;

    /// <param name="compiled">Whether the panels' code is compiled; an error counts as yes (the panels are never kept away by it).</param>
    /// <param name="now">The clock in milliseconds (the tests' own; the machine's by default).</param>
    /// <param name="bound"><see cref="Bound"/> by default.</param>
    /// <summary>X-564 (0.1631): a panel is drawn once the gate is open, or earlier when its own <paramref name="early"/> says so (the chat
    /// window, once its first frame's code is compiled: it waited for the gate, 6 s after the load, with its code ready at 2.2 s).</summary>
    public static bool Lets(bool open,Func<bool>? early)=>open || early?.Invoke()==true;

    public PanelGate(Func<bool> compiled,Func<long>? now=null,TimeSpan? bound=null)
    {
        this.compiled=compiled;
        this.now=now ?? (static ()=>Environment.TickCount64);
        started=this.now();
        this.bound=(long)(bound ?? Bound).TotalMilliseconds;
    }

    /// <summary>Whether the panels may be drawn: their code is compiled, or the bound is over. Once true, it stays true.</summary>
    public bool Open
    {
        get
        {
            if(open)return true;
            bool ready;
            try{ready=compiled();}
            catch(Exception){ready=true;}
            var elapsed=now()-started;
            if(!ready && elapsed<bound)return false;
            open=true;
            OpenedAfter=elapsed;
            ByBound=!ready;
            return true;
        }
    }

    /// <summary>How long after the load the gate opened, in milliseconds (-1 while it is closed).</summary>
    public long OpenedAfter{get;private set;}=-1;
    /// <summary>Whether it opened at the bound rather than because the code was compiled.</summary>
    public bool ByBound{get;private set;}
}
