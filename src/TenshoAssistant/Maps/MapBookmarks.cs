namespace TenshoAssistant.Maps;

public sealed record MapBookmark(Guid Id, string Name, uint MapId, uint TerritoryId, float X, float Y);

public static class MapBookmarks
{
    public const int Capacity = 200;
    public const int NameLimit = 80;

    public static MapBookmark Create(MapDefinition map, string name, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (map.MapId == 0 || map.TerritoryId == 0 || !MapCatalog.CoordinatesValid(x, y))
            throw new ArgumentException("Carte ou coordonnées invalides.");
        return new(Guid.NewGuid(), ValidateName(name), map.MapId, map.TerritoryId, x, y);
    }
    public static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var trimmed = name.Trim();
        if (trimmed.Length is < 1 or > NameLimit || trimmed.Any(char.IsControl))
            throw new ArgumentException($"Le nom doit contenir de 1 à {NameLimit} caractères, sans saut de ligne.");
        return trimmed;
    }
    public static bool TryResolve(MapBookmark? bookmark, MapCatalog catalog, out MapDefinition? map)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        map = bookmark is null ? null : catalog.Find(bookmark.MapId);
        return bookmark is not null && bookmark.Id != Guid.Empty && map is not null
            && map.TerritoryId == bookmark.TerritoryId && MapCatalog.CoordinatesValid(bookmark.X, bookmark.Y);
    }
    public static IReadOnlyList<MapBookmark> Add(IReadOnlyList<MapBookmark> source, MapBookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidateBookmark(bookmark);
        if (source.Count >= Capacity) throw new InvalidOperationException($"La limite est de {Capacity} favoris.");
        if (source.Any(existing => existing?.Id == bookmark.Id || Equivalent(existing, bookmark)))
            throw new InvalidOperationException("Ce repère existe déjà dans les favoris.");
        return Array.AsReadOnly(source.Append(bookmark).ToArray());
    }
    public static IReadOnlyList<MapBookmark> Replace(IReadOnlyList<MapBookmark> source, MapBookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidateBookmark(bookmark);
        var index = Enumerable.Range(0, source.Count).FirstOrDefault(index => source[index]?.Id == bookmark.Id, -1);
        if (index < 0) throw new KeyNotFoundException("Le favori à modifier n'existe plus.");
        if (source.Where((_, position) => position != index).Any(existing => Equivalent(existing, bookmark)))
            throw new InvalidOperationException("Un autre favori contient déjà ce repère.");
        var copied = source.ToArray(); copied[index] = bookmark;
        return Array.AsReadOnly(copied);
    }
    public static IReadOnlyList<MapBookmark> RemoveAt(IReadOnlyList<MapBookmark> source, int index)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (index < 0 || index >= source.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return Array.AsReadOnly(source.Where((_, position) => position != index).ToArray());
    }
    private static void ValidateBookmark(MapBookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        if (bookmark.Id == Guid.Empty || bookmark.MapId == 0 || bookmark.TerritoryId == 0 || !MapCatalog.CoordinatesValid(bookmark.X, bookmark.Y))
            throw new ArgumentException("Favori invalide.");
        _ = ValidateName(bookmark.Name);
    }
    private static bool Equivalent(MapBookmark? left, MapBookmark right) => left is not null
        && left.MapId == right.MapId && left.TerritoryId == right.TerritoryId && left.X == right.X && left.Y == right.Y
        && string.Equals(left.Name?.Trim(), right.Name.Trim(), StringComparison.OrdinalIgnoreCase);
}

public static class MapBookmarkPersistence
{
    public static void Commit(Configuration configuration, Action save,
        Func<IReadOnlyList<MapBookmark>, IReadOnlyList<MapBookmark>> edit)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(edit);
        var previous = configuration.MapBookmarks ?? [];
        var updated = edit(previous).ToList();
        configuration.MapBookmarks = updated;
        try { save(); }
        catch { configuration.MapBookmarks = previous; throw; }
    }
}
