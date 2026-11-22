using System.IO.Compression;

namespace TenshoAssistant;

/// <summary>
/// X-76 step 3 (0.945, user request): a zip copy of a folder taken just before it is emptied (cache, discoveries), in the plugin's Backups folder
/// (backup-yyyyMMdd-HHmmss-label.zip). Files named in <c>skip</c> (the GitHub token) are never copied; only the newest <see cref="Keep"/> copies of a
/// label are kept.
/// X-169 (0.1141, GPT audit R05): two copies in the same second no longer share a name (the second wrote over the first): a name already taken gets
/// « -2 », « -3 »… before the label, and the move never writes over a file. The copies kept are the newest by the time they were written, not by
/// name (a name is local time: it goes back an hour when summer time ends, and « -2 » sorts before the first copy).
/// X-784 (0.1797, review of 0.1795, I1): a file another handle holds open for writing is read in shared mode (ZipArchive's
/// CreateEntryFromFile asked for FileShare.Read and failed on Discoveries.db, kept open by the plugin since 0.1794: the copy, so the emptying,
/// always refused); <c>read</c> gives the content of a file the caller copies its own way (a database's consistent snapshot), null for the others.
/// X-793 (0.1800, review of 0.1799, M2): a copy cut short by a killed or crashed game left its passing files in Backups for good (the snapshot of
/// the discoveries, a whole copy of the base; a zip half written): <see cref="Sweep"/> removes them once an hour old, at each copy and at load.
/// </summary>
public static class BackupZip
{
    public const int Keep=5;
    private const int MaxSameSecond=1000;

    public static string? Create(string source,string backups,string label,DateTimeOffset at,IReadOnlySet<string>? skip=null,Func<string,Stream?>? read=null)
    {
        if(!Directory.Exists(source))return null;
        Directory.CreateDirectory(backups);
        Sweep(backups,DateTime.UtcNow);   // X-793: what an earlier copy cut short left
        var stamp=$"backup-{at.ToLocalTime():yyyyMMdd-HHmmss}";
        var temporary=Path.Combine(backups,stamp+"-"+label+".zip."+Guid.NewGuid().ToString("N")+".tmp");
        string? path=null;
        try
        {
            using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create))
                foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories))
                {
                    if(skip?.Contains(Path.GetFileName(file))==true || file.EndsWith(".tmp",StringComparison.Ordinal))continue;
                    var entry=zip.CreateEntry(Path.GetRelativePath(source,file).Replace('\\','/'),CompressionLevel.Optimal);
                    try{entry.LastWriteTime=File.GetLastWriteTime(file);}
                    catch(ArgumentOutOfRangeException){}   // outside the zip format's dates (1980 to 2107): its default, as CreateEntryFromFile
                    using var content=read?.Invoke(file) ?? new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite | FileShare.Delete);
                    using var target=entry.Open();
                    content.CopyTo(target);
                }
            for(var number=1;path is null;number++)
            {
                var candidate=Path.Combine(backups,number==1 ? $"{stamp}-{label}.zip":$"{stamp}-{number}-{label}.zip");
                if(File.Exists(candidate))
                {
                    if(number>=MaxSameSecond)throw new IOException("Too many backups in the same second.");
                    continue;
                }
                try{File.Move(temporary,candidate,false);path=candidate;}
                catch(IOException) when(File.Exists(candidate) && number<MaxSameSecond){}   // taken meanwhile (another copy): the next number
            }
        }
        finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}}
        foreach(var old in Directory.GetFiles(backups,$"backup-*-{label}.zip")
                    .OrderByDescending(File.GetLastWriteTimeUtc).ThenByDescending(name=>name,StringComparer.Ordinal).Skip(Keep))
            if(!string.Equals(old,path,StringComparison.OrdinalIgnoreCase))
                try{File.Delete(old);}catch(IOException){}
        return path;
    }

    /// <summary>X-793 (0.1800): a passing file is removed once it is this old; a copy still running (another game client's, on the same
    /// profile) wrote its files later, and its open file refuses the removal anyway.</summary>
    public static readonly TimeSpan LeftoverAge=TimeSpan.FromHours(1);

    private static readonly System.Text.RegularExpressions.Regex LeftoverName=new(
        @"^(snapshot-[0-9a-f]{32}\.db\.tmp|backup-\d{8}-\d{6}-.+\.zip\.[0-9a-f]{32}\.tmp)$",System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>X-793 (0.1800): a name a copy gives its passing files, and only those: SQLite's snapshot of the discoveries
    /// (<c>snapshot-&lt;guid&gt;.db.tmp</c>, DiscoveryDatabase.Snapshot) and the zip being written (<c>backup-…-label.zip.&lt;guid&gt;.tmp</c>).</summary>
    public static bool Leftover(string name)=>LeftoverName.IsMatch(name);

    /// <summary>X-793 (0.1800, review of 0.1799, M2): the passing files a copy cut short left in <paramref name="backups"/> (the game killed or
    /// crashed during it: DeleteOnClose and the finally never ran) removed once older than <see cref="LeftoverAge"/>; a file that cannot be
    /// removed (held open, read-only) stays for the next time. The number removed.</summary>
    public static int Sweep(string backups,DateTime utcNow)
    {
        if(!Directory.Exists(backups))return 0;
        var removed=0;
        foreach(var file in Directory.GetFiles(backups,"*.tmp"))
        {
            if(!Leftover(Path.GetFileName(file)))continue;
            try
            {
                if(utcNow-File.GetLastWriteTimeUtc(file)<LeftoverAge)continue;
                File.Delete(file);
                if(!File.Exists(file))removed++;
            }
            catch(IOException){}
            catch(UnauthorizedAccessException){}
        }
        return removed;
    }
}
