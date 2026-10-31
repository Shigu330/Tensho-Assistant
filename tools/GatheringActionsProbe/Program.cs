using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Gathering;

if(args.Length!=1) throw new ArgumentException("Supply the installed game's sqpack directory.");
using var data=new GameData(args[0],new LuminaOptions { DefaultExcelLanguage=Language.French });
var descriptions=data.Excel.GetSheet<ActionTransient>();
foreach(var row in data.Excel.GetSheet<Lumina.Excel.Sheets.Action>().Where(row=>row.ClassJob.RowId is 16 or 17 && row.PrimaryCostValue>0))
{
    Console.WriteLine($"Action {row.RowId} {row.Name.ExtractText()}, job={row.ClassJob.RowId}, level={row.ClassJobLevel}, cost={row.PrimaryCostType}:{row.PrimaryCostValue}, secondary={row.SecondaryCostType}:{row.SecondaryCostValue.RowId}");
    if(descriptions.TryGetRow(row.RowId,out var detail)) Console.WriteLine(detail.Description.ExtractText().Replace('\n',' '));
}
var actions=data.Excel.GetSheet<Lumina.Excel.Sheets.Action>();
var copied=new List<GpAction>();
foreach(var spec in GpActionCatalog.Specs)
{
    var action=actions.GetRow(spec.Id);
    if(action.ClassJob.RowId!=spec.JobId || action.ClassJobLevel!=spec.Level || action.PrimaryCostType!=7 || action.PrimaryCostValue!=spec.Cost
        || action.SecondaryCostValue.RowId!=0) throw new InvalidOperationException($"Unsupported native GP action {spec.Id}.");
    copied.Add(new(spec,action.Name.ExtractText(),action.Icon,descriptions.GetRow(spec.Id).Description.ExtractText()));
}
var version=string.Join(";",data.Repositories.Values.OrderBy(repository=>repository.Name).Select(repository=>$"{repository.Name}={repository.Version}"));
var catalog=new GpActionCatalog(copied,version);
foreach(var job in new uint[]{16,17})
{
    var result=GpOptimizer.Solve(new(job,100,600,4,1,100,15,3),catalog);
    if(result.Best.Cost!=550 || result.Best.ExpectedYield!=18.2m || result.Baseline!=4.6m || result.NextUpgrade?.Cost!=700)
        throw new InvalidOperationException("Native-action manual GP scenario differs from independent hand calculation.");
    if(result.Best.Actions.Any(use=>!data.FileExists($"ui/icon/{use.Action.Icon/1000*1000:000000}/{use.Action.Icon:000000}.tex")))
        throw new InvalidOperationException("Native GP action icon missing.");
    Console.WriteLine($"PASS native-action GP model: job={job}, 600 budget -> {result.Best.ExpectedYield} expected objects / {result.Best.Cost} GP; baseline={result.Baseline}, next threshold={result.NextUpgrade.Cost}; {catalog.Actions.Count} validated action rows.");
    var active=GpOptimizer.Solve(new(job,100,300,4,3,100,100,3,new(true,true,true,true,3)),catalog);
    if(active.Baseline!=23 || active.Best.Cost!=300 || active.Best.ExpectedYield!=32 || active.Best.Actions.Count!=1
        || active.Best.Actions[0].Action.Spec.Effect!=GpEffect.Bountiful2 || active.Best.Actions[0].Count!=3)
        throw new InvalidOperationException("Paid effects scenario recharges or duplicates an existing bonus.");
    Console.WriteLine($"PASS native-action declared effects: job={job}, paid families +first3 -> baseline23, new bonus3x3 /300GP -> expected32; no paid-family recast.");
}

// Read declared self-status associations without assuming an absent association proves no effect.
var statuses=data.Excel.GetSheet<Status>();
var gatherActions=actions.Where(action=>action.ClassJob.RowId is 16 or 17).ToArray();
foreach(var spec in GpActionCatalog.Specs)
    Console.WriteLine($"STATUS LINK action={spec.Id}, status={actions.GetRow(spec.Id).StatusGainSelf.RowId}");
var statusIds=gatherActions.Select(action=>action.StatusGainSelf.RowId).Where(id=>id!=0)
    .Concat(new uint[]{2667,219,2666,759,1286}).Distinct().Order().ToArray();
foreach(var id in statusIds)
{
    var status=statuses.GetRow(id);
    Console.WriteLine($"STATUS {id} {status.Name.ExtractText()}, param={status.ParamModifier}/{status.ParamEffect}, stacks={status.MaxStacks}; actions={string.Join(',',gatherActions.Where(action=>action.StatusGainSelf.RowId==id).Select(action=>action.RowId))}");
    Console.WriteLine(status.Description.ExtractText().Replace('\n',' '));
}
