// Test audit of 2026-11-09, batch 2 (0.1021): CraftMacroImport425Tests merged into CraftMacroTests; every case is kept.
using TenshoAssistant.Craft;
using TenshoAssistant.UI;
internal static class CraftMacroTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static CraftActionCatalog Catalog(string name="Travail de base")=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,name+job,1,"Native"))),"fixture");
    private static CraftSimulationResult Completed(int count)
    {
        var recipe=new RecipeDefinition(1,2,1,"Test",100,[],0,new(1000,100,10000,100,100,100,100,0,0,0));
        var sequence=Enumerable.Repeat(CraftActionKind.Observe,count-1).Append(CraftActionKind.BasicSynthesis);
        return CraftSimulation.Run(recipe,new(100,1000,1000,10000),0,sequence);
    }
    private static void Throws<T>(Action run) where T:Exception {try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
        }),
        // From CraftMacroImport425Tests: A-08 (0.425): a pasted crafting macro read back into simulator actions.
        ("Macro import: /ac lines with or without quotes and waits; other commands ignored; unknown names listed",() =>
        {
            var names=new Dictionary<string,CraftActionKind>(StringComparer.OrdinalIgnoreCase)
                {["Mémoire musculaire"]=CraftActionKind.MuscleMemory,["Manipulation"]=CraftActionKind.Manipulation,["Ouvrage de base"]=CraftActionKind.BasicSynthesis};
            CraftActionKind? Find(string name)=>names.TryGetValue(name,out var kind) ? kind:null;
            var text="/mlock\n/ac \"Mémoire musculaire\" <wait.3>\r\n/ac Manipulation <wait.2>\n  /action \"ouvrage de base\"\n/ac \"Inconnue\" <wait.3>\n/echo fini <se.1>\n/nextmacro\n";
            var result=CraftMacroImport.Parse(text,Find);
            Check(result.Actions.SequenceEqual([CraftActionKind.MuscleMemory,CraftActionKind.Manipulation,CraftActionKind.BasicSynthesis]),"actions in order, case ignored");
            Check(result.Unknown.SequenceEqual(["Inconnue"]) && result.Ignored==3,"unknown listed, other commands counted");
            Check(CraftMacroImport.Parse(new string('a',CraftMacroImport.MaxText+1),Find).Actions.Count==0 && CraftMacroImport.Parse(null,Find).Ignored==0,"bounded");
            var many=string.Join('\n',Enumerable.Repeat("/ac Manipulation",150));
            Check(CraftMacroImport.Parse(many,Find).Actions.Count==CraftMacroImport.MaxActions,"at most 100 actions");
        }),
        ("0.573: the macro's end line has its sound and can stay on the last page; the panel's cache key follows",() =>
        {
            var export=Read("Craft/CraftMacroExport.cs");
            Check(export.Contains("if(echo && (!echoLastOnly || index==chunks.Length-1))") && export.Contains("echoSound is >=1 and <=16"),"rule");
            Check(Read("Craft/CraftLogSession.cs").Contains("input.Echo,input.EchoSound,input.EchoLastOnly,"),"in the cache key");
            var settings=new TenshoAssistant.CraftSearchSettings();
            Check(settings.BoundedEchoSound==1 && !settings.EchoLastOnly,"as before until set");
            settings.EchoSound=99;Check(settings.BoundedEchoSound==16,"bounded");
        }),
    ];
}
