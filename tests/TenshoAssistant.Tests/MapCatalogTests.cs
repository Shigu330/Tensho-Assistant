using TenshoAssistant.Maps;

internal static class MapCatalogTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map catalog separates map and territory IDs and preserves submaps", () =>
        {
            var first = new MapDefinition(10, 100, "La Noscea", "Noscea centrale", "Étage supérieur", 1, 100);
            var source = new List<MapDefinition> { first, first with { MapId = 11, MapIndex = 2, Subzone = "Étage inférieur" } };
            var catalog = new MapCatalog(source); source.Clear();
            Check(catalog.Entries.Count == 2 && catalog.Find(10)?.TerritoryId == 100 && catalog.Find(100) is null,
                "map lookup never treats territory ID as map ID or merges floors");
            Check(catalog.Search("noscea ETAGE superieur").Single().MapId == 10 && catalog.Search("11").Single().MapIndex == 2,
                "native region zone subzone accent-insensitive multi-token and ID search");
            Throws<NotSupportedException>(() => ((IList<MapDefinition>)catalog.Entries).Clear());
            Throws<NotSupportedException>(() => ((IList<MapDefinition>)catalog.Search("noscea")).Clear());
        }),
        ("Map catalog rejects invalid identity duplicate IDs and cancelled work", () =>
        {
            var map = new MapDefinition(10, 100, "Region", "Zone", "", 1, 100);
            Check(new MapCatalog([map with { MapId = 0 }, map with { TerritoryId = 0 }, map with { Zone = "" },
                map with { SizeFactor = 0 }]).Entries.Count == 0, "unknown native rows do not become selectable zones");
            Throws<ArgumentException>(() => new MapCatalog([map, map]));
            using var cancel = new CancellationTokenSource(); cancel.Cancel();
            Throws<OperationCanceledException>(() => new MapCatalog([], cancel.Token));
            Check(!MapCatalog.CoordinatesValid(float.NaN, 10) && !MapCatalog.CoordinatesValid(10, float.PositiveInfinity)
                && !MapCatalog.CoordinatesValid(10001, 10) && MapCatalog.CoordinatesValid(20.5f, 30.1f), "native coordinate encoder protected from invalid inputs");
        }),
        ("Map service keeps loading failures and disposal distinct from an empty catalog", () =>
        {
            var service = new MapCatalogService();
            service.Progress(32, 100);
            Check(service.State.Catalog is null && service.State.Copied == 32, "pending catalog is not a known empty map list");
            var catalog = new MapCatalog([]); service.Publish(catalog);
            Check(ReferenceEquals(service.State.Catalog, catalog), "managed catalog published");
            service.Fail(new InvalidDataException("bad source"));
            Check(service.State.Catalog is null && service.State.Error == "bad source", "failure carries evidence");
            service.Clear();
            Check(service.State.Catalog is null && service.State.Error is null, "disposal clears service");
        }),
    ];
}
