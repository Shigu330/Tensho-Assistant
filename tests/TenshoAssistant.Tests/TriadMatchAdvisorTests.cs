using TenshoAssistant.MiniGames;

/// <summary>Match against Munglig (NPC 1019323), docs/evidence/0.160.0-triad-match-capture.log: position at 14:39:58, the player's third move.</summary>
internal static class TriadMatchAdvisorTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static TriadCardDefinition Card(uint id,int stars,int top,int right,int bottom,int left,uint type) => new(id,(int)id,stars,top,right,bottom,left,type,"","");
    // Munglig's deck (docs/evidence/0.159.0-triad-npc-probe.log, PROBE_TRIAD=1019323): fixed Ramuh, Cid, Sadu; variable Garuda, Lucia, Edmont.
    private static readonly TriadCardDefinition[] Deck =
    [
        Card(53,4,8,1,8,8,1),Card(58,4,5,9,9,2,0),Card(196,3,6,6,7,4,0),Card(42,3,7,6,1,7,1),Card(89,3,8,2,3,8,0),Card(93,3,1,8,3,8,0),
    ];
    private static readonly TriadRawCard Empty = new(false,0,0,0,0,0,0,0);
    private static TriadRawCard Raw(byte owner,byte stars,byte type,byte top,byte right,byte bottom,byte left) => new(true,owner,stars,type,top,right,bottom,left);
    private static readonly TriadRawCard Hidden = new(true,0,1,0,0,0,0,0);
    private static TriadMatchContext Context(int? forced,bool deck=true) =>
        new([10,12,4,9],forced,1019323,deck ? [53,58,196] : [],deck ? [42,89,93] : [],null,null);
    private static TriadObservedFrame Frame(byte phase,TriadMatchContext context) => new(phase,
        [Raw(1,3,0,1,5,8,7),Raw(1,3,0,8,8,4,1),Empty,Raw(1,4,4,9,3,6,8),Empty],
        [Empty,Empty,Empty,Hidden,Hidden],
        [Raw(1,3,1,7,6,1,7),Raw(2,5,0,7,10,9,1),Empty,Empty,Raw(2,3,0,8,2,3,8),Empty,Raw(1,4,1,8,1,8,8),Raw(2,3,0,6,8,7,1),Empty],
        context);
    private static TriadCardDefinition? Find(uint id) => Deck.FirstOrDefault(card => card.Id == id);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Triad advice fills hidden NPC cards from the deck and plays the card imposed by Chaos",()=>
        {
            var advice = TriadMatchAdvisor.Advise(Frame(2,Context(1)),Find,new TriadSolveOptions(Milliseconds:5000));
            Check(advice.Problem == TriadAdviceProblem.None,$"advice computed ({advice.Problem})");
            // Garuda, Lucia and Ramuh were seen: the two hidden cards are the unseen fixed Cid and Sadu (revealed later in the match).
            Check(advice.Hands == 1,"one possible opponent hand");
            Check(advice.All.All(move => move.HandSlot == 1) && advice.Best.Count > 0,"only the imposed card (slot 2) is advised");
            Check(advice.All.All(move => move.Square is 2 or 3 or 5 or 8),"only empty squares");
        }),
        ("Triad advice maps rules and refuses unclear matches instead of guessing",()=>
        {
            Check(TriadMatchAdvisor.RulesOf([10,12,4,9,2]) == (TriadRules.Reverse | TriadRules.Ascension | TriadRules.Same),"Inversion, Bonus, Identique; Chaos and All Open are not move rules");
            Check(TriadMatchAdvisor.Advise(Frame(0,Context(null)),Find).Problem == TriadAdviceProblem.NotYourTurn,"waiting phase");
            Check(TriadMatchAdvisor.Advise(Frame(1,Context(null,false)),Find).Problem == TriadAdviceProblem.NpcDeckUnknown,"hidden cards without deck");
            var wrong = new TriadMatchContext([4],null,1,[93],[],null,null);
            Check(TriadMatchAdvisor.Advise(Frame(1,wrong),Find).Problem == TriadAdviceProblem.DeckMismatch,"hidden cards not covered by the deck");
            var free = TriadMatchAdvisor.Advise(Frame(1,Context(null)),Find,new TriadSolveOptions(Milliseconds:5000));
            Check(free.Problem == TriadAdviceProblem.None && free.All.Select(move => move.HandSlot).Distinct().Count() == 3,"free move: the three cards in hand are ranked");
        }),
        ("Triad advice: rules not read, or a masked move without its card, give no advice (R-14)",()=>
        {
            Check(TriadMatchAdvisor.Advise(Frame(1,Context(null) with {RulesKnown=false}),Find).Problem == TriadAdviceProblem.RulesUnknown,"rules not read");
            Check(TriadMatchAdvisor.Advise(Frame(2,Context(null)),Find).Problem == TriadAdviceProblem.UnknownCard,"masked move, imposed card not read");
            Check(new TriadRequestInfo(1,[],[],[],default,[],DateTimeOffset.UtcNow).RulesKnown,"known unless said otherwise");
            var root=Path.GetFullPath("src/TenshoAssistant");
            var reader=File.ReadAllText(Path.Combine(root,"MiniGames/TriadObservationReader.Dalamud.cs"));
            Check(reader.Contains("Triad request rule values") && reader.Contains("ContainsValue((uint)value.Int)") && !reader.Contains("is >= 1 and <= 15"),"ids from the sheet, values logged for the evidence");
            Check(reader.Contains("catch (Exception error) { ruleIds = [];"),"an unreadable sheet does not cost the observation");
            var panel=File.ReadAllText(Path.Combine(root,"MainWindow.TriadRequest.cs"));
            Check(panel.Contains("if(request.RulesKnown)") && panel.Contains("TriadRequest.RulesUnknown"),"no odds, and it says so");
        }),
        ("Map and dresser readers: what is not read stays unknown (R-14)",()=>
        {
            var root=Path.GetFullPath("src/TenshoAssistant");
            var map=File.ReadAllText(Path.Combine(root,"Maps/MapLiveReader.Dalamud.cs"));
            Check(map.Contains("if(player==null)return (null,null);") && map.Contains("adventureRows.Length>bits.Length*8)return (unlocked,null);") && map.Contains("\"Sightseeing rows unavailable.\""),"unknown collected sets");
            var bank=File.ReadAllText(Path.Combine(root,"Inventory/BankInventoryBridge.Dalamud.cs"));
            Check(bank.Contains("mirage->IsSetSlotUnlocked(index, column)") && bank.Contains("held.Any(value => value) ? held : null") && bank.Contains("Dresser outfit {0}: slots held {1}."),"outfit pieces by slot, nothing held = as before, logged");
            var plugin=File.ReadAllText(Path.Combine(root,"Plugin.cs"));
            Check(plugin.Contains("outfit.Ring.RowId }") && !plugin.Contains("outfit.Ring.RowId }.Where"),"columns kept in order");
        }),
        ("Triad frame keeps match context copies and drops incomplete rectangles",()=>
        {
            var rules = new List<uint>{4};
            var rects = Enumerable.Range(0,5).Select(index => new TriadRect(index,0,10,10)).ToArray();
            var frame = Frame(2,new TriadMatchContext(rules,7,1,[],[],rects,rects));
            rules.Add(9);
            Check(frame.Context!.RuleIds.Count == 1 && frame.Context.ForcedSlot is null,"rules copied, out-of-range imposed slot dropped");
            Check(frame.Context.BlueRects is { Count:5 } && frame.Context.BoardRects is null,"hand rectangles kept, board needs nine");
            Check(frame.BlueToMove && !Frame(0,Context(null)).BlueToMove,"turn states 1 and 2 are the player's");
        }),
    ];
}
