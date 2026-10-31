using TenshoAssistant.Craft;
using TenshoAssistant.Items;

internal static class CraftConditionTests
{
    private static readonly RecipeDefinition Recipe=new(1,2,1,"Menuisier",100,[],0,new(80,10000,10000,100,100,100,100,0,0,0));
    private static readonly CraftStats Stats=new(100,1000,1000,500,true);
    private static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
    private static CraftActionCatalog Catalog()=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,spec.Kind.ToString(),1,"Native"))),"fixture",
        CraftObservedEffects.Specs.Select(spec=>new CraftStatusMetadata(spec.Id,spec.Id.ToString(),1,"Native")));
    private static CraftLiveRaw Raw(CraftActionCatalog catalog,CraftStepCondition condition,CraftLiveTool? tool=null)=>new(1,2,0,2,(byte)((int)condition+1),4,2,8,Stats,500,500,"Objet",condition.ToString(),"",
        new("100","10000","100","10000","80","80","2"),[],[],true,new(100,10000,100,10000,80,80,2,2,(uint)condition,false),
        CraftActionCatalog.Specs.Select(spec=>new CraftNativeAction(spec.Kind,catalog.Find(8,spec.Kind)!.NativeId,0)).ToArray(),true,tool);
    private static CraftProjectionResult Project(CraftLiveRaw raw,CraftActionCatalog catalog)
    {
        var date=DateTimeOffset.UtcNow;var recipes=RecipeCatalog.Build([Recipe],"fixture");
        return CraftLiveProjection.Build(new(true,42,CraftLiveStatusKind.Observed,date,new(raw,recipes),null),catalog,date);
    }
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Ordinary craft condition factors affect quality only with one final floor",()=>
        {
            var state=new CraftSimState(1,0,0,80,500,2,Innovation:4,GreatStrides:3);
            foreach(var (condition,good,expected) in new[]{(CraftStepCondition.Normal,150,405),(CraftStepCondition.Good,150,607),(CraftStepCondition.Good,175,708),(CraftStepCondition.Excellent,150,1620),(CraftStepCondition.Poor,150,202)})
            {
                var step=CraftSimulation.Apply(Recipe.Simulation!,Stats,100,102,135,state,CraftActionKind.DelicateSynthesis,out var error,condition,good)!;
                Check(error is null && step.QualityGain==expected && step.ProgressGain==153 && step.CpCost==32 && step.After.Durability==70,"condition-independent progress CP durability");
            }
        }),
        ("Tool Good bonus classification never treats unknown declaration as ordinary",()=>
        {
            Check(CraftToolBonuses.FromDeclaredMetadata("",0,0)==150,"explicitly empty metadata");
            Check(CraftToolBonuses.FromDeclaredMetadata(CraftToolBonuses.EnhancedDescription,0,0)==175,"exact known native declaration");
            foreach(var value in new[]{CraftToolBonuses.FromDeclaredMetadata(null,0,0),CraftToolBonuses.FromDeclaredMetadata("Some new crafting effect",0,0),
                CraftToolBonuses.FromDeclaredMetadata("",1,0),CraftToolBonuses.FromDeclaredMetadata("",0,1),CraftToolBonuses.FromDeclaredMetadata(CraftToolBonuses.EnhancedDescription,1,0)})
                Check(value is null,"unclassified metadata remains unknown");
        }),
        ("Live Good projection distinguishes ordinary enhanced and unknown tools",()=>
        {
            var catalog=Catalog();
            var ordinary=Project(Raw(catalog,CraftStepCondition.Good,new(10,8,150)),catalog);
            var enhanced=Project(Raw(catalog,CraftStepCondition.Good,new(11,8,175)),catalog);
            Check(ordinary.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.BasicTouch).Quality==new CraftRange(302,302),"100 +floor135*1.5");
            Check(enhanced.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.BasicTouch).Quality==new CraftRange(336,336),"100 +floor135*1.75, not1.5*1.75");
            foreach(var tool in new CraftLiveTool?[]{null,new(12,8,null),new(13,9,175)})
            {
                var unknown=Project(Raw(catalog,CraftStepCondition.Good,tool),catalog);
                Check(unknown.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.BasicTouch).Quality is null,"quality not invented");
                Check(unknown.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.BasicSynthesis).Quality==new CraftRange(100,100) && unknown.Issues.Count>0,"other resources still useful with explicit warning");
            }
        }),
        ("Live Excellent Poor and Normal do not require a Good-only tool assumption",()=>
        {
            var catalog=Catalog();
            foreach(var (condition,quality) in new[]{(CraftStepCondition.Normal,235),(CraftStepCondition.Excellent,640),(CraftStepCondition.Poor,167)})
            {
                var result=Project(Raw(catalog,condition),catalog);
                Check(result.Actions.Single(row=>row.Metadata.Spec.Kind==CraftActionKind.BasicTouch).Quality==new CraftRange(quality,quality),"observed ordinary condition");
            }
            var raw=Raw(catalog,CraftStepCondition.Normal);
            Check(Project(raw with {Condition=2},catalog).Actions.Count==0,"mismatched native conventions");
            Check(Project(raw with {Condition=5,NumbersNative=raw.NumbersNative! with {Condition=4}},catalog).Actions.Count==0,"expert condition excluded");
        }),
        ("Trained Eye max quality and condition-independent results are preserved",()=>
        {
            var recipe=Recipe with {Level=90};var state=new CraftSimState(0,0,0,80,500,0);
            foreach(var condition in Enum.GetValues<CraftStepCondition>())
            {
                var step=CraftSimulation.Apply(recipe.Simulation!,Stats,90,102,135,state,CraftActionKind.TrainedEye,out _,condition,175)!;
                Check(step.QualityGain==10000 && step.After.InnerQuiet==1,"raw maximum unaffected by condition");
            }
            Check(CraftSimulation.Apply(recipe.Simulation!,Stats,90,102,135,state,CraftActionKind.BasicTouch,out var error,(CraftStepCondition)99) is null && error is not null,"invalid condition rejected");
        })
    };
}
