using TenshoAssistant.MiniGames;

internal static class TriadMatchSimulatorTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static TriadCardDefinition Card(uint id,int stars,int top,int right,int bottom,int left,uint type = 0) => new(id,(int)id,stars,top,right,bottom,left,type,"","");
    // Munglig's deck (docs/evidence/0.159.0-triad-npc-probe.log).
    private static readonly TriadCardDefinition[] Fixed = [Card(53,4,8,1,8,8,1),Card(58,4,5,9,9,2),Card(196,3,6,6,7,4)];
    private static readonly TriadCardDefinition[] Variable = [Card(42,3,7,6,1,7,1),Card(89,3,8,2,3,8),Card(93,3,1,8,3,8)];
    private static readonly TriadCardDefinition[] Weak = Enumerable.Range(1,5).Select(index => Card((uint)(500 + index),1,1,2,1,2)).ToArray();
    private static readonly TriadCardDefinition[] Strong = [Card(601,5,10,10,9,9),Card(602,4,9,9,9,9),Card(603,4,9,8,9,9),Card(604,3,8,8,8,8),Card(605,3,8,7,8,8)];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Triad simulation is reproducible and ranks a strong deck above a weak one",()=>
        {
            var first = TriadMatchSimulator.Estimate(Strong,Fixed,Variable,TriadRules.Normal,TriadForcedCards.None,200,7);
            var again = TriadMatchSimulator.Estimate(Strong,Fixed,Variable,TriadRules.Normal,TriadForcedCards.None,200,7);
            var weak = TriadMatchSimulator.Estimate(Weak,Fixed,Variable,TriadRules.Normal,TriadForcedCards.None,200,7);
            Check(first == again && first.Games == 200 && first.Wins + first.Draws + first.Losses == 200,"same seed, same result, all games counted");
            Check(first.Win > 0.8 && weak.Win < 0.2,$"strong {first.Win:0.00} vs weak {weak.Win:0.00}");
            // Reverse: low numbers win, so the weak deck becomes the better one.
            Check(TriadMatchSimulator.Estimate(Weak,Fixed,Variable,TriadRules.Reverse,TriadForcedCards.Chaos,200,7).Win
                > TriadMatchSimulator.Estimate(Strong,Fixed,Variable,TriadRules.Reverse,TriadForcedCards.Chaos,200,7).Win,"Reverse with Chaos favours low cards");
            Check(TriadMatchSimulator.ForcedOf([10,9]) == TriadForcedCards.Chaos && TriadMatchSimulator.ForcedOf([8]) == TriadForcedCards.Order
                && TriadMatchSimulator.ForcedOf([4]) == TriadForcedCards.None,"Chaos and Order impose the card");
        }),
        ("Triad deck generator respects rarity limits and does not do worse than its start",()=>
        {
            var owned = Weak.Concat(Strong).Concat([Card(606,5,10,9,10,9),Card(607,4,9,9,8,9)]).ToArray();
            var suggestion = TriadMatchSimulator.Optimize(owned,Fixed,Variable,TriadRules.Normal,TriadForcedCards.None,1500,3);
            Check(suggestion is not null && suggestion.Deck.Count == 5 && suggestion.Deck.Select(card => card.Id).Distinct().Count() == 5,"five distinct cards");
            Check(suggestion!.Deck.Count(card => card.Stars >= 4) <= 2 && suggestion.Deck.Count(card => card.Stars == 5) <= 1,"at most two cards of four stars or more, one of five");
            Check(suggestion.Odds.Win > 0.7,$"generated deck wins ({suggestion.Odds.Win:0.00})");
            Check(TriadMatchSimulator.Optimize(Weak[..4],Fixed,Variable,TriadRules.Normal,TriadForcedCards.None,100,3) is null,"fewer than five owned cards: no deck");
            // An existing deck is a starting point and a floor: the result is never worse on the same draws.
            // A playable deck (one 5★, one 4★): decks breaking the rarity limits are never starting points.
            var existing = (IReadOnlyList<TriadCardDefinition>)[Strong[0],Strong[1],Card(613,3,9,8,9,9),Strong[3],Strong[4]];
            var floor = TriadMatchSimulator.Estimate(existing,Fixed,Variable,TriadRules.Normal,TriadForcedCards.Chaos,200,5);
            var seeded = TriadMatchSimulator.Optimize(Weak.Concat(Strong).ToArray(),Fixed,Variable,TriadRules.Normal,TriadForcedCards.Chaos,200,5,default,[existing],200);
            Check(seeded is not null && seeded.Odds.Score >= floor.Score,$"never below the existing deck ({seeded?.Odds.Score:0.00} vs {floor.Score:0.00})");
        }),
    ];
}
