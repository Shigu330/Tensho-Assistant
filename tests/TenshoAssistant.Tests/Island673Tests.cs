using TenshoAssistant.Island;

// N-07 (0.673): the materials of a craft and of an agenda.
internal static class Island673Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly IslandMaterial Palm=new(1,"Palm",2,"Material"),Wort=new(2,"Wort",1,"Material"),Cabbage=new(3,"Cabbage",1,"Produce");
    private static IslandCraft Craft(uint id,IReadOnlyList<IslandMaterial>? materials)=>new(id,id+100,"c"+id,4,10,1,[1],materials);
    private static IslandAgenda Agenda(params IReadOnlyList<IslandCraft>[] chains)
        =>new(chains.Select((chain,index)=>new IslandChain(index,1,chain.Select((craft,step)=>new IslandStep(craft,step*4,step>0,1)).ToArray())).ToArray(),0);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Island materials: row 0 is a material, only the amount tells an empty slot",() =>
        {
            var used=IslandMaterials.Used([(0,2),(7,2),(0,0),(0,0)]);
            Check(used.Count==2,"two slots used, as Isleworks Potion in the game's files");
            Check(used[0]==(0u,2) && used[1]==(7u,2),"row 0 with an amount is kept");
            Check(IslandMaterials.Used([(0,0)]).Count==0,"row 0 without an amount is empty");
        }),
        ("Island materials: totals add up across workshops and sort the same way",() =>
        {
            var a=Craft(1,[Palm,Wort]);var b=Craft(2,[Palm,Cabbage]);
            var totals=IslandMaterials.Totals(Agenda([a,b],[a]));
            Check(totals.Count==3,"one line per material");
            Check(totals.Single(item=>item.ItemId==1).Amount==6,"palm three times 2");
            Check(totals.Single(item=>item.ItemId==2).Amount==2,"wort twice");
            Check(totals[0].Name=="Palm" && totals[1].Name=="Wort" && totals[2].Name=="Cabbage","by category, then name");
            Check(IslandMaterials.Totals(Agenda([a],[a,b])).SequenceEqual(totals),"the agenda's order changes nothing");
        }),
        ("Island materials: an efficient craft counts once",() =>
        {
            var agenda=Agenda([Craft(1,[Palm]),Craft(2,[Palm])]);
            Check(agenda.Chains[0].Steps[1].Efficient,"the second step is efficient");
            Check(IslandMaterials.Totals(agenda)[0].Amount==4,"2 + 2, not 2 + 4");
        }),
        ("Island materials: a craft without materials says the list is incomplete",() =>
        {
            Check(IslandMaterials.Complete(Agenda([Craft(1,[Palm])])),"complete");
            Check(!IslandMaterials.Complete(Agenda([Craft(1,[Palm]),Craft(2,null)])),"unknown");
            Check(!IslandMaterials.Complete(Agenda([Craft(1,[])])),"none in the sheet");
            Check(IslandMaterials.Totals(Agenda([Craft(2,null)])).Count==0,"nothing invented");
        }),
    ];
}
