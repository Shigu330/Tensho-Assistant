using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;

// 0.696 (user request): « What can I craft? » from the ore, through every step, on one shared stock.
internal static class CraftChained696Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const uint Ore=1,Ingot=2,Rivet=3,Part=4;
    private static RecipeDefinition Recipe(uint id,uint result,int amount,params (uint Item,int Amount)[] ingredients)=>new(id,result,amount,"Job",1,ingredients.Select(row=>new RecipeIngredient(row.Item,row.Amount)).ToArray());
    private static readonly RecipeDefinition IngotRecipe=Recipe(10,Ingot,1,(Ore,2)),RivetRecipe=Recipe(11,Rivet,2,(Ingot,1)),PartRecipe=Recipe(12,Part,1,(Ingot,1),(Rivet,1));
    private static RecipeDefinition? Of(uint item)=>item switch { Ingot=>IngotRecipe,Rivet=>RivetRecipe,Part=>PartRecipe,_=>null };
    private static ItemHoldingsSummary Held(long total)=>new([],total,0,true,false,false,true);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Most profitable: value first, unknown last, the yield and the crafts counted",() =>
        {
            CraftFromStockRow Row(uint id,long count,int yield)=>new(new(id,id,yield,"Job",1,[new(Ore,1)]),count,false);
            var one=Row(1,1,1);var many=Row(2,3,3);var none=Row(3,9,1);
            Check(CraftFromStock.Worth(many,10)==90,"3 crafts of 3 at 10");
            var ranked=CraftFromStock.Ranked([none,one,many],row=>row==none ? null:CraftFromStock.Worth(row,10));
            Check(ranked[0]==many && ranked[1]==one && ranked[2]==none,"most valuable first, unknown last");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftFromStock.cs"));
            Check(page.Contains("missing.Take(32)") && page.Contains("configuration.CommunityUniversalis"),"bounded, under the source switch");
        }),
        ("Chained crafts: from the ore alone, through every step, on one stock",() =>
        {
            // One part: 1 ingot and 1 rivet. Two parts: 2 ingots and one run of 2 rivets, so 3 ingots, 6 ore; three parts would need 10.
            Check(CraftFromStock.ChainedCount(PartRecipe,item=>Held(item==Ore ? 8:0),Of)==2,"8 ore, 2 parts");
            Check(CraftFromStock.ChainedCount(PartRecipe,item=>Held(item==Ore ? 3:0),Of)==0,"3 ore: none");
        }),
        ("Chained crafts: what is already made is used first",() =>
        {
            Check(CraftFromStock.ChainedCount(PartRecipe,item=>Held(item switch { Ingot=>1,Rivet=>1,_=>0 }),Of)==1,"both in stock");
            Check(CraftFromStock.ChainedCount(PartRecipe,item=>Held(item switch { Rivet=>5,Ore=>4,_=>0 }),Of)==2,"rivets held, ingots from the ore");
        }),
        ("Chained crafts: a recipe loop and an endless chain stop",() =>
        {
            var a=Recipe(1,100,1,(101,1));var b=Recipe(2,101,1,(100,1));
            Check(CraftFromStock.ChainedCount(a,_=>Held(0),item=>item==100 ? a:item==101 ? b:null)==0,"a loop");
            RecipeDefinition? Deep(uint item)=>Recipe(item,item,1,(item+1,1));
            Check(CraftFromStock.ChainedCount(Recipe(9,1000,1,(1001,1)),_=>Held(0),Deep)==0,"endless, stopped by the depth");
        }),
        ("Chained crafts: the count is bounded, and a row says it comes from sub-recipes",() =>
        {
            Check(CraftFromStock.ChainedCount(Recipe(9,50,1,(Ore,1)),_=>Held(1000000),Of)==CraftFromStock.MaxChained,"the bound");
            var result=CraftFromStock.Find([PartRecipe],item=>Held(item==Ore ? 8:0),default,Of);
            Check(result.Rows.Single().Chained && result.Rows.Single().Count==2,"chained row");
            Check(CraftFromStock.Find([PartRecipe],item=>Held(item==Ore ? 8:0)).Rows.Count==0,"direct only without it");
        }),
    ];
}
