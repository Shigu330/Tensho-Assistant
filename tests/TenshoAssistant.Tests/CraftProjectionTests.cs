using TenshoAssistant.Craft;

internal static class CraftProjectionTests
{
    private static readonly RecipeDefinition Recipe=new(1,2,1,"Menuisier",100,[],0,new(80,1000,1000,100,100,100,100,0,0,0));
    private static readonly RecipeCatalog Recipes=RecipeCatalog.Build([Recipe],"fixture");
    private static CraftActionCatalog Catalog()=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,spec.Kind.ToString(),1,"Native description"))),"fixture",
        CraftObservedEffects.Specs.Select(spec=>new CraftStatusMetadata(spec.Id,spec.Id.ToString(),1,"Native status")));
    private static CraftLiveRaw Raw(CraftActionCatalog catalog)=>new(1,2,0,2,1,4,2,8,new(100,1000,1000,100),100,100,"Objet","Normale","",
        new("100","1000","100","1000","80","80","2"),[],[],true,new(100,1000,100,1000,80,80,2,2,0,false),
        CraftActionCatalog.Specs.Select(spec=>new CraftNativeAction(spec.Kind,catalog.Find(8,spec.Kind)!.NativeId,0)).ToArray(),true);
    private static CraftProjectionResult Build(CraftLiveRaw raw,CraftActionCatalog catalog,DateTimeOffset? date=null,RecipeCatalog? recipes=null)
    {
        var now=DateTimeOffset.UtcNow;return CraftLiveProjection.Build(new(true,42,CraftLiveStatusKind.Observed,date ?? now,new(raw,recipes ?? Recipes),null),catalog,now);
    }
    private static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Live projection preserves uncertain combo costs and refined stack alternatives",()=>
        {
            var catalog=Catalog();var result=Build(Raw(catalog),catalog);
            var standard=result.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.StandardTouch);
            var advanced=result.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.AdvancedTouch);
            var refined=result.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.RefinedTouch);
            Check(standard.CpCost==new CraftRange(18,32) && advanced.CpCost==new CraftRange(18,46),"no guessed combo");
            Check(refined.InnerQuiet==new CraftRange(1,2) && standard.Quality==new CraftRange(268,268),"quality and stacks separately projected");
        }),
        ("Live projection native availability excludes locked actions and narrows incompatible alternatives",()=>
        {
            var catalog=Catalog();var raw=Raw(catalog);var native=raw.ActionsNative!.Select(action=>action.Kind==CraftActionKind.Manipulation ? action with {Status=123}:action).ToArray();
            var result=Build(raw with {Cp=20,ActionsNative=native},catalog);
            Check(!result.Actions.Any(row=>row.Metadata.Spec.Kind==CraftActionKind.Manipulation),"nonzero native status never presented as usable");
            Check(result.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.AdvancedTouch).CpCost==new CraftRange(18,18),"available action and current CP rule out unaffordable combo");
            Check(result.Actions.Any(row=>row.Metadata.Spec.Kind==CraftActionKind.TrainedPerfection),"native availability permits unused history alternative without claiming it observed");
        }),
        ("Live projection stale animated nonnormal or mismatched observations produce no actions",()=>
        {
            var catalog=Catalog();var raw=Raw(catalog);
            foreach(var bad in new[]{raw with {Condition=2},raw with {Flags=1},raw with {NumbersNative=null},raw with {ActionsComplete=false},
                raw with {NumbersNative=raw.NumbersNative! with {Condition=1}},raw with {NumbersNative=raw.NumbersNative! with {MaxProgress=999}},raw with {Numbers=raw.Numbers with {Progress="?"}},raw with {StatusesComplete=false}})
                Check(Build(bad,catalog).Actions.Count==0,"gate refuses incomplete state");
            Check(Build(raw,catalog,DateTimeOffset.UtcNow.AddSeconds(-10)).Actions.Count==0,"stale rejected");
            Check(Build(raw,catalog,DateTimeOffset.UtcNow.AddMinutes(1)).Actions.Count==0,"future timestamp rejected");
            var now=DateTimeOffset.UtcNow;
            Check(CraftLiveProjection.Build(new(true,0,CraftLiveStatusKind.Observed,now,new(raw,Recipes),null),catalog,now).Actions.Count==0,"zero owner rejected");
        }),
        ("Live projection known effects reject unmodeled status and preserve protection",()=>
        {
            var catalog=Catalog();var raw=Raw(catalog);
            Check(Build(raw with {Statuses=[new(2190,3,0)]},catalog).Actions.Count==0,"unmodeled FinalAppraisal cannot alter completion silently");
            var protectedResult=Build(raw with {Statuses=[new(3813,0,0)],Numbers=raw.Numbers with {Durability="10"},NumbersNative=raw.NumbersNative! with {Durability=10}},catalog);
            var groundwork=protectedResult.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.Groundwork);
            Check(groundwork.Durability==new CraftRange(10,10) && groundwork.Progress==new CraftRange(467,467),"active protection affects full Groundwork efficiency");
            Check(!protectedResult.Actions.Any(row=>row.Metadata.Spec.Kind==CraftActionKind.TrainedPerfection),"no reuse with known used history");
        }),
        ("Live projection completion takes precedence over zero durability and reports broken actions",()=>
        {
            var catalog=Catalog();var raw=Raw(catalog);
            var result=Build(raw with {Numbers=raw.Numbers with {Progress="900",Durability="10"},NumbersNative=raw.NumbersNative! with {Progress=900,Durability=10}},catalog);
            var synth=result.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.BasicSynthesis);
            var touch=result.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.BasicTouch);
            Check(synth.Completed && !synth.Broken && synth.Durability==new CraftRange(0,0),"finish atzero");Check(touch.Broken && !touch.Completed,"failure exposed");
        }),
        ("Live projection recipe and metadata identity never use first result or foreign action id",()=>
        {
            var catalog=Catalog();var raw=Raw(catalog);
            Check(Build(raw with {RecipeId=99},catalog).Actions.Count==0,"unresolved recipe");
            var foreign=raw.ActionsNative!.Select((row,index)=>index==0 ? row with {NativeId=999}:row).ToArray();
            Check(Build(raw with {ActionsNative=foreign},catalog).Actions.Count==0,"native action variant mismatch");
            var duplicates=raw.ActionsNative!.ToArray();duplicates[0]=duplicates[1];Check(Build(raw with {ActionsNative=duplicates},catalog).Actions.Count==0,"duplicated action rows");
        }),
        ("Live projection effect gains match independent one-step arithmetic and outputs immutable",()=>
        {
            var catalog=Catalog();var raw=Raw(catalog);
            var result=Build(raw with {Statuses=[new(251,2,0),new(2189,3,0),new(254,2,0)]},catalog);
            var row=result.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.BasicTouch);
            Check(row.Quality==new CraftRange(505,505) && row.CpCost==new CraftRange(18,18) && row.InnerQuiet==new CraftRange(3,3),"100 +floor(135*1.2*2.5)");
            try{((IList<CraftActionProjection>)result.Actions).Clear();throw new Exception("mutable");}catch(NotSupportedException){}
        })
    };
}
