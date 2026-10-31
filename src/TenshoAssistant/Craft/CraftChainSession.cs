using System.Collections.Frozen;
using TenshoAssistant.Inventory;

namespace TenshoAssistant.Craft;

public sealed record CraftChainInput(ulong CharacterId, uint ContextItemId, RecipeDefinition Recipe, int Requested,
    RecipeCatalog Catalog, InventorySnapshot? Live, BankState Bank, IReadOnlyDictionary<uint, uint>? RecipeChoices = null, int ChoiceRevision = 0);
public sealed record CraftChainState(CraftChainInput? Input, CraftChainResult? Result, DateTimeOffset? CalculatedAt, bool Loading, string? Error);

/// <summary>Owns manual calculations and rejects publications after context changes or disposal.</summary>
public sealed class CraftChainSession : IDisposable
{
    private readonly object gate = new();
    private CancellationTokenSource? cancellation;
    private int generation;
    private bool disposed;
    private CraftChainState state = new(null, null, null, false, null);
    public CraftChainState State => Volatile.Read(ref state);

    public Task StartAsync(CraftChainInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        input = input with { Recipe = input.Recipe with { Ingredients = Array.AsReadOnly(input.Recipe.Ingredients.ToArray()) },
            RecipeChoices = input.RecipeChoices?.ToFrozenDictionary() };
        lock (gate)
        {
            if (disposed) return Task.CompletedTask;
            cancellation?.Cancel(); cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var ticket = ++generation;
            state = new(input, null, null, true, null);
            return Task.Run(() =>
            {
                try
                {
                    var result = CraftChain.Build(input.Recipe, input.Requested, input.Catalog,
                        id => ItemHoldings.Build(id, input.CharacterId, input.Live, input.Bank.Entries, includeCrystals: true), token,
                        recipeChoices: input.RecipeChoices);
                    lock (gate)
                        if (!disposed && ticket == generation)
                            state = new(input, result, DateTimeOffset.UtcNow, false, null);
                }
                catch (Exception error)
                {
                    lock (gate)
                        if (!disposed && ticket == generation)
                            state = new(input, null, null, false, error is OperationCanceledException ? "Calcul annulé." : error.Message);
                }
            });
        }
    }

    public void ValidateContext(ulong character, uint contextItem, int requested, RecipeCatalog? catalog)
    {
        lock (gate)
        {
            if (disposed || state.Input is not { } input) return;
            if (input.CharacterId == character && input.ContextItemId == contextItem && input.Requested == requested
                && ReferenceEquals(input.Catalog, catalog)) return;
            Clear();
        }
    }
    public void Cancel()
    {
        lock (gate) { if (!disposed) Clear(); }
    }
    private void Clear()
    {
        generation++;
        cancellation?.Cancel();
        state = new(null, null, null, false, null);
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            Clear(); disposed = true;
            cancellation?.Dispose();
        }
    }
}
