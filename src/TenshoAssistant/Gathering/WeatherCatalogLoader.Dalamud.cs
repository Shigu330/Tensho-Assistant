using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gathering;

internal sealed class WeatherCatalogLoader : IDisposable
{
    private readonly object gate=new();private readonly WeatherCatalogService service;private readonly IDataManager data;private readonly IFramework framework;private readonly IPluginLog log;
    private readonly List<ZoneWeather> zones=[];private readonly Dictionary<uint,WeatherDefinition> weathers=[];private int position,weatherPosition;private bool stopped,englishUnavailable;
    public WeatherCatalogLoader(WeatherCatalogService service,IDataManager data,IFramework framework,IPluginLog log)
    {this.service=service;this.data=data;this.framework=framework;this.log=log;framework.Update+=Update;}
    private void Update(IFramework unused)
    {
        lock(gate)
        {
            if(stopped || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            try
            {
                var weatherSheet=data.GetExcelSheet<Weather>();
                if(weatherPosition<weatherSheet.Count)
                {
                    Lumina.Excel.ExcelSheet<Weather>? english=null;
                    if(!englishUnavailable)
                    {
                        try{english=data.GetExcelSheet<Weather>(Dalamud.Game.ClientLanguage.English);}
                        catch(Exception error){englishUnavailable=true;log.Warning(error,"English weather names unavailable; community names remain unresolved.");}
                    }
                    var weatherEnd=Math.Min(weatherPosition+32,weatherSheet.Count);var weatherStarted=Stopwatch.GetTimestamp();
                    while(weatherPosition<weatherEnd)
                    {
                        var row=weatherSheet.GetRowAt(weatherPosition++);
                        Weather? translated=null;
                        if(!englishUnavailable && english is not null)
                        {
                            try{if(english.TryGetRow(row.RowId,out var found))translated=found;}
                            catch(Exception error){englishUnavailable=true;log.Warning(error,"English weather row unavailable; remaining aliases are unknown.");}
                        }
                        if(row.RowId!=0)weathers[row.RowId]=WeatherCopy.FromRows(row,translated);
                        if(Stopwatch.GetElapsedTime(weatherStarted)>=TimeSpan.FromMilliseconds(2))break;
                    }
                    return;
                }
                var sheet=data.GetExcelSheet<TerritoryType>();var rates=data.GetExcelSheet<WeatherRate>();
                var end=Math.Min(position+32,sheet.Count);var started=Stopwatch.GetTimestamp();
                while(position<end)
                {
                    var row=sheet.GetRowAt(position++);if(row.RowId==0)continue;var shares=new List<WeatherShare>();
                    if(row.WeatherRate.RowId!=0 && rates.TryGetRow(row.WeatherRate.RowId,out var rate))
                    {
                        for(var index=0;index<rate.Rate.Count && index<rate.Weather.Count;index++)
                        {
                            if(rate.Rate[index]==0)continue;var id=rate.Weather[index].RowId;shares.Add(new(id,rate.Rate[index]));
                        }
                    }
                    zones.Add(new(row.RowId,row.WeatherRate.RowId,shares,row.IndividualWeather.RowId!=0));
                    if(Stopwatch.GetElapsedTime(started)>=TimeSpan.FromMilliseconds(2))break;
                }
                if(position!=sheet.Count)return;
                var version=string.Join(";",data.GameData.Repositories.Values.OrderBy(row=>row.Name).Select(row=>$"{row.Name}={row.Version}"));
                service.Publish(new(weathers.Values,zones,version));Stop();
            }
            catch(Exception error){service.Fail(error);log.Warning(error,"Weather catalog unavailable.");Stop();}
        }
    }
    private void Stop(){stopped=true;framework.Update-=Update;zones.Clear();weathers.Clear();}
    public void Dispose(){lock(gate){if(!stopped)Stop();service.Clear();}}
}
