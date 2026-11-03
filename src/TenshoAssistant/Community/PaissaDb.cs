using System.Net.Http;
using System.Text.Json;

namespace TenshoAssistant.Community;

/// <summary>
/// N-05 (0.445, user's choice in GAPS: PaissaDB accepted, read only, watch by setting): an open plot as PaissaDB (zhudotexe, no licence
/// stated, public NA servers only) reports it — district (TerritoryType id and its name), ward and plot (shown 1-based; the API is
/// 0-based), size (0 S, 1 M, 2 L), price, lottery or first come, lottery phase and entries. Nothing is sent but the world id.
/// </summary>
public sealed record OpenPlot(uint District,string DistrictName,int Ward,int Plot,int Size,long Price,bool Lottery,bool FreeCompany,bool Individual,
    int? LottoPhase,int? LottoEntries,DateTimeOffset? PhaseUntil);

public static class PaissaDb
{
    public const string Host="paissadb.zhu.codes";
    public const int MaximumBytes=2*1024*1024;

    /// <summary>The open plots of a WorldDetail document; a malformed plot is skipped, a malformed document throws.</summary>
    public static IReadOnlyList<OpenPlot> Parse(string json)
    {
        using var document=JsonDocument.Parse(json);
        var plots=new List<OpenPlot>();
        foreach(var district in document.RootElement.GetProperty("districts").EnumerateArray())
        {
            var name=district.TryGetProperty("name",out var text) ? text.GetString() ?? "":"";
            if(!district.TryGetProperty("open_plots",out var open) || open.ValueKind!=JsonValueKind.Array)continue;
            foreach(var plot in open.EnumerateArray())
            {
                // A field of the wrong type (a string where a number is expected…) skips the plot instead of failing the document.
                static bool Number(JsonElement element,string field,out long value)
                {value=0;return element.TryGetProperty(field,out var item) && item.ValueKind==JsonValueKind.Number && item.TryGetInt64(out value);}
                if(plot.ValueKind!=JsonValueKind.Object || !Number(plot,"district_id",out var districtLong) || !Number(plot,"ward_number",out var wardLong)
                   || !Number(plot,"plot_number",out var plotLong) || !Number(plot,"size",out var sizeLong) || !Number(plot,"price",out var priceLong)
                   || !Number(plot,"purchase_system",out var purchaseLong))continue;
                if(districtLong is <= 0 or > uint.MaxValue || wardLong is < 0 or > 98 || plotLong is < 0 or > 998 || sizeLong is < 0 or > 2 || priceLong<0)continue;
                var districtId=(uint)districtLong;var wardNumber=(int)wardLong;var plotNumber=(int)plotLong;var sizeValue=(int)sizeLong;var priceValue=priceLong;var purchase=(int)(purchaseLong & 0xFF);
                int? Int(string field)=>plot.TryGetProperty(field,out var value) && value.ValueKind==JsonValueKind.Number && value.TryGetInt32(out var number) ? number:null;
                DateTimeOffset? Time(string field)=>plot.TryGetProperty(field,out var value) && value.ValueKind==JsonValueKind.Number && value.TryGetDouble(out var seconds) && seconds>0
                    ? DateTimeOffset.FromUnixTimeMilliseconds((long)(seconds*1000)):null;
                plots.Add(new(districtId,name,wardNumber+1,plotNumber+1,sizeValue,priceValue,(purchase & 1)!=0,(purchase & 2)!=0,(purchase & 4)!=0,
                    Int("lotto_phase"),Int("lotto_entries"),Time("lotto_phase_until")));
            }
        }
        return plots.OrderBy(plot=>plot.District).ThenBy(plot=>plot.Ward).ThenBy(plot=>plot.Plot).ToArray();
    }

    public static string Key(OpenPlot plot)=>$"{plot.District}/{plot.Ward}/{plot.Plot}";

    /// <summary>The plots not in the previous read (none on the first read: nothing is « new » before a first list).</summary>
    public static IReadOnlyList<OpenPlot> New(IReadOnlyList<OpenPlot>? previous,IReadOnlyList<OpenPlot> current)
    {
        if(previous is null)return [];
        var known=previous.Select(Key).ToHashSet(StringComparer.Ordinal);
        return current.Where(plot=>!known.Contains(Key(plot))).ToArray();
    }
}

public sealed record PaissaState(uint WorldId,IReadOnlyList<OpenPlot>? Plots,DateTimeOffset? ReadAt,bool Loading,string? Error);

/// <summary>Reads a world's open plots from PaissaDB, one request at a time, bounded size; the caller decides when (click or watch).</summary>
public sealed class PaissaDbService(HttpClient? client=null) : IDisposable
{
    private readonly HttpClient http=client ?? new HttpClient{Timeout=TimeSpan.FromSeconds(20)};
    private readonly bool ownsClient=client is null;
    private readonly CancellationTokenSource cancellation=new();
    private PaissaState state=new(0,null,null,false,null);
    private int busy;
    public PaissaState State=>Volatile.Read(ref state);

    public void Request(uint worldId,Action<IReadOnlyList<OpenPlot>?,IReadOnlyList<OpenPlot>>? read=null)
    {
        if(worldId==0 || Interlocked.Exchange(ref busy,1)==1)return;
        var previous=State;
        Volatile.Write(ref state,previous with {WorldId=worldId,Loading=true,Error=null});
        var token=cancellation.Token;
        _=Task.Run(async()=>
        {
            try
            {
                var json=await BoundedHttp.ReadAsync(http,new Uri($"https://{PaissaDb.Host}/worlds/{worldId}"),PaissaDb.MaximumBytes,token).ConfigureAwait(false);
                var plots=PaissaDb.Parse(json);
                Volatile.Write(ref state,new(worldId,plots,DateTimeOffset.UtcNow,false,null));
                read?.Invoke(previous.WorldId==worldId ? previous.Plots:null,plots);
            }
            catch(Exception error) when(!token.IsCancellationRequested){Volatile.Write(ref state,previous with {WorldId=worldId,Loading=false,Error=error.Message});}
            finally{Interlocked.Exchange(ref busy,0);}
        },token);
    }

    public void Dispose(){cancellation.Cancel();cancellation.Dispose();if(ownsClient)http.Dispose();}
}
