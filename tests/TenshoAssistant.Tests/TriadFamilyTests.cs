using TenshoAssistant.MiniGames;

internal static class TriadFamilyTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    private static TriadCardDefinition[] Cards(int value = 4) => Enumerable.Range(1,10)
        .Select(id => new TriadCardDefinition((uint)id,id,1,value,value,value,value,0,"","")).ToArray();
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Triad Order enforces first remaining card for both starting players and restored prefixes", () =>
        {
            var cards = Cards();
            foreach (var first in new[] {TriadPlayer.Blue,TriadPlayer.Red})
            {
                var state = TriadPosition.Create(cards.Take(5),cards.Skip(5),first,TriadRules.Order);
                var firstToken = first == TriadPlayer.Blue ? 0 : 5;
                Check(state.LegalMoves().Count == 9 && state.LegalMoves().All(move => move.CardIndex == firstToken), "nine choices of square, one card only");
                Throws<ArgumentException>(() => state.Play(new(firstToken+1,0)));
                state = state.Play(new(firstToken,0));
                Check(state.LegalMoves().All(move => move.CardIndex == (firstToken == 0 ? 5 : 0)), "opponent also follows entered order");
            }
            Throws<ArgumentException>(() => TriadPosition.Restore(cards.Take(5),cards.Skip(5),[new(1,0,TriadPlayer.Blue)],TriadPlayer.Blue,TriadRules.Order));
            var valid = TriadPosition.Restore(cards.Take(5),cards.Skip(5),[new(0,0,TriadPlayer.Blue),new(5,1,TriadPlayer.Red)],TriadPlayer.Blue,TriadRules.Order);
            Check(valid.CanPlayCard(1) && !valid.CanPlayCard(2), "restored next card derives from prefix");
        }),
        ("Triad family modifiers count typed board instances across colors and apply to both hands", () =>
        {
            var cards = Cards();
            cards[0] = cards[0] with {TypeId=9000}; cards[4] = cards[4] with {TypeId=9000,Top=10};
            cards[5] = cards[5] with {TypeId=9000,Left=5,Right=1};
            cards[1] = cards[1] with {Left=9}; cards[6] = cards[6] with {TypeId=42};
            var state = TriadPosition.Create(cards.Take(5),cards.Skip(5),TriadPlayer.Blue,TriadRules.Ascension);
            Check(state.EffectiveSide(0,1) == 4, "empty board has no bonus");
            state = state.Play(new(0,0));
            Check(state.EffectiveSide(0,1) == 5 && state.EffectiveSide(5,3) == 6 && state.FamilyModifier(4) == 1, "board and both hands share count");
            state = state.Play(new(5,1));
            Check(state.OwnerAt(0) == TriadPlayer.Red && state.FamilyModifier(0) == 2 && state.FamilyModifier(5) == 2, "color changes do not change family count");
            Check(state.EffectiveSide(4,0) == 10 && state.FamilyModifier(6) == 0 && state.FamilyModifier(1) == 0, "clamp to A, distinct family and untyped unaffected");
            state = state.Play(new(1,2));
            Check(state.OwnerAt(1) == TriadPlayer.Blue && state.FamilyModifier(0) == 2, "recapture without new typed card does not add bonus");
        }),
        ("Triad Bonus and Malus apply the new card increment only after captures", () =>
        {
            var cards = Cards(5); cards[0] = cards[0] with {TypeId=1};
            var before = TriadPosition.Restore(cards.Take(5),cards.Skip(5),[new(5,1,TriadPlayer.Red)],TriadPlayer.Red,TriadRules.Ascension);
            var after = before.Play(new(0,0));
            Check(after.OwnerAt(1) == TriadPlayer.Red && after.EffectiveSide(0,1) == 6, "new bonus cannot turn an equality into a capture retroactively");
            cards[5] = cards[5] with {Left=4};
            var malus = TriadPosition.Restore(cards.Take(5),cards.Skip(5),[new(5,1,TriadPlayer.Red)],TriadPlayer.Red,TriadRules.Descension).Play(new(0,0));
            Check(malus.OwnerAt(1) == TriadPlayer.Blue && malus.EffectiveSide(0,1) == 4, "new malus does not cancel capture made before the decrement");
            cards[0] = cards[0] with {Top=1};
            var low = TriadPosition.Restore(cards.Take(5),cards.Skip(5),[new(0,0,TriadPlayer.Blue)],TriadPlayer.Blue,TriadRules.Descension);
            Check(low.EffectiveSide(0,0) == 1 && low.FamilyModifier(0) == -1, "minimum remains one");
        }),
        ("Triad family update waits until Same combo has fully resolved", () =>
        {
            var cards = Cards(1);
            cards[0] = cards[0] with {Left=5};
            cards[2] = cards[2] with {TypeId=1};
            cards[5] = cards[5] with {TypeId=1,Right=4};
            cards[6] = cards[6] with {Right=2};
            var before = TriadPosition.Restore(cards.Take(5),cards.Skip(5),
                [new(0,2,TriadPlayer.Red),new(1,0,TriadPlayer.Blue),new(5,1,TriadPlayer.Red),new(6,3,TriadPlayer.Blue)],TriadPlayer.Blue,TriadRules.Same|TriadRules.Ascension);
            var after = before.Play(new(2,4));
            Check(before.EffectiveSide(5,1) == 5 && after.OwnerAt(1) == TriadPlayer.Blue && after.OwnerAt(2) == TriadPlayer.Red,
                "combo equality uses previous family count");
            Check(after.EffectiveSide(5,1) == 6 && after.FamilyModifier(2) == 2, "completed position applies count including new root");
        }),
        ("Triad family rules reject incompatible flags and preserve copied arbitrary family identities", () =>
        {
            var cards = Cards(8); cards[0] = cards[0] with {TypeId=uint.MaxValue}; cards[5] = cards[5] with {TypeId=uint.MaxValue};
            Throws<ArgumentException>(() => TriadPosition.Create(cards.Take(5),cards.Skip(5),TriadPlayer.Blue,TriadRules.Ascension|TriadRules.Descension));
            var state = TriadPosition.Create(cards.Take(5),cards.Skip(5),TriadPlayer.Blue,TriadRules.Ascension);
            cards[0] = cards[0] with {TypeId=123};
            var next = state.Play(new(0,0));
            Check(next.FamilyModifier(5) == 1 && state.FamilyModifier(5) == 0, "family masks copied from immutable starting cards, no array indexed by native type ID");
            Throws<ArgumentOutOfRangeException>(() => state.EffectiveSide(10,0));
            Throws<ArgumentOutOfRangeException>(() => state.EffectiveSide(0,4));
        }),
        ("Triad all 96 supported family and Order combinations match independent endgame search", () =>
        {
            var random = new Random(250025);
            for (var family = 0; family < 3; family++)
            for (var order = 0; order < 2; order++)
            for (var mask = 0; mask < 16; mask++)
            for (var sample = 0; sample < 4; sample++)
            {
                var rules = (TriadRules)mask | (order == 1 ? TriadRules.Order : 0)
                    | (family == 1 ? TriadRules.Ascension : family == 2 ? TriadRules.Descension : 0);
                var cards = Cards().Select(card => card with {TypeId=(uint)random.Next(0,4),Top=random.Next(1,11),Right=random.Next(1,11),Bottom=random.Next(1,11),Left=random.Next(1,11)}).ToArray();
                var first = sample % 2 == 0 ? TriadPlayer.Blue : TriadPlayer.Red;
                var state = TriadPosition.Create(cards.Take(5),cards.Skip(5),first,rules);
                var grid = Enumerable.Repeat(-1,9).ToArray(); var colors = new bool[9];
                for (var step = 0; step < 6; step++)
                {
                    var blue = (step % 2 == 0) == (first == TriadPlayer.Blue);
                    IEnumerable<int> hand = Enumerable.Range(blue ? 0 : 5,5).Where(token => !grid.Contains(token));
                    if (order == 1) hand = hand.Take(1);
                    var legal = hand.SelectMany(token => Enumerable.Range(0,9).Where(cell => grid[cell] < 0).Select(cell => new TriadMove(token,cell))).ToArray();
                    Check(state.LegalMoves().ToHashSet().SetEquals(legal), "all legal choices agree independently including Order");
                    var move = legal[random.Next(legal.Length)];
                    TriadSolverTests.ReferencePlay(grid,colors,cards,move.CardIndex,move.Square,blue,rules);
                    state = state.Play(move);
                    var effective = TriadSolverTests.ReferenceStrength(cards,grid,rules);
                    for (var index = 0; index < 10; index++)
                        Check(state.EffectiveSide(index,0) == effective[index].Top && state.EffectiveSide(index,1) == effective[index].Right
                            && state.EffectiveSide(index,2) == effective[index].Bottom && state.EffectiveSide(index,3) == effective[index].Left, "display and next-turn ranks agree with independent post-capture count");
                    Check(Enumerable.Range(0,9).All(cell => state.CardAt(cell) == grid[cell] && (grid[cell] < 0 || (state.OwnerAt(cell) == TriadPlayer.Blue) == colors[cell])), "independent family transition agrees");
                }
                var result = TripleTriadSolver.Solve(state,new(100000,10000));
                Check(result.Exact && result.Moves.Count == (order == 1 ? 3 : 6), "exact three-ply search obeys card order");
                foreach (var candidate in result.Moves)
                {
                    var copiedGrid = grid.ToArray(); var copiedColors = colors.ToArray();
                    var own = Enumerable.Range(0,5).Where(token => !grid.Contains(token)).ToHashSet();
                    var enemy = Enumerable.Range(5,5).Where(token => !grid.Contains(token)).ToHashSet();
                    var move = candidate.Move; var turn = state.Turn == TriadPlayer.Blue;
                    TriadSolverTests.ReferencePlay(copiedGrid,copiedColors,cards,move.CardIndex,move.Square,turn,rules);
                    if (turn) own.Remove(move.CardIndex); else enemy.Remove(move.CardIndex);
                    Check(candidate.BlueDifference == TriadSolverTests.ReferenceSolve(copiedGrid,copiedColors,cards,own,enemy,!turn,rules), "each minimax root agrees with independent family and Order rules");
                }
            }
        }),
    ];
}
