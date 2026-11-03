using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Items;

/// <summary>
/// 0.407 (user rule, 2026-11-03): the game's name, in the client's language, for an English name read from a community page (wiki,
/// Garland). The monster, place and duty tables are built once, off the framework thread, from the game's English and client sheets;
/// until they are ready, and for a name the game does not know, the caller keeps the page's text. Nothing is built for an English
/// client. Garland rows carrying a game id are read by that id, and kept only when the game's English name matches Garland's.
/// </summary>
public sealed class GameNames
{
    private sealed record Tables(Dictionary<string,string> Monsters,Dictionary<string,string> Places,Dictionary<string,string> Duties);
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private Tables? tables;
    private int started;
    private int warned;
    private readonly Dictionary<(string Type,ulong Id),string?> garland=new(); // read on the framework (draw) thread only

    public GameNames(IDataManager data,IPluginLog log){this.data=data;this.log=log;}

    private bool Active=>data.Language!=ClientLanguage.English;

    public string? Monster(string english)=>Find(english,tables=>tables.Monsters);
    public string? Place(string english)=>Find(english,tables=>tables.Places) ?? Find(english,tables=>tables.Duties);

    public string? Garland(string type,ulong id,string english)
    {
        if(!Active)return null;
        if(type=="mob")return Monster(english);
        if(type=="instance")return Place(english);
        if(id is 0 or > uint.MaxValue)return null;
        if(garland.TryGetValue((type,id),out var known))return known;
        if(garland.Count>=4096)garland.Clear();
        var row=(uint)id;
        try
        {
            return garland[(type,id)]=type switch
            {
                "quest"=>Same(english,data.GetExcelSheet<Quest>(ClientLanguage.English).GetRowOrDefault(row)?.Name.ExtractText(),
                    ()=>data.GetExcelSheet<Quest>().GetRowOrDefault(row)?.Name.ExtractText()),
                "fate"=>Same(english,data.GetExcelSheet<Fate>(ClientLanguage.English).GetRowOrDefault(row)?.Name.ExtractText(),
                    ()=>data.GetExcelSheet<Fate>().GetRowOrDefault(row)?.Name.ExtractText()),
                "achievement"=>Same(english,data.GetExcelSheet<Achievement>(ClientLanguage.English).GetRowOrDefault(row)?.Name.ExtractText(),
                    ()=>data.GetExcelSheet<Achievement>().GetRowOrDefault(row)?.Name.ExtractText()),
                "leve"=>Same(english,data.GetExcelSheet<Leve>(ClientLanguage.English).GetRowOrDefault(row)?.Name.ExtractText(),
                    ()=>data.GetExcelSheet<Leve>().GetRowOrDefault(row)?.Name.ExtractText()),
                "npc"=>Same(english,data.GetExcelSheet<ENpcResident>(ClientLanguage.English).GetRowOrDefault(row)?.Singular.ExtractText(),
                    ()=>data.GetExcelSheet<ENpcResident>().GetRowOrDefault(row)?.Singular.ExtractText()),
                _=>null,
            };
        }
        // R-14 (0.454): a lost sheet throws on every call: the failure is kept like a missing name, or it threw every frame in the hover.
        catch(Exception error){Warn(error);garland[(type,id)]=null;return null;}
    }

    private static string? Same(string english,string? gameEnglish,Func<string?> local)
        =>gameEnglish is { Length:>0 } && string.Equals(gameEnglish.Trim(),english.Trim(),StringComparison.OrdinalIgnoreCase) ? local():null;

    private string? Find(string english,Func<Tables,Dictionary<string,string>> table)
    {
        if(!Active || english.Length==0)return null;
        if(Volatile.Read(ref tables) is { } ready)return table(ready).GetValueOrDefault(english.Trim());
        if(Interlocked.Exchange(ref started,1)==0)_=Task.Run(Build);
        return null;
    }

    private void Build()
    {
        try
        {
            static Dictionary<string,string> Pairs(IEnumerable<(uint Row,string Name)> english,IReadOnlyDictionary<uint,string> local)
            {
                var pairs=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                foreach(var (row,name) in english)
                    if(name.Length>0 && local.TryGetValue(row,out var text) && text.Length>0)pairs.TryAdd(name,text);
                return pairs;
            }
            // R-14 (0.454): each table on its own: a sheet lost after an update leaves only its names in English.
            Dictionary<string,string> Safe(Func<Dictionary<string,string>> build)
            {
                try{return build();}
                catch(Exception error){Warn(error);return [];}
            }
            var monsters=Safe(()=>Pairs(data.GetExcelSheet<BNpcName>(ClientLanguage.English).Select(row=>(row.RowId,row.Singular.ExtractText().Trim())),
                data.GetExcelSheet<BNpcName>().ToDictionary(row=>row.RowId,row=>row.Singular.ExtractText().Trim())));
            var places=Safe(()=>Pairs(data.GetExcelSheet<PlaceName>(ClientLanguage.English).Select(row=>(row.RowId,row.Name.ExtractText().Trim())),
                data.GetExcelSheet<PlaceName>().ToDictionary(row=>row.RowId,row=>row.Name.ExtractText().Trim())));
            var duties=Safe(()=>Pairs(data.GetExcelSheet<ContentFinderCondition>(ClientLanguage.English).Select(row=>(row.RowId,row.Name.ExtractText().Trim())),
                data.GetExcelSheet<ContentFinderCondition>().ToDictionary(row=>row.RowId,row=>row.Name.ExtractText().Trim())));
            Volatile.Write(ref tables,new Tables(monsters,places,duties));
        }
        catch(Exception error){Warn(error);}
    }

    private void Warn(Exception error)
    {
        if(Interlocked.Exchange(ref warned,1)==0)log.Warning(error,"Game names for community pages unavailable; the pages' English names are shown.");
    }
}
