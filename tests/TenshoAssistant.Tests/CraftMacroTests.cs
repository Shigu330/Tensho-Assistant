using TenshoAssistant.Craft;

internal static class CraftMacroTests
{
    private static CraftActionCatalog Catalog(string name="Travail de base")=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,name+job,1,"Native"))),"fixture");
    private static CraftSimulationResult Completed(int count)
    {
        var recipe=new RecipeDefinition(1,2,1,"Test",100,[],0,new(1000,100,10000,100,100,100,100,0,0,0));
        var sequence=Enumerable.Repeat(CraftActionKind.Observe,count-1).Append(CraftActionKind.BasicSynthesis);
        return CraftSimulation.Run(recipe,new(100,1000,1000,10000),0,sequence);
    }
    private static void Check(bool condition,string why) {if(!condition)throw new Exception(why);}
    private static void Throws<T>(Action run) where T:Exception {try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Craft macro split preserves all actions and fifteen line boundary",()=>
        {
            foreach(var count in new[]{1,14,15,28,29,100})
            foreach(var echo in new[]{false,true})
            {
                var export=CraftMacroExporter.Build(Completed(count),8,Catalog(),0,echo);
                var actual=export.Pages.SelectMany(page=>page.Lines).ToArray();
                Check(export.Pages.All(page=>page.Lines.Count<=15) && actual.Count(line=>line.StartsWith("/ac "))==count,"all actions retained");
                Check(export.Pages.Count==(count+(echo?13:14))/(echo?14:15),"minimum blocks");
                Check(actual.Count(line=>line.StartsWith("/echo "))==(echo?export.Pages.Count:0),"echo counted in capacity");
                Check(export.Pages[0].Text.Split('\n').Length==export.Pages[0].Lines.Count,"one native command per line");
            }
        }),
        ("Craft macro localized names jobs order waits and explicit completion prefix",()=>
        {
            var recipe=new RecipeDefinition(1,2,1,"Test",100,[],0,new(100,100,10000,100,100,100,100,0,0,0));
            var result=CraftSimulation.Run(recipe,new(100,1000,1000,1000),0,[CraftActionKind.Veneration,CraftActionKind.Observe,CraftActionKind.BasicSynthesis,CraftActionKind.Reflect]);
            var export=CraftMacroExporter.Build(result,15,Catalog("Élan d'artisan "),1,false);
            Check(export.Pages[0].Lines.SequenceEqual(new[]{"/ac \"Élan d'artisan 15\" <wait.3>","/ac \"Élan d'artisan 15\" <wait.4>","/ac \"Élan d'artisan 15\" <wait.4>"}),"localized quotes and buff/action wait order");
            Check(export.WaitSeconds==11 && export.Actions==3 && export.OmittedActions==1,"only successfully simulated prefix");
            Check(CraftMacroExporter.BaseWait(CraftActionKind.MastersMend)==3 && CraftMacroExporter.BaseWait(CraftActionKind.Manipulation)==2,"not all durability actions have buff timing");
        }),
        ("Craft macro rejects incomplete failed invalid and injected names",()=>
        {
            var result=Completed(1);
            Throws<ArgumentException>(()=>CraftMacroExporter.Build(result with {Error="bad"},8,Catalog()));
            Throws<ArgumentException>(()=>CraftMacroExporter.Build(result with {Final=result.Final with {Finish=CraftFinish.Broken}},8,Catalog()));
            Throws<ArgumentException>(()=>CraftMacroExporter.Build(result with {Final=result.Final with {Finish=CraftFinish.InProgress}},8,Catalog()));
            foreach(var name in new[]{"Bad\n/echo injected","Bad\" <wait.1>","Bad\u2028line",new string('x',129)})
                Throws<ArgumentException>(()=>CraftMacroExporter.Build(result,8,Catalog(name)));
            Throws<ArgumentOutOfRangeException>(()=>CraftMacroExporter.Build(result,8,Catalog(),4));
            Throws<ArgumentOutOfRangeException>(()=>CraftMacroExporter.Build(result,0,Catalog()));
        }),
        ("Craft macro output is immutable and culture independent",()=>
        {
            var previous=System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("ar-SA");
                var export=CraftMacroExporter.Build(Completed(1),8,Catalog(),2,false);
                Check(export.Pages[0].Text.EndsWith("<wait.5>"),"invariant ASCII wait");
                Throws<NotSupportedException>(()=>((IList<string>)export.Pages[0].Lines).Clear());
                Throws<NotSupportedException>(()=>((IList<CraftMacroPage>)export.Pages).Clear());
            }
            finally {System.Globalization.CultureInfo.CurrentCulture=previous;}
        })
    };
}
