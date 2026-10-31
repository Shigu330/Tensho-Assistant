namespace TenshoAssistant.Market;

public sealed record PreparationPriceContext(ulong CharacterId, uint WorldId, uint ContextItemId, uint RecipeId, int Requested, int Mode, int Quality,
    bool IsChain = false, int ChainRevision = 0);
public sealed record PreparationPriceState(PreparationPriceContext? Context, IReadOnlyList<uint> Items, int Completed,
    uint CurrentItem, bool Loading, string? Error);

/// <summary>Owns one user-started, bounded price list, using the shared service cache and network cadence.</summary>
public sealed class PreparationPriceSession : IDisposable
{
    private readonly object gate = new();
    private CancellationTokenSource? cancellation;
    private int generation;
    private bool disposed;
    private PreparationPriceState state = new(null,[],0,0,false,null);
    public PreparationPriceState State => Volatile.Read(ref state);

    public Task StartAsync(MarketService market, PreparationPriceContext context, IEnumerable<uint> ids)
    {
        ArgumentNullException.ThrowIfNull(market); ArgumentNullException.ThrowIfNull(context); ArgumentNullException.ThrowIfNull(ids);
        var copied=ids.Distinct().ToArray();
        if (copied.Length > 32 || copied.Any(id=>id==0) || context.CharacterId==0 || context.WorldId==0 || context.ContextItemId==0
            || context.RecipeId==0 || context.Requested<1) throw new ArgumentException("Liste de prix invalide.");
        lock (gate)
        {
            if (disposed) return Task.CompletedTask;
            cancellation?.Cancel(); cancellation?.Dispose();
            cancellation=new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var token=cancellation.Token; var ticket=++generation;
            var list=Array.AsReadOnly(copied);
            state=new(context,list,0,0,true,null);
            return Task.Run(async ()=>
            {
                var completed=0;
                try
                {
                    foreach (var id in copied)
                    {
                        token.ThrowIfCancellationRequested();
                        lock (gate) { if (disposed || ticket!=generation) return; state=new(context,list,completed,id,true,null); }
                        while (true)
                        {
                            token.ThrowIfCancellationRequested();
                            var result=await market.RequestForPreparationAsync(id,context.CharacterId,context.WorldId,token).ConfigureAwait(false);
                            if (result.Status==MarketFetchStatus.Success) break;
                            if (result.Status is MarketFetchStatus.Busy or MarketFetchStatus.Cooldown or MarketFetchStatus.Preempted)
                            {
                                var delay=result.RetryAfter > TimeSpan.Zero ? result.RetryAfter : TimeSpan.FromMilliseconds(250);
                                await Task.Delay(delay < TimeSpan.FromMilliseconds(25) ? TimeSpan.FromMilliseconds(25) : delay,token).ConfigureAwait(false);
                                continue;
                            }
                            throw new InvalidOperationException(result.Error ?? (result.Status==MarketFetchStatus.ScopeChanged ? "Personnage ou monde modifié." : "Consultation interrompue."));
                        }
                        completed++;
                    }
                    lock (gate) { if (!disposed && ticket==generation) state=new(context,list,completed,0,false,null); }
                }
                catch (Exception error)
                {
                    lock (gate)
                        if (!disposed && ticket==generation) state=new(context,list,completed,0,false,
                            error is OperationCanceledException ? "Consultation annulée ou délai de deux minutes atteint." : error.Message);
                }
                finally
                {
                    lock(gate) { if(ticket==generation) {cancellation?.Dispose(); cancellation=null;} }
                }
            });
        }
    }
    public void ValidateContext(ulong character,uint world,uint contextItem,int requested,int mode,int quality,int chainRevision = 0)
    {
        lock (gate)
            if (!disposed && state.Context is { } input && (input.CharacterId!=character || input.WorldId!=world
                || input.ContextItemId!=contextItem || input.Requested!=requested || input.Quality!=quality
                || (input.IsChain ? input.ChainRevision!=chainRevision : input.Mode!=mode))) Clear();
    }
    private void Clear() { generation++; cancellation?.Cancel(); cancellation?.Dispose(); cancellation=null; state=new(null,[],0,0,false,null); }
    public void Cancel() { lock(gate) {if(!disposed) Clear();} }
    public void Dispose() { lock(gate) {if(disposed)return; Clear(); disposed=true; cancellation?.Dispose();} }
}
