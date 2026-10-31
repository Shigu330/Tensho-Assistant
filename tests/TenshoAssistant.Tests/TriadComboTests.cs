using TenshoAssistant.MiniGames;

internal static class TriadComboTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static TriadCardDefinition[] Cards() => Enumerable.Range(1,10)
        .Select(id => new TriadCardDefinition((uint)id,id,1,1,1,1,1,0,"","")).ToArray();
    private static TriadPosition Position(TriadCardDefinition[] cards, TriadPlacement[] places, TriadRules rules)
        => TriadPosition.Restore(cards.Take(5),cards.Skip(5),places,TriadPlayer.Blue,rules);
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Triad Same counts friendly partners but ignores walls and single matches", () =>
        {
            var cards = Cards();
            cards[2] = cards[2] with { Top = 2, Left = 2 };
            cards[5] = cards[5] with { Bottom = 2 };
            cards[6] = cards[6] with { Right = 2 };
            cards[0] = cards[0] with { Left = 10 };
            var position = Position(cards,[new(0,2,TriadPlayer.Red),new(1,0,TriadPlayer.Blue),new(5,1,TriadPlayer.Red),new(6,3,TriadPlayer.Blue)],TriadRules.Same);
            Check(position.Play(new(2,4)).OwnerAt(1) == TriadPlayer.Blue, "one enemy and one friendly equality trigger Same");
            var single = TriadPosition.Restore(cards.Take(5),cards.Skip(5),[new(5,1,TriadPlayer.Red)],TriadPlayer.Red,TriadRules.Same);
            cards[0] = cards[0] with { Right = 1 };
            var corner = single.Play(new(0,0));
            Check(corner.OwnerAt(1) == TriadPlayer.Red, "one matched actual neighbor plus walls does not trigger Same");
        }),
        ("Triad Plus resolves two distinct sum groups simultaneously", () =>
        {
            var cards = Cards();
            cards[2] = cards[2] with { Top = 2, Right = 3, Bottom = 4, Left = 5 };
            cards[0] = cards[0] with { Bottom = 8 }; cards[1] = cards[1] with { Left = 7 };
            cards[5] = cards[5] with { Top = 8 }; cards[6] = cards[6] with { Right = 7 };
            var places = new TriadPlacement[] {new(0,1,TriadPlayer.Red),new(1,5,TriadPlayer.Red),new(5,7,TriadPlayer.Red),new(6,3,TriadPlayer.Red)};
            var after = Position(cards,places,TriadRules.Plus).Play(new(2,4));
            Check(new[] {1,3,5,7}.All(square => after.OwnerAt(square) == TriadPlayer.Blue), "both sum ten and sum twelve groups captured");
        }),
        ("Triad Same combo propagates multiple steps with Reverse and retains source snapshot", () =>
        {
            foreach (var reverse in new[] {false,true})
            {
                var cards = Cards();
                cards[3] = cards[3] with { Top = 3, Left = 3 };
                cards[5] = cards[5] with { Bottom = 3, Right = 9 };
                cards[6] = cards[6] with { Right = 3, Bottom = 9 };
                cards[0] = cards[0] with { Bottom = 9 };
                cards[1] = cards[1] with { Left = 9 };
                cards[7] = cards[7] with { Top = 9, Left = 9 };
                if (reverse) cards = cards.Select(card => card with {Top = 11-card.Top,Right = 11-card.Right,Bottom = 11-card.Bottom,Left = 11-card.Left}).ToArray();
                var rules = TriadRules.Same | (reverse ? TriadRules.Reverse : 0);
                var before = Position(cards,[new(5,1,TriadPlayer.Red),new(6,3,TriadPlayer.Red),new(0,2,TriadPlayer.Red),
                    new(1,5,TriadPlayer.Red),new(2,6,TriadPlayer.Red),new(7,8,TriadPlayer.Red)],rules);
                var after = before.Play(new(3,4));
                Check(new[] {1,2,3,4,5,6}.All(square => after.OwnerAt(square) == TriadPlayer.Blue)
                    && after.OwnerAt(8) == TriadPlayer.Red, "two seeds plus two-step chain, no unrelated capture");
                Check(before.OwnerAt(2) == TriadPlayer.Red && after.Rules == rules, "immutable rule snapshot retained");
            }
        }),
        ("Triad ordinary captures do not start combos and combo cannot retrigger Plus or Fallen Ace", () =>
        {
            var cards = Cards();
            cards[2] = cards[2] with { Top = 5, Left = 2 }; cards[5] = cards[5] with { Bottom = 4, Right = 9 };
            cards[6] = cards[6] with { Right = 3 };
            var places = new TriadPlacement[] {new(0,2,TriadPlayer.Red),new(1,0,TriadPlayer.Blue),new(5,1,TriadPlayer.Red),new(6,3,TriadPlayer.Blue)};
            var normal = Position(cards,places,TriadRules.Same).Play(new(2,4));
            Check(normal.OwnerAt(1) == TriadPlayer.Blue && normal.OwnerAt(2) == TriadPlayer.Red, "ordinary top capture not a combo seed");
            cards[2] = cards[2] with { Top = 2 }; cards[5] = cards[5] with { Bottom = 2 }; cards[6] = cards[6] with { Right = 2 };
            cards[0] = cards[0] with { Bottom = 2 }; cards[1] = cards[1] with { Top = 8, Left = 9 };
            var noRetrigger = Position(cards,[new(0,2,TriadPlayer.Red),new(1,5,TriadPlayer.Red),new(5,1,TriadPlayer.Red),new(6,3,TriadPlayer.Blue)],TriadRules.Plus).Play(new(2,4));
            Check(noRetrigger.OwnerAt(2) == TriadPlayer.Blue && noRetrigger.OwnerAt(5) == TriadPlayer.Red, "equal sums at combo corner do not trigger another Plus");
            foreach (var reverse in new[] {false,true})
            {
                cards = Cards(); cards[2] = cards[2] with { Top = 2, Left = 2 };
                cards[5] = cards[5] with { Bottom = 2, Right = reverse ? 10 : 1 };
                cards[6] = cards[6] with { Right = 2 }; cards[0] = cards[0] with { Left = reverse ? 1 : 10 };
                var rules = TriadRules.Same | TriadRules.FallenAce | (reverse ? TriadRules.Reverse : 0);
                var after = Position(cards,places,rules).Play(new(2,4));
                Check(after.OwnerAt(1) == TriadPlayer.Blue && after.OwnerAt(2) == TriadPlayer.Red, "Fallen Ace exception excluded from combo while Reverse remains active");
            }
        }),
        ("Triad all sixteen rule combinations agree with independent exhaustive combo search", () =>
        {
            var random = new Random(240024);
            for (var mask = 0; mask < 16; mask++)
            for (var sample = 0; sample < 12; sample++)
            {
                var maximum = sample % 2 == 0 ? 6 : 11;
                var cards = Cards().Select(card => card with {Top=random.Next(1,maximum),Right=random.Next(1,maximum),Bottom=random.Next(1,maximum),Left=random.Next(1,maximum)}).ToArray();
                var rules = (TriadRules)mask;
                var state = TriadPosition.Create(cards.Take(5),cards.Skip(5),sample % 2 == 0 ? TriadPlayer.Blue : TriadPlayer.Red,rules);
                for (var step = 0; step < 6; step++)
                {
                    var legal = state.LegalMoves(); var move = legal[random.Next(legal.Count)];
                    var grid = Enumerable.Range(0,9).Select(state.CardAt).ToArray();
                    var colors = Enumerable.Range(0,9).Select(square => state.OwnerAt(square) == TriadPlayer.Blue).ToArray();
                    TriadSolverTests.ReferencePlay(grid,colors,cards,move.CardIndex,move.Square,state.Turn == TriadPlayer.Blue,rules);
                    state = state.Play(move);
                    Check(Enumerable.Range(0,9).All(square => state.CardAt(square) == grid[square]
                        && (grid[square] < 0 || (state.OwnerAt(square) == TriadPlayer.Blue) == colors[square])), "independent move reconstruction agrees before solving");
                }
                var result = TripleTriadSolver.Solve(state,new(100000,10000));
                Check(result.Exact, "three-ply endgame exact");
                foreach (var candidate in result.Moves)
                {
                    var grid = Enumerable.Range(0,9).Select(state.CardAt).ToArray();
                    var colors = Enumerable.Range(0,9).Select(square => state.OwnerAt(square) == TriadPlayer.Blue).ToArray();
                    var own = Enumerable.Range(0,5).Where(state.InHand).ToHashSet(); var enemy = Enumerable.Range(5,5).Where(state.InHand).ToHashSet();
                    var move = candidate.Move;
                    TriadSolverTests.ReferencePlay(grid,colors,cards,move.CardIndex,move.Square,state.Turn == TriadPlayer.Blue,rules);
                    if (state.Turn == TriadPlayer.Blue) own.Remove(move.CardIndex); else enemy.Remove(move.CardIndex);
                    Check(candidate.BlueDifference == TriadSolverTests.ReferenceSolve(grid,colors,cards,own,enemy,state.Turn != TriadPlayer.Blue,rules), "each root minimax score agrees including combos");
                }
            }
        }),
    ];
}
