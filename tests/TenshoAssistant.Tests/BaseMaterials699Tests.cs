using TenshoAssistant.Craft;

// W-09 (0.699): the list panels as what is left to fetch, by origin.
internal static class BaseMaterials699Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const uint Ore=1,Ingot=2,Rivet=3,Part=4,Nail=5;
    private static RecipeDefinition Recipe(uint id,uint result,int amount,params (uint Item,int Amount)[] ingredients)=>new(id,result,amount,"Job",1,ingredients.Select(row=>new RecipeIngredient(row.Item,row.Amount)).ToArray());
    private static RecipeDefinition? Of(uint item)=>item switch { Ingot=>Recipe(10,Ingot,1,(Ore,2)),Rivet=>Recipe(11,Rivet,2,(Ingot,1)),Part=>Recipe(12,Part,1,(Ingot,1),(Rivet,1)),Nail=>Recipe(13,Nail,1,(Ore,1)),_=>null };

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Base materials: a crafted item goes down to the ore",() =>
        {
            var lines=BaseMaterials.Expand([(Part,1)],_=>0,Of,_=>false);
            Check(lines.Count==1 && lines[0].Item==Ore && lines[0].Needed==4,"1 ingot and 1 run of rivets: 4 ore");
        }),
        ("Base materials: crafted items in stock count first, from one stock",() =>
        {
            var lines=BaseMaterials.Expand([(Part,1),(Ingot,1)],item=>item==Ingot ? 1:0,Of,_=>false);
            // The stock's ingot goes to the part; the rivet's run and the second ingot need 2 ingots: 4 ore.
            // 0.838 (X-65): the ingot taken from the stock stays a line, for that one.
            Check(lines.Count==2 && lines[0]==(Ingot,1) && lines[1]==(Ore,4),"one shared ingot, shown");
        }),
        ("Base materials: a crafted item that can be gathered or bought stays as it is, with its whole need",() =>
        {
            var lines=BaseMaterials.Expand([(Nail,3),(Ore,2)],item=>item==Nail ? 2:0,Of,item=>item==Nail);
            Check(lines.Count==2 && lines[0].Item==Nail && lines[0].Needed==3 && lines[1].Item==Ore && lines[1].Needed==2,"kept, in order");
        }),
        ("Base materials (0.838): a crafted ingredient held stays a line, for the part taken, at every level",() =>
        {
            var lines=BaseMaterials.Expand([(Part,1)],item=>item==Ingot ? 157:0,Of,_=>false);
            Check(lines.Count==1 && lines[0]==(Ingot,2),"held ingredient shown: one for the part, one for the rivet");
            Check(BaseMaterials.Expand([(Ingot,1)],item=>item==Ingot ? 5:0,Of,_=>false).Single()==(Ingot,1),"0.841: an item asked at the first level and held: a line too (the crafting list hands its ingredients)");
        }),
        ("Base materials: a loop and an endless chain stop",() =>
        {
            RecipeDefinition? Loop(uint item)=>Recipe(item,item,1,(item==100 ? 101u:100u,1));
            Check(BaseMaterials.Expand([(100,1)],_=>0,Loop,_=>false).Count==1,"a loop ends on a base item");
            RecipeDefinition? Deep(uint item)=>Recipe(item,item,1,(item+1,1));
            Check(BaseMaterials.Expand([(1000,1)],_=>0,Deep,_=>false).Single().Item==1000+BaseMaterials.MaxDepth,"the depth");
        }),
        ("Base materials: the origin takes the first that applies",() =>
        {
            Check(BaseMaterials.OriginOf(true,true,true)==ItemOrigin.Gathering && BaseMaterials.OriginOf(false,true,true)==ItemOrigin.Vendor,"order");
            Check(BaseMaterials.OriginOf(false,false,true)==ItemOrigin.Monster && BaseMaterials.OriginOf(false,false,false)==ItemOrigin.Other,"rest");
        }),
        ("Vendors: nothing ticked at the start, an offer counts when every currency it costs is ticked",() =>
        {
            IReadOnlyList<uint>[] tomes=[[28u]];IReadOnlyList<uint>[] either=[[28u],[1u]];IReadOnlyList<uint>[] two=[[28u,1u]];IReadOnlyList<uint>[] free=[[]];IReadOnlyList<uint>[] encoded=[[0u]];
            var gil=new HashSet<uint>{1};
            Check(!BaseMaterials.VendorCounts(true,either,new HashSet<uint>()),"nothing ticked: nothing counts");
            Check(new TenshoAssistant.Configuration().ListPanelsCurrenciesOn is null,"none at the start");
            Check(!BaseMaterials.VendorCounts(true,tomes,gil),"another currency");
            Check(BaseMaterials.VendorCounts(true,either,gil),"one offer in gil is enough");
            Check(!BaseMaterials.VendorCounts(true,two,gil),"an offer costing two needs both");
            Check(!BaseMaterials.VendorCounts(false,either,gil),"vendors off");
            Check(!BaseMaterials.VendorCounts(true,free,gil) && !BaseMaterials.VendorCounts(true,encoded,gil),"no cost read, or encoded: it does not count");
        }),
        ("Vendors: a requirement blocks only when it is known not to be met, and the option starts off",() =>
        {
            Check(BaseMaterials.RequirementBlocks(false,null) && BaseMaterials.RequirementBlocks(null,false),"not met");
            Check(!BaseMaterials.RequirementBlocks(true,true) && !BaseMaterials.RequirementBlocks(null,null),"met, or not read");
            Check(!new TenshoAssistant.Configuration().ListPanelsMetOnly,"off by default");
        }),
        ("Vendors: only the currencies of what is really a material",() =>
        {
            (uint Item,IEnumerable<uint> Currencies)[] sales=[(10,[1u]),(11,[28u]),(12,[0u]),(13,[40u,41u])];
            var found=BaseMaterials.MaterialCurrencies(sales,new HashSet<uint>{10,12,13});
            Check(found.Contains(1u) && found.Contains(40u) && found.Contains(41u),"sellers of materials");
            Check(!found.Contains(28u),"a shop with no material");
            Check(!found.Contains(0u),"an encoded currency");
        }),
        ("Vendors: the list leaves out what has no name, names company credits, and searches",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("Where(row=>row.Name.Length>0)"),"no nameless row");
            Check(page.Contains("Task.Run(()=>") && page.Contains("BaseMaterials.MaterialCurrencies("),"worked out off the frame");
            Check(page.Contains("id==NpcCost.CompanyCredits ? credits"),"company credits");
            Check(page.Contains("ListPanelCurrencySearch"),"search");
        }),
        ("Panels: categories fold, each panel on its own",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("###ListPanel{key}{origin}") && page.Contains("ImGuiTreeNodeFlags.DefaultOpen"),"folding header");
            Check(page.Contains("ClassifyCrystal(item) is not null ? ItemOrigin.Crystal"),"crystals apart");
        }),
    ];
}
