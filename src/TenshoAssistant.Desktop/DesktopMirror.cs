using TenshoAssistant.Link;

namespace TenshoAssistant.Desktop;

/// <summary>
/// X-119 step 2, part 2 (0.1131): the application reads the plugin's saved data from its own copy, never from the plugin's folder (some
/// stores write their file back, e.g. the bank splits its former shared file): for a category of the link (LocalLinkProtocol.Categories),
/// its file « &lt;Stem&gt;.json » and its folder « &lt;Stem&gt;\*.json » of pluginConfigs\TenshoAssistant are copied into
/// %LOCALAPPDATA%\TenshoAssistant\Desktop\mirror. Each file is read shared (the plugin may be writing it), bounded, written to a temporary
/// file with a unique name and moved over the copy, so a store reading the mirror never sees half a file; a file gone from the plugin's
/// folder goes from the copy. The plugin's folder is only read. Pure (System.IO), any thread.
/// </summary>
public static class DesktopMirror
{
    public const long MaxFile=64L<<20;
    public const int MaxFiles=2000;

    /// <summary>X-119 (0.1152): the discoveries' database is large and saved every minute while playing: copied again at most this often.</summary>
    public static readonly TimeSpan DiscoveriesEvery=TimeSpan.FromMinutes(5);

    /// <summary>
    /// X-119 (0.1152): the categories of a burst of notices to copy now (the application's, once each); the discoveries wait for their turn, a notice
    /// in between is kept (waiting) and copied when the turn comes, never lost.
    /// </summary>
    public static List<string> Due(IEnumerable<string> asked,DateTime now,ref DateTime next,ref bool waiting)
    {
        var due=asked.Where(LocalLinkProtocol.DesktopCategories.Contains).Distinct().ToList();
        if(due.Remove(LocalLinkProtocol.DiscoveriesCategory))waiting=true;
        if(waiting && now>=next){waiting=false;next=now+DiscoveriesEvery;due.Add(LocalLinkProtocol.DiscoveriesCategory);}
        return due;
    }

    /// <summary>The stems (file or folder names) of a category, from the link's closed list.</summary>
    public static IReadOnlyList<string> Stems(string category)
        => [.. LocalLinkProtocol.Categories.Where(pair=>string.Equals(pair.Value,category,StringComparison.Ordinal)).Select(pair=>pair.Key)];

    /// <summary>Copies a category into the mirror; the number of files copied and the problems met (English, for the log).</summary>
    public static (int Copied,IReadOnlyList<string> Problems) Copy(string source,string mirror,string category)
    {
        var problems=new List<string>();var copied=0;
        var from=Path.GetFullPath(source);var to=Path.GetFullPath(mirror);
        if(IsInside(to,from) || IsInside(from,to))return (0,["the mirror and the plugin's folder overlap: nothing copied"]);
        foreach(var stem in Stems(category))
        {
            var file=Path.Combine(from,stem+".json");
            if(File.Exists(file)){if(One(file,Path.Combine(to,stem+".json"),problems))copied++;}
            else Remove(Path.Combine(to,stem+".json"),problems);
            var folder=Path.Combine(from,stem);var target=Path.Combine(to,stem);
            if(!Directory.Exists(folder)){if(Directory.Exists(target))Clear(target,[],problems);continue;}
            string[] files;
            try{files=Directory.GetFiles(folder,"*.json",SearchOption.TopDirectoryOnly);}
            catch(Exception error) when (error is IOException or UnauthorizedAccessException){problems.Add($"{stem}: folder not read ({error.GetType().Name})");continue;}
            if(files.Length>MaxFiles){problems.Add($"{stem}: {files.Length} files, more than {MaxFiles}: not copied");continue;}
            var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var path in files)
            {
                var name=Path.GetFileName(path);names.Add(name);
                if(One(path,Path.Combine(target,name),problems))copied++;
            }
            Clear(target,names,problems);
        }
        return (copied,problems);
    }

    private static bool One(string source,string target,List<string> problems)
    {
        var name=Path.GetFileName(source);
        string? temporary=null;
        try
        {
            using var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
            if(input.Length>MaxFile){problems.Add($"{name}: larger than {MaxFile>>20} MB, not copied");return false;}
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            temporary=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                var buffer=new byte[81920];long total=0;int read;
                while((read=input.Read(buffer,0,buffer.Length))>0)
                {
                    total+=read;
                    if(total>MaxFile){problems.Add($"{name}: grew past {MaxFile>>20} MB while read, not copied");return false;}
                    output.Write(buffer,0,read);
                }
            }
            File.Move(temporary,target,true);
            temporary=null;
            return true;
        }
        catch(Exception error) when (error is IOException or UnauthorizedAccessException){problems.Add($"{name}: not copied ({error.GetType().Name})");return false;}
        finally
        {
            if(temporary is not null)try{File.Delete(temporary);}catch(Exception error) when (error is IOException or UnauthorizedAccessException){}
        }
    }

    private static void Remove(string path,List<string> problems)
    {
        try{if(File.Exists(path))File.Delete(path);}
        catch(Exception error) when (error is IOException or UnauthorizedAccessException){problems.Add($"{Path.GetFileName(path)}: old copy not removed ({error.GetType().Name})");}
    }

    // The copies of files gone from the plugin's folder go (only *.json: a store's own lock or temporary files are left alone).
    private static void Clear(string folder,HashSet<string> keep,List<string> problems)
    {
        if(!Directory.Exists(folder))return;
        foreach(var path in Directory.GetFiles(folder,"*.json",SearchOption.TopDirectoryOnly))
            if(!keep.Contains(Path.GetFileName(path)))Remove(path,problems);
    }

    private static bool IsInside(string path,string folder)
    {
        var root=folder.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
        return (path.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar).StartsWith(root,StringComparison.OrdinalIgnoreCase);
    }
}
