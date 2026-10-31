namespace TenshoAssistant.MiniGames;

public sealed record TriadCardDefinition(uint Id, int Number, int Stars, int Top, int Right, int Bottom, int Left,
    uint TypeId, string TypeName, string Description)
{
    public bool IsValid => Id != 0 && Number > 0 && Stars is >= 1 and <= 5
        && Top is >= 1 and <= 10 && Right is >= 1 and <= 10 && Bottom is >= 1 and <= 10 && Left is >= 1 and <= 10;
    public uint IconId => checked(88000 + Id);
    public static string Value(int number) => number == 10 ? "A" : number.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed record TriadDeckAnalysis(bool IsComplete, bool RarityAllowed, IReadOnlyList<uint> UnknownIds,
    bool HasDuplicates, bool TooManyCards)
{
    public bool Valid => IsComplete && RarityAllowed && UnknownIds.Count == 0 && !HasDuplicates && !TooManyCards;
}

public static class TriadDeck
{
    /// <summary>Deck composition only; ownership and special match rules are separate observations.</summary>
    public static TriadDeckAnalysis Analyze(IEnumerable<uint> cards, Func<uint, TriadCardDefinition?> find)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(find);
        var ids = cards.ToArray();
        var unknown = new List<uint>();
        var rare = 0;
        var legendary = 0;
        foreach (var id in ids)
        {
            var card = find(id);
            if (card is null || !card.IsValid || card.Id != id) { unknown.Add(id); continue; }
            if (card.Stars >= 4) rare++;
            if (card.Stars == 5) legendary++;
        }
        return new(ids.Length == 5, rare <= 2 && legendary <= 1, Array.AsReadOnly(unknown.Distinct().ToArray()),
            ids.Distinct().Count() != ids.Length, ids.Length > 5);
    }
}
