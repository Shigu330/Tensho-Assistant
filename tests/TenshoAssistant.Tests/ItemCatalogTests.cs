using TenshoAssistant.Items;

internal static class ItemCatalogTests
{
    private static readonly ItemCatalogMetadata Metadata = new("test/Item", "French", "game=test;schema=1", 1);
    private static ItemCatalog Catalog() => ItemCatalog.Build(
        [new(12892, "Sirop de bouleau", true), new(4551, "Potion", true), new(10, "Éclat de feu", false),
            new(11, "Éclat de glace", false), new(0, "Sentinelle", false), new(12, "  ", false)], Metadata);

    public static (string Name, Action Run)[] Cases =>
    [
        ("Item search ignores accents and case and matches every word", () =>
        {
            var catalog = Catalog();
            Check(catalog.Search("ECLAT feu").Single().Id == 10, "accent/case and multiword");
            Check(catalog.Search("bouleau sirop").Single().Id == 12892, "term order irrelevant");
            Check(catalog.Search("sirop glace").Count == 0, "all terms required");
            Check(catalog.Search("pOt").Single().Name == "Potion", "substring");
        }),
        ("Item numeric search resolves exact base ID", () =>
        {
            Check(Catalog().Search("12892").Single().Name == "Sirop de bouleau", "ID maps to name");
            Check(Catalog().Search(" 4551 ").Single().Id == 4551, "whitespace");
            Check(Catalog().Search("99999").Count == 0, "unknown ID has no invented row");
            Check(Catalog().Find(0) is null && Catalog().Find(12892)!.CanBeHq, "sentinel excluded, HQ capability retained");
        }),
        ("Item results are deterministic and bounded", () =>
        {
            var items = Enumerable.Range(1, 200).Reverse().Select(i => new ItemDefinition((uint)i, $"Objet {i:D3}", false));
            var catalog = ItemCatalog.Build(items, Metadata);
            Check(catalog.Search("objet").Count == 30, "default result cap");
            Check(catalog.Search("objet", 2).Select(i => i.Id).SequenceEqual([1u, 2u]), "stable order and custom cap");
            Check(catalog.Search(" ").Count == 0 && catalog.Search(new string('x', 129)).Count == 0, "empty/oversized query bounded");
            Throws<ArgumentOutOfRangeException>(() => catalog.Search("objet", 0));
            Throws<ArgumentOutOfRangeException>(() => catalog.Search("objet", 101));
        }),
        ("Item index owns its data and keeps generation context", () =>
        {
            var source = new List<ItemDefinition> { new(4551, "Potion", true, 12345) };
            var catalog = ItemCatalog.Build(source, Metadata);
            source.Clear();
            Check(catalog.Count == 1 && catalog.Find(4551)!.Name == "Potion", "source list not retained");
            Check(catalog.Find(4551)!.IconId == 12345 && catalog.Search("Potion").Single().IconId == 12345,
                "game icon ID retained by lookup and search");
            Check(catalog.Metadata == Metadata, "source/language/version/generation retained");
            Throws<ArgumentException>(() => ItemCatalog.Build([new(1, "A", false), new(1, "B", true)], Metadata));
            Check(Catalog().Count == 4, "empty/sentinel names excluded");
        }),
        ("Item index supports cancellation and error/publication state", () =>
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Throws<OperationCanceledException>(() => ItemCatalog.Build([new(1, "A", false)], Metadata, cancellation.Token));
            var service = new ItemCatalogService();
            Check(service.State.Catalog is null && service.State.Error is null, "initial loading not fake empty catalog");
            service.Progress(3, 10);
            service.Publish(Catalog());
            var published = service.State;
            Check(published.Catalog!.Count == 4 && published.RowsCopied == 3 && published.TotalRows == 10, "atomic publication context");
            service.Fail();
            Check(service.State.Catalog is null && service.State.Error is not null, "failure explicit");
            service.Clear();
            Check(service.State.Catalog is null && service.State.Error is null, "dispose state cleared");
            Check(published.Catalog.Count == 4, "existing readers keep immutable index");
        }),
    ];

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }
}
