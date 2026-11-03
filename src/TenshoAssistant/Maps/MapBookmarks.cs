using System.Text;
using System.Text.Json;

namespace TenshoAssistant.Maps;

// 0.378 (g, M-08): Note and Group are optional (null in configurations saved before), so older bookmarks load unchanged.
public sealed record MapBookmark(Guid Id, string Name, uint MapId, uint TerritoryId, float X, float Y, string? Note = null, string? Group = null);

public static class MapBookmarks
{
    public const int Capacity = 200;
    public const int NameLimit = 80;
    public const int NoteLimit = 300, GroupLimit = 40, ShareLimit = 200;
    public const string SharePrefix = "TENSHO-MAP1:";

    /// <summary>0.378: an optional note (line breaks allowed, 300 characters) or group (40, one line); empty = none.</summary>
    public static string? CleanNote(string? note)
    {
        var trimmed = (note ?? "").Trim();
        if (trimmed.Length == 0) return null;
        if (trimmed.Length > NoteLimit || trimmed.Any(ch => char.IsControl(ch) && ch != '\n')) throw new ArgumentException($"The note holds at most {NoteLimit} characters.");
        return trimmed;
    }
    public static string? CleanGroup(string? group)
    {
        var trimmed = (group ?? "").Trim();
        if (trimmed.Length == 0) return null;
        if (trimmed.Length > GroupLimit || trimmed.Any(char.IsControl)) throw new ArgumentException($"The group holds 1 to {GroupLimit} characters, without line breaks.");
        return trimmed;
    }

    private sealed record ShareEntry(string N, uint M, uint T, float X, float Y, string? O, string? G);
    private static readonly JsonSerializerOptions ShareJson = new() { MaxDepth = 4 };

    /// <summary>
    /// 0.378 (g, M-08): bookmarks as a text code to share (like TENSHO-GEAR1, no third-party site): prefix then base64url of a JSON
    /// list (name, map, territory, X, Y, note, group); 200 bookmarks at most.
    /// </summary>
    public static string ShareCode(IEnumerable<MapBookmark> bookmarks)
    {
        var entries = bookmarks.Where(bookmark => bookmark is not null).Take(ShareLimit)
            .Select(bookmark => new ShareEntry(bookmark.Name, bookmark.MapId, bookmark.TerritoryId, bookmark.X, bookmark.Y, bookmark.Note, bookmark.Group)).ToArray();
        return SharePrefix + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(entries, ShareJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Reads a share code into new bookmarks (new ids); a malformed code or any invalid bookmark throws FormatException.</summary>
    public static IReadOnlyList<MapBookmark> ParseShareCode(string code)
    {
        var text = (code ?? "").Trim();
        if (!text.StartsWith(SharePrefix, StringComparison.Ordinal) || text.Length > 200_000) throw new FormatException("Unknown share code.");
        var body = text[SharePrefix.Length..].Replace('-', '+').Replace('_', '/');
        body = body.PadRight(body.Length + (4 - body.Length % 4) % 4, '=');
        ShareEntry[]? entries;
        try { entries = JsonSerializer.Deserialize<ShareEntry[]>(Convert.FromBase64String(body), ShareJson); }
        catch (Exception error) when (error is FormatException or JsonException) { throw new FormatException("Unreadable share code.", error); }
        if (entries is null || entries.Length is 0 or > ShareLimit) throw new FormatException("Share code empty or too long.");
        try
        {
            return entries.Select(entry =>
            {
                var bookmark = new MapBookmark(Guid.NewGuid(), ValidateName(entry.N ?? ""), entry.M, entry.T, entry.X, entry.Y, CleanNote(entry.O), CleanGroup(entry.G));
                ValidateBookmark(bookmark);
                return bookmark;
            }).ToArray();
        }
        catch (ArgumentException error) { throw new FormatException("A bookmark of the code is invalid.", error); }
    }

    /// <summary>Adds the imported bookmarks that are not already there, up to the capacity; says how many were added and left out.</summary>
    public static (IReadOnlyList<MapBookmark> Bookmarks, int Added, int Skipped) Merge(IReadOnlyList<MapBookmark> source, IReadOnlyList<MapBookmark> imported)
    {
        var list = source.ToList(); var added = 0;
        foreach (var bookmark in imported)
        {
            if (list.Count >= Capacity || list.Any(existing => Equivalent(existing, bookmark))) continue;
            list.Add(bookmark); added++;
        }
        return (Array.AsReadOnly(list.ToArray()), added, imported.Count - added);
    }

    // N-11 (0.437): « Place ( 12.3 , 4.5 ) » lines pasted from hunt sites (Siren, Bear, Faloop) or the chat; coordinates with a dot.
    private static readonly System.Text.RegularExpressions.Regex CoordinateLine = new(
        @"(?<place>[^()\n]+?)\s*\(\s*(?:x\s*:?\s*)?(?<x>\d{1,2}(?:\.\d+)?)\s*,\s*(?:y\s*:?\s*)?(?<y>\d{1,2}(?:\.\d+)?)",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    /// <summary>
    /// N-11 (0.437): bookmarks from pasted « Place ( x , y ) » lines: the place is the end of the text before the parenthesis that names a
    /// map zone (game icons and labels before it ignored); lines with no known zone are counted, never guessed. 200 lines at most.
    /// </summary>
    public static (IReadOnlyList<MapBookmark> Bookmarks, int Unknown) ParseCoordinateList(string text, Func<string, MapDefinition?> zone, string group)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var found = new List<MapBookmark>(); var unknown = 0;
        foreach (var line in (text ?? "").Split('\n').Take(200))
        {
            var match = CoordinateLine.Match(line);
            if (!match.Success) continue;
            var place = new string(match.Groups["place"].Value.Where(character => character is not (>= '' and <= '')).ToArray()).Trim();
            var words = place.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            MapDefinition? map = null;
            for (var start = 0; start < words.Length && map is null; start++) map = zone(string.Join(' ', words[start..]).Trim(':', '-', '·', ' '));
            if (map is null) { unknown++; continue; }
            var x = float.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var y = float.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var name = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{map.Zone} ({x:0.0}, {y:0.0})");
            if (name.Length > NameLimit) name = name[..NameLimit];
            found.Add(Create(map, name, x, y) with { Group = CleanGroup(group) });
        }
        return (Array.AsReadOnly(found.ToArray()), unknown);
    }

    public static MapBookmark Create(MapDefinition map, string name, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (map.MapId == 0 || map.TerritoryId == 0 || !MapCatalog.CoordinatesValid(x, y))
            throw new ArgumentException("Invalid map or coordinates.");
        return new(Guid.NewGuid(), ValidateName(name), map.MapId, map.TerritoryId, x, y);
    }
    public static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var trimmed = name.Trim();
        if (trimmed.Length is < 1 or > NameLimit || trimmed.Any(char.IsControl))
            throw new ArgumentException($"The name must hold 1 to {NameLimit} characters, without line breaks.");
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
        if (source.Count >= Capacity) throw new InvalidOperationException($"The limit is {Capacity} bookmarks.");
        if (source.Any(existing => existing?.Id == bookmark.Id || Equivalent(existing, bookmark)))
            throw new InvalidOperationException("This point is already a bookmark.");
        return Array.AsReadOnly(source.Append(bookmark).ToArray());
    }
    public static IReadOnlyList<MapBookmark> Replace(IReadOnlyList<MapBookmark> source, MapBookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidateBookmark(bookmark);
        var index = Enumerable.Range(0, source.Count).FirstOrDefault(index => source[index]?.Id == bookmark.Id, -1);
        if (index < 0) throw new KeyNotFoundException("The bookmark to change no longer exists.");
        if (source.Where((_, position) => position != index).Any(existing => Equivalent(existing, bookmark)))
            throw new InvalidOperationException("Another bookmark already holds this point.");
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
        _ = CleanNote(bookmark.Note);
        _ = CleanGroup(bookmark.Group);
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
