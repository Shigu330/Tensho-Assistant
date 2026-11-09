using Dalamud.Plugin.Services;
using Lumina.Excel;

namespace TenshoAssistant.Diagnostics;

// X-92 (0.972): the game data check, once per load, off the frame (Diagnostics/GameDataChanges.cs). The snapshot file is replaced only when it
// changed, so the change report is written to the log once, at the first load after a game update.
internal static class GameDataWatch
{
    public static Task<GameDataReport?> Start(IDataManager data,string file,IPluginLog log,CancellationToken token)=>Task.Run(()=>
    {
        try
        {
            var names=data.Excel.SheetNames.ToArray();
            var known=new HashSet<string>(names,StringComparer.Ordinal);
            var version=string.Join(" ",data.GameData.Repositories.OrderBy(pair=>pair.Key,StringComparer.Ordinal).Select(pair=>$"{pair.Key}={pair.Value.Version}"));
            var sheets=new Dictionary<string,SheetShape>(StringComparer.Ordinal);
            foreach(var name in GameDataChanges.Watched)
            {
                token.ThrowIfCancellationRequested();
                if(!known.Contains(name))continue;
                try
                {
                    var raw=data.Excel.GetRawSheet(name);
                    var last=raw is RawSubrowExcelSheet ? 0u:data.Excel.GetSheet<RawRow>(null,name).Select(row=>row.RowId).DefaultIfEmpty().Max();
                    sheets[name]=new(raw.Count,raw.Columns.Count,last);
                }
                catch(Exception error) when (error is not OperationCanceledException){log.Debug("Game data check: sheet {0} unreadable ({1}).",name,error.GetType().Name);}
            }
            var now=new GameDataSnapshot(version,sheets,names);
            var before=File.Exists(file) ? GameDataChanges.Read(File.ReadAllText(file)):null;
            var report=GameDataChanges.Compare(before,now);
            foreach(var line in GameDataChanges.Lines(report))log.Information("Game data check: {0}",line);
            if(report.First || report.Changed)AtomicFile.Write(file,GameDataChanges.Write(now));
            return report;
        }
        catch(OperationCanceledException){return null;}
        catch(Exception error){log.Warning(error,"Game data check failed.");return null;}
    },token);
}
