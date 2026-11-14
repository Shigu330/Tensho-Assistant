using Dalamud.Plugin.Services;

namespace TenshoAssistant.Gathering;

/// <summary>g, R-01 (0.392), moved out of Plugin.cs by X-285 (0.1300): the ocean fishing timetable from the game's loop (IKDRouteTable)
/// and voyages (IKDRoute). Sheets only: the game and the desktop application read it the same way (MainWindow.UseSheetLoaders).</summary>
internal static class OceanScheduleLoader
{
    public static OceanSchedule? Read(IDataManager data)
    {
        var routes=data.GetExcelSheet<Lumina.Excel.Sheets.IKDRoute>().Where(row=>!row.Name.IsEmpty).ToDictionary(row=>row.RowId,row=>new OceanRoute(row.RowId,row.Name.ExtractText(),
            row.Spot.Zip(row.Time).Where(pair=>pair.First.RowId!=0)
                .Select(pair=>new OceanStop(pair.First.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? "",OceanSchedule.TimeOf(pair.Second.RowId))).ToArray()));
        var loop=data.GetExcelSheet<Lumina.Excel.Sheets.IKDRouteTable>().OrderBy(row=>row.RowId).Select(row=>(row.Route.RowId,row.Unknown0)).ToArray();
        return loop.Length>0 ? new OceanSchedule(loop,routes):null;
    }
}
