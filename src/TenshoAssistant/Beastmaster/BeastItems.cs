namespace TenshoAssistant.Beastmaster;

/// <summary>
/// X-744 (0.1768, the user's idea I-026 of 18/11; first part): one ephemeral item of the Crucible of the Unbroken (XBMItem): its kind
/// (XBMItemType: 1 beast gear, 2 Crucible item, 3 feed), the game's name and icon, its effect (the description without its last bracket)
/// and, for a feed, the families that can eat it, read in that bracket (« [Utilisable sur : Thériens, …] », « [Suitable for beastkin, …] »:
/// no column holds them; docs/evidence/0.1768-crucible-items.md). <see cref="Families"/> empty: not a feed, or no family recognised (unknown).
/// </summary>
public sealed record BeastItem(uint Row, int Kind, string Name, uint Icon, string Effect, IReadOnlyList<int> Families);

/// <summary>X-744 (0.1768): a rule of the game's own guide of the Crucible (DescriptionString): its title and text, in the client's language.</summary>
public sealed record BeastGuideRule(string Key, string Title, string Text);

public static class BeastItems
{
    /// <summary>XBMItem column 0: the item's kind (XBMItemType's rows).</summary>
    public const int Gear = 1, CrucibleItem = 2, Feed = 3;

    /// <summary>XBMItem's column count; another count means the game changed the sheet: no item is read then (R08).</summary>
    public const int Columns = 14;

    /// <summary>
    /// The game's guide rows of the board's spaces and of the item kinds (DescriptionString: a title row, then its text row): campsite
    /// 1708/1735, shop 1709/1736, treasure 1710/1737, random 1711/1738; beast gear 1713/1740, Crucible items 1714/1741, feed 1715/1742.
    /// </summary>
    public static readonly IReadOnlyList<(string Key, uint Title, uint Text)> Guide =
    [
        ("Campsite", 1708, 1735), ("Shop", 1709, 1736), ("Treasure", 1710, 1737), ("Random", 1711, 1738),
        ("Gear", 1713, 1740), ("CrucibleItem", 1714, 1741), ("Feed", 1715, 1742),
    ];

    /// <summary>
    /// The item of one XBMItem row: column 0 the kind, 1 the icon, 11 the name, 12 the description. Null for the empty row, an unknown kind
    /// or a row without a name.
    /// </summary>
    public static BeastItem? Read(XbmRow row, IReadOnlyDictionary<int, string> familyNames)
    {
        if (row.Cells.Length != Columns) return null;
        var kind = row.Int(0);
        var name = row.Text(11).Trim();
        if (kind is not (Gear or CrucibleItem or Feed) || name.Length == 0) return null;
        var (effect, families) = kind == Feed ? Split(row.Text(12), familyNames) : (Clean(row.Text(12)), (IReadOnlyList<int>)[]);
        return new BeastItem(row.Row, kind, name, row.UInt(1), effect, families);
    }

    /// <summary>
    /// A feed's description cut in two: its effect (before the last « [ ») and the families named in that bracket, as numbers (the families'
    /// names are the game's, Addon 17741 to 17748, matched whole and ignoring case). No bracket, or no family recognised in it: the whole
    /// text and no family (unknown, never « every family »).
    /// </summary>
    public static (string Effect, IReadOnlyList<int> Families) Split(string description, IReadOnlyDictionary<int, string> familyNames)
    {
        var open = description.LastIndexOf('[');
        var close = open < 0 ? -1 : description.IndexOf(']', open);
        if (open < 0 || close < 0) return (Clean(description), []);
        var bracket = description[(open + 1)..close];
        var families = familyNames.Where(pair => pair.Value.Length > 0 && Named(bracket, pair.Value)).Select(pair => pair.Key).Order().ToArray();
        return families.Length == 0 ? (Clean(description), []) : (Clean(description[..open]), families);
    }

    /// <summary>Whether a feed can be given to a beast of this family (false when its families are not known).</summary>
    public static bool UsableBy(BeastItem item, int family) => item.Kind == Feed && item.Families.Contains(family);

    /// <summary>X-770 (0.1769): a feed whose families were not read in its description (unknown, R06: never taken for « no family »).</summary>
    public static bool FamiliesUnread(BeastItem item) => item.Kind == Feed && item.Families.Count == 0;

    /// <summary>
    /// The items of one kind, in the sheet's order; feeds of one family only when one is given. X-770 (0.1769): the feeds whose families
    /// were not read stay in a family's list (marked « Families not read » by the page): unknown is not « cannot eat it ».
    /// </summary>
    public static IReadOnlyList<BeastItem> Of(IEnumerable<BeastItem> items, int kind, int family, string query) =>
        items.Where(item => item.Kind == kind && (family <= 0 || kind != Feed || UsableBy(item, family) || FamiliesUnread(item))
            && (query.Length == 0 || SearchText.Contains(item.Name, query, StringComparison.CurrentCultureIgnoreCase) || SearchText.Contains(item.Effect, query, StringComparison.CurrentCultureIgnoreCase)))
            .ToArray();

    // A family's name (the game's, not a search typed by the player) inside the bracket, as a whole word, any case (« ashkin » is not
    // inside « beastkin »).
    private static bool Named(string bracket, string name)
    {
        bracket = bracket.ToLowerInvariant();
        name = name.ToLowerInvariant();
        for (var at = bracket.IndexOf(name, StringComparison.Ordinal); at >= 0; at = bracket.IndexOf(name, at + 1, StringComparison.Ordinal))
        {
            var before = at == 0 || !char.IsLetter(bracket[at - 1]);
            var end = at + name.Length;
            if (before && (end >= bracket.Length || !char.IsLetter(bracket[end]))) return true;
        }
        return false;
    }

    // The game's lines kept (one effect per line), each trimmed, empty ones left out.
    private static string Clean(string text) => string.Join("\n", text.Split((char[])['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).Where(line => line.Length > 0));
}
