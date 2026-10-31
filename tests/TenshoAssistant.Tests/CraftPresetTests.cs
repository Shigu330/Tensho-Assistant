using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Craft;

internal static class CraftPresetTests
{
    private static RecipeDefinition Recipe(uint id=1,uint type=0)=>new(id,2,1,"Fixture",100,[],type);
    private static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
    private static void Throws<T>(Action run) where T:Exception{try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Craft presets copy actions roundtrip configuration and resolve exact recipe variant",()=>
        {
            var actions=new List<CraftActionKind>{CraftActionKind.BasicTouch,CraftActionKind.BasicSynthesis};var preset=CraftPresets.Create(Recipe()," Test ",actions);actions.Clear();
            var config=new Configuration{CraftPresets=CraftPresets.Add([],preset).ToList()};var copy=JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;
            Check(copy.CraftPresets![0].Name=="Test" && copy.CraftPresets[0].Actions.Count==2 && copy.Version==1,"json data and config version preserved");
            var catalog=RecipeCatalog.Build([Recipe(),Recipe(9,1)],"fixture");
            Check(CraftPresets.Resolve(copy.CraftPresets[0],catalog)?.RecipeId==1,"exact recipe not first result variant guess");
            Check(CraftPresets.Resolve(preset with {RecipeId=99},catalog) is null && CraftPresets.Resolve(preset with {CraftTypeId=1},catalog) is null,"unresolved identities remain unavailable");
            Throws<NotSupportedException>(()=>((IList<CraftActionKind>)preset.Actions).Clear());
        }),
        ("Craft preset bounds unknown actions duplicates and removal preserve original input",()=>
        {
            var preset=CraftPresets.Create(Recipe(),"Test",[CraftActionKind.BasicSynthesis]);var rows=CraftPresets.Add([],preset);
            Throws<InvalidOperationException>(()=>CraftPresets.Add(rows,preset with {Id=Guid.NewGuid(),Name="test"}));
            Throws<ArgumentException>(()=>CraftPresets.Create(Recipe(),"",[CraftActionKind.BasicSynthesis]));
            Throws<ArgumentException>(()=>CraftPresets.Create(Recipe(),"Test",[(CraftActionKind)999]));
            Throws<ArgumentException>(()=>CraftPresets.Create(Recipe(),"Test",Enumerable.Repeat(CraftActionKind.Observe,101)));
            Throws<InvalidOperationException>(()=>CraftPresets.Add(Enumerable.Repeat(preset,100).ToArray(),preset with {Id=Guid.NewGuid(),Name="Other"}));
            Check(CraftPresets.Remove(rows,0).Count==0 && rows.Count==1,"pure removal");
            Check(!CraftPresets.Valid(null) && !CraftPresets.Valid(preset with {Actions=null!}),"invalid loaded rows not silently trusted");
        }),
        ("Craft preset persistence rolls back failed add and delete without dropping unresolved rows",()=>
        {
            var preset=CraftPresets.Create(Recipe(),"Test",[CraftActionKind.BasicSynthesis]);var config=new Configuration();var previous=config.CraftPresets;
            Throws<IOException>(()=>CraftPresets.Commit(config,()=>throw new IOException("disk full"),rows=>CraftPresets.Add(rows,preset)));
            Check(ReferenceEquals(config.CraftPresets,previous) && config.CraftPresets!.Count==0,"failed save rollback");
            CraftPresets.Commit(config,()=>{},rows=>CraftPresets.Add(rows,preset));previous=config.CraftPresets;
            Throws<IOException>(()=>CraftPresets.Commit(config,()=>throw new IOException(),rows=>CraftPresets.Remove(rows,0)));Check(ReferenceEquals(config.CraftPresets,previous),"failed delete rollback");
            config.CraftPresets=[null!,preset with {Id=Guid.NewGuid(),RecipeId=999}];CraftPresets.Commit(config,()=>{},rows=>CraftPresets.Add(rows,preset));Check(config.CraftPresets.Count==3 && config.CraftPresets[0] is null,"unresolved and corrupt entries retained");
        })
    };
}
