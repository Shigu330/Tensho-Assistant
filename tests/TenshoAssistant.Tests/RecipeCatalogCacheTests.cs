using TenshoAssistant.Caching;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

internal static class RecipeCatalogCacheTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static T Wait<T>(Task<T> task)=>task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
    private const string Game="ffxiv=2026.10.01;ex1=2026.10.01";
    private static RecipeDefinition[] Recipes()=>
    [
        new(1,42,1,"Forgeron",50,[new(5,2),new(6,1)],1,new CraftRecipeParameters(80,3000,7000,130,115,80,70,2000,1900,0,Expert:true,RequiredQuality:4000)),
        new(2,42,3,"Armurier",40,[new(5,1)],2),
        new(3,43,1,"Menuisier",10,[new(7,4)]),
    ];
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Recipe cache roundtrips nested ingredients and simulation parameters and rebuilds both indexes",()=>
        {
            var root=Path.Combine(Path.GetTempPath(),"Tensho-RecipeCacheTests-"+Guid.NewGuid().ToString("N"));
            try
            {
                using(var cache=new DataCache(root))
                    Check(Wait(cache.WriteAsync(RecipeCatalogCache.Family,"French",RecipeCatalogCache.Version(Game),RecipeCatalogCache.Snapshot(Game,Recipes()),DateTimeOffset.UtcNow)),"written");
                using var reloaded=new DataCache(root);
                var value=Wait(reloaded.ReadAsync<CatalogCacheData<RecipeDefinition>>(RecipeCatalogCache.Family,"French",RecipeCatalogCache.Version(Game)))?.Value;
                Check(RecipeCatalogCache.Valid(value,Game),"restored snapshot valid");
                var catalog=RecipeCatalog.Build(value!.Rows,Game);
                var first=catalog.ForResult(42).Single(recipe=>recipe.RecipeId==1);
                Check(catalog.Count==3 && catalog.ForResult(42).Count==2 && catalog.ForIngredient(5).Count==2 && catalog.ForIngredient(7).Single().RecipeId==3,"indexes rebuilt");
                Check(first.Ingredients.SequenceEqual(Recipes()[0].Ingredients) && first.Simulation==Recipes()[0].Simulation && first.CraftTypeId==1 && catalog.ForResult(43)[0].CraftTypeId==uint.MaxValue,"nested data and defaults kept");
                Check(Wait(reloaded.ReadAsync<CatalogCacheData<RecipeDefinition>>(RecipeCatalogCache.Family,"English",RecipeCatalogCache.Version(Game))) is null,"language scopes the file");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }),
        ("Recipe cache key follows client version and shape and corrupt rows are refused",()=>
        {
            Check(RecipeCatalogCache.Version(Game)!=RecipeCatalogCache.Version("ffxiv=2026.11.15") && RecipeCatalogCache.Version(Game).Contains("Ingredients:IReadOnlyList`1<RecipeIngredient>",StringComparison.Ordinal)
                && RecipeCatalogCache.Shape.Contains("CraftRecipeParameters(",StringComparison.Ordinal),"key carries game version and nested shape");
            var good=RecipeCatalogCache.Snapshot(Game,Recipes());
            Check(RecipeCatalogCache.Valid(good,Game) && !RecipeCatalogCache.Valid(good,"ffxiv=other") && !RecipeCatalogCache.Valid(null,Game),"identity rechecked");
            var row=Recipes()[1];
            foreach(var bad in new[]{good with{Rows=[]},good with{Rows=null!},good with{Rows=[row,row]},good with{Rows=[row with{RecipeId=0}]},good with{Rows=[row with{ResultItemId=0}]},
                good with{Rows=[row with{ResultAmount=0}]},good with{Rows=[row with{Ingredients=null!}]},good with{Rows=[row with{Ingredients=[new(0,1)]}]},
                good with{Rows=[row with{Ingredients=[new(5,0)]}]},good with{Rows=[row with{CraftType=null!}]},good with{Rows=[null!]}})
                Check(!RecipeCatalogCache.Valid(bad,Game),"invalid recipe snapshot refused");
            var source=Recipes();var ingredients=new List<RecipeIngredient>{new(9,1)};source[2]=source[2] with{Ingredients=ingredients};
            var copy=RecipeCatalogCache.Snapshot(Game,source);ingredients.Add(new(10,1));source[0]=source[1];
            Check(copy.Rows[0].RecipeId==1 && copy.Rows[2].Ingredients.Count==1,"snapshot owns rows and ingredient lists");
        }),
        ("Shared catalogue keys keep the validated item cache key unchanged",()=>
        {
            Check(ItemCatalogCache.Shape.StartsWith("ItemDefinition(",StringComparison.Ordinal) && ItemCatalogCache.Shape.Contains("/EquipmentSlots(",StringComparison.Ordinal)
                && !ItemCatalogCache.Shape.Contains('<'),"item shape has no generic suffix so caches written by 0.81 stay valid");
            Check(CatalogCacheKeys.Version("v",1,"s")=="v|copy=1|s","version layout");
        }),
    };
}
