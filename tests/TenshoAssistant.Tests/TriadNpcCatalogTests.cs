using TenshoAssistant.MiniGames;

internal static class TriadNpcCatalogTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static TriadNpc Npc(uint match,string name,params uint[] rewards) => new(match,match+1000,name,0,"Zone",1,1,rewards,[],10);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Triad NPC: available from the quests the game lists; locked ones last, hidden on request; unread is not locked",()=>
        {
            TriadNpc Npc(uint id,bool any,params uint[] quests)=>new(id,id,"N"+id,1,"Z",0,0,[],[],0,quests.Select(quest=>new TriadRequiredQuest(quest,"Q"+quest)).ToArray(),any);
            HashSet<uint> done=[10];
            Check(Npc(1,false).Available(null)==true,"no quest asked: available, even unread");
            Check(Npc(2,false,10).Available(done)==true && Npc(3,false,11).Available(done)==false,"one quest");
            Check(Npc(4,false,10,11).Available(done)==false,"join 1: all of them");
            Check(Npc(5,true,10,11,12).Available(done)==true && Npc(6,true,11,12).Available(done)==false,"join 2: any of them");
            Check(Npc(7,false,11).Available(null) is null,"quests not read: unknown, not locked");
            var catalog=new TriadNpcCatalog([Npc(3,false,11),Npc(2,false,10)],new Dictionary<uint,TriadCardSource>());
            var rows=catalog.ToFace(_=>false,_=>true,false,npc=>npc.Available(done));
            Check(rows[0].Npc.MatchId==2 && rows[1].Npc.MatchId==3,"locked last");
            Check(catalog.ToFace(_=>false,_=>true,false,npc=>npc.Available(done),true).Single().Npc.MatchId==2,"hidden on request");
            Check(catalog.ToFace(_=>false,_=>true,false,npc=>npc.Available(null),true).Count==2,"unread stays shown");
            var loader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MiniGames/TriadNpcLoader.Dalamud.cs"));
            Check(loader.Contains("match.PreviousQuestJoin == 2") && loader.Contains("unlocks.IsQuestCompleted(quest)"),"read from the game");
        }),
        ("Triad NPCs to face: not beaten or with missing cards first, unknown counts as not beaten",()=>
        {
            var catalog = new TriadNpcCatalog([Npc(1,"Battu complet",5),Npc(2,"Battu manque",6),Npc(3,"Pas battu"),Npc(4,"Inconnu",7)],new Dictionary<uint,TriadCardSource>());
            bool? Beaten(uint match) => match switch { 1 or 2 => true, 3 => false, _ => null };
            bool Owned(uint card) => card is 5 or 7;
            var open = catalog.ToFace(Beaten,Owned,true);
            Check(open.Count == 3 && open.All(row => row.Npc.Name != "Battu complet"),"beaten with every card is hidden");
            Check(open[^1].Npc.Name == "Battu manque" && open[^1].Missing.SequenceEqual([6u]),"beaten NPCs after the others, missing cards listed");
            Check(catalog.ToFace(Beaten,Owned,false).Count == 4,"all NPCs when the filter is off");
        }),
    ];
}
