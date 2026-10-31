namespace TenshoAssistant.MiniGames;

public enum TriadAdviceProblem { None, NotYourTurn, NoContext, UnknownCard, NpcDeckUnknown, DeckMismatch, Unsolvable }
/// <summary>A move for the player: hand slot (0–4, as in the game window) and board square (0–8, row by row).</summary>
public readonly record struct TriadAdviceMove(int HandSlot, int Square, double AverageDifference);
public sealed record TriadAdvice(IReadOnlyList<TriadAdviceMove> Best, IReadOnlyList<TriadAdviceMove> All, int Hands, bool Exact, TriadAdviceProblem Problem)
{
    public static TriadAdvice Failed(TriadAdviceProblem problem) => new([], [], 0, false, problem);
}

/// <summary>
/// Turns an observed match into solver positions (TRIAD-003): current board with its colours, the player's remaining cards, and the
/// opponent's hidden cards filled from the NPC's deck — every fixed card not seen yet, then each combination of unseen variable cards,
/// equally likely. Each position is solved and the player's moves are ranked by their average final difference.
/// </summary>
public static class TriadMatchAdvisor
{
    private const uint SyntheticBase = 1_000_000;
    // TripleTriadRule rows (docs/evidence/0.159.0-triad-npc-probe.log) the solver plays; the others change visibility, the deck or a
    // draw, not the move value. Chaos (9) and Order (8) impose the card: handled through the imposed slot.
    private static readonly IReadOnlyDictionary<uint, TriadRules> SolverRules = new Dictionary<uint, TriadRules>
    {
        [4] = TriadRules.Same, [6] = TriadRules.Plus, [10] = TriadRules.Reverse, [11] = TriadRules.FallenAce,
        [12] = TriadRules.Ascension, [13] = TriadRules.Descension,
    };

    public static TriadRules RulesOf(IEnumerable<uint> ruleIds)
    {
        var rules = TriadRules.Normal;
        foreach (var id in ruleIds) if (SolverRules.TryGetValue(id, out var rule)) rules |= rule;
        return rules;
    }

    public static TriadAdvice Advise(TriadObservedFrame frame, Func<uint, TriadCardDefinition?> card, TriadSolveOptions? options = null,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(frame); ArgumentNullException.ThrowIfNull(card);
        if (!frame.BlueToMove) return TriadAdvice.Failed(TriadAdviceProblem.NotYourTurn);
        if (frame.Context is not { } context) return TriadAdvice.Failed(TriadAdviceProblem.NoContext);
        var blueHand = new List<(int Slot, TriadCardDefinition Card)>();
        for (var slot = 0; slot < 5; slot++)
        {
            var observed = frame.BlueHand[slot];
            if (observed.Presence == TriadObservedPresence.Empty) continue;
            if (observed.Presence != TriadObservedPresence.Known) return TriadAdvice.Failed(TriadAdviceProblem.UnknownCard);
            blueHand.Add((slot, Synthetic(SyntheticBase + (uint)slot, observed)));
        }
        var board = new List<(int Square, TriadObservedCard Card)>();
        for (var square = 0; square < 9; square++)
        {
            var observed = frame.Board[square];
            if (observed.Presence == TriadObservedPresence.Empty) continue;
            if (observed.Presence != TriadObservedPresence.Known || observed.Owner is null) return TriadAdvice.Failed(TriadAdviceProblem.UnknownCard);
            board.Add((square, observed));
        }
        var redHand = frame.RedHand.Where(observed => observed.Presence != TriadObservedPresence.Empty).ToList();
        var bluePlayed = 5 - blueHand.Count; var redPlayed = 5 - redHand.Count;
        if (bluePlayed + redPlayed != board.Count || redHand.Any(observed => observed.Presence == TriadObservedPresence.Incomplete))
            return TriadAdvice.Failed(TriadAdviceProblem.UnknownCard);

        // The NPC's deck: identify every red card already seen (played or shown) by its faces and type, each deck card at most once.
        var deck = context.NpcFixed.Concat(context.NpcVariable).Distinct().Select(id => card(id)).OfType<TriadCardDefinition>().ToList();
        var hidden = redHand.Count(observed => observed.Presence == TriadObservedPresence.Hidden);
        if (hidden > 0 && deck.Count == 0) return TriadAdvice.Failed(TriadAdviceProblem.NpcDeckUnknown);
        var used = new HashSet<uint>();
        TriadCardDefinition? Identify(TriadObservedCard observed) => deck.FirstOrDefault(entry => !used.Contains(entry.Id) && Same(entry, observed));
        var redKnown = new List<TriadCardDefinition>();
        foreach (var observed in redHand.Where(observed => observed.Presence == TriadObservedPresence.Known))
        {
            var known = Identify(observed) ?? Synthetic(SyntheticBase + 100 + (uint)redKnown.Count, observed);
            used.Add(known.Id); redKnown.Add(known);
        }
        // Board cards that came from the red hand: the ones matching the NPC's deck first; the player's played cards are the rest.
        var redOrigin = new List<(int Square, TriadObservedCard Card, TriadCardDefinition Definition)>();
        var blueOrigin = new List<(int Square, TriadObservedCard Card, TriadCardDefinition Definition)>();
        foreach (var (square, observed) in board)
            if (redOrigin.Count < redPlayed && Identify(observed) is { } definition) { used.Add(definition.Id); redOrigin.Add((square, observed, definition)); }
            else blueOrigin.Add((square, observed, Synthetic(SyntheticBase + 10 + (uint)square, observed)));
        while (redOrigin.Count < redPlayed && blueOrigin.Count > bluePlayed)
        {
            // An opponent card outside the NPC deck (Swap, regional changes): keep it as a known red card.
            var (square, observed, _) = blueOrigin[^1]; blueOrigin.RemoveAt(blueOrigin.Count - 1);
            redOrigin.Add((square, observed, Synthetic(SyntheticBase + 20 + (uint)square, observed)));
        }
        if (redOrigin.Count != redPlayed || blueOrigin.Count != bluePlayed) return TriadAdvice.Failed(TriadAdviceProblem.UnknownCard);

        // Hidden red cards: all unseen fixed cards, then every combination of unseen variable cards.
        var unseenFixed = context.NpcFixed.Distinct().Where(id => !used.Contains(id)).Select(id => card(id)).OfType<TriadCardDefinition>().ToList();
        var unseenVariable = context.NpcVariable.Distinct().Where(id => !used.Contains(id) && !context.NpcFixed.Contains(id))
            .Select(id => card(id)).OfType<TriadCardDefinition>().ToList();
        var hands = new List<IReadOnlyList<TriadCardDefinition>>();
        if (hidden == 0) hands.Add([]);
        else if (unseenFixed.Count > hidden || unseenFixed.Count + unseenVariable.Count < hidden) return TriadAdvice.Failed(TriadAdviceProblem.DeckMismatch);
        else foreach (var choice in Combinations(unseenVariable, hidden - unseenFixed.Count)) hands.Add([.. unseenFixed, .. choice]);

        var rules = RulesOf(context.RuleIds);
        var first = bluePlayed == redPlayed ? TriadPlayer.Blue : redPlayed == bluePlayed + 1 ? TriadPlayer.Red : (TriadPlayer?)null;
        if (first is null) return TriadAdvice.Failed(TriadAdviceProblem.UnknownCard);
        options ??= new();
        var budget = options with { Milliseconds = Math.Max(50, options.Milliseconds / hands.Count), NodeLimit = Math.Max(1000, options.NodeLimit / hands.Count) };
        var totals = new Dictionary<(int Slot, int Square), double>();
        var exact = true;
        foreach (var hand in hands)
        {
            cancellation.ThrowIfCancellationRequested();
            var blue = blueHand.Select(entry => entry.Card).Concat(blueOrigin.Select(entry => entry.Definition)).ToList();
            var red = redKnown.Concat(hand).Concat(redOrigin.Select(entry => entry.Definition)).ToList();
            var placements = blueOrigin.Select((entry, index) => new TriadPlacement(blueHand.Count + index, entry.Square, entry.Card.Owner!.Value))
                .Concat(redOrigin.Select((entry, index) => new TriadPlacement(5 + redKnown.Count + hand.Count + index, entry.Square, entry.Card.Owner!.Value)));
            TriadSolveResult result;
            try { result = TripleTriadSolver.Solve(TriadPosition.Restore(blue, red, placements, first.Value, rules), budget, cancellation); }
            catch (ArgumentException) { return TriadAdvice.Failed(TriadAdviceProblem.Unsolvable); }
            exact &= result.Exact;
            foreach (var value in result.Moves)
            {
                if (value.Move.CardIndex >= blueHand.Count) continue;
                var key = (blueHand[value.Move.CardIndex].Slot, value.Move.Square);
                totals[key] = totals.GetValueOrDefault(key) + value.BlueDifference;
            }
        }
        var moves = totals.Select(pair => new TriadAdviceMove(pair.Key.Slot, pair.Key.Square, pair.Value / hands.Count))
            .Where(move => context.ForcedSlot is not { } forced || move.HandSlot == forced)
            .OrderByDescending(move => move.AverageDifference).ThenBy(move => move.HandSlot).ThenBy(move => move.Square).ToArray();
        if (moves.Length == 0) return TriadAdvice.Failed(TriadAdviceProblem.Unsolvable);
        var best = moves.Where(move => Math.Abs(move.AverageDifference - moves[0].AverageDifference) < 1e-9).ToArray();
        return new(Array.AsReadOnly(best), Array.AsReadOnly(moves), hands.Count, exact, TriadAdviceProblem.None);
    }

    private static bool Same(TriadCardDefinition definition, TriadObservedCard observed) =>
        definition.Top == observed.Top && definition.Right == observed.Right && definition.Bottom == observed.Bottom
        && definition.Left == observed.Left && definition.TypeId == observed.TypeId && definition.Stars == observed.Stars;
    private static TriadCardDefinition Synthetic(uint id, TriadObservedCard observed) =>
        new(id, 1, observed.Stars, observed.Top, observed.Right, observed.Bottom, observed.Left, observed.TypeId, "", "");
    private static IEnumerable<IReadOnlyList<T>> Combinations<T>(IReadOnlyList<T> items, int count)
    {
        if (count == 0) { yield return []; yield break; }
        for (var index = 0; index <= items.Count - count; index++)
            foreach (var rest in Combinations(items.Skip(index + 1).ToList(), count - 1))
                yield return [items[index], .. rest];
    }
}
