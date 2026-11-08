using System.IO.Compression;

namespace TenshoAssistant;

/// <summary>
/// X-76 step 3 (0.945, user request): a zip copy of a folder taken just before it is emptied (cache, discoveries), in the plugin's Backups folder
/// (backup-yyyyMMdd-HHmmss-label.zip). Files named in <c>skip</c> (the GitHub token) are never copied; only the newest <see cref="Keep"/> copies of a
/// label are kept.
/// </summary>
public static class BackupZip
{
    public const int Keep=5;

    public static string? Create(string source,string backups,string label,DateTimeOffset at,IReadOnlySet<string>? skip=null)
    {
        if(!Directory.Exists(source))return null;
        Directory.CreateDirectory(backups);
        var path=Path.Combine(backups,$"backup-{at.ToLocalTime():yyyyMMdd-HHmmss}-{label}.zip");
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create))
                foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories))
                {
                    if(skip?.Contains(Path.GetFileName(file))==true || file.EndsWith(".tmp",StringComparison.Ordinal))continue;
                    zip.CreateEntryFromFile(file,Path.GetRelativePath(source,file).Replace('\\','/'),CompressionLevel.Optimal);
                }
            File.Move(temporary,path,true);
        }
        finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}}
        foreach(var old in Directory.GetFiles(backups,$"backup-*-{label}.zip").OrderByDescending(name=>name,StringComparer.Ordinal).Skip(Keep))
            try{File.Delete(old);}catch(IOException){}
        return path;
    }
}
