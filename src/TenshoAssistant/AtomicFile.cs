using System.Text;

namespace TenshoAssistant;

/// <summary>
/// X-76 H10 (0.923): one way to write a saved file: a temporary file with a unique name next to it, then moved over the old one, so a crash or
/// a full disk never leaves half a file; the temporary file is deleted in any case (some writers used a fixed « .tmp » name, two writers at
/// once could clash, and a failed write left it behind).
/// </summary>
public static class AtomicFile
{
    public static void Write(string path,byte[] bytes)=>Write(path,bytes,false);

    /// <summary>
    /// X-833 (security review I5): the temporary file is flushed to the disk (Flush(true)) before it is moved over the old one: after a power cut
    /// or a blue screen just after a save, NTFS could keep the rename without the data (a file of the right size holding zeros). With
    /// <paramref name="keep"/> (the player's own data: crafting lists, my tools, beast teams, duty progress, gil and voyage history), the version
    /// replaced is kept as « .bak » (<see cref="BackupOf"/>, File.Replace), which <see cref="ReadKept{T}"/> reads when the file does not.
    /// </summary>
    public static void Write(string path,byte[] bytes,bool keep)
    {
        var folder=Path.GetDirectoryName(path);
        if(!string.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                file.Write(bytes);
                file.Flush(flushToDisk:true);
            }
            if(keep)ReplaceWhenFree(temporary,path,BackupOf(path));
            else OverwriteWhenFree(temporary,path);
        }
        finally{DeleteWhenFree(temporary);}
    }

    /// <summary>X-833: the copy of a kept file's version before the last save.</summary>
    public static string BackupOf(string path)=>path+".bak";

    /// <summary>
    /// X-833: a kept file read by <paramref name="read"/> (which throws on a file it does not accept). When the file is missing while its
    /// « .bak » is there (a replace cut short), or does not read for a reason other than being held by another program (Busy) or gone, its
    /// « .bak » is read instead; the damaged file is first moved aside under a dated name (never written over, never deleted) and
    /// <paramref name="recovered"/> told where (the file's own path when it could not be moved). A « .bak » that does not read either: the
    /// first error is thrown, as before (the caller keeps the file as it is and writes nothing over it).
    /// </summary>
    public static T ReadKept<T>(string path,Func<string,T> read,Action<string,Exception>? recovered=null,DateTimeOffset? now=null)
    {
        var backup=BackupOf(path);
        if(!File.Exists(path) && File.Exists(backup))
        {
            var restored=read(backup);
            recovered?.Invoke(path,new FileNotFoundException("The file was missing; its previous version (.bak) was read.",path));
            return restored;
        }
        try{return read(path);}
        catch(Exception error) when(!Discoveries.DiscoveryFiles.Busy(error) && !Discoveries.DiscoveryFiles.Away(error) && File.Exists(backup))
        {
            T kept;
            try{kept=read(backup);}
            catch(Exception){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(error);throw;}
            var aside=Discoveries.DiscoveryFiles.SetAsideName(path,now ?? DateTimeOffset.UtcNow);
            try{File.Move(path,aside,false);}
            catch(Exception moving) when(moving is IOException or UnauthorizedAccessException){aside=path;}
            recovered?.Invoke(aside,error);
            return kept;
        }
    }

    /// <summary>
    /// X-267 (0.1275, CI of 0.1267 red: « Disk cache survives a new instance… »): a file just written, or the one it replaces, can be held
    /// for an instant by another program (a scanner reading it): the move over the old file is tried again a few times before it is given
    /// up. Only while the new file is still there: a failed attempt changed nothing. « wait » is the test's clock (told the attempt that failed).
    /// </summary>
    public static void OverwriteWhenFree(string from,string to,int attempts=6,Action<int>? wait=null)
    {
        for(var attempt=1;;attempt++)
        {
            try{File.Move(from,to,true);return;}
            catch(Exception error) when(attempt<attempts && error is IOException or UnauthorizedAccessException && File.Exists(from))
            {
                if(wait is not null)wait(attempt);else Thread.Sleep(50*attempt);
            }
        }
    }

    /// <summary>
    /// X-182 (0.1197): a file just written can be held for an instant by another program (a scanner reading it): the move is tried again a
    /// few times before it is given up. Only while the file is there and its destination is not: a destination that exists is never
    /// waited for nor replaced (the caller decides). « wait » is the test's clock (told the attempt that failed).
    /// </summary>
    public static void MoveWhenFree(string from,string to,int attempts=6,Action<int>? wait=null)
    {
        for(var attempt=1;;attempt++)
        {
            try{File.Move(from,to);return;}
            catch(Exception error) when(attempt<attempts && error is IOException or UnauthorizedAccessException && File.Exists(from) && !File.Exists(to))
            {
                if(wait is not null)wait(attempt);else Thread.Sleep(50*attempt);
            }
        }
    }

    /// <summary>
    /// 0.1202 (« Bank 1118 » failed in a full run: « Unable to remove the file to be replaced »): a saved file replaced by its new version, the
    /// old one kept as « backup » (File.Replace), or moved in place when there is none yet; tried again a few times while another program
    /// (a scanner) holds the file an instant. Tried again only while the new version is still there: a failed attempt changed nothing that
    /// counts (or left the old file as the backup and no file in place, which the next attempt fills). « wait » is the test's clock.
    /// </summary>
    public static void ReplaceWhenFree(string from,string to,string? backup,int attempts=6,Action<int>? wait=null)
    {
        for(var attempt=1;;attempt++)
        {
            try
            {
                if(File.Exists(to))File.Replace(from,to,backup,ignoreMetadataErrors:true);
                else File.Move(from,to);
                return;
            }
            catch(Exception error) when(attempt<attempts && error is IOException or UnauthorizedAccessException && File.Exists(from))
            {
                if(wait is not null)wait(attempt);else Thread.Sleep(50*attempt);
            }
        }
    }

    /// <summary>X-182 (0.1197): the same for a file to remove, and never an error: false when it is still there after the last attempt.</summary>
    public static bool DeleteWhenFree(string path,int attempts=6,Action<int>? wait=null)
    {
        for(var attempt=1;;attempt++)
        {
            try{if(File.Exists(path))File.Delete(path);return true;}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException)
            {
                if(attempt>=attempts)return false;
                if(wait is not null)wait(attempt);else Thread.Sleep(50*attempt);
            }
        }
    }

    /// <summary>Text in UTF-8 without a byte order mark (as File.WriteAllText).</summary>
    public static void Write(string path,string text)=>Write(path,new UTF8Encoding(false).GetBytes(text));
    /// <summary>X-833: the same, kept (« .bak »).</summary>
    public static void Write(string path,string text,bool keep)=>Write(path,new UTF8Encoding(false).GetBytes(text),keep);
}
