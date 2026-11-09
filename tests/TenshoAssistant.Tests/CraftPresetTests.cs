// Test audit of 2026-11-09, batch 2 (0.1021): CraftPresets590Tests merged into CraftPresetTests; every case is kept.
using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Craft;
internal static class CraftPresetTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static RecipeDefinition Recipe(uint id=1,uint type=0)=>new(id,2,1,"Fixture",100,[],type);
    private static void Throws<T>(Action run) where T:Exception{try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static void ThrowsWith<T>(Action run,string message) where T:Exception { try{run();}catch(T){return;} throw new Exception(message); }
    private static RecipeDefinition MenuRecipe(uint id,uint item)=>new(id,item,1,"Menuiserie",100,[],0,new(80,1000,1000,100,100,100,100,0,0,1000));
    private static readonly CraftActionKind[] Sequence=[CraftActionKind.BasicTouch,CraftActionKind.BasicSynthesis];
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
        }),
        // From CraftPresets590Tests: 0.590 (C16): the saved sequences' page.
        ("Sequences: rename, unique for the recipe, capitals allowed",() =>
        {
            IReadOnlyList<CraftPreset> rows=[CraftPresets.Create(MenuRecipe(1,10),"Alpha",Sequence),CraftPresets.Create(MenuRecipe(1,10),"Beta",Sequence),CraftPresets.Create(MenuRecipe(2,20),"Gamma",Sequence)];
            var renamed=CraftPresets.Rename(rows,0,"  Delta ");
            Check(renamed[0].Name=="Delta" && renamed[0].Id==rows[0].Id && renamed[1]==rows[1],"only this one, trimmed, same identity");
            Check(CraftPresets.Rename(rows,0,"ALPHA")[0].Name=="ALPHA","its own capitals");
            Check(CraftPresets.Rename(rows,0,"Gamma")[0].Name=="Gamma","another recipe's name is free");
            ThrowsWith<InvalidOperationException>(()=>CraftPresets.Rename(rows,0,"beta"),"taken on this recipe");
            ThrowsWith<ArgumentException>(()=>CraftPresets.Rename(rows,0,"   "),"empty");
            ThrowsWith<ArgumentOutOfRangeException>(()=>CraftPresets.Rename(rows,9,"Zeta"),"no such row");
        }),
        ("Sequences: duplicate under the next free name, with its own identity",() =>
        {
            IReadOnlyList<CraftPreset> rows=[CraftPresets.Create(MenuRecipe(1,10),"Alpha",Sequence)];
            var twice=CraftPresets.Duplicate(CraftPresets.Duplicate(rows,0),0);
            Check(twice.Count==3 && twice[1].Name=="Alpha (2)" && twice[2].Name=="Alpha (3)","next numbers");
            Check(twice[1].Id!=rows[0].Id && twice[1].Actions.SequenceEqual(Sequence),"another identity, same actions");
            var full=Enumerable.Range(0,CraftPresets.Capacity).Select(index=>CraftPresets.Create(MenuRecipe(1,10),$"N{index}",Sequence)).ToArray();
            ThrowsWith<InvalidOperationException>(()=>CraftPresets.Duplicate(full,0),"a full list refuses");
        }),
        ("Sequences: search on each word in name, item and job; orders",() =>
        {
            IReadOnlyList<CraftPreset> rows=[CraftPresets.Create(MenuRecipe(1,10),"Zed fast",Sequence),null!,CraftPresets.Create(MenuRecipe(2,20),"Able",Sequence),CraftPresets.Create(MenuRecipe(3,30),"Mid",Sequence)];
            string Item(CraftPreset row)=>row.ResultItemId==10 ? "Oak Log":row.ResultItemId==20 ? "Tin Ore":"Ash";
            string Job(CraftPreset row)=>row.ResultItemId==20 ? "Armorer":"Carpenter";
            int[] Show(string? query,CraftPresetSort sort=CraftPresetSort.Saved)=>CraftPresets.View(rows,query,sort,Item,Job);
            Check(Show(null).SequenceEqual([0,2,3,1]),"all, the unreadable one last");
            Check(Show("oak").SequenceEqual([0]),"by item, capitals ignored");
            Check(Show("armorer").SequenceEqual([2]),"by job");
            Check(Show("fast carpenter").SequenceEqual([0]),"each word");
            Check(Show("fast armorer").Length==0,"every word must match");
            Check(Show(null,CraftPresetSort.Name).SequenceEqual([2,3,0,1]),"by name");
            Check(Show(null,CraftPresetSort.Item).SequenceEqual([3,0,2,1]),"by item");
            Check(Show(null,CraftPresetSort.Job).SequenceEqual([2,3,0,1]),"by job, then name");
        }),
        ("Sequences: copied as text, read back by the import",() =>
        {
            string? Name(CraftActionKind kind)=>kind==CraftActionKind.BasicTouch ? "Basic Touch":kind==CraftActionKind.BasicSynthesis ? "Basic Synthesis":null;
            var text=CraftMacroExporter.Text(Sequence,Name);
            Check(text is not null && text.Split('\n').Length==2 && text.StartsWith("/ac \"Basic Touch\" <wait."),"one line per action");
            var back=CraftMacroImport.Parse(text,name=>name=="Basic Touch" ? CraftActionKind.BasicTouch:name=="Basic Synthesis" ? CraftActionKind.BasicSynthesis:null);
            Check(back.Actions.SequenceEqual(Sequence) && back.Unknown.Count==0 && back.Ignored==0,"the same sequence");
            Check(CraftMacroExporter.Text([CraftActionKind.Innovation],Name) is null,"an action without a name: nothing rather than a broken line");
            Check(CraftMacroExporter.Text([CraftActionKind.BasicTouch],_=>"Bad\"name") is null,"a name that breaks the line");
        }),
    ];
}
