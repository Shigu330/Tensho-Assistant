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
                                throw new InvalidOperationException($"Action artisanale {id} incompatible avec le modèle.");
                            copied.Add(new(spec,job,id,row.Name.ExtractText(),row.Icon,row.Description.ExtractText()));
                        }
                    }
                    else
                    {
                        var row=actions.GetRow(spec.CanonicalId);
                        if(row.ClassJobLevel!=spec.Level || row.PrimaryCostType!=8 || row.PrimaryCostValue!=spec.Cp || row.SecondaryCostType!=0 || row.SecondaryCostValue.RowId!=0)
                            throw new InvalidOperationException($"Effet artisanal {spec.CanonicalId} incompatible avec le modèle.");
                        var description=descriptions.GetRow(spec.CanonicalId).Description.ExtractText();
                        for(uint job=8;job<=15;job++) copied.Add(new(spec,job,row.RowId,row.Name.ExtractText(),row.Icon,description));
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
    private void Stop() { stopped=true; framework.Update-=Update; copied.Clear(); }
    public void Dispose() { lock(gate) { if(!stopped) Stop(); service.Clear(); } }
}
