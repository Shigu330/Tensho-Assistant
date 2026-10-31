using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Maps;

internal static class MapBookmarkTests
{
    private static readonly MapDefinition Map = new(10, 100, "Region", "Zone", "Étage 1", 1, 100);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map bookmarks preserve exact map territory coordinates and identity through configuration", () =>
        {
            var point = MapBookmarks.Create(Map, "  Récolte  ", 10.25f, 30.75f);
            Check(point.Id != Guid.Empty && point.Name == "Récolte" && point.MapId == 10 && point.TerritoryId == 100, "stable identity distinct from native row IDs");
            var config = new Configuration { MapBookmarks = [point] };
            var copied = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;
            Check(copied.MapBookmarks.Single() == point && copied.MapBookmarks[0].X == 10.25f, "identity and exact coordinates roundtrip");
            Check(JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!.MapBookmarks.Count == 0, "legacy configuration starts without bookmarks");
        }),
        ("Map bookmark edits copy producer collections and preserve unchanged entries", () =>
        {
            var first = MapBookmarks.Create(Map, "First", 10, 20);
            var second = MapBookmarks.Create(Map, "Second", 30, 40);
            var source = new List<MapBookmark> { first };
            var added = MapBookmarks.Add(source, second); source.Clear();
            Check(added.Count == 2 && added[0] == first, "source list detached");
            var edited = MapBookmarks.Replace(added, second with { Name = "Changed", X = 31 });
            Check(edited[1].Id == second.Id && edited[1].X == 31 && added[1].Name == "Second", "edit keeps ID and old snapshot");
            var removed = MapBookmarks.RemoveAt(edited, 0);
            Check(removed.Single().Id == second.Id && edited.Count == 2, "removal affects only chosen row");
            Throws<NotSupportedException>(() => ((IList<MapBookmark>)added).Clear());
            Throws<InvalidOperationException>(() => MapBookmarks.Add(added, first with { Id = Guid.NewGuid(), Name = " first " }));
        }),
        ("Map bookmarks reject invalid names coordinates duplicates and capacity overflow", () =>
        {
            Throws<ArgumentException>(() => MapBookmarks.Create(Map, " ", 10, 20));
            Throws<ArgumentException>(() => MapBookmarks.Create(Map, "line\nbreak", 10, 20));
            Throws<ArgumentException>(() => MapBookmarks.Create(Map, new string('x', 81), 10, 20));
            Throws<ArgumentException>(() => MapBookmarks.Create(Map, "Invalid", float.NaN, 20));
            Throws<ArgumentException>(() => MapBookmarks.Create(Map with { MapId = 0 }, "Invalid", 10, 20));
            var point = MapBookmarks.Create(Map, "Point", 10, 20);
            Throws<ArgumentException>(() => MapBookmarks.Add([], point with { Id = Guid.Empty }));
            var full = Enumerable.Range(0, MapBookmarks.Capacity).Select(index => point with { Id = Guid.NewGuid(), Name = $"Point {index}" }).ToArray();
            Throws<InvalidOperationException>(() => MapBookmarks.Add(full, point));
            Throws<KeyNotFoundException>(() => MapBookmarks.Replace([], point));
            Throws<ArgumentOutOfRangeException>(() => MapBookmarks.RemoveAt([], 0));
        }),
        ("Map bookmarks preserve unavailable points without reinterpreting another territory", () =>
        {
            var point = MapBookmarks.Create(Map, "Saved", 10, 20);
            var catalog = new MapCatalog([Map]);
            Check(MapBookmarks.TryResolve(point, catalog, out var map) && map == Map, "native pair resolves");
            Check(!MapBookmarks.TryResolve(point, new MapCatalog([Map with { TerritoryId = 101 }]), out _), "same map ID reused in another territory is not accepted");
            Check(!MapBookmarks.TryResolve(point, new MapCatalog([]), out _)
                && !MapBookmarks.TryResolve(point with { X = float.PositiveInfinity }, catalog, out _), "missing maps and invalid coordinates cannot open native map");
            var preserved = MapBookmarks.Add([point with { Id = Guid.NewGuid(), MapId = 999, Name = null! }], point);
            Check(preserved[0].MapId == 999 && preserved.Count == 2, "unavailable old records not silently discarded");
        }),
        ("Map bookmark persistence rolls back failed saves and leaves input untouched on validation errors", () =>
        {
            var point = MapBookmarks.Create(Map, "Saved", 10, 20);
            var config = new Configuration { MapBookmarks = [point] };
            var previous = config.MapBookmarks;
            Throws<IOException>(() => MapBookmarkPersistence.Commit(config, () => throw new IOException("disk"), items => MapBookmarks.RemoveAt(items, 0)));
            Check(ReferenceEquals(config.MapBookmarks, previous) && config.MapBookmarks.Single() == point, "failed persistence restores exact prior list");
            var saves = 0;
            Throws<InvalidOperationException>(() => MapBookmarkPersistence.Commit(config, () => saves++, items => MapBookmarks.Add(items, point)));
            Check(saves == 0 && ReferenceEquals(config.MapBookmarks, previous), "invalid edit never invokes persistence");
            MapBookmarkPersistence.Commit(config, () => saves++, items => MapBookmarks.RemoveAt(items, 0));
            Check(saves == 1 && config.MapBookmarks.Count == 0 && previous.Count == 1, "successful save publishes new list only");
        }),
    ];
}
