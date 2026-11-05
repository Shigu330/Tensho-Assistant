using TenshoAssistant.Craft;

// 0.590 (C16): the saved sequences' page.
internal static class CraftPresets590Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action run,string message) where T:Exception { try{run();}catch(T){return;} throw new Exception(message); }
    private static RecipeDefinition Recipe(uint id,uint item)=>new(id,item,1,"Menuiserie",100,[],0,new(80,1000,1000,100,100,100,100,0,0,1000));
    private static readonly CraftActionKind[] Sequence=[CraftActionKind.BasicTouch,CraftActionKind.BasicSynthesis];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sequences: rename, unique for the recipe, capitals allowed",() =>
        {
            IReadOnlyList<CraftPreset> rows=[CraftPresets.Create(Recipe(1,10),"Alpha",Sequence),CraftPresets.Create(Recipe(1,10),"Beta",Sequence),CraftPresets.Create(Recipe(2,20),"Gamma",Sequence)];
            var renamed=CraftPresets.Rename(rows,0,"  Delta ");
            Check(renamed[0].Name=="Delta" && renamed[0].Id==rows[0].Id && renamed[1]==rows[1],"only this one, trimmed, same identity");
            Check(CraftPresets.Rename(rows,0,"ALPHA")[0].Name=="ALPHA","its own capitals");
            Check(CraftPresets.Rename(rows,0,"Gamma")[0].Name=="Gamma","another recipe's name is free");
            Throws<InvalidOperationException>(()=>CraftPresets.Rename(rows,0,"beta"),"taken on this recipe");
            Throws<ArgumentException>(()=>CraftPresets.Rename(rows,0,"   "),"empty");
            Throws<ArgumentOutOfRangeException>(()=>CraftPresets.Rename(rows,9,"Zeta"),"no such row");
        }),
        ("Sequences: duplicate under the next free name, with its own identity",() =>
        {
            IReadOnlyList<CraftPreset> rows=[CraftPresets.Create(Recipe(1,10),"Alpha",Sequence)];
            var twice=CraftPresets.Duplicate(CraftPresets.Duplicate(rows,0),0);
            Check(twice.Count==3 && twice[1].Name=="Alpha (2)" && twice[2].Name=="Alpha (3)","next numbers");
            Check(twice[1].Id!=rows[0].Id && twice[1].Actions.SequenceEqual(Sequence),"another identity, same actions");
            var full=Enumerable.Range(0,CraftPresets.Capacity).Select(index=>CraftPresets.Create(Recipe(1,10),$"N{index}",Sequence)).ToArray();
            Throws<InvalidOperationException>(()=>CraftPresets.Duplicate(full,0),"a full list refuses");
        }),
        ("Sequences: search on each word in name, item and job; orders",() =>
        {
            IReadOnlyList<CraftPreset> rows=[CraftPresets.Create(Recipe(1,10),"Zed fast",Sequence),null!,CraftPresets.Create(Recipe(2,20),"Able",Sequence),CraftPresets.Create(Recipe(3,30),"Mid",Sequence)];
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
