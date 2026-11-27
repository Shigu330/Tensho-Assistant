using System.Diagnostics.Tracing;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-891 (0.1916, docs/evidence/0.1916-event-listener-deadlock.md): the one gate under which every listener of the runtime's own events
/// (EventListener on Microsoft-Windows-DotNETRuntime) is made and disposed in this process. The runtime's library takes two locks in both
/// orders: a listener disposed takes its listeners' lock and then the EventPipe dispatcher's, while another disposed at the same time takes
/// the dispatcher's lock and then, in the runtime's enable callback, the listeners' lock (the same holds for a listener made beside one
/// disposed). Both threads then wait for ever, and every thread that later makes any EventSource waits on the listeners' lock too: the whole
/// process stands still, its watchdog included (the frozen test process of 27/11, read from its memory image). Made and disposed one at a
/// time, never at once, the two locks are never taken in both orders.
/// </summary>
public static class RuntimeListeners
{
    /// <summary>Held while a listener of the runtime's events is made or disposed (re-entrant: a Monitor).</summary>
    public static readonly object Gate=new();

    /// <summary>A listener made under <see cref="Gate"/> (its constructor enables the runtime's source at once).</summary>
    public static T Open<T>(Func<T> make) where T:EventListener
    {
        lock(Gate)return make();
    }
}
