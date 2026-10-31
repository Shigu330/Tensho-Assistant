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

    public Task<BankImportResult> ImportAsync(BankEntry entry)
    {
        lock (gate)
        {
            if (disposed) return Rejected("La banque est arrêtée.");
            if (state.Loading) return Rejected("La banque est encore en cours de chargement.");
            if (loadFailed) return Rejected("Le chargement de la banque a échoué ; les imports sont bloqués pour préserver le fichier.");
            if (pending is not null) return Rejected("Un import est déjà en cours.");
            try { BankEntryValidator.Validate(entry); }
            catch (ArgumentException ex) { return Rejected(ex.Message); }

            var request = new ImportRequest(entry);
            pending = request;
            Publish(new(state.Entries, false, true, null));
            if (!requests.Writer.TryWrite(request))
            {
                pending = null;
                Publish(new(state.Entries, false, false, state.Error));
                request.Result.TrySetResult(new(false, "Le service de sauvegarde est indisponible."));
            }
            return request.Result.Task;
        }
    }

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
                    Publish(new(state.Entries, false, false, $"Impossible de charger la banque : {ex.Message}"));
                    ready.TrySetResult();
                }
            }

            await foreach (var request in requests.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                BankEntry[] next;
                lock (gate)
                {
                    if (disposed) return;
                    next = state.Entries.Where(e => e.Key != request.Entry.Key).Append(request.Entry).ToArray();
                }
                string? error = null;
                try { store.Save(Array.AsReadOnly(next)); }
                catch (Exception ex) { error = $"Échec de sauvegarde de la banque : {ex.Message}"; }
                lock (gate)
                {
                    if (disposed) return;
                    Publish(new(error is null ? next : state.Entries, false, false, state.Error));
                    pending = null;
                    request.Result.TrySetResult(new(error is null, error ?? "Import sauvegardé dans la banque."));
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
                    Publish(new(state.Entries, false, false, $"Service de banque arrêté : {ex.Message}"));
                    pending?.Result.TrySetResult(new(false, $"Service de banque arrêté : {ex.Message}"));
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
            pending?.Result.TrySetResult(new(false, "Banque arrêtée ; import annulé."));
            pending = null;
            Publish(new(state.Entries, false, false, state.Error));
            ready.TrySetResult();
            completion.TrySetResult();
        }
    }

    private sealed record ImportRequest(BankEntry Entry)
    {
        public TaskCompletionSource<BankImportResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
