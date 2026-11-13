using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TenshoAssistant.Quests;

namespace TenshoAssistant.Caching;

public sealed record CachedValue<T>(T Value,DateTimeOffset RecordedAt);
public sealed record QuestCacheData(ulong Owner,IReadOnlyList<QuestEntry> Entries);
public sealed record CollectionCacheData(ulong Owner,IReadOnlyList<Collections.CollectionEntry> Entries,IReadOnlyList<Collections.CollectionCategoryIssue>? Issues,
    DateTimeOffset? TitlesCheckedAt=null,DateTimeOffset? AchievementsCheckedAt=null);
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
    // X-117 (0.1046): a catalogue copy replaced by one from a new game version is kept in « previous » (one per family + scope)
    // and compared with the new one; the reports live in previous/patch-reports.json.
    private sealed record PatchMove(string Family,string Scope,string Previous,string Current);
    private IReadOnlyList<PatchCatalogReport> patchReports=[];
    /// <summary>X-117 (0.1046): what the last game patch changed in each catalogue copy (newest report per family + scope).</summary>
    public IReadOnlyList<PatchCatalogReport> PatchReports=>Volatile.Read(ref patchReports);
    /// <summary>X-117 (0.1046): called off the framework thread with each new report.</summary>
    public Action<PatchCatalogReport>? PatchReported{get;set;}
    private string PreviousFolder=>Path.Combine(root,"previous");
    private string PatchReportsPath=>Path.Combine(PreviousFolder,"patch-reports.json");
    private bool disposed;
    private string? error;
    public string? Error=>Volatile.Read(ref error);
    public DataCache(string directory)=>root=Path.GetFullPath(directory);
    /// <summary>X-142 part 2 (0.1122): what a purge did: files removed, files that could not be (in use), the copy taken before.</summary>
    public sealed record CacheClearResult(int Removed,int Failed,string? Backup);
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
        catch(Exception failure){Volatile.Write(ref error,"Local cache unavailable: "+failure.Message);return null;}
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
                    List<PatchMove> moved;
                    await io.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        var value=JsonSerializer.SerializeToElement(snapshot,JsonOptions);
                        var bytes=JsonSerializer.SerializeToUtf8Bytes(new Envelope(1,family,scope,version,recordedAt,value),JsonOptions);
                        if(bytes.Length>MaximumEntryBytes)throw new InvalidDataException("Cache snapshot exceeds size limit.");
                        Directory.CreateDirectory(root);temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
                        await File.WriteAllBytesAsync(temporary,bytes,token).ConfigureAwait(false);
                        // X-267 (0.1275): tried again while a scanner holds the new file or the old one an instant (a lost write before).
                        AtomicFile.OverwriteWhenFree(temporary,path);temporary=null;
                        moved=Trim();Volatile.Write(ref error,null);
                    }
                    finally{io.Release();}
                    if(moved.Count>0)Report(moved);
                    return true;
                }
                catch(Exception failure){if(failure is not OperationCanceledException)Volatile.Write(ref error,"Local cache not saved: "+failure.Message);return false;}
                finally{if(temporary is not null)AtomicFile.DeleteWhenFree(temporary);}
            });
            writes=pending;return pending;
        }
    }
    // 0.145: the cache had reached its 256 MiB limit (one 4.8 MB quest file per plugin version, old catalogue shapes) and the
    // oldest-first rule then deleted the catalogues and the Garland sources, which were read again each time. Now: only the
    // newest file of each family + scope is kept (a new version replaces the old one); « catalog-* » files are never removed
    // by the file count; other entries (sources, wiki…) keep up to MaximumFiles; the byte limit removes small entries first.
    // X-117 (0.1046): the newest older copy of a « catalog-* » file from another game version is moved to « previous » instead.
    // X-147 (0.1217, audit A14): measured on a copy of a real cache (2 053 files, 69 MB): each write read the header of every file again, about
    // 150 ms under the disk lock (reads wait behind it). The identity read from a file is now kept while its name, time and size stay the same:
    // only new or changed files are read again. The copies kept in « previous » count in the byte limit.
    private readonly Dictionary<string,(DateTime Written,long Length,(string Family,string Scope,string? Game)? Identity)> identities=new(StringComparer.OrdinalIgnoreCase);
    /// <summary>X-147: how many file headers were read (tests).</summary>
    internal long HeaderReads;
    private (string Family,string Scope,string? Game)? KnownIdentity(FileInfo file,Dictionary<string,(DateTime,long,(string,string,string?)?)> next)
    {
        var written=file.LastWriteTimeUtc;var length=file.Length;
        var identity=identities.TryGetValue(file.Name,out var known) && known.Written==written && known.Length==length ? known.Identity:ReadIdentity(file);
        next[file.Name]=(written,length,identity);
        return identity;
    }
    private (string Family,string Scope,string? Game)? ReadIdentity(FileInfo file){HeaderReads++;return Identity(file);}
    private List<PatchMove> Trim()
    {
        var next=new Dictionary<string,(DateTime,long,(string,string,string?)?)>(StringComparer.OrdinalIgnoreCase);
        var files=new DirectoryInfo(root).EnumerateFiles("*.json").Where(file=>file.Name.Length==69 && file.Name[..64].All(Uri.IsHexDigit))
            .OrderByDescending(file=>file.LastWriteTimeUtc).Select(file=>(File:file,Identity:KnownIdentity(file,next))).ToArray();
        identities.Clear();foreach(var (name,known) in next)identities[name]=known;
        var seen=new Dictionary<(string,string),(string? Game,string Path)>();var kept=new List<(FileInfo File,bool Catalogue)>();
        var moved=new List<PatchMove>();var keptOld=new HashSet<(string,string)>();
        foreach(var (file,identity) in files)
        {
            if(identity is { } id)
            {
                var key=(id.Family,id.Scope);
                if(seen.TryGetValue(key,out var newest))
                {
                    if(id.Family.StartsWith("catalog-",StringComparison.Ordinal) && id.Game is not null && newest.Game is not null && id.Game!=newest.Game
                        && keptOld.Add(key) && KeepPrevious(file,PreviousPath(id.Family,id.Scope)))
                        moved.Add(new(id.Family,id.Scope,PreviousPath(id.Family,id.Scope),newest.Path));
                    else Delete(file);
                    continue;
                }
                seen[key]=(id.Game,file.FullName);
            }
            kept.Add((file,identity?.Family.StartsWith("catalog-",StringComparison.Ordinal)==true));
        }
        var others=0;long bytes=kept.Where(entry=>entry.Catalogue).Sum(entry=>entry.File.Length)+PreviousBytes();
        foreach(var (file,catalogue) in kept)
        {
            if(catalogue)continue;
            bytes+=file.Length;
            if(++others>MaximumFiles || bytes>MaximumDiskBytes)Delete(file);
        }
        return moved;
    }
    private string PreviousPath(string family,string scope)=>
        Path.Combine(PreviousFolder,Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{"previous",family,scope})))+".json");
    private long PreviousBytes()
    {
        try{return Directory.Exists(PreviousFolder) ? new DirectoryInfo(PreviousFolder).EnumerateFiles("*.json").Sum(file=>file.Length):0;}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){return 0;}
    }
    private static bool KeepPrevious(FileInfo file,string path)
    {
        try{Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.Move(file.FullName,path,true);return true;}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){return false;}
    }
    // Off the io lock: both copies are read again (each at most MaximumEntryBytes) and compared once, when the patch is first seen.
    private void Report(List<PatchMove> moved)
    {
        try
        {
            var reports=ReadReports().ToList();
            foreach(var move in moved)
            {
                var before=ReadEnvelope(move.Previous);var after=ReadEnvelope(move.Current);
                if(before is null || after is null)continue;
                var report=PatchCatalogDiff.Compare(move.Family,move.Scope,before.Version,before.Value,after.Version,after.Value,DateTimeOffset.UtcNow);
                reports.RemoveAll(row=>row.Family==report.Family && row.Scope==report.Scope);reports.Add(report);
                Volatile.Write(ref patchReports,reports.ToArray());
                try{PatchReported?.Invoke(report);}catch(Exception){}
            }
            AtomicFile.Write(PatchReportsPath,JsonSerializer.SerializeToUtf8Bytes(reports,JsonOptions));
        }
        catch(Exception failure) when(failure is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {Volatile.Write(ref error,"Patch comparison not saved: "+failure.Message);}
    }
    private static Envelope? ReadEnvelope(string path)
    {
        if(!File.Exists(path) || new FileInfo(path).Length is <=0 or >MaximumEntryBytes)return null;
        return JsonSerializer.Deserialize<Envelope>(File.ReadAllBytes(path),JsonOptions) is {Schema:1} envelope ? envelope:null;
    }
    private IReadOnlyList<PatchCatalogReport> ReadReports()
    {
        var path=PatchReportsPath;
        if(!File.Exists(path) || new FileInfo(path).Length is <=0 or >MaximumEntryBytes)return [];
        return JsonSerializer.Deserialize<List<PatchCatalogReport>>(File.ReadAllBytes(path),JsonOptions) ?? [];
    }
    /// <summary>X-117 (0.1046): the reports kept from the last game patch, read off the framework thread.</summary>
    public Task<IReadOnlyList<PatchCatalogReport>> LoadPatchReportsAsync()=>Task.Run(()=>
    {
        try{var reports=ReadReports();Volatile.Write(ref patchReports,reports);return reports;}
        catch(Exception failure) when(failure is IOException or UnauthorizedAccessException or JsonException)
        {Volatile.Write(ref error,"Patch comparison unreadable: "+failure.Message);return (IReadOnlyList<PatchCatalogReport>)[];}
    });
    private static void Delete(FileInfo file){try{file.Delete();}catch(Exception error) when(error is IOException or UnauthorizedAccessException){}}
    // Family, scope and game version from the file header ({"Schema":1,"Family":"…","Scope":"…","Version":"…",…}); null when unreadable
    // (then only the limits apply).
    private static (string Family,string Scope,string? Game)? Identity(FileInfo file)
    {
        try
        {
            using var stream=file.OpenRead();var buffer=new byte[8192];var read=stream.ReadAtLeast(buffer,buffer.Length,false);
            var head=System.Text.Encoding.UTF8.GetString(buffer,0,read);
            var family=System.Text.RegularExpressions.Regex.Match(head,@"""Family"":""((?:[^""\\]|\\.)*)""");
            var scope=System.Text.RegularExpressions.Regex.Match(head,@"""Scope"":""((?:[^""\\]|\\.)*)""");
            return family.Success && scope.Success ? (family.Groups[1].Value,scope.Groups[1].Value,PatchCatalogDiff.GameVersion(head)):null;
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){return null;}
    }
    public Task FlushAsync(){lock(gate)return writes;}
    /// <summary>X-76 step 3 (0.945): the cache folder.</summary>
    public string Root=>root;
    /// <summary>
    /// X-76 step 3 (0.945): every cached file removed. X-142 part 2 (0.1122, audit A05): the copy and the purge are one operation of the write
    /// queue: after every write asked before, holding the disk lock (no read or write runs meanwhile), the copy is taken (backup, when given),
    /// then every file removed; a write asked after starts once the purge is done, so it is neither lost in the copy nor removed. A copy that
    /// fails removes nothing (its exception is the task's). The files that could not be removed are counted: a partial purge is said.
    /// </summary>
    public Task<CacheClearResult> ClearAsync(Func<string?>? backup=null)
    {
        lock(gate)
        {
            if(disposed)return Task.FromResult(new CacheClearResult(0,0,null));
            var previous=writes;
            var pending=Task.Run(async()=>
            {
                try{await previous.ConfigureAwait(false);}catch(Exception){}
                await io.WaitAsync(lifetime.Token).ConfigureAwait(false);
                try
                {
                    var copy=backup?.Invoke();
                    int removed=0,failed=0;
                    if(Directory.Exists(root))
                        foreach(var file in Directory.GetFiles(root,"*",SearchOption.AllDirectories))
                            try{File.Delete(file);removed++;}catch(IOException){failed++;}catch(UnauthorizedAccessException){failed++;}
                    return new CacheClearResult(removed,failed,copy);
                }
                finally{io.Release();}
            });
            // The writes after it wait for it, never for its failure.
            writes=pending.ContinueWith(static _=>{},CancellationToken.None,TaskContinuationOptions.None,TaskScheduler.Default);
            return pending;
        }
    }
    public Task<bool> InvalidateAsync(string family,string scope,string version)=>WriteAsync<object?>(family,scope,version,null,DateTimeOffset.MinValue);
    public void Dispose()
    {
        Task pending;lock(gate){if(disposed)return;disposed=true;pending=Task.WhenAll(writes,reads);}
        try{if(!pending.Wait(TimeSpan.FromSeconds(2)))lifetime.Cancel();}catch(AggregateException){lifetime.Cancel();}
        lifetime.Cancel();
        _=pending.ContinueWith(_=>{io.Dispose();lifetime.Dispose();},TaskScheduler.Default);
    }
}
