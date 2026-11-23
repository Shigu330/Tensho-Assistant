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
    public static int Clean(string backups,CancellationToken cancel=default)=>Clean(backups,out _,cancel);

    /// <summary>
    /// X-851 (review 0.1854, M1): a zip damaged for good (not a zip, a broken entry, an unknown compression method) is left as it is and counted
    /// in <paramref name="damaged"/>: nothing can read it, it does not stop the zips after it, and it does not keep the pass from ending (before,
    /// it kept the mark away for ever and every « cache » zip was read again at each load). Only a zip held by another program (IOException,
    /// UnauthorizedAccessException) leaves the pass to the next load.
    /// </summary>
    public static int Clean(string backups,out int damaged,CancellationToken cancel=default)=>Clean(backups,out damaged,out _,cancel);

    /// <summary>
    /// X-854 (review 0.1858, M1): a zip whose damage shows only when it is copied (an entry broken after the head the first reading takes, or an
    /// entry that is not .json) no longer counts as damaged as a whole, keeping its retired copies for ever: the zip is written again without
    /// them, and the entries that cannot be read are left out (nothing could read them), counted in <paramref name="dropped"/>.
    /// <paramref name="damaged"/> counts the zips left as they are because nothing in them could be read (not a zip, entries unreadable from
    /// their start and no retired copy to remove).
    /// </summary>
    public static int Clean(string backups,out int damaged,out int dropped,CancellationToken cancel=default)
    {
        damaged=0;dropped=0;
        if(!Directory.Exists(backups))return 0;
        var mark=Path.Combine(backups,MarkName);
        if(File.Exists(mark))return 0;
        int cleaned=0;var whole=true;
        foreach(var zip in Directory.GetFiles(backups,"backup-*-cache.zip"))
        {
            cancel.ThrowIfCancellationRequested();
            try
            {
                if(CleanOne(zip,out var broken,cancel)){cleaned++;dropped+=broken;}
                else if(broken>0)damaged++;
            }
            catch(Exception error) when(error is InvalidDataException or NotSupportedException){damaged++;}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException){whole=false;}
        }
        if(whole)
            try{File.WriteAllText(mark,MarkText);}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException){}
        return cleaned;
    }

    /// <summary>True when the zip was written again without its retired entries; its time is kept (the five copies kept are the newest).
    /// Through a passing file named as BackupZip's (BackupZip.Leftover: removed by its sweep when a crash cuts the copy short).</summary>
    public static bool CleanOne(string zip,CancellationToken cancel=default)=>CleanOne(zip,out _,cancel);

    /// <summary>
    /// X-854 (review 0.1858, M1): as <see cref="CleanOne(string,CancellationToken)"/>; an entry that cannot be read (its head, or its whole
    /// content when copied: broken data, an unknown compression method, fewer bytes than its size says) no longer stops the copy: it is left
    /// out of the zip written again (nothing can read it), counted in <paramref name="broken"/>. With no retired entry the zip is left as it is,
    /// and <paramref name="broken"/> counts the .json entries whose head could not be read. A zip that cannot be opened still throws.
    /// </summary>
    public static bool CleanOne(string zip,out int broken,CancellationToken cancel=default)
    {
        broken=0;
        var retired=new HashSet<string>(StringComparer.Ordinal);
        var unreadable=new HashSet<string>(StringComparer.Ordinal);
        using(var archive=ZipFile.OpenRead(zip))
            foreach(var entry in archive.Entries)
            {
                cancel.ThrowIfCancellationRequested();
                if(!entry.FullName.EndsWith(".json",StringComparison.OrdinalIgnoreCase))continue;
                try
                {
                    using var stream=entry.Open();
                    if(DataCache.IsRetired(DataCache.IdentityOf(stream)))retired.Add(entry.FullName);
                }
                catch(Exception error) when(error is InvalidDataException or NotSupportedException){unreadable.Add(entry.FullName);}
            }
        broken=unreadable.Count;
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
                    if(retired.Contains(entry.FullName) || unreadable.Contains(entry.FullName))continue;
                    // X-854: read whole before its copy is made, so that an entry broken further in leaves nothing half written.
                    using var content=Content(entry);
                    if(content is null){broken++;continue;}
                    var copy=target.CreateEntry(entry.FullName,CompressionLevel.Optimal);
                    copy.LastWriteTime=entry.LastWriteTime;
                    using var to=copy.Open();
                    content.CopyTo(to);
                }
            File.SetLastWriteTimeUtc(temporary,written);
            AtomicFile.OverwriteWhenFree(temporary,zip);
        }
        finally{AtomicFile.DeleteWhenFree(temporary);}
        return true;
    }

    /// <summary>X-854: an entry's whole content, or null when it cannot be read (broken data, an unknown method, fewer bytes than its size).
    /// X-856 (review 0.1860, M1): or when its bytes do not match the entry's CRC-32 (.NET's reader does not check it): an entry changed in
    /// silence, at the right length, is left out instead of being copied with a new, valid CRC that would hide its damage from any zip tool.</summary>
    private static MemoryStream? Content(ZipArchiveEntry entry)
    {
        try
        {
            var content=new MemoryStream();
            using(var from=entry.Open())from.CopyTo(content);
            if(content.Length!=entry.Length || Crc32(content.GetBuffer().AsSpan(0,(int)content.Length))!=entry.Crc32)return null;
            content.Position=0;
            return content;
        }
        catch(Exception error) when(error is InvalidDataException or NotSupportedException){return null;}
    }

    // X-856: the zip format's CRC-32 (polynomial 0xEDB88320, reflected, as ZipArchiveEntry.Crc32 states it).
    private static readonly uint[] CrcTable=Enumerable.Range(0,256).Select(index=>
    {
        var value=(uint)index;
        for(var bit=0;bit<8;bit++)value=(value&1)!=0 ? 0xEDB88320u^(value>>1):value>>1;
        return value;
    }).ToArray();

    internal static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var crc=0xFFFFFFFFu;
        foreach(var value in bytes)crc=CrcTable[(crc^value)&0xFF]^(crc>>8);
        return ~crc;
    }
}
