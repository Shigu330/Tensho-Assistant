using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Items;

internal sealed class ItemUseCatalogLoader : IDisposable
{
    private readonly object gate=new();private readonly ItemUseCatalogService service;private readonly IDataManager data;private readonly IFramework framework;private readonly IPluginLog log;
    private readonly CancellationTokenSource cancellation=new();private readonly List<ItemLeveUse> leves=[];private readonly HashSet<uint> supplies=[];private readonly HashSet<uint> aquarium=[];
    private ExcelSheet<CraftLeve>? craft;private ExcelSheet<GCSupplyDuty>? supply;private ExcelSheet<AquariumFish>? fish;
    private int position,unresolved;private bool stopped,disposed;private Task<(ItemUseCatalog? Catalog,Exception? Error)>? build;
    public ItemUseCatalogLoader(ItemUseCatalogService service,IDataManager data,IFramework framework,IPluginLog log)
    {this.service=service;this.data=data;this.framework=framework;this.log=log;try{framework.Update+=Update;}catch{cancellation.Dispose();throw;}}
    private void Update(IFramework unused)
    {
        if(!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        lock(gate)
        {
            if(stopped)return;
            try
            {
                if(build is not null)
                {if(!build.IsCompleted)return;var result=build.GetAwaiter().GetResult();if(result.Error is { } error)throw error;service.Publish(result.Catalog!);Stop();return;}
                craft??=data.GetExcelSheet<CraftLeve>();supply??=data.GetExcelSheet<GCSupplyDuty>();fish??=data.GetExcelSheet<AquariumFish>();
                var total=craft.Count+supply.Count+fish.Count;var end=Math.Min(total,position+32);var started=Stopwatch.GetTimestamp();
                while(position<end)
                {
                    var index=position++;
                    if(index<craft.Count)
                    {var row=craft.GetRowAt(index);var copy=ItemUseSheetCopy.Craft(row);if(copy is not null)leves.Add(copy);else if(row.RowId!=0 && row.Leve.RowId!=0)unresolved++;}
                    else if(index<craft.Count+supply.Count)
                    {foreach(var item in ItemUseSheetCopy.Supplies(supply.GetRowAt(index-craft.Count)))supplies.Add(item);}
                    else
                    {var item=fish.GetRowAt(index-craft.Count-supply.Count).Item.RowId;if(item!=0)aquarium.Add(item);}
                    if(Stopwatch.GetElapsedTime(started)>=TimeSpan.FromMilliseconds(2))break;
                }
                service.Progress(position,total);if(position<total)return;
                var copiedLeves=leves.ToArray();var copiedSupplies=supplies.ToArray();var copiedFish=aquarium.ToArray();var skipped=unresolved;
                var version=string.Join(";",data.GameData.Repositories.Values.OrderBy(repository=>repository.Name).Select(repository=>$"{repository.Name}={repository.Version}"));
                var language=data.Language.ToString();var token=cancellation.Token;
                craft=null;supply=null;fish=null;
                build=Task.Run<(ItemUseCatalog?,Exception?)>(()=>
                {try{return(new(copiedLeves,copiedSupplies,copiedFish,language,version,skipped,token),null);}catch(Exception error){return(null,error);}});
            }
            catch(Exception error){log.Warning(error,"Item usage catalogue unavailable.");service.Fail(error);Stop();}
        }
    }
    private void Stop(){stopped=true;framework.Update-=Update;craft=null;supply=null;fish=null;leves.Clear();supplies.Clear();aquarium.Clear();}
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;if(!stopped){cancellation.Cancel();Stop();}service.Clear();cancellation.Dispose();}}
}
