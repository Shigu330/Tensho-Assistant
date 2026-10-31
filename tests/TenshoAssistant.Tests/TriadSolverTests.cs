using TenshoAssistant.MiniGames;

internal static class TriadSolverTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    private static TriadCardDefinition[] Deck(int start, int value = 1) => Enumerable.Range(start, 5)
        .Select(id => new TriadCardDefinition((uint)id, id, 1, value, value, value, value, 0, "", "")).ToArray();
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Triad normal captures compare all opposing sides without ties or row wrapping", () =>
        {
            var blue = Deck(1); blue[2] = blue[2] with { Top = 9, Right = 9, Bottom = 9, Left = 9 };
            var red = Deck(6);
            var placements = new TriadPlacement[] { new(0,1,TriadPlayer.Red), new(5,3,TriadPlayer.Red),
                new(1,5,TriadPlayer.Red), new(6,7,TriadPlayer.Red) };
            var before = TriadPosition.Restore(blue, red, placements, TriadPlayer.Blue);
            var after = before.Play(new(2,4));
            Check(new[] {1,3,4,5,7}.All(square => after.OwnerAt(square) == TriadPlayer.Blue), "four enemy neighbors captured even if originally own card");
            Check(after.BlueScore == 7 && after.RedScore == 3 && before.BlueScore == 3, "only copied position changes and cards in hand count");
            var equal = TriadPosition.Restore(Deck(1,9), Deck(6,9), placements, TriadPlayer.Blue).Play(new(2,4));
            Check(equal.OwnerAt(1) == TriadPlayer.Red && equal.OwnerAt(3) == TriadPlayer.Red, "equal sides do not capture");
            var edge = TriadPosition.Restore(blue, red, [new(0,0,TriadPlayer.Blue), new(5,2,TriadPlayer.Red)], TriadPlayer.Blue);
            Check(edge.Play(new(2,3)).OwnerAt(2) == TriadPlayer.Red, "square 2 and 3 are not adjacent across row boundary");
            Check(before.CardAt(4) == -1 && before.InHand(2), "source snapshot unchanged");
        }),
        ("Triad score includes unplayed tenth card and preserves deck instances across both players", () =>
        {
            var blue = Deck(1); var red = Deck(1); // Same card IDs in opposing decks are separate instances.
            var placements = Enumerable.Range(0, 9).Select(index => new TriadPlacement(index, index, index < 5 ? TriadPlayer.Blue : TriadPlayer.Red));
            var terminal = TriadPosition.Restore(blue, red, placements, TriadPlayer.Blue);
            Check(terminal.IsTerminal && terminal.BlueScore == 5 && terminal.RedScore == 5, "five blue board cards vs four red plus one unplayed is a draw");
            Check(terminal.LegalMoves().Count == 0 && TripleTriadSolver.Solve(terminal).Exact, "terminal state has no recommendations");
            var fresh = TriadPosition.Create(blue, red, TriadPlayer.Red);
            Check(fresh.LegalMoves().Count == 45 && fresh.Turn == TriadPlayer.Red && fresh.BlueScore == 5, "red can start and all cards remain distinct");
        }),
        ("Triad positions reject illegal turns slots duplicate instances and inconsistent histories", () =>
        {
            var blue = Deck(1); var red = Deck(6);
            var fresh = TriadPosition.Create(blue, red, TriadPlayer.Blue);
            Throws<ArgumentException>(() => fresh.Play(new(5,0)));
            Throws<ArgumentException>(() => fresh.Play(new(0,9)));
            var next = fresh.Play(new(0,0));
            Throws<ArgumentException>(() => next.Play(new(5,0)));
            Throws<ArgumentException>(() => next.Play(new(0,1)));
            Throws<ArgumentException>(() => TriadPosition.Create([blue[0],blue[0],blue[2],blue[3],blue[4]], red, TriadPlayer.Blue));
            Throws<ArgumentException>(() => TriadPosition.Restore(blue, red, [new(0,0,TriadPlayer.Blue), new(1,1,TriadPlayer.Blue)], TriadPlayer.Blue));
            Throws<ArgumentException>(() => TriadPosition.Restore(blue, red, [new(0,0,TriadPlayer.Blue), new(0,1,TriadPlayer.Blue)], TriadPlayer.Blue));
            Throws<ArgumentException>(() => TripleTriadSolver.Solve(default));
            Throws<NotSupportedException>(() => ((IList<TriadCardDefinition>)fresh.Cards).Clear());
            blue[0] = blue[0] with { Top = 10 };
            Check(fresh.Cards[0].Top == 1, "producer decks copied");
        }),
        ("Triad exact endgames agree with independent exhaustive array engine for every root move", () =>
        {
            var random = new Random(20261028);
            for (var sample = 0; sample < 80; sample++)
            {
                var cards = Enumerable.Range(1,10).Select(id => new TriadCardDefinition((uint)id, id, 1,
                    random.Next(1,11), random.Next(1,11), random.Next(1,11), random.Next(1,11), 0, "", "")).ToArray();
                var state = TriadPosition.Create(cards.Take(5), cards.Skip(5), sample % 2 == 0 ? TriadPlayer.Blue : TriadPlayer.Red);
                var played = 5 + sample % 3;
                for (var step = 0; step < played; step++)
                {
                    var legal = state.LegalMoves(); state = state.Play(legal[random.Next(legal.Count)]);
                }
                var result = TripleTriadSolver.Solve(state, new(1_000_000, 10000));
                Check(result.Exact && result.CompletedDepth == 9 - played && result.Moves.Count == state.LegalMoves().Count, "all candidates fully searched to terminal");
                foreach (var move in result.Moves)
                {
                    var grid = Enumerable.Range(0,9).Select(state.CardAt).ToArray();
                    var colors = Enumerable.Range(0,9).Select(square => state.OwnerAt(square) == TriadPlayer.Blue).ToArray();
                    var own = Enumerable.Range(0,5).Where(state.InHand).ToHashSet();
                    var enemy = Enumerable.Range(5,5).Where(state.InHand).ToHashSet();
                    ReferencePlay(grid, colors, cards, move.Move.CardIndex, move.Move.Square, state.Turn == TriadPlayer.Blue);
                    if (state.Turn == TriadPlayer.Blue) own.Remove(move.Move.CardIndex); else enemy.Remove(move.Move.CardIndex);
                    var expected = ReferenceSolve(grid, colors, cards, own, enemy, state.Turn != TriadPlayer.Blue);
                    Check(move.BlueDifference == expected, "every minimax score matches separate exhaustive transitions and evaluation");
                }
                var expectedBest = state.Turn == TriadPlayer.Blue ? result.Moves.Max(move => move.BlueDifference) : result.Moves.Min(move => move.BlueDifference);
                Check(result.Best(state.Turn).All(move => move.BlueDifference == expectedBest), "recommendation follows current player's objective");
            }
        }),
        ("Triad budget reports only complete equal-depth iterations and never labels a heuristic exact", () =>
        {
            var state = TriadPosition.Create(Deck(1), Deck(6), TriadPlayer.Blue);
            var partial = TripleTriadSolver.Solve(state, new(50, 10000));
            Check(partial.CompletedDepth == 1 && partial.Moves.Count == 45 && partial.Limited && !partial.Exact, "unfinished second iteration discarded");
            var depth = TripleTriadSolver.Solve(state, new(1000, 10000, 1));
            Check(depth.CompletedDepth == 1 && !depth.Exact, "explicit shallow depth is not exact");
            var none = TripleTriadSolver.Solve(state, new(1, 10000));
            Check(none.Moves.Count == 0 && none.CompletedDepth == 0 && none.Limited, "no guessed result when even first iteration incomplete");
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            Throws<OperationCanceledException>(() => TripleTriadSolver.Solve(state, cancellation: cancellation.Token));
            Throws<ArgumentOutOfRangeException>(() => TripleTriadSolver.Solve(state, new(0)));
            Throws<NotSupportedException>(() => ((IList<TriadMoveValue>)partial.Moves).Clear());
        }),
        ("Triad solve session invalidates input owner replacement cancellation and disposal", () =>
        {
            var position = TriadPosition.Create(Deck(1), Deck(6), TriadPlayer.Blue);
            using var session = new TriadSolveSession();
            session.StartAsync(position, 10, 1, new(1000, 10000, 1)).GetAwaiter().GetResult();
            Check(session.State.Result?.CompletedDepth == 1 && !session.State.Loading, "managed worker publishes result");
            session.ValidateContext(11, 1);
            Check(session.State.Position is null && session.State.Result is null, "owner changed");
            var old = session.StartAsync(position, 10, 2, new(10_000_000,30000));
            var latest = session.StartAsync(position.Play(new(0,0)), 10, 3, new(1000,10000,1));
            Task.WhenAll(old, latest).GetAwaiter().GetResult();
            Check(session.State.Revision == 3 && session.State.Result is not null, "superseded worker cannot publish");
            session.ValidateContext(10,4);
            Check(session.State.Result is null, "input changed");
            var pending = session.StartAsync(position, 10, 5, new(10_000_000,30000));
            session.Cancel(); pending.GetAwaiter().GetResult();
            Check(session.State.Position is null && !session.State.Loading, "cancel clears and rejects late result");
            session.StartAsync(default, 10, 6).GetAwaiter().GetResult();
            Check(session.State.Result is null && session.State.Error is not null && !session.State.Loading, "invalid input clears previous advice and reports a controlled error");
            var disposing = session.StartAsync(position, 10, 6, new(10_000_000,30000));
            session.Dispose(); disposing.GetAwaiter().GetResult();
            session.StartAsync(position,10,7).GetAwaiter().GetResult();
            Check(session.State.Result is null && !session.State.Loading, "disposed session stays inert");
        }),
    ];

    internal static void ReferencePlay(int[] grid, bool[] colors, TriadCardDefinition[] cards, int token, int cell, bool blue, TriadRules rules = TriadRules.Normal)
    {
        var strength = ReferenceStrength(cards,grid,rules);
        var placed = strength[token];
        var neighbors = new List<(int Square, int Attack, int Defense, bool Enemy)>();
        for (var other = 0; other < 9; other++)
        {
            if (grid[other] < 0 || Math.Abs(other / 3 - cell / 3) + Math.Abs(other % 3 - cell % 3) != 1) continue;
            var defender = strength[grid[other]];
            var (attack, defense) = other < cell
                ? (other / 3 == cell / 3 ? (placed.Left, defender.Right) : (placed.Top, defender.Bottom))
                : (other / 3 == cell / 3 ? (placed.Right, defender.Left) : (placed.Bottom, defender.Top));
            neighbors.Add((other,attack,defense,colors[other] != blue));
        }
        var special = new HashSet<int>();
        foreach (var pair in neighbors.SelectMany((first,index) => neighbors.Skip(index + 1).Select(second => (first,second))))
        {
            var same = (rules & TriadRules.Same) != 0 && pair.first.Attack == pair.first.Defense && pair.second.Attack == pair.second.Defense;
            var plus = (rules & TriadRules.Plus) != 0 && pair.first.Attack + pair.first.Defense == pair.second.Attack + pair.second.Defense;
            if (!same && !plus) continue;
            if (pair.first.Enemy) special.Add(pair.first.Square);
            if (pair.second.Enemy) special.Add(pair.second.Square);
        }
        foreach (var neighbor in neighbors)
            if (neighbor.Enemy && (special.Contains(neighbor.Square) || ReferenceWins(neighbor.Attack,neighbor.Defense,rules,false))) colors[neighbor.Square] = blue;
        grid[cell] = token; colors[cell] = blue;
        var frontier = special;
        while (frontier.Count > 0)
        {
            var next = new HashSet<int>();
            foreach (var source in frontier)
            for (var target = 0; target < 9; target++)
            {
                if (grid[target] < 0 || colors[target] == blue || Math.Abs(source / 3 - target / 3) + Math.Abs(source % 3 - target % 3) != 1) continue;
                var attacker = strength[grid[source]]; var defender = strength[grid[target]];
                var (attack,defense) = target < source
                    ? (target / 3 == source / 3 ? (attacker.Left,defender.Right) : (attacker.Top,defender.Bottom))
                    : (target / 3 == source / 3 ? (attacker.Right,defender.Left) : (attacker.Bottom,defender.Top));
                if (!ReferenceWins(attack,defense,rules,true)) continue;
                colors[target] = blue; next.Add(target);
            }
            frontier = next;
        }
    }
    internal static TriadCardDefinition[] ReferenceStrength(TriadCardDefinition[] cards, int[] grid, TriadRules rules)
    {
        if ((rules & (TriadRules.Ascension | TriadRules.Descension)) == 0) return cards;
        var counts = grid.Where(index => index >= 0).Select(index => cards[index].TypeId).Where(type => type != 0)
            .GroupBy(type => type).ToDictionary(group => group.Key, group => group.Count());
        return cards.Select(card =>
        {
            var modifier = counts.GetValueOrDefault(card.TypeId) * ((rules & TriadRules.Descension) != 0 ? -1 : 1);
            int Rank(int value) => Math.Min(10,Math.Max(1,value + modifier));
            return card with {Top=Rank(card.Top),Right=Rank(card.Right),Bottom=Rank(card.Bottom),Left=Rank(card.Left)};
        }).ToArray();
    }
    private static bool ReferenceWins(int attack, int defense, TriadRules rules, bool combo)
    {
        var comparison = attack.CompareTo(defense);
        if ((rules & TriadRules.Reverse) != 0) comparison = -comparison;
        return comparison > 0 || (!combo && (rules & TriadRules.FallenAce) != 0 && attack + defense == 11 && (attack == 1 || attack == 10));
    }
    internal static int ReferenceSolve(int[] grid, bool[] colors, TriadCardDefinition[] cards, HashSet<int> own, HashSet<int> enemy, bool turn, TriadRules rules = TriadRules.Normal)
    {
        if (grid.All(index => index >= 0)) return 2 * (Enumerable.Range(0,9).Count(index => colors[index]) + own.Count) - 10;
        var values = new List<int>();
        var available = turn ? own : enemy;
        foreach (var token in (rules & TriadRules.Order) != 0 ? available.Order().Take(1) : available)
        for (var cell = 0; cell < 9; cell++)
        {
            if (grid[cell] >= 0) continue;
            var nextGrid = grid.ToArray(); var nextColors = colors.ToArray();
            var nextOwn = own.ToHashSet(); var nextEnemy = enemy.ToHashSet();
            ReferencePlay(nextGrid,nextColors,cards,token,cell,turn,rules);
            if (turn) nextOwn.Remove(token); else nextEnemy.Remove(token);
            values.Add(ReferenceSolve(nextGrid,nextColors,cards,nextOwn,nextEnemy,!turn,rules));
        }
        return turn ? values.Max() : values.Min();
    }
}
