using System.Threading.Channels;

namespace TenshoAssistant.Inventory;

/// <summary>Serializes synchronous persistence on a worker. Public tasks never fault.
/// Dispose ends the public lifetime immediately; an in-flight store call may finish afterwards,
/// but cannot publish. Completion denotes this public lifetime, not cancellation of synchronous IO.</summary>
public sealed class InventoryBankService : IDisposable
{
    private readonly object gate = new();
    private readonly IInventoryBankStore store;
    private readonly Channel<ImportRequest> requests = Channel.CreateBounded<ImportRequest>(1);
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private BankState state = new(Array.Empty<BankEntry>(), true, false, null);
    private ImportRequest? pending;
    private bool disposed;
    private bool loadFailed;

    public InventoryBankService(IInventoryBankStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        _ = Task.Run(RunAsync);
    }

    public Task Ready => ready.Task;
    public Task Completion => completion.Task;
    public BankState State => Volatile.Read(ref state);

    /// <summary>0.576 (I1): one storage's read, for a retainer sent away or an old storage; only on the player's click.</summary>
    public Task<BankImportResult> ForgetAsync(BankKey key)
    {
        lock (gate)
        {
            if (disposed) return Rejected("The bank is stopped.");
            if (state.Loading) return Rejected("The bank is still loading.");
            if (loadFailed) return Rejected("Bank loading failed; nothing is changed to protect the file.");
            if (pending is not null) return Rejected("An import is already running.");
            var request = new ImportRequest(null, 0, key);
            pending = request;
            Publish(new(state.Entries, false, true, null));
            if (!requests.Writer.TryWrite(request))
            {
                pending = null;
                Publish(new(state.Entries, false, false, state.Error));
                request.Result.TrySetResult(new(false, "The save service is unavailable."));
            }
            return request.Result.Task;
        }
    }

    /// <summary>0.345 (g, I-03): erases every saved storage of one character (the « Erase this character » button); queued like an import.</summary>
    public Task<BankImportResult> EraseCharacterAsync(ulong character)
    {
        lock (gate)
        {
            if (disposed) return Rejected("The bank is stopped.");
            if (state.Loading) return Rejected("The bank is still loading.");
            if (loadFailed) return Rejected("Bank loading failed; nothing is changed to protect the file.");
            if (pending is not null) return Rejected("An import is already running.");
            if (character == 0) return Rejected("Unknown character.");
            var request = new ImportRequest(null, character);
            pending = request;
            Publish(new(state.Entries, false, true, null));
            if (!requests.Writer.TryWrite(request))
            {
                pending = null;
                Publish(new(state.Entries, false, false, state.Error));
                request.Result.TrySetResult(new(false, "The save service is unavailable."));
            }
            return request.Result.Task;
        }
    }

    public Task<BankImportResult> ImportAsync(BankEntry entry)
    {
        lock (gate)
        {
            if (disposed) return Rejected("The bank is stopped.");
            if (state.Loading) return Rejected("The bank is still loading.");
            if (loadFailed) return Rejected("Bank loading failed; imports are blocked to protect the file.");
            if (pending is not null) return Rejected("An import is already running.");
            try { BankEntryValidator.Validate(entry); }
            catch (ArgumentException ex) { return Rejected(ex.Message); }

            var request = new ImportRequest(entry);
            pending = request;
            Publish(new(state.Entries, false, true, null));
            if (!requests.Writer.TryWrite(request))
            {
                pending = null;
                Publish(new(state.Entries, false, false, state.Error));
                request.Result.TrySetResult(new(false, "The save service is unavailable."));
            }
            return request.Result.Task;
        }
    }

    // Only touched by the worker: when each entry's file copy was last written in this session.
    private readonly Dictionary<BankKey, DateTimeOffset> savedAt = [];

    private async Task RunAsync()
    {
        try
        {
            try
            {
                var loaded = store.Load();
                ValidateEntries(loaded);
                lock (gate)
                {
                    if (disposed) return;
                    Publish(new(loaded, false, false, null));
                    ready.TrySetResult();
                }
            }
            catch (Exception ex)
            {
                lock (gate)
                {
                    if (disposed) return;
                    loadFailed = true;
                    Publish(new(state.Entries, false, false, $"The bank could not be loaded: {ex.Message}"));
                    ready.TrySetResult();
                }
            }

            await foreach (var request in requests.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                BankEntry[] next;
                BankEntry[] memory;
                // X-141 (0.1118): the change itself goes to the store (only the character concerned is written; erasing only on Erase).
                var change = request.Forget is { } forgotten ? new BankChange(Forget: forgotten)
                    : request.Erase != 0 ? new BankChange(Erase: request.Erase) : new BankChange(Put: request.Entry);
                lock (gate)
                {
                    if (disposed) return;
                    memory = state.Entries.ToArray();
                    next = change.On(memory);
                }
                string? error = null;
                // 0.605: the date is new in memory, the file is only rewritten when the contents changed or the saved date grows old.
                var unchanged = false;
                lock (gate) unchanged = request.Entry is { } incoming && BankEntryCompare.CanSkipSave(state.Entries.FirstOrDefault(e => e.Key == incoming.Key), incoming, savedAt);
                try { if (!unchanged) { next = store.Apply(change, Array.AsReadOnly(memory)); if (request.Entry is { } saved) savedAt[saved.Key] = saved.Snapshot.ObservedAt; } }
                catch (Exception ex) { error = $"Bank save failed: {ex.Message}"; }
                lock (gate)
                {
                    if (disposed) return;
                    Publish(new(error is null ? next : state.Entries, false, false, state.Error));
                    pending = null;
                    request.Result.TrySetResult(new(error is null, error ?? (request.Forget is not null ? "Read erased." : request.Erase != 0 ? "Character erased." : "Import saved in the bank.")));
                }
            }
        }
        catch (Exception ex)
        {
            lock (gate)
            {
                if (!disposed)
                {
                    loadFailed = true;
                    Publish(new(state.Entries, false, false, $"Bank service stopped: {ex.Message}"));
                    pending?.Result.TrySetResult(new(false, $"Bank service stopped: {ex.Message}"));
                    pending = null;
                }
            }
        }
        finally
        {
            ready.TrySetResult();
            completion.TrySetResult();
        }
    }

    internal static void ValidateEntries(IReadOnlyList<BankEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count > 4096) throw new ArgumentException("Too many bank entries.");
        var keys = new HashSet<BankKey>();
        foreach (var entry in entries)
        {
            BankEntryValidator.Validate(entry);
            if (!keys.Add(entry.Key)) throw new ArgumentException("Duplicate bank storage key.");
        }
    }

    private void Publish(BankState value) => Volatile.Write(ref state, value);
    private static Task<BankImportResult> Rejected(string message) => Task.FromResult(new BankImportResult(false, message));

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            requests.Writer.TryComplete();
            pending?.Result.TrySetResult(new(false, "Bank stopped; import cancelled."));
            pending = null;
            Publish(new(state.Entries, false, false, state.Error));
            ready.TrySetResult();
            completion.TrySetResult();
        }
    }

    // 0.345 (g, I-03): Erase != 0 = erase every entry of that character (its file is deleted by the store) instead of importing Entry.
    private sealed record ImportRequest(BankEntry? Entry, ulong Erase = 0, BankKey? Forget = null)
    {
        public TaskCompletionSource<BankImportResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
