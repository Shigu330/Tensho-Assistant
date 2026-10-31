using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Fashion;

internal sealed class FashionHintLoader : IDisposable
{
    private readonly object gate=new();
    private readonly FashionHintService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly List<FashionHint> copied=[];
    private ExcelSheet<FashionCheckThemeCategory>? sheet;
    private int position;
    private bool stopped;
    public FashionHintLoader(FashionHintService service,IDataManager data,IFramework framework,IPluginLog log)
    { this.service=service; this.data=data; this.framework=framework; this.log=log; framework.Update+=Update; }
    private void Update(IFramework unused)
    {
        lock(gate)
        {
            if(stopped || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            try
            {
                sheet ??= data.GetExcelSheet<FashionCheckThemeCategory>();
                var end=Math.Min(position+8,sheet.Count);
                while(position<end)
                {
                    var row=sheet.GetRowAt(position++); var name=row.Name.ExtractText();
                    if(row.RowId!=0 && !string.IsNullOrWhiteSpace(name)) copied.Add(new(row.RowId,name));
                }
                if(position!=sheet.Count) return;
                service.Publish(new(copied)); Stop();
            }
            catch(Exception error) { service.Fail(error); log.Warning(error,"Fashion hint catalog unavailable."); Stop(); }
        }
    }
    private void Stop() { stopped=true; framework.Update-=Update; copied.Clear(); sheet=null; }
    public void Dispose() { lock(gate) { if(!stopped) Stop(); service.Clear(); } }
}
