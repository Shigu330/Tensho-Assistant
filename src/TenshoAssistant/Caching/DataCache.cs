using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TenshoAssistant.Quests;

namespace TenshoAssistant.Caching;

public sealed record CachedValue<T>(T Value,DateTimeOffset RecordedAt);
public sealed record QuestCacheData(ulong Owner,IReadOnlyList<QuestEntry> Entries);
public sealed record CollectionCacheData(ulong Owner,IReadOnlyList<Collections.CollectionEntry> Entries,IReadOnlyList<Collections.CollectionCategoryIssue>? Issues);
public sealed record FashionCacheData(string Json,string? Metadata);

/// <summary>Versioned, bounded local snapshots. All disk I/O and JSON work run off the framework thread.</summary>
public sealed class DataCache : IDisposable
{
    private sealed record Envelope(int Schema,string Family,string Scope,string Version,DateTimeOffset RecordedAt,JsonElement Value);
    private sealed record RewardData(byte Mode,uint Gil,QuestRewardItem[] Items,QuestUnresolvedReward[] Unresolved,bool Additional,string Version);
    private sealed class RewardConverter : JsonConverter<QuestRewardSnapshot>
    {
        public override QuestRewardSnapshot Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
        {
            var row=JsonSerializer.Deserialize<RewardData>(ref reader,options) ?? throw new JsonException();
            return new(row.Mode,row.Gil,row.Items,row.Unresolved,row.Additional,row.Version);
        }
        public override void Write(Utf8JsonWriter writer,QuestRewardSnapshot value,JsonSerializerOptions options)=>
            JsonSerializer.Serialize(writer,new RewardData(value.Mode,value.Gil,value.Items.ToArray(),value.Unresolved.ToArray(),value.HasAdditionalRewards,value.DataVersion),options);
    }
    public const int MaximumEntryBytes=32*1024*1024;
    private const int MaximumFiles=2048;
    private const long MaximumDiskBytes=256L*1024*1024;
    private static readonly JsonSerializerOptions JsonOptions=new(){MaxDepth=64,Converters={new RewardConverter()}};
    private readonly string root;
    private readonly SemaphoreSlim io=new(1);
    private readonly CancellationTokenSource lifetime=new();
    private readonly object gate=new();
    private Task writes=Task.CompletedTask;
    private Task reads=Task.CompletedTask;
    private bool disposed;
    private string? error;
    public string? Error=>Volatile.Read(ref error);
    public DataCache(string directory)=>root=Path.GetFullPath(directory);
    private string FileName(string family,string scope,string version)
    {
        if(string.IsNullOrWhiteSpace(family) || family.Length>40 || scope.Length>2048 || version.Length>4096)throw new ArgumentException("Invalid cache identity.");
        var hash=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{"1",family,scope,version})));
        return Path.Combine(root,hash+".json");
    }
    public Task<CachedValue<T>?> ReadAsync<T>(string family,string scope,string version,CancellationToken token=default)
    {
        lock(gate)
        {
        if(disposed)return Task.FromResult<CachedValue<T>?>(null);
        var barrier=writes;
        var pending=Task.Run(async()=>
        {
        try
        {
            using var linked=CancellationTokenSource.CreateLinkedTokenSource(token,lifetime.Token);
            await barrier.WaitAsync(linked.Token).ConfigureAwait(false);
            var path=FileName(family,scope,version);
            await io.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                if(!File.Exists(path) || new FileInfo(path).Length is <=0 or >MaximumEntryBytes)return null;
                var bytes=await File.ReadAllBytesAsync(path,linked.Token).ConfigureAwait(false);
                var envelope=JsonSerializer.Deserialize<Envelope>(bytes,JsonOptions);
                if(envelope is not {Schema:1} || envelope.Family!=family || envelope.Scope!=scope || envelope.Version!=version)return null;
                var value=envelope.Value.Deserialize<T>(JsonOptions);
                return value is null ? null:new CachedValue<T>(value,envelope.RecordedAt);
            }
            finally{io.Release();}
        }
        catch(OperationCanceledException){return null;}
        catch(Exception failure){Volatile.Write(ref error,"Cache local indisponible : "+failure.Message);return null;}
        });
        reads=Task.WhenAll(reads,pending);return pending;
        }
    }
    public Task<bool> WriteAsync<T>(string family,string scope,string version,T snapshot,DateTimeOffset recordedAt)
    {
        lock(gate)
        {
            if(disposed)return Task.FromResult(false);
            var previous=writes;
            var pending=Task.Run(async()=>
            {
                string? temporary=null;
                try
                {
                    await previous.ConfigureAwait(false);
                    var path=FileName(family,scope,version);var token=lifetime.Token;
                    await io.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        var value=JsonSerializer.SerializeToElement(snapshot,JsonOptions);
                        var bytes=JsonSerializer.SerializeToUtf8Bytes(new Envelope(1,family,scope,version,recordedAt,value),JsonOptions);
                        if(bytes.Length>MaximumEntryBytes)throw new InvalidDataException("Cache snapshot exceeds size limit.");
                        Directory.CreateDirectory(root);temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
                        await File.WriteAllBytesAsync(temporary,bytes,token).ConfigureAwait(false);
                        File.Move(temporary,path,true);temporary=null;
                        Trim();Volatile.Write(ref error,null);return true;
                    }
                    finally{io.Release();}
                }
                catch(Exception failure){if(failure is not OperationCanceledException)Volatile.Write(ref error,"Cache local non sauvegardé : "+failure.Message);return false;}
                finally{if(temporary is not null)try{File.Delete(temporary);}catch(Exception cleanup) when(cleanup is IOException or UnauthorizedAccessException){} }
            });
            writes=pending;return pending;
        }
    }
    // 0.145: the cache had reached its 256 MiB limit (one 4.8 MB quest file per plugin version, old catalogue shapes) and the
    // oldest-first rule then deleted the catalogues and the Garland sources, which were read again each time. Now: only the
    // newest file of each family + scope is kept (a new version replaces the old one); « catalog-* » files are never removed
    // by the file count; other entries (sources, wiki…) keep up to MaximumFiles; the byte limit removes small entries first.
    private void Trim()
    {
        var files=new DirectoryInfo(root).EnumerateFiles("*.json").Where(file=>file.Name.Length==69 && file.Name[..64].All(Uri.IsHexDigit))
            .OrderByDescending(file=>file.LastWriteTimeUtc).Select(file=>(File:file,Identity:Identity(file))).ToArray();
        var seen=new HashSet<(string,string)>();var kept=new List<(FileInfo File,bool Catalogue)>();
        foreach(var (file,identity) in files)
        {
            if(identity is { } key && !seen.Add(key)){Delete(file);continue;}
            kept.Add((file,identity?.Family.StartsWith("catalog-",StringComparison.Ordinal)==true));
        }
        var others=0;long bytes=kept.Where(entry=>entry.Catalogue).Sum(entry=>entry.File.Length);
        foreach(var (file,catalogue) in kept)
        {
            if(catalogue)continue;
            bytes+=file.Length;
            if(++others>MaximumFiles || bytes>MaximumDiskBytes)Delete(file);
        }
    }
    private static void Delete(FileInfo file){try{file.Delete();}catch(Exception error) when(error is IOException or UnauthorizedAccessException){}}
    // Family and scope from the file header ({"Schema":1,"Family":"…","Scope":"…",…}); null when unreadable (then only the limits apply).
    private static (string Family,string Scope)? Identity(FileInfo file)
    {
        try
        {
            using var stream=file.OpenRead();var buffer=new byte[4096];var read=stream.Read(buffer,0,buffer.Length);
            var head=System.Text.Encoding.UTF8.GetString(buffer,0,read);
            var family=System.Text.RegularExpressions.Regex.Match(head,@"""Family"":""((?:[^""\\]|\\.)*)""");
            var scope=System.Text.RegularExpressions.Regex.Match(head,@"""Scope"":""((?:[^""\\]|\\.)*)""");
            return family.Success && scope.Success ? (family.Groups[1].Value,scope.Groups[1].Value):null;
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){return null;}
    }
    public Task FlushAsync(){lock(gate)return writes;}
    public Task<bool> InvalidateAsync(string family,string scope,string version)=>WriteAsync<object?>(family,scope,version,null,DateTimeOffset.MinValue);
    public void Dispose()
    {
        Task pending;lock(gate){if(disposed)return;disposed=true;pending=Task.WhenAll(writes,reads);}
        try{if(!pending.Wait(TimeSpan.FromSeconds(2)))lifetime.Cancel();}catch(AggregateException){lifetime.Cancel();}
        lifetime.Cancel();
        _=pending.ContinueWith(_=>{io.Dispose();lifetime.Dispose();},TaskScheduler.Default);
    }
}
