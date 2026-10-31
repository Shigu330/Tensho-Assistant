using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using GameAction=Lumina.Excel.Sheets.Action;
using TenshoAssistant.Craft;

if(args.Length!=1) throw new ArgumentException("Supply the installed game's sqpack directory.");
using var data=new GameData(args[0],new LuminaOptions {DefaultExcelLanguage=Language.French});
var actions=data.Excel.GetSheet<GameAction>(); var descriptions=data.Excel.GetSheet<ActionTransient>(); var crafts=data.Excel.GetSheet<CraftAction>();
var metadata=new List<CraftActionMetadata>();
foreach(var spec in CraftActionCatalog.Specs)
{
    if(spec.CanonicalId>=100000)
    {
        var canonical=crafts.GetRow(spec.CanonicalId);
        for(uint job=8;job<=15;job++)
        {
            var id=job switch {8=>canonical.CRP.RowId,9=>canonical.BSM.RowId,10=>canonical.ARM.RowId,11=>canonical.GSM.RowId,
                12=>canonical.LTW.RowId,13=>canonical.WVR.RowId,14=>canonical.ALC.RowId,_=>canonical.CUL.RowId};
            var row=crafts.GetRow(id);
            if(row.ClassJob.RowId!=job || row.ClassJobLevel!=spec.Level || row.Cost!=spec.Cp) throw new Exception($"Mismatch {id}");
            metadata.Add(new(spec,job,id,row.Name.ExtractText(),row.Icon,row.Description.ExtractText()));
        }
    }
    else
    {
        var row=actions.GetRow(spec.CanonicalId);
        if(row.ClassJobLevel!=spec.Level || row.PrimaryCostType!=8 || row.PrimaryCostValue!=spec.Cp || row.SecondaryCostType!=0 || row.SecondaryCostValue.RowId!=0) throw new Exception($"Mismatch {row.RowId}");
        for(uint job=8;job<=15;job++) metadata.Add(new(spec,job,row.RowId,row.Name.ExtractText(),row.Icon,descriptions.GetRow(row.RowId).Description.ExtractText()));
    }
}
var statusSheet=data.Excel.GetSheet<Lumina.Excel.Sheets.Status>();
var statusMetadata=CraftObservedEffects.Specs.Select(spec=>
{var row=statusSheet.GetRow(spec.Id);return new CraftStatusMetadata(row.RowId,row.Name.ExtractText(),row.Icon,row.Description.ExtractText(),row.MaxStacks);}).ToArray();
var catalog=new CraftActionCatalog(metadata,"installed-game",statusMetadata);
foreach(var row in statusMetadata)
{
    if(string.IsNullOrWhiteSpace(row.Description) || !data.FileExists($"ui/icon/{row.Icon/1000*1000:D6}/{row.Icon:D6}.tex"))throw new Exception($"Missing native status metadata {row.Id}");
    Console.WriteLine($"STATUS {row.Id} {row.Name} icon={row.Icon} stacks={row.MaxStacks}");
}
Console.WriteLine($"PASS {statusMetadata.Length} native effect ids, names, descriptions and icons");
foreach(var action in metadata)
    if(action.Icon==0 || !data.FileExists($"ui/icon/{action.Icon/1000*1000:D6}/{action.Icon:D6}.tex")) throw new Exception($"Missing icon {action.Icon}");
Console.WriteLine($"PASS {metadata.Count} native job actions, levels, CP, descriptions and icons");
foreach(var kind in new[]{CraftActionKind.Byregot,CraftActionKind.GreatStrides})
{
    var action=catalog.Find(15,kind)!;
    Console.WriteLine($"ACTION {kind} native={action.NativeId} name={action.Name} icon={action.Icon} description={action.Description}");
}
var recipes=data.Excel.GetSheet<Recipe>(); var levels=data.Excel.GetSheet<RecipeLevelTable>();
var supported=0; var excluded=0; var collectibleFixtures=0; RecipeDefinition? sample=null; RecipeDefinition? advancedSample=null;
foreach(var row in recipes)
{
    if(row.ItemResult.RowId==0) continue;
    var table=levels.GetRow(row.RecipeLevelTable.RowId);
    var p=RecipeSimulationCopy.Copy(row,table);
    var recipe=new RecipeDefinition(row.RowId,row.ItemResult.RowId,row.AmountResult,"native",table.ClassJobLevel,[],row.CraftType.RowId,p);
    if(row.ItemResult.Value.Name.ExtractText().Contains("Tacos de carne asada collectionnables",StringComparison.OrdinalIgnoreCase))
    {
        collectibleFixtures++;
        Console.WriteLine($"COLLECTABLE recipe={row.RowId} item={row.ItemResult.RowId} level={table.ClassJobLevel} HQ={row.CanHq} collectable={p.Collectable} progress={p.Progress} quality={p.Quality} durability={p.Durability} requiredQuality={p.RequiredQuality} unsupported={CraftSimulation.Unsupported(recipe) ?? "none"}");
        if(!p.Collectable || p.Progress!=6600 || p.Quality!=12000 || p.Durability!=80 || CraftSimulation.Unsupported(recipe) is not null)throw new Exception("Tacos collectible native fixture incompatible.");
        var solved=CraftSolver.Solve(recipe,new(100,5622,5081,565),0,new(p.Quality,40,128,100000));
        Console.WriteLine($"COLLECTABLE_SEARCH completed={solved.Simulation?.Final.Finish} quality={solved.Simulation?.Final.Quality} target={solved.TargetReached} transitions={solved.Expanded}");
        if(solved.Simulation is not {Final.Finish:CraftFinish.Completed})throw new Exception("No completed collectible sequence in fixture search.");
        var exported=CraftMacroExporter.Build(solved.Simulation,15,catalog);
        Console.WriteLine($"COLLECTABLE_MACRO actions={exported.Actions} blocks={exported.Pages.Count} quality={solved.Simulation.Final.Quality}");
        Console.WriteLine("COLLECTABLE_ACTIONS "+string.Join(" → ",solved.Simulation.Steps.Select(step=>catalog.Find(15,step.Action)!.Name)));
        foreach(var budget in new[]{(Width:128,Nodes:100000),(Width:512,Nodes:500000)})
        {
            var hq=CraftSolver.Solve(recipe,new(100,5622,5081,565),6000,new(p.Quality,40,budget.Width,budget.Nodes));
            var hqExport=hq.Simulation is null ? null:CraftMacroExporter.Build(hq.Simulation,15,catalog);
            Console.WriteLine($"HQ_COMPARE start=6000 width={budget.Width} nodes={budget.Nodes} target={hq.TargetReached} quality={hq.Simulation?.Final.Quality} actions={hqExport?.Actions} waits={hqExport?.WaitSeconds} pruned={hq.BeamPruned} budgetReached={hq.NodeLimitReached}");
            if(hq.Simulation is not null)Console.WriteLine("HQ_ACTIONS "+string.Join(" → ",hq.Simulation.Steps.Select(step=>catalog.Find(15,step.Action)!.Name)));
            if(budget.Width==128 && hq.Simulation is not null)
            {
                var basis=hq.Simulation.Steps.Select(step=>step.Action).ToArray();var fastest=hqExport!;CraftSimulationResult fastestSimulation=hq.Simulation;
                for(var removed=-1;removed<basis.Length;removed++)
                {
                    var shorter=basis.Where((_,index)=>index!=removed).ToArray();
                    for(var changed=0;changed<shorter.Length;changed++)foreach(var spec in CraftActionCatalog.Specs)
                    {
                        var candidate=(CraftActionKind[])shorter.Clone();candidate[changed]=spec.Kind;
                        var replay=CraftSimulation.Run(recipe,new(100,5622,5081,565),6000,candidate);
                        if(replay.Error is not null || replay.Final.Finish!=CraftFinish.Completed || replay.Final.Quality<p.Quality)continue;
                        var export=CraftMacroExporter.Build(replay,15,catalog);
                        if(export.WaitSeconds<fastest.WaitSeconds){fastest=export;fastestSimulation=replay;}
                    }
                }
                Console.WriteLine($"LOCAL_COMPARE actions={fastest.Actions} waits={fastest.WaitSeconds} quality={fastestSimulation.Final.Quality}");
                Console.WriteLine("LOCAL_ACTIONS "+string.Join(" → ",fastestSimulation.Steps.Select(step=>catalog.Find(15,step.Action)!.Name)));
                for(var removed=-1;removed<basis.Length;removed++)
                {
                    var shorter=basis.Where((_,index)=>index!=removed).ToArray();
                    for(var buff=0;buff<shorter.Length;buff++)for(var changed=0;changed<shorter.Length;changed++)
                    {
                        if(buff==changed)continue;
                        foreach(var spec in CraftActionCatalog.Specs)
                        {
                            var candidate=(CraftActionKind[])shorter.Clone();candidate[buff]=CraftActionKind.GreatStrides;candidate[changed]=spec.Kind;
                            var waits=candidate.Sum(action=>CraftMacroExporter.BaseWait(action));if(waits>=fastest.WaitSeconds)continue;
                            var replay=CraftSimulation.Run(recipe,new(100,5622,5081,565),6000,candidate);
                            if(replay.Error is not null || replay.Final.Finish!=CraftFinish.Completed || replay.Final.Quality<p.Quality)continue;
                            var export=CraftMacroExporter.Build(replay,15,catalog);
                            if(export.WaitSeconds<fastest.WaitSeconds){fastest=export;fastestSimulation=replay;}
                        }
                    }
                }
                Console.WriteLine($"FINISHER_COMPARE actions={fastest.Actions} waits={fastest.WaitSeconds} quality={fastestSimulation.Final.Quality}");
                Console.WriteLine("FINISHER_ACTIONS "+string.Join(" → ",fastestSimulation.Steps.Select(step=>catalog.Find(15,step.Action)!.Name)));
            }
        }
        if(solved.Simulation.Final.Quality<7106)throw new Exception("Collectible search regressed below the verified finisher fixture.");
        var rejected=false;
        try{RecipeSimulationCopy.Copy(row,levels.First(levelRow=>levelRow.RowId!=table.RowId));}catch(InvalidDataException){rejected=true;}
        if(!rejected)throw new Exception("Foreign recipe level identity accepted.");
    }
    if(CraftSimulation.Unsupported(recipe) is not null) {excluded++;continue;}
    supported++;
    if(recipe.ResultItemId==12892) sample=recipe;
    if(advancedSample is null && recipe.Level==100 && p.Durability==80 && p.Quality>0 && p.RequiredCraftsmanship<=5000 && p.RequiredControl<=5000) advancedSample=recipe;
}
Console.WriteLine($"PASS native recipe factors: {supported} supported, {excluded} explicitly excluded");
if(collectibleFixtures!=1)throw new Exception("Expected exactly one native Tacos collectible fixture.");
if(sample is null) throw new Exception("Birch syrup recipe missing");
var parameters=sample.Simulation!;
var scenario=new CraftStats(100,5000,5000,700,true); // Deliberately simulated stats, never observed player attributes.
var simulation=CraftSimulation.Run(sample,scenario,0,[CraftActionKind.Reflect,CraftActionKind.Innovation,CraftActionKind.BasicTouch,CraftActionKind.Byregot,CraftActionKind.CarefulSynthesis]);
var expectedBase=(int)(5000*10f/parameters.ProgressDivider+2);
if(simulation.BaseProgress!=expectedBase || simulation.Steps[0].QualityGain!=Math.Min(parameters.Quality,simulation.BaseQuality*3) || simulation.Error is not null) throw new Exception("Native recipe simulation fixture failed");
Console.WriteLine($"PASS simulated stats (not player): recipe {sample.RecipeId}, durability {parameters.Durability}, progress {parameters.Progress}, quality {parameters.Quality}, initial cap {parameters.MaxStartingQuality}, bases {simulation.BaseProgress}/{simulation.BaseQuality}, final {simulation.Final.Finish}");
foreach(var job in Enumerable.Range(8,8))
{
    var export=CraftMacroExporter.Build(simulation,(uint)job,catalog);
    if(export.Actions!=simulation.Steps.Count || export.Pages.Any(page=>page.Lines.Count>15) || !export.Pages[0].Text.Contains(catalog.Find((uint)job,simulation.Steps[0].Action)!.Name)) throw new Exception("Localized macro mismatch");
}
Console.WriteLine("PASS native localized macro export for eight jobs, ordered simulated prefix and fifteen-line limit");
var search=CraftSolver.Solve(sample,scenario,0,new(parameters.Quality,40,128,100000));
if(!search.TargetReached || search.Simulation is null) throw new Exception("Native recipe search fixture failed");
Console.WriteLine($"PASS native recipe search at fictitious stats: {search.Simulation.Steps.Count} actions, quality {search.Simulation.Final.Quality}, {search.Expanded} transitions, replay verified");
if(advancedSample is not null)
{
    var started=System.Diagnostics.Stopwatch.StartNew();
    var advanced=CraftSolver.Solve(advancedSample,scenario,0,new(advancedSample.Simulation!.Quality,40,128,100000));
    Console.WriteLine($"INFO native level100 recipe {advancedSample.RecipeId} item {advancedSample.ResultItemId}: target {advanced.TargetQuality}, reached {advanced.TargetReached}, actions {advanced.Simulation?.Steps.Count}, quality {advanced.Simulation?.Final.Quality}, transitions {advanced.Expanded}, elapsed {started.ElapsedMilliseconds}ms; fictitious stats only");
    if(advanced.Expanded>100000) throw new Exception("Search budget exceeded");
}
foreach(var id in new uint[]{19297,19004,260,4631,4639,4574})
{
    var row=actions.GetRow(id);
    Console.WriteLine($"Action {id} {row.Name.ExtractText()}, level={row.ClassJobLevel}, cost={row.PrimaryCostType}:{row.PrimaryCostValue}, secondary={row.SecondaryCostType}:{row.SecondaryCostValue.RowId}");
    Console.WriteLine(descriptions.GetRow(id).Description.ExtractText().Replace('\n',' '));
}
foreach(var id in new uint[]{100001,100203,100403,100002,100004,100411,100339,100003,100010,100387,100379,100323,100299,100427,100227,100443,100283,100435,100467,100475})
{
    var row=crafts.GetRow(id);
    Console.WriteLine($"CraftAction {id} {row.Name.ExtractText()}, job={row.ClassJob.RowId}, level={row.ClassJobLevel}, CP={row.Cost}, CUL={row.CUL.RowId}");
    Console.WriteLine(row.Description.ExtractText().Replace('\n',' '));
}
