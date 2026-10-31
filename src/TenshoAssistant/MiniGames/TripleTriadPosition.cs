using System.Numerics;

namespace TenshoAssistant.MiniGames;

public enum TriadPlayer { Blue, Red }
[Flags]
public enum TriadRules { Normal = 0, Reverse = 1, FallenAce = 2, Same = 4, Plus = 8, Order = 16, Ascension = 32, Descension = 64 }
public readonly record struct TriadMove(int CardIndex, int Square);
public readonly record struct TriadPlacement(int CardIndex, int Square, TriadPlayer Owner);

/// <summary>Immutable position with explicit supported rules. Instances 0..4 start in Blue's deck, 5..9 in Red's.</summary>
public readonly struct TriadPosition
{
    public IReadOnlyList<TriadCardDefinition> Cards { get; }
    private readonly IReadOnlyList<ushort> families;
    internal ulong Board { get; }
    internal ushort BlueCells { get; }
    internal ushort Remaining { get; }
    public TriadPlayer Turn { get; }
    public TriadRules Rules { get; }
    public int EmptySquares => 9 - BitOperations.PopCount((uint)(1023 ^ Remaining));
    public bool IsTerminal => EmptySquares == 0;
    public bool IsInitialized => Cards is { Count: 10 };
    public int BlueScore => BitOperations.PopCount((uint)BlueCells) + BitOperations.PopCount((uint)(Remaining & 31));
    public int RedScore => 10 - BlueScore;
    internal int Difference => 2 * BlueScore - 10;

    private TriadPosition(IReadOnlyList<TriadCardDefinition> cards, IReadOnlyList<ushort> families, ulong board, ushort blueCells, ushort remaining, TriadPlayer turn, TriadRules rules)
    { Cards = cards; this.families = families; Board = board; BlueCells = blueCells; Remaining = remaining; Turn = turn; Rules = rules; }

    public static TriadPosition Create(IEnumerable<TriadCardDefinition> blue, IEnumerable<TriadCardDefinition> red, TriadPlayer first, TriadRules rules = TriadRules.Normal)
        => Restore(blue, red, [], first, rules);

    public static TriadPosition Restore(IEnumerable<TriadCardDefinition> blue, IEnumerable<TriadCardDefinition> red,
        IEnumerable<TriadPlacement> placements, TriadPlayer first, TriadRules rules = TriadRules.Normal)
    {
        ArgumentNullException.ThrowIfNull(blue); ArgumentNullException.ThrowIfNull(red); ArgumentNullException.ThrowIfNull(placements);
        if (!Enum.IsDefined(first)) throw new ArgumentOutOfRangeException(nameof(first));
        if ((rules & ~(TriadRules.Reverse | TriadRules.FallenAce | TriadRules.Same | TriadRules.Plus | TriadRules.Order | TriadRules.Ascension | TriadRules.Descension)) != 0)
            throw new ArgumentOutOfRangeException(nameof(rules), "Règles non couvertes.");
        if ((rules & (TriadRules.Ascension | TriadRules.Descension)) == (TriadRules.Ascension | TriadRules.Descension))
            throw new ArgumentException("Bonus et Malus ne peuvent pas être appliqués simultanément.", nameof(rules));
        var left = blue.ToArray(); var right = red.ToArray();
        foreach (var deck in new[] { left, right })
            if (deck.Length != 5 || deck.Any(card => card is null || !card.IsValid) || deck.Select(card => card.Id).Distinct().Count() != 5)
                throw new ArgumentException("Chaque main initiale doit contenir cinq cartes connues et distinctes.");
        var cards = Array.AsReadOnly(left.Concat(right).ToArray());
        ulong board = 0;
        ushort blueCells = 0, remaining = 1023;
        var bluePlayed = 0; var redPlayed = 0;
        foreach (var place in placements)
        {
            if (place.CardIndex is < 0 or > 9 || place.Square is < 0 or > 8 || !Enum.IsDefined(place.Owner))
                throw new ArgumentException("Carte, case ou couleur invalide.");
            if ((remaining & (1 << place.CardIndex)) == 0 || ((board >> (place.Square * 4)) & 15) != 0)
                throw new ArgumentException("Une carte et une case ne peuvent être utilisées qu'une fois.");
            board |= (ulong)(place.CardIndex + 1) << (place.Square * 4);
            remaining &= (ushort)~(1 << place.CardIndex);
            if (place.Owner == TriadPlayer.Blue) blueCells |= (ushort)(1 << place.Square);
            if (place.CardIndex < 5) bluePlayed++; else redPlayed++;
        }
        var firstCount = first == TriadPlayer.Blue ? bluePlayed : redPlayed;
        var secondCount = first == TriadPlayer.Blue ? redPlayed : bluePlayed;
        if (firstCount < secondCount || firstCount > secondCount + 1)
            throw new ArgumentException("Le nombre de cartes jouées ne correspond pas à une alternance des tours.");
        if ((rules & TriadRules.Order) != 0 && remaining != ((31 & ~((1 << bluePlayed) - 1)) | ((31 & ~((1 << redPlayed) - 1)) << 5)))
            throw new ArgumentException("Les cartes jouées ne respectent pas l'ordre des mains initiales.");
        var familyMasks = new ushort[10];
        for (var index = 0; index < 10; index++)
            if (cards[index].TypeId != 0)
                for (var other = 0; other < 10; other++)
                    if (cards[index].TypeId == cards[other].TypeId) familyMasks[index] |= (ushort)(1 << other);
        return new(cards, Array.AsReadOnly(familyMasks), board, blueCells, remaining, firstCount == secondCount ? first : Other(first), rules);
    }

    public static TriadPlayer Other(TriadPlayer player) => player == TriadPlayer.Blue ? TriadPlayer.Red : TriadPlayer.Blue;
    public int CardAt(int square)
    {
        if (square is < 0 or > 8) throw new ArgumentOutOfRangeException(nameof(square));
        return (int)((Board >> (square * 4)) & 15) - 1;
    }
    public TriadPlayer? OwnerAt(int square) => CardAt(square) < 0 ? null : (BlueCells & (1 << square)) != 0 ? TriadPlayer.Blue : TriadPlayer.Red;
    public bool InHand(int index) => index is >= 0 and <= 9 && (Remaining & (1 << index)) != 0;
    public bool CanPlayCard(int index)
    {
        if (!IsInitialized || IsTerminal || !InHand(index) || (index < 5) != (Turn == TriadPlayer.Blue)) return false;
        return (Rules & TriadRules.Order) == 0 || index == BitOperations.TrailingZeroCount((uint)(Remaining & (Turn == TriadPlayer.Blue ? 31 : 992)));
    }
    public int FamilyModifier(int index)
    {
        if (!IsInitialized || index is < 0 or > 9) throw new ArgumentOutOfRangeException(nameof(index));
        if ((Rules & (TriadRules.Ascension | TriadRules.Descension)) == 0) return 0;
        var count = BitOperations.PopCount((uint)(families[index] & (1023 ^ Remaining)));
        return (Rules & TriadRules.Ascension) != 0 ? count : -count;
    }
    public int EffectiveSide(int index, int side)
    {
        if (side is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(side));
        var modifier = FamilyModifier(index);
        return Math.Clamp(Side(Cards[index], side) + modifier, 1, 10);
    }
    public IReadOnlyList<TriadMove> LegalMoves()
    {
        if (!IsInitialized) throw new InvalidOperationException("Position non initialisée.");
        var moves = new List<TriadMove>();
        if (!IsTerminal)
            for (var card = Turn == TriadPlayer.Blue ? 0 : 5; card < (Turn == TriadPlayer.Blue ? 5 : 10); card++)
                if (CanPlayCard(card))
                    for (var square = 0; square < 9; square++) if (CardAt(square) < 0) moves.Add(new(card, square));
        return Array.AsReadOnly(moves.ToArray());
    }

    public TriadPosition Play(TriadMove move)
    {
        if (!CanPlayCard(move.CardIndex) || move.Square is < 0 or > 8 || CardAt(move.Square) >= 0)
            throw new ArgumentException("Coup impossible dans cette position.");
        return PlayUnchecked(move.CardIndex, move.Square);
    }

    internal TriadPosition PlayUnchecked(int cardIndex, int square)
    {
        if ((Rules & (TriadRules.Same | TriadRules.Plus)) != 0) return PlayMatches(cardIndex, square);
        var previous = this;
        var color = BlueCells;
        var blue = Turn == TriadPlayer.Blue;
        var row = square / 3; var column = square % 3;
        void Capture(int neighbor, int attack, int defenseSide)
        {
            var index = previous.CardAt(neighbor);
            if (index < 0 || ((previous.BlueCells & (1 << neighbor)) != 0) == blue) return;
            var defending = previous.EffectiveSide(index, defenseSide);
            if (!Beats(attack, defending, previous.Rules, false)) return;
            if (blue) color |= (ushort)(1 << neighbor); else color &= (ushort)~(1 << neighbor);
        }
        if (row > 0) Capture(square - 3, EffectiveSide(cardIndex, 0), 2);
        if (column < 2) Capture(square + 1, EffectiveSide(cardIndex, 1), 3);
        if (row < 2) Capture(square + 3, EffectiveSide(cardIndex, 2), 0);
        if (column > 0) Capture(square - 1, EffectiveSide(cardIndex, 3), 1);
        if (blue) color |= (ushort)(1 << square); else color &= (ushort)~(1 << square);
        return new(Cards, families, Board | ((ulong)(cardIndex + 1) << (square * 4)), color,
            (ushort)(Remaining & ~(1 << cardIndex)), Other(Turn), Rules);
    }

    private static bool Beats(int attack, int defense, TriadRules rules, bool combo)
    {
        var reverse = (rules & TriadRules.Reverse) != 0;
        if (!combo && (rules & TriadRules.FallenAce) != 0
            && (reverse ? attack == 10 && defense == 1 : attack == 1 && defense == 10)) return true;
        return reverse ? attack < defense : attack > defense;
    }
    private static int Neighbor(int square, int side) => side switch
    {
        0 => square >= 3 ? square - 3 : -1,
        1 => square % 3 < 2 ? square + 1 : -1,
        2 => square < 6 ? square + 3 : -1,
        _ => square % 3 > 0 ? square - 1 : -1,
    };
    private static int Side(TriadCardDefinition card, int side) => side switch
    { 0 => card.Top, 1 => card.Right, 2 => card.Bottom, _ => card.Left };

    private TriadPosition PlayMatches(int cardIndex, int square)
    {
        Span<int> neighbors = stackalloc int[4];
        Span<int> attack = stackalloc int[4];
        Span<int> defense = stackalloc int[4];
        for (var side = 0; side < 4; side++)
        {
            var cell = Neighbor(square, side);
            neighbors[side] = cell >= 0 && CardAt(cell) >= 0 ? cell : -1;
            if (neighbors[side] < 0) continue;
            attack[side] = EffectiveSide(cardIndex, side);
            defense[side] = EffectiveSide(CardAt(cell), (side + 2) % 4);
        }
        var matches = 0;
        // Resolve every matching pair against the original colors and values, before any capture.
        for (var first = 0; first < 4; first++)
        for (var second = first + 1; second < 4; second++)
        {
            if (neighbors[first] < 0 || neighbors[second] < 0) continue;
            var same = (Rules & TriadRules.Same) != 0 && attack[first] == defense[first] && attack[second] == defense[second];
            var plus = (Rules & TriadRules.Plus) != 0 && attack[first] + defense[first] == attack[second] + defense[second];
            if (same || plus) matches |= (1 << first) | (1 << second);
        }
        var blue = Turn == TriadPlayer.Blue;
        ushort captured = 0;
        Span<int> queue = stackalloc int[9];
        var tail = 0;
        for (var side = 0; side < 4; side++)
        {
            var cell = neighbors[side];
            if (cell < 0 || ((BlueCells & (1 << cell)) != 0) == blue) continue;
            var matched = (matches & (1 << side)) != 0;
            if (matched || Beats(attack[side], defense[side], Rules, false)) captured |= (ushort)(1 << cell);
            if (matched) queue[tail++] = cell; // Only newly captured Same/Plus cards start a combo.
        }
        var board = Board | ((ulong)(cardIndex + 1) << (square * 4));
        var color = blue ? (ushort)(BlueCells | captured | (1 << square)) : (ushort)(BlueCells & ~captured & ~(1 << square));
        for (var head = 0; head < tail; head++)
        {
            var source = queue[head];
            var sourceIndex = (int)((board >> (source * 4)) & 15) - 1;
            for (var side = 0; side < 4; side++)
            {
                var cell = Neighbor(source, side);
                if (cell < 0 || ((color & (1 << cell)) != 0) == blue) continue;
                var index = (int)((board >> (cell * 4)) & 15) - 1;
                if (index < 0 || !Beats(EffectiveSide(sourceIndex, side), EffectiveSide(index, (side + 2) % 4), Rules, true)) continue;
                if (blue) color |= (ushort)(1 << cell); else color &= (ushort)~(1 << cell);
                queue[tail++] = cell;
            }
        }
        return new(Cards, families, board, color, (ushort)(Remaining & ~(1 << cardIndex)), Other(Turn), Rules);
    }
}
