namespace TenshoAssistant.MiniGames;

public enum TriadForcedCards { None, Order, Chaos }
/// <summary>Simulated matches of a deck against an NPC: wins, draws and losses out of <see cref="Games"/>.</summary>
public sealed record TriadDeckOdds(int Games, int Wins, int Draws, int Losses)
{
    public double Win => Games == 0 ? 0 : (double)Wins / Games;
    public double Draw => Games == 0 ? 0 : (double)Draws / Games;
    // Comparison score: a draw is worth half a win.
    public double Score => Games == 0 ? 0 : (Wins + Draws * 0.5) / Games;
}
public sealed record TriadDeckSuggestion(IReadOnlyList<TriadCardDefinition> Deck, TriadDeckOdds Odds);

/// <summary>
/// Win estimate of a deck against an NPC (TRIAD-003, as TriadBuddy shows on the challenge window): many simulated matches with the NPC's
/// hand drawn from its deck (all fixed cards, then random variable cards), a random first player, and both sides choosing their move
/// by a short look-ahead on open cards. Chaos imposes a random card, Order the next card of the hand. An estimate, not a promise:
/// the game's NPC logic is not known.
/// </summary>
public static class TriadMatchSimulator
{
    public static TriadDeckOdds Estimate(IReadOnlyList<TriadCardDefinition> deck, IReadOnlyList<TriadCardDefinition> npcFixed,
        IReadOnlyList<TriadCardDefinition> npcVariable, TriadRules rules, TriadForcedCards forced, int games, int seed, int depth = 1,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(deck); ArgumentNullException.ThrowIfNull(npcFixed); ArgumentNullException.ThrowIfNull(npcVariable);
        if (deck.Count != 5 || deck.Select(card => card.Id).Distinct().Count() != 5) throw new ArgumentException("Le deck doit contenir cinq cartes distinctes.", nameof(deck));
        if (npcFixed.Count + npcVariable.Count < 5) throw new ArgumentException("Deck du PNJ incomplet.");
        if (games is < 1 or > 100_000 || depth is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(games));
        rules &= ~TriadRules.Order;
        var random = new Random(seed);
        int wins = 0, draws = 0, losses = 0;
        for (var game = 0; game < games; game++)
        {
            cancellation.ThrowIfCancellationRequested();
            var npc = npcFixed.Take(5).Concat(npcVariable.OrderBy(_ => random.Next()).Take(Math.Max(0, 5 - npcFixed.Count))).ToList();
            var position = TriadPosition.Create(deck, npc, random.Next(2) == 0 ? TriadPlayer.Blue : TriadPlayer.Red, rules);
            while (!position.IsTerminal)
            {
                var moves = position.LegalMoves();
                var offset = position.Turn == TriadPlayer.Blue ? 0 : 5;
                if (forced != TriadForcedCards.None)
                {
                    var hand = moves.Select(move => move.CardIndex).Distinct().OrderBy(index => index).ToArray();
                    var card = forced == TriadForcedCards.Order ? hand[0] : hand[random.Next(hand.Length)];
                    moves = moves.Where(move => move.CardIndex == card).ToArray();
                }
                position = position.Play(Choose(position, moves, depth, random, offset));
            }
            var difference = position.BlueScore - position.RedScore;
            if (difference > 0) wins++; else if (difference == 0) draws++; else losses++;
        }
        return new(games, wins, draws, losses);
    }

    private static TriadMove Choose(TriadPosition position, IReadOnlyList<TriadMove> moves, int depth, Random random, int offset)
    {
        var maximize = position.Turn == TriadPlayer.Blue;
        var best = int.MinValue; var choices = new List<TriadMove>();
        foreach (var move in moves)
        {
            var value = Search(position.Play(move), depth - 1) * (maximize ? 1 : -1);
            if (value > best) { best = value; choices.Clear(); }
            if (value == best) choices.Add(move);
        }
        return choices[random.Next(choices.Count)];
    }
    private static int Search(TriadPosition position, int depth)
    {
        if (depth == 0 || position.IsTerminal) return position.Difference;
        var maximize = position.Turn == TriadPlayer.Blue;
        var best = maximize ? int.MinValue : int.MaxValue;
        foreach (var move in position.LegalMoves())
        {
            var value = Search(position.Play(move), depth - 1);
            best = maximize ? Math.Max(best, value) : Math.Min(best, value);
        }
        return best;
    }

    /// <summary>
    /// Best deck found from the owned cards within the time budget: candidates ranked by their sides for the rules, a valid starting deck
    /// (at most two cards of four stars or more, one of five), then single-card swaps kept when the simulated score improves. The same
    /// seeds are used for every deck so that comparisons are fair.
    /// </summary>
    public static TriadDeckSuggestion? Optimize(IReadOnlyList<TriadCardDefinition> owned, IReadOnlyList<TriadCardDefinition> npcFixed,
        IReadOnlyList<TriadCardDefinition> npcVariable, TriadRules rules, TriadForcedCards forced, int milliseconds, int seed,
        CancellationToken cancellation = default, IReadOnlyList<IReadOnlyList<TriadCardDefinition>>? starts = null, int finalGames = 400)
    {
        ArgumentNullException.ThrowIfNull(owned);
        var cards = owned.Where(card => card.IsValid).GroupBy(card => card.Id).Select(group => group.First()).ToList();
        if (cards.Count < 5 || npcFixed.Count + npcVariable.Count < 5) return null;
        double Strength(TriadCardDefinition card)
        {
            var sides = new[] { card.Top, card.Right, card.Bottom, card.Left };
            var values = (rules & TriadRules.Reverse) != 0 ? sides.Select(side => 11 - side) : sides;
            return values.Sum() + values.Max() * 0.5;
        }
        var ranked = cards.OrderByDescending(Strength).ThenBy(card => card.Id).ToList();
        var pool = ranked.Where(card => card.Stars == 5).Take(6).Concat(ranked.Where(card => card.Stars == 4).Take(10))
            .Concat(ranked.Where(card => card.Stars <= 3).Take(20)).ToList();
        static bool Valid(IReadOnlyList<TriadCardDefinition> deck) => deck.Count(card => card.Stars >= 4) <= 2 && deck.Count(card => card.Stars == 5) <= 1;
        var deck = new List<TriadCardDefinition>();
        foreach (var card in pool.OrderByDescending(Strength))
            if (deck.Count < 5 && Valid([.. deck, card])) deck.Add(card);
        foreach (var card in ranked)
            if (deck.Count < 5 && !deck.Contains(card) && Valid([.. deck, card])) deck.Add(card);
        if (deck.Count < 5) return null;
        // 0.172: the player's existing decks are starting points too (a generated deck once scored below an existing one), trials use more
        // games, and the result is checked at the end on the same draws as the decks list: never worse than the best existing deck.
        const int Trial = 150;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var score = Estimate(deck, npcFixed, npcVariable, rules, forced, Trial, seed, 1, cancellation).Score;
        var existing = (starts ?? []).Where(start => start.Count == 5 && start.All(card => card.IsValid) && start.Select(card => card.Id).Distinct().Count() == 5 && Valid(start))
            .Select(start => start.ToList()).ToList();
        foreach (var start in existing)
        {
            var value = Estimate(start, npcFixed, npcVariable, rules, forced, Trial, seed, 1, cancellation).Score;
            if (value > score) { deck = start; score = value; }
        }
        foreach (var card in existing.SelectMany(start => start)) if (!pool.Contains(card)) pool.Add(card);
        var improved = true;
        while (improved && System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds < milliseconds)
        {
            improved = false;
            for (var slot = 0; slot < 5 && !improved; slot++)
                foreach (var candidate in pool)
                {
                    if (System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds >= milliseconds) break;
                    if (deck.Contains(candidate)) continue;
                    var trial = deck.ToList(); trial[slot] = candidate;
                    if (!Valid(trial)) continue;
                    var value = Estimate(trial, npcFixed, npcVariable, rules, forced, Trial, seed, 1, cancellation).Score;
                    if (value > score + 1e-9) { deck = trial; score = value; improved = true; break; }
                }
        }
        var final = deck.OrderByDescending(card => card.Stars).ThenByDescending(Strength).ToList();
        var odds = Estimate(final, npcFixed, npcVariable, rules, forced, finalGames, seed, 1, cancellation);
        foreach (var start in existing)
        {
            var value = Estimate(start, npcFixed, npcVariable, rules, forced, finalGames, seed, 1, cancellation);
            if (value.Score > odds.Score) { final = start; odds = value; }
        }
        return new(final.AsReadOnly(), odds);
    }

    /// <summary>Forced-card mode from the match rules (TripleTriadRule 8 Order, 9 Chaos).</summary>
    public static TriadForcedCards ForcedOf(IEnumerable<uint> ruleIds)
    {
        var ids = ruleIds.ToArray();
        return ids.Contains(9u) ? TriadForcedCards.Chaos : ids.Contains(8u) ? TriadForcedCards.Order : TriadForcedCards.None;
    }
}
