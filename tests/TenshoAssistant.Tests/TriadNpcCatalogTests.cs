using TenshoAssistant.MiniGames;

internal static class TriadNpcCatalogTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static TriadNpc Npc(uint match,string name,params uint[] rewards) => new(match,match+1000,name,0,"Zone",1,1,rewards,[],10);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
