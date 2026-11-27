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
/// X-897 (review of 0.1916 M2): one gate for the whole process, not one per load of the plugin. Each reload in Dalamud opens a new load
/// context with its own statics: a listener of the old load disposed late (the chat's, 2 s after its first frame) and one the new load
/// made took two different gates, so the deadlock stayed possible across a reload. The gate is now a plain object kept in the process's
/// data (AppDomain.SetData), shared by every load of the plugin and by the desktop app (which loads the plugin in its own process); a
/// System.Object, so it never keeps an unloaded load context alive.
/// </summary>
public static class RuntimeListeners
{
    /// <summary>The name of the gate in the process's data (AppDomain.GetData), the same for every load of the plugin.</summary>
    internal const string GateKey="TenshoAssistant.RuntimeListeners.Gate";

    /// <summary>Held while a listener of the runtime's events is made or disposed (re-entrant: a Monitor). X-897: the process's one gate.</summary>
    public static readonly object Gate=Shared();

    /// <summary>X-897: the gate kept in the process's data, made by the first load that asks (under the domain object, shared by every load).</summary>
    private static object Shared()
    {
        var domain=AppDomain.CurrentDomain;
        lock(domain)
        {
            if(domain.GetData(GateKey) is { } found && found.GetType()==typeof(object))return found;
            var made=new object();
            domain.SetData(GateKey,made);
            return made;
        }
    }

    /// <summary>A listener made under <see cref="Gate"/> (its constructor enables the runtime's source at once).</summary>
    public static T Open<T>(Func<T> make) where T:EventListener
    {
        lock(Gate)return make();
    }
}

/// <summary>
/// X-897 (review of 0.1916 M2): a disposable let go later by a work of its own (the chat's listener, named 2 s after the first frame) and
/// owned meanwhile: <see cref="Take"/> gives it once, to the later work or to the owner's Dispose, never to both; Dispose lets it go at
/// once when nobody took it (the window closed at an unload before the 2 s ended: the later work then finds nothing and does nothing).
/// </summary>
public sealed class Held<T>(T value):IDisposable where T:class,IDisposable
{
    private T? held=value;
    /// <summary>The value, once: null after it was taken or let go.</summary>
    public T? Take()=>Interlocked.Exchange(ref held,null);
    public void Dispose()=>Take()?.Dispose();
}
