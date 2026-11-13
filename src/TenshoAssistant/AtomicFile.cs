using System.Text;

namespace TenshoAssistant;

/// <summary>
/// X-76 H10 (0.923): one way to write a saved file: a temporary file with a unique name next to it, then moved over the old one, so a crash or
/// a full disk never leaves half a file; the temporary file is deleted in any case (some writers used a fixed « .tmp » name, two writers at
/// once could clash, and a failed write left it behind).
/// </summary>
public static class AtomicFile
{
    public static void Write(string path,byte[] bytes)
    {
        var folder=Path.GetDirectoryName(path);
        if(!string.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            File.WriteAllBytes(temporary,bytes);
            File.Move(temporary,path,true);
        }
        finally
        {
            try{if(File.Exists(temporary))File.Delete(temporary);}
            catch(IOException){}
            catch(UnauthorizedAccessException){}
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
}
