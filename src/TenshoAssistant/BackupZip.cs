using System.IO.Compression;

namespace TenshoAssistant;

/// <summary>
/// X-76 step 3 (0.945, user request): a zip copy of a folder taken just before it is emptied (cache, discoveries), in the plugin's Backups folder
/// (backup-yyyyMMdd-HHmmss-label.zip). Files named in <c>skip</c> (the GitHub token) are never copied; only the newest <see cref="Keep"/> copies of a
/// label are kept.
/// X-169 (0.1141, GPT audit R05): two copies in the same second no longer share a name (the second wrote over the first): a name already taken gets
/// « -2 », « -3 »… before the label, and the move never writes over a file. The copies kept are the newest by the time they were written, not by
/// name (a name is local time: it goes back an hour when summer time ends, and « -2 » sorts before the first copy).
/// </summary>
public static class BackupZip
{
    public const int Keep=5;
    private const int MaxSameSecond=1000;

    public static string? Create(string source,string backups,string label,DateTimeOffset at,IReadOnlySet<string>? skip=null)
    {
        if(!Directory.Exists(source))return null;
        Directory.CreateDirectory(backups);
        var stamp=$"backup-{at.ToLocalTime():yyyyMMdd-HHmmss}";
        var temporary=Path.Combine(backups,stamp+"-"+label+".zip."+Guid.NewGuid().ToString("N")+".tmp");
        string? path=null;
        try
        {
            using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create))
                foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories))
                {
                    if(skip?.Contains(Path.GetFileName(file))==true || file.EndsWith(".tmp",StringComparison.Ordinal))continue;
                    zip.CreateEntryFromFile(file,Path.GetRelativePath(source,file).Replace('\\','/'),CompressionLevel.Optimal);
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
}
