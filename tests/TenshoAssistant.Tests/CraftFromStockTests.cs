// Test audit of 2026-11-09, batch 2 (0.1021): CraftFromStock592Tests merged into CraftFromStockTests; every case is kept.
using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;
internal static class CraftFromStockTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static RecipeDefinition Recipe(uint id,string job,int level,params (uint Item,int Amount)[] ingredients)
        =>new(id,id*10,1,job,level,ingredients.Select(row=>new RecipeIngredient(row.Item,row.Amount)).ToArray());
    private static ItemHoldingsSummary Stock(long total,bool unknown=false,bool observed=true)
        =>new([],total,0,observed,unknown,false,!unknown);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From CraftFromStock592Tests: 0.592 (C21): what the stocks can make now.
        ("From stock: the lowest ingredient decides; nothing to make is not listed; highest level first",() =>
        {
            var stocks=new Dictionary<uint,ItemHoldingsSummary>{[1]=Stock(10),[2]=Stock(3),[3]=Stock(0)};
            var found=CraftFromStock.Find([Recipe(1,"Carpenter",10,(1,2)),Recipe(2,"Carpenter",50,(1,2),(2,1)),Recipe(3,"Armorer",90,(1,1),(3,1)),Recipe(4,"Armorer",90)],id=>stocks[id]);
            Check(found.Rows.Select(row=>row.Recipe.RecipeId).SequenceEqual<uint>([2,1]),"level 50, then level 10; none from an empty ingredient, none without ingredients");
            Check(found.Rows[0].Count==3 && found.Rows[1].Count==5,"10/2 and min(5,3)");
            Check(found.Undecided==0 && !found.Rows[0].LowerBound,"all seen");
        }),
        ("From stock: a storage not seen makes a lower bound, a stock never seen is not listed",() =>
        {
            var stocks=new Dictionary<uint,ItemHoldingsSummary>{[1]=Stock(4,unknown:true),[2]=Stock(0,observed:false),[3]=Stock(0)};
            var found=CraftFromStock.Find([Recipe(1,"Carpenter",10,(1,1)),Recipe(2,"Carpenter",10,(1,1),(2,1)),Recipe(3,"Carpenter",10,(2,1),(3,1))],id=>stocks[id]);
            Check(found.Rows.Count==1 && found.Rows[0] is {Count:4,LowerBound:true},"at least 4");
            Check(found.Undecided==1,"the unseen one is counted, not shown as zero; one already ruled out by an empty ingredient is not");
        }),
        ("From stock: a stock is read once, and the search can be stopped",() =>
        {
            var reads=0;
            CraftFromStock.Find([Recipe(1,"Carpenter",10,(1,1)),Recipe(2,"Carpenter",10,(1,1))],_=>{reads++;return Stock(1);});
            Check(reads==1,"once per item");
            var cancel=new CancellationTokenSource();cancel.Cancel();
            try{CraftFromStock.Find([Recipe(1,"Carpenter",10,(1,1))],_=>Stock(1),cancel.Token);throw new Exception("not stopped");}
            catch(OperationCanceledException){}
        }),
        ("From stock: filtered by job and level",() =>
        {
            CraftFromStockRow[] rows=[new(Recipe(1,"Carpenter",10,(1,1)),1,false),new(Recipe(2,"Armorer",50,(1,1)),1,false),new(Recipe(3,"Carpenter",90,(1,1)),1,false)];
            Check(CraftFromStock.Filter(rows,"",1,100).Length==3,"all");
            Check(CraftFromStock.Filter(rows,"Carpenter",1,100).Length==2,"a job");
            Check(CraftFromStock.Filter(rows,"Carpenter",50,100) is [{Recipe.RecipeId:3}],"and a level");
        }),
        ("From stock: off the frame, on a click",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftFromStock.cs"));
            Check(view.Contains("fromStockTask=Task.Run(") && view.Contains("if(ImGui.Button(localization.Get(\"CraftStock.Search\")))StartCraftFromStock(catalog);"),"worked out on a worker, on a click");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Lifetime.cs")).Contains("fromStockCancel"),"stopped when the window closes");
        }),
    ];
}
