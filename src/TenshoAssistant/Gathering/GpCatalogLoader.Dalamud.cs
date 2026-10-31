using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using GameAction=Lumina.Excel.Sheets.Action;

namespace TenshoAssistant.Gathering;

internal sealed class GpCatalogLoader : IDisposable
{
    private readonly object gate=new();
    private readonly GpCatalogService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly List<GpAction> copied=[];
    private int position;
    private bool stopped;
    public GpCatalogLoader(GpCatalogService service,IDataManager data,IFramework framework,IPluginLog log)
    { this.service=service; this.data=data; this.framework=framework; this.log=log; framework.Update+=Update; }
    private void Update(IFramework unused)
    {
        lock(gate)
        {
            if(stopped || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            try
            {
                var sheet=data.GetExcelSheet<GameAction>(); var descriptions=data.GetExcelSheet<ActionTransient>();
                for(var count=0;count<4 && position<GpActionCatalog.Specs.Count;count++,position++)
                {
                    var spec=GpActionCatalog.Specs[position];
                    if(!sheet.TryGetRow(spec.Id,out var row) || row.ClassJob.RowId!=spec.JobId || row.ClassJobLevel!=spec.Level
                        || row.PrimaryCostType!=7 || row.PrimaryCostValue!=spec.Cost || row.SecondaryCostValue.RowId!=0
                        || !descriptions.TryGetRow(spec.Id,out var description))
                        throw new InvalidOperationException($"Action de récolte {spec.Id} incompatible avec le modèle vérifié.");
                    copied.Add(new(spec,row.Name.ExtractText(),row.Icon,description.Description.ExtractText()));
                }
                if(position!=GpActionCatalog.Specs.Count) return;
                var version=string.Join(";",data.GameData.Repositories.Values.OrderBy(repository=>repository.Name).Select(repository=>$"{repository.Name}={repository.Version}"));
                service.Publish(new(copied,version)); Stop();
            }
            catch(Exception error) { service.Fail(error); log.Warning(error,"Gathering GP model catalog unavailable."); Stop(); }
        }
    }
    private void Stop() { stopped=true; framework.Update-=Update; copied.Clear(); }
    public void Dispose() { lock(gate) { if(!stopped) Stop(); service.Clear(); } }
}
