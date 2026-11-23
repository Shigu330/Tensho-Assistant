using System.IO.Compression;

namespace TenshoAssistant.Caching;

/// <summary>
/// X-845 (0.1854, review 0.1844 M1, security review before the alpha): X-836 M1 (0.1840) removed the retired cache copies (the market boards
/// of v1, which kept other players' retainer names) from the cache folder only. The zip copies that « Empty the cache » took before
/// (backup-…-cache.zip in Backups, up to five) still held them. They are written again once without those entries; a zip that holds none is
/// left as it is. The pass ends with a mark in Backups (<see cref="MarkName"/>): every later « cache » zip is made without them
/// (<see cref="DataCache.RetiredFiles"/>), so the pass is never needed again; a zip that could not be read or written (held by another program)
/// leaves no mark, and the pass runs again at the next load.
/// </summary>
public static class RetiredCopies
{
    public const string MarkName="cache-copies-cleaned-v1.txt";
    private const string MarkText="Tensho Assistant: the old zip copies of the cache were checked once and the retired copies (market boards of v1) removed.\n";

    /// <summary>The cache zips written again (0 when none held a retired copy, or the pass was done before).</summary>
    public static int Clean(string backups,CancellationToken cancel=default)
    {
        if(!Directory.Exists(backups))return 0;
        var mark=Path.Combine(backups,MarkName);
        if(File.Exists(mark))return 0;
        int cleaned=0;var whole=true;
        foreach(var zip in Directory.GetFiles(backups,"backup-*-cache.zip"))
        {
            cancel.ThrowIfCancellationRequested();
            try{if(CleanOne(zip,cancel))cleaned++;}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException or InvalidDataException){whole=false;}
        }
        if(whole)
            try{File.WriteAllText(mark,MarkText);}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException){}
        return cleaned;
    }

    /// <summary>True when the zip was written again without its retired entries; its time is kept (the five copies kept are the newest).
    /// Through a passing file named as BackupZip's (BackupZip.Leftover: removed by its sweep when a crash cuts the copy short).</summary>
    public static bool CleanOne(string zip,CancellationToken cancel=default)
    {
        var retired=new HashSet<string>(StringComparer.Ordinal);
        using(var archive=ZipFile.OpenRead(zip))
            foreach(var entry in archive.Entries)
            {
                cancel.ThrowIfCancellationRequested();
                if(!entry.FullName.EndsWith(".json",StringComparison.OrdinalIgnoreCase))continue;
                using var stream=entry.Open();
                if(DataCache.IsRetired(DataCache.IdentityOf(stream)))retired.Add(entry.FullName);
            }
        if(retired.Count==0)return false;
        var written=File.GetLastWriteTimeUtc(zip);
        var temporary=zip+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var source=ZipFile.OpenRead(zip))
            using(var target=ZipFile.Open(temporary,ZipArchiveMode.Create))
                foreach(var entry in source.Entries)
                {
                    cancel.ThrowIfCancellationRequested();
                    if(retired.Contains(entry.FullName))continue;
                    var copy=target.CreateEntry(entry.FullName,CompressionLevel.Optimal);
                    copy.LastWriteTime=entry.LastWriteTime;
                    using var from=entry.Open();
                    using var to=copy.Open();
                    from.CopyTo(to);
                }
            File.SetLastWriteTimeUtc(temporary,written);
            AtomicFile.OverwriteWhenFree(temporary,zip);
        }
        finally{AtomicFile.DeleteWhenFree(temporary);}
        return true;
    }
}
