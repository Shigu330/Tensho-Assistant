using TenshoAssistant.MiniGames;

// 0.1835 (X-835, security review I7; DEC-033, Dalamud's rules): the move advice at Triple Triad is for a match against an NPC only. It used to
// mean « outside places 940 and 941 », the NPC being whatever was targeted (even without a match row) and kept from one match to the next: with
// open cards, a match against a player could get the best move framed. Now the opponent the match window names (value 4) must be the NPC
// targeted, one with a TripleTriad row; the challenge window (value 7) the same for the NPC's deck and odds. Evidence:
// docs/evidence/0.160.0-triad-match-capture.log (match against Munglig: value 4 « Munglig », challenge value 7 « Munglig », value 1 the player).
internal static class TriadOpponent1835Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static TriadCardDefinition Card(uint id,int stars,int top,int right,int bottom,int left,uint type) => new(id,(int)id,stars,top,right,bottom,left,type,"","");
    private static readonly TriadCardDefinition[] Deck = [Card(53,4,8,1,8,8,1),Card(58,4,5,9,9,2,0),Card(196,3,6,6,7,4,0),Card(42,3,7,6,1,7,1),Card(89,3,8,2,3,8,0),Card(93,3,1,8,3,8,0)];
    private static readonly TriadRawCard Empty = new(false,0,0,0,0,0,0,0);
    private static TriadRawCard Raw(byte owner,byte stars,byte type,byte top,byte right,byte bottom,byte left) => new(true,owner,stars,type,top,right,bottom,left);
    private static TriadCardDefinition? Find(uint id) => Deck.FirstOrDefault(card => card.Id == id);
    // All Open: the opponent's two cards left are shown, so the advice needs no NPC deck (the case of the finding).
    private static TriadObservedFrame Open(TriadMatchContext context) => new(1,
        [Raw(1,3,0,1,5,8,7),Raw(1,3,0,8,8,4,1),Empty,Raw(1,4,4,9,3,6,8),Empty],
        [Empty,Empty,Empty,Raw(2,4,0,5,9,9,2),Raw(2,3,0,6,6,7,4)],
        [Raw(1,3,1,7,6,1,7),Raw(2,5,0,7,10,9,1),Empty,Empty,Raw(2,3,0,8,2,3,8),Empty,Raw(1,4,1,8,1,8,8),Raw(2,3,0,6,8,7,1),Empty],
        context);
    private static TriadMatchContext Context(bool againstNpc) => new([10,12,4],null,againstNpc ? 1019323u : 0u,[],[],null,null,true,againstNpc);

    public static IEnumerable<(string Name,Action Test)> Cases =>
    [
        ("Triad opponent 1835: the opponent the window names must be the NPC targeted, with a match row (X-835)",() =>
        {
            Check(TriadMatchAdvisor.OpponentIsNpc("\"Munglig\"","Munglig",true),"the capture's NPC: the same name");
            Check(TriadMatchAdvisor.OpponentIsNpc("munglig ","Munglig",true),"case and spaces ignored");
            Check(TriadMatchAdvisor.OpponentIsNpc("Triple-Triad-Händlerin","Triple-Triad-Händler[a]in",true),"the sheet's grammar marks ignored");
            Check(!TriadMatchAdvisor.OpponentIsNpc("\"Joueur Exemple\"","Munglig",true),"a player named by the window, an NPC targeted: no");
            Check(!TriadMatchAdvisor.OpponentIsNpc("\"Munglig\"","Munglig",false),"an NPC without a TripleTriad row: no");
            Check(!TriadMatchAdvisor.OpponentIsNpc(null,"Munglig",true) && !TriadMatchAdvisor.OpponentIsNpc("\"\"","Munglig",true),"the name not read: no");
            Check(!TriadMatchAdvisor.OpponentIsNpc("\"\"","",true) && !TriadMatchAdvisor.OpponentIsNpc("\"Munglig\"","",true),"no NPC name: no");
        }),
        ("Triad opponent 1835: open cards against a player get no move, against the NPC they do (X-835)",() =>
        {
            var npc = TriadMatchAdvisor.Advise(Open(Context(TriadMatchAdvisor.OpponentIsNpc("\"Munglig\"","Munglig",true))),Find,new TriadSolveOptions(Milliseconds:5000));
            Check(npc.Problem == TriadAdviceProblem.None && npc.Best.Count > 0,$"against the NPC named by the window: advised ({npc.Problem})");
            var player = TriadMatchAdvisor.Advise(Open(Context(TriadMatchAdvisor.OpponentIsNpc("\"Joueur Exemple\"","Munglig",true))),Find,new TriadSolveOptions(Milliseconds:5000));
            Check(player.Problem == TriadAdviceProblem.NotAgainstNpc && player.Best.Count == 0,$"against a player, an NPC still targeted: nothing ({player.Problem})");
        }),
        ("Triad opponent 1835: the reader takes the NPC per match and checks both windows' opponent (X-835)",() =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MiniGames/TriadObservationReader.Dalamud.cs"));
            Check(reader.Contains("var named = TriadMatchAdvisor.OpponentIsNpc(opponent, deckNpcName, deckHasMatch);",StringComparison.Ordinal)
                && reader.Contains("AtkValueRead.IsText(unit->AtkValues[4].Type) ? Bounded(unit->AtkValues[4].String) : null",StringComparison.Ordinal)
                && reader.Contains("named ? deckNpc : 0,",StringComparison.Ordinal)
                && reader.Contains("known, !TriadMatchAdvisor.Competitive(client.TerritoryType) && named);",StringComparison.Ordinal),"the match: the opponent of value 4 against the NPC targeted");
            Check(reader.Contains("var npcConfirmed = TriadMatchAdvisor.OpponentIsNpc(challenger, deckNpcName, deckHasMatch);",StringComparison.Ordinal)
                && reader.Contains("AtkValueRead.IsText(unit->AtkValues[7].Type) ? Bounded(unit->AtkValues[7].String) : null",StringComparison.Ordinal)
                && reader.Contains("new(npcId, rules.AsReadOnly(), npcFixed, npcVariable,",StringComparison.Ordinal),"the challenge: the NPC's deck only for the opponent it names");
            Check(reader.Contains("if (target == 0) ClearDeck();",StringComparison.Ordinal) && reader.Contains("matchOpen = false;",StringComparison.Ordinal)
                && reader.Contains("if (!matchOpen)",StringComparison.Ordinal) && reader.Contains("deckHasMatch = true;",StringComparison.Ordinal),
                "no NPC kept from an earlier match or without its match row");
        }),
    ];
}
