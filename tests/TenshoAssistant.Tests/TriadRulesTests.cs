using TenshoAssistant.MiniGames;

internal static class TriadRulesTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static TriadCardDefinition[] Deck(int start, int value) => Enumerable.Range(start,5)
        .Select(id => new TriadCardDefinition((uint)id,id,1,value,value,value,value,0,"","")).ToArray();
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Triad rule matrix covers all ranks four orientations and Reverse Fallen Ace combinations", () =>
        {
            for (var mask = 0; mask < 4; mask++)
            for (var attack = 1; attack <= 10; attack++)
            for (var defense = 1; defense <= 10; defense++)
            foreach (var neighbor in new[] {1,3,5,7})
            {
                var rules = (TriadRules)mask;
                var position = TriadPosition.Restore(Deck(1,attack),Deck(6,defense),
                    [new(0,0,TriadPlayer.Blue),new(5,neighbor,TriadPlayer.Red)],TriadPlayer.Blue,rules);
                var expected = (mask & 1) == 0 ? attack > defense : attack < defense;
                // In either direction 1 and A can capture each other with Fallen Ace; ties never do.
                if ((mask & 2) != 0 && ((attack == 1 && defense == 10) || (attack == 10 && defense == 1))) expected = true;
                var after = position.Play(new(1,4));
                Check((after.OwnerAt(neighbor) == TriadPlayer.Blue) == expected, $"rules={rules}, {attack} vs {defense}, neighbor={neighbor}");
                Check(after.Rules == rules && position.OwnerAt(neighbor) == TriadPlayer.Red, "rules retained in immutable successors");
            }
        }),
        ("Triad unsupported flags are rejected and default rules remain normal", () =>
        {
            var blue = Deck(1,1); var red = Deck(6,10);
            Check(TriadPosition.Create(blue,red,TriadPlayer.Blue).Rules == TriadRules.Normal, "legacy callers stay normal");
            try { TriadPosition.Create(blue,red,TriadPlayer.Blue,(TriadRules)128); throw new Exception("unsupported flags accepted"); }
            catch (ArgumentOutOfRangeException) { }
            try { TriadPosition.Create(blue,red,TriadPlayer.Blue,(TriadRules)(-1)); throw new Exception("unknown mask accepted"); }
            catch (ArgumentOutOfRangeException) { }
        }),
        ("Triad special rule endgames match independent exhaustive search for every root candidate", () =>
        {
            var random = new Random(230023);
            for (var mask = 0; mask < 4; mask++)
            for (var sample = 0; sample < 24; sample++)
            {
                var rules = (TriadRules)mask;
                var cards = Enumerable.Range(1,10).Select(id => new TriadCardDefinition((uint)id,id,1,
                    random.Next(1,11),random.Next(1,11),random.Next(1,11),random.Next(1,11),0,"","")).ToArray();
                var state = TriadPosition.Create(cards.Take(5),cards.Skip(5),sample % 2 == 0 ? TriadPlayer.Blue : TriadPlayer.Red,rules);
                for (var step = 0; step < 6; step++)
                {
                    var legal = state.LegalMoves(); state = state.Play(legal[random.Next(legal.Count)]);
                }
                var result = TripleTriadSolver.Solve(state,new(100000,10000));
                Check(result.Exact && result.CompletedDepth == 3, "all three remaining turns searched");
                foreach (var candidate in result.Moves)
                {
                    var grid = Enumerable.Range(0,9).Select(state.CardAt).ToArray();
                    var colors = Enumerable.Range(0,9).Select(square => state.OwnerAt(square) == TriadPlayer.Blue).ToArray();
                    var own = Enumerable.Range(0,5).Where(state.InHand).ToHashSet();
                    var enemy = Enumerable.Range(5,5).Where(state.InHand).ToHashSet();
                    var move = candidate.Move;
                    TriadSolverTests.ReferencePlay(grid,colors,cards,move.CardIndex,move.Square,state.Turn == TriadPlayer.Blue,rules);
                    if (state.Turn == TriadPlayer.Blue) own.Remove(move.CardIndex); else enemy.Remove(move.CardIndex);
                    var expected = TriadSolverTests.ReferenceSolve(grid,colors,cards,own,enemy,state.Turn != TriadPlayer.Blue,rules);
                    Check(candidate.BlueDifference == expected, "rule-aware minimax agrees with independent state transitions");
                }
            }
        }),
        ("Triad session replacement retains the new rules and rejects old rule advice", () =>
        {
            var blue = Deck(1,1); var red = Deck(6,10);
            var normal = TriadPosition.Create(blue,red,TriadPlayer.Blue);
            var changed = TriadPosition.Create(blue,red,TriadPlayer.Blue,TriadRules.Reverse | TriadRules.FallenAce);
            using var session = new TriadSolveSession();
            var old = session.StartAsync(normal,10,1,new(1000000,3000));
            var next = session.StartAsync(changed,10,2,new(1000,10000,1));
            Task.WhenAll(old,next).GetAwaiter().GetResult();
            Check(session.State.Revision == 2 && session.State.Position?.Rules == changed.Rules && session.State.Result is not null,
                "generation and snapshot include the active rule combination");
        }),
    ];
}
