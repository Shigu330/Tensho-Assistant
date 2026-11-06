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
            Check(lines.Single().Needed==4,"one shared ingot");
        }),
        ("Base materials: a crafted item that can be gathered or bought stays as it is, with its whole need",() =>
        {
            var lines=BaseMaterials.Expand([(Nail,3),(Ore,2)],item=>item==Nail ? 2:0,Of,item=>item==Nail);
            Check(lines.Count==2 && lines[0].Item==Nail && lines[0].Needed==3 && lines[1].Item==Ore && lines[1].Needed==2,"kept, in order");
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
        ("Vendors: off altogether, or by currency: an offer in a currency still on keeps the item",() =>
        {
            IReadOnlyList<uint>[] tomes=[[28u]];IReadOnlyList<uint>[] both=[[28u],[1u]];IReadOnlyList<uint>[] free=[[]];
            var off=new HashSet<uint>{28};
            Check(BaseMaterials.VendorCounts(true,tomes,new HashSet<uint>()),"on");
            Check(!BaseMaterials.VendorCounts(false,tomes,new HashSet<uint>()),"vendors off");
            Check(!BaseMaterials.VendorCounts(true,tomes,off),"its only currency left out");
            Check(BaseMaterials.VendorCounts(true,both,off),"gil still takes it");
            Check(BaseMaterials.VendorCounts(true,free,off),"no cost read: it counts");
        }),
        ("Panels: categories fold, each panel on its own",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("###ListPanel{key}{origin}") && page.Contains("ImGuiTreeNodeFlags.DefaultOpen"),"folding header");
            Check(page.Contains("ClassifyCrystal(item) is not null ? ItemOrigin.Crystal"),"crystals apart");
        }),
    ];
}
