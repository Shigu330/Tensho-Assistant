using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using GameAction=Lumina.Excel.Sheets.Action;

namespace TenshoAssistant.Craft;

internal sealed class CraftActionCatalogLoader : IDisposable
{
    private readonly object gate=new();
    private readonly CraftActionCatalogService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly List<CraftActionMetadata> copied=[];
    private int position;
    private GameAction[]? crafterActions;
    private bool stopped;
    public CraftActionCatalogLoader(CraftActionCatalogService service,IDataManager data,IFramework framework,IPluginLog log)
    { this.service=service; this.data=data; this.framework=framework; this.log=log; framework.Update+=Update; }
    private void Update(IFramework unused)
    {
        lock(gate)
        {
            if(stopped || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            try
            {
                var crafts=data.GetExcelSheet<CraftAction>(); var actions=data.GetExcelSheet<GameAction>();
                var descriptions=data.GetExcelSheet<ActionTransient>();
                for(var count=0;count<2 && position<CraftActionCatalog.Specs.Count;count++,position++)
                {
                    var spec=CraftActionCatalog.Specs[position];
                    if(spec.CanonicalId>=100000)
                    {
                        var canonical=crafts.GetRow(spec.CanonicalId);
                        for(uint job=8;job<=15;job++)
                        {
                            var id=job switch {8=>canonical.CRP.RowId,9=>canonical.BSM.RowId,10=>canonical.ARM.RowId,11=>canonical.GSM.RowId,
                                12=>canonical.LTW.RowId,13=>canonical.WVR.RowId,14=>canonical.ALC.RowId,_=>canonical.CUL.RowId};
                            var row=crafts.GetRow(id);
                            if(row.ClassJob.RowId!=job || row.ClassJobLevel!=spec.Level || row.Cost!=spec.Cp)
                                throw new InvalidOperationException($"Crafting action {id} incompatible with the model.");
                            copied.Add(new(spec,job,id,row.Name.ExtractText(),row.Icon,row.Description.ExtractText()));
                        }
                    }
                    else
                    {
                        var row=actions.GetRow(spec.CanonicalId);
                        if(row.ClassJobLevel!=spec.Level || row.PrimaryCostType!=8 || row.PrimaryCostValue!=spec.Cp || row.SecondaryCostType!=0 || row.SecondaryCostValue.RowId!=0)
                            throw new InvalidOperationException($"Crafting effect {spec.CanonicalId} incompatible with the model.");
                        // A-14 (0.452, review): the buffs have one Action row per class (Veneration: 19297 CRP … 19304 CUL), ids not always contiguous,
                        // with the name, level and cost of the CRP row. All jobs used to get the CRP row: on the seven others the recorder dropped them
                        // and the live reading asked the game about another class's action.
                        var perJob=new Dictionary<uint,GameAction>();
                        crafterActions??=actions.Where(candidate=>candidate.ClassJob.RowId is >= 8 and <= 15 && candidate.IsPlayerAction && candidate.PrimaryCostType==8).ToArray();
                        foreach(var candidate in crafterActions)
                            if(candidate.ClassJobLevel==spec.Level && candidate.PrimaryCostValue==spec.Cp && candidate.Name==row.Name && !perJob.ContainsKey(candidate.ClassJob.RowId))
                                perJob[candidate.ClassJob.RowId]=candidate;
                        for(uint job=8;job<=15;job++)
                        {
                            if(!perJob.TryGetValue(job,out var own))throw new InvalidOperationException($"Crafting effect {spec.CanonicalId} has no row for job {job}.");
                            copied.Add(new(spec,job,own.RowId,own.Name.ExtractText(),own.Icon,descriptions.GetRow(own.RowId).Description.ExtractText()));
                        }
                    }
                }
                if(position!=CraftActionCatalog.Specs.Count) return;
                var version=string.Join(";",data.GameData.Repositories.Values.OrderBy(row=>row.Name).Select(row=>$"{row.Name}={row.Version}"));
                var statusSheet=data.GetExcelSheet<Lumina.Excel.Sheets.Status>();
                var statuses=CraftObservedEffects.Specs.Select(spec=>
                {var row=statusSheet.GetRow(spec.Id);return new CraftStatusMetadata(row.RowId,row.Name.ExtractText(),row.Icon,row.Description.ExtractText(),row.MaxStacks);}).ToArray();
                service.Publish(new(copied,version,statuses)); Stop();
            }
            catch(Exception error) { service.Fail(error); log.Warning(error,"Craft action model catalog unavailable."); Stop(); }
        }
    }
    private void Stop() { stopped=true; framework.Update-=Update; copied.Clear(); crafterActions=null; }
    public void Dispose() { lock(gate) { if(!stopped) Stop(); service.Clear(); } }
}
