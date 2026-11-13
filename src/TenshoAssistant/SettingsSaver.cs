namespace TenshoAssistant;

/// <summary>
/// X-177 step 2 (0.1164): the settings saved off the game's thread. Measured on 2026-11-12 (dalamud.log, « Slow settings save » lines of 0.1162,
/// and a copy of the user's 63 KB settings): Dalamud's save took 64 to 71 ms on the game's thread at each save and 371 to 648 ms for the first
/// one after a load, while turning the settings into text takes about 2 ms once warm. So, on the game's thread, only the text is made
/// (<c>serialize</c>); a worker turns it back into a copy, checks that the copy gives the very same text (otherwise nothing of it is written)
/// and saves the copy. One worker, one write at a time, the latest text only (a later save never overtakes an earlier one; saves asked
/// while one is written are folded into the next). Any doubt falls back to the old way, the live settings saved on the game's thread
/// (<c>onGameThread</c>), and every save after it is made there too, as before; it is said once (<c>warn</c>). <see cref="Flush"/> at unload
/// writes what is left. <see cref="Warm"/> prepares the serializer from the saved file off the game's thread, so the first save is not cold.
/// </summary>
public sealed class SettingsSaver<T>(T live, Func<T, string> serialize, Func<string, T?> deserialize, Action<T> save, Action<Action> onGameThread, Action<Exception?, string> warn)
    where T : class
{
    private readonly object gate = new();
    private string? pending;
    private bool running;
    private bool direct;
    private bool closed;
    private bool warned;
    private Task worker = Task.CompletedTask;

    /// <summary>True once a doubt sent every save back to the game's thread.</summary>
    public bool Direct { get { lock (gate) return direct; } }
    /// <summary>Copies written by the worker (for the log and the tests).</summary>
    public int Written { get; private set; }

    /// <summary>On the game's thread: the settings as they are now will be written.</summary>
    public void Save()
    {
        bool now;
        lock (gate) now = direct || closed;
        if (now) { SaveLive(); return; }
        string text;
        try { text = serialize(live); }
        catch (Exception error) { Fallback(error, "The settings could not be turned into text off the game's thread; they are saved on it, as before."); SaveLive(); return; }
        lock (gate)
        {
            if (direct || closed) { now = true; }
            else
            {
                pending = text;
                if (!running) { running = true; worker = Task.Run(Drain); }
            }
        }
        if (now) SaveLive();
    }

    /// <summary>The old way, after the worker's write under way (so an older copy never lands over the live settings).</summary>
    private void SaveLive()
    {
        Task under;
        lock (gate) under = worker;
        try { under.Wait(TimeSpan.FromSeconds(5)); }
        catch (AggregateException) { }
        save(live);
    }

    private void Drain()
    {
        while (true)
        {
            string text;
            lock (gate)
            {
                if (pending is null || direct) { pending = null; running = false; Monitor.PulseAll(gate); return; }
                text = pending; pending = null;
            }
            try
            {
                var copy = deserialize(text);
                if (copy is null || serialize(copy) != text) { Fallback(null, "A copy of the settings did not give the same text; they are saved on the game's thread, as before."); onGameThread(() => save(live)); continue; }
                save(copy);
                lock (gate) Written++;
            }
            catch (Exception error)
            {
                Fallback(error, "The settings could not be saved off the game's thread; they are saved on it, as before.");
                onGameThread(() => save(live));
            }
        }
    }

    private void Fallback(Exception? error, string message)
    {
        bool say;
        lock (gate) { direct = true; say = !warned; warned = true; }
        if (say) warn(error, message);
    }

    /// <summary>Off the game's thread, once at load: the serializer prepared from the saved file's text (nothing written, errors ignored).</summary>
    public Task Warm(Func<string?> savedText) => Task.Run(() =>
    {
        try { if (savedText() is { Length: > 0 } text && deserialize(text) is { } copy) _ = serialize(copy); }
        catch (Exception) { }
    });

    /// <summary>
    /// At unload (game's thread): waits for the write under way, then writes the live settings if anything was left. When the wait runs out,
    /// the live settings' text is handed to the worker, written right after its write under way (never two writes at once, never an older
    /// one last); false then.
    /// </summary>
    public bool Flush(TimeSpan wait)
    {
        Task under;
        lock (gate) { closed = true; under = worker; }
        var done = false;
        try { done = under.Wait(wait); }
        catch (AggregateException) { done = true; }
        if (!done)
        {
            string? text = null;
            try { text = serialize(live); }
            catch (Exception) { }
            lock (gate) if (text is not null && running && !direct) { pending = text; return false; }
        }
        bool left;
        lock (gate) { left = pending is not null || direct || !done; pending = null; }
        if (left) save(live);
        return done;
    }

    /// <summary>
    /// X-184 N22 (0.1207, night review of 12/11): at unload, after <see cref="Flush"/> handed the live settings to the worker (a write of more
    /// than its wait: a scanner, OneDrive): waits once more, longer, until the worker has written them; true once they are on disk. Without it
    /// the plugin's unload went on, the game could close with the last settings never written, and the log said they were saved.
    /// </summary>
    public bool Settle(TimeSpan wait)
    {
        Task under;
        lock (gate) under = worker;
        try { return under.Wait(wait); }
        catch (AggregateException) { return true; }
    }
}
