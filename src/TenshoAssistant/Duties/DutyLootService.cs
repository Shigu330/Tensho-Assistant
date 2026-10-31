using System.Net.Http;
using TenshoAssistant.Community;

namespace TenshoAssistant.Duties;

public sealed record DutyLootState(DutyDefinition? Duty, DutyLootDocument? Document, bool Loading, string? Error);

/// <summary>Only explicit RequestAsync performs network I/O; selection and cache reads are local.</summary>
public sealed class DutyLootService : IDisposable
{
    public const int CacheCapacity = 32;
    public static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan RequestInterval = GarlandClient.RequestInterval;
    private readonly object gate = new();
    private readonly GarlandClient transport;
    private readonly bool ownsTransport;
    private readonly TimeProvider clock;
    private readonly Dictionary<uint, DutyLootDocument> cache = [];
    private CancellationTokenSource? request;
    private DutyLootState state = new(null,null,false,null);
    private long generation;
    private bool disposed;
    public DutyLootState State => Volatile.Read(ref state);
    public TimeSpan CacheLifetime=>transport.CacheLifetime;
    public DutyLootService(HttpClient? client = null, TimeProvider? clock = null)
    {
        ownsTransport = true;
        transport = new GarlandClient(client,clock);
        this.clock = clock ?? TimeProvider.System;
    }
    public DutyLootService(GarlandClient transport, TimeProvider? clock = null)
    { this.transport = transport; this.clock = clock ?? TimeProvider.System; }
    public void Select(DutyDefinition? duty)
    {
        lock (gate)
        {
            if (disposed || Equals(state.Duty,duty)) return;
            if (duty is not null && (duty.DutyId == 0 || duty.InstanceId == 0 || duty.TerritoryId == 0))
                throw new ArgumentException("Invalid duty identity.",nameof(duty));
            generation++; request?.Cancel();
            state = new(duty,duty is null ? null : cache.GetValueOrDefault(duty.InstanceId),false,null);
        }
    }
    public void Cancel()
    {
        lock (gate)
        {
            if (disposed || !state.Loading) return;
            generation++; request?.Cancel();
            state = state with { Loading = false, Error = "Consultation annulée." };
        }
    }
    public async Task RequestAsync(bool force = false, CancellationToken cancellation = default)
    {
        DutyDefinition duty;
        long ticket;
        CancellationTokenSource source;
        lock (gate)
        {
            if (disposed || state.Duty is null || state.Loading || cancellation.IsCancellationRequested) return;
            duty = state.Duty;
            if (!force && cache.TryGetValue(duty.InstanceId,out var saved) && Caching.CacheAge.Fresh(saved.FetchedAt,clock.GetUtcNow(),CacheLifetime))
            { state = state with { Document = saved, Error = null }; return; }
            source = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            source.CancelAfter(TimeSpan.FromSeconds(12));
            request = source; ticket = ++generation;
            state = state with { Loading = true, Error = null };
        }
        try
        {
            var response = await transport.ReadDocumentAsync(GarlandResource.Instance,duty.InstanceId,source.Token,force).ConfigureAwait(false);
            var parsed = DutyLootParser.Parse(response.Json,duty.InstanceId,response.FetchedAt,source.Token);
            lock (gate)
            {
                if (disposed || ticket != generation || source.IsCancellationRequested) return;
                if (!cache.ContainsKey(duty.InstanceId) && cache.Count >= CacheCapacity)
                    cache.Remove(cache.MinBy(pair => pair.Value.FetchedAt).Key);
                cache[duty.InstanceId] = parsed;
                transport.Remember(GarlandResource.Instance,duty.InstanceId,response);
                state = new(duty,parsed,false,null);
            }
        }
        catch (Exception error)
        {
            lock (gate)
            {
                if (disposed || ticket != generation) return;
                state = state with { Loading = false, Error = error is OperationCanceledException
                    ? "Consultation annulée ou délai dépassé." : $"Lecture du butin impossible : {error.Message}" };
            }
        }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(request,source))
                {
                    request = null;
                    // A borrowed handler may ignore cancellation and return late without throwing.
                    if (!disposed && ticket == generation && state.Loading)
                        state = state with { Loading = false, Error = "Consultation annulée ou délai dépassé." };
                }
                source.Dispose();
            }
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true; generation++; request?.Cancel(); cache.Clear();
            state = new(null,null,false,null);
            if (ownsTransport) transport.Dispose();
        }
    }
}
