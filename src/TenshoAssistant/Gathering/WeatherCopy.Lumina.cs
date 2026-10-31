using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gathering;

/// <summary>Shared native projection for the framework loader and offline data verification.</summary>
public static class WeatherCopy
{
    public static WeatherDefinition FromRows(Weather local,Weather? english)=>new(local.RowId,local.Name.ExtractText(),
        local.Icon>0 ? (uint)local.Icon:0,
        english is { } source && source.RowId==local.RowId ? source.Name.ExtractText():null);
}
