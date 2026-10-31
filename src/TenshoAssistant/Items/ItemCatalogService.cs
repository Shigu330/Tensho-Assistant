namespace TenshoAssistant.Items;

public sealed record ItemCatalogState(ItemCatalog? Catalog, int RowsCopied, int TotalRows, string? Error);

public sealed class ItemCatalogService
{
    private ItemCatalogState state = new(null, 0, 0, null);
    public ItemCatalogState State => Volatile.Read(ref state);
    internal void Progress(int copied, int total) => Interlocked.Exchange(ref state, new(null, copied, total, null));
    internal void Publish(ItemCatalog catalog) => Interlocked.Exchange(ref state,
        new(catalog, state.RowsCopied, state.TotalRows, null));
    internal void Fail() => Interlocked.Exchange(ref state,
        new(null, state.RowsCopied, state.TotalRows, "Catalogue indisponible. Consultez le journal Dalamud."));
    internal void Clear() => Interlocked.Exchange(ref state, new(null, 0, 0, null));
}
