namespace TenshoAssistant;

/// <summary>
/// X-310 (0.1309): the lock file shared by the clients of one profile, fair to a client that waits. The lock itself is the file opened without
/// sharing (as before); a client whose first try fails leaves a waiting mark beside it (« name.wait-guid.lock », deleted when it gets the lock or gives
/// up, and by Windows if the process ends: delete on close), and a client about to take the lock first lets the marked clients go. Before, the
/// client that saved in a row released the lock and took it again within microseconds while the other tried only every 50 ms: it could wait past
/// its limit and its save was refused (CI run 43546325155: « Bank save failed: The bank lock is held by another program »).
/// </summary>
public static class FairFileLock
{
    public const int PollMs=50;
    /// <summary>A waiting mark older than this is ignored (a client stopped without closing it on a disk that keeps it).</summary>
    public static readonly TimeSpan StaleMark=TimeSpan.FromSeconds(60);
    private const string MarkInfix=".wait-";
    // Ends in .lock: the link to the desktop application sends no change for it (LocalLink.ChangeCategory), like the lock itself.
    private const string MarkEnd=".lock";

    /// <summary>
    /// The lock held (dispose to release): the other clients' waiting marks let through first (half the wait at most), then tried every
    /// <see cref="PollMs"/> ms up to <paramref name="waitMs"/>, with this client's own mark while it waits; past it, <paramref name="busy"/>'s exception.
    /// </summary>
    public static FileStream Hold(string path,int waitMs,Func<IOException,Exception> busy)
    {
        var full=Path.GetFullPath(path);
        var folder=Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(folder);
        var start=Environment.TickCount64;
        var until=start+Math.Max(0,waitMs);
        var deferUntil=start+Math.Max(0,waitMs)/2;
        while(Environment.TickCount64<deferUntil && OthersWaiting(full))Thread.Sleep(PollMs);
        FileStream? mark=null;
        try
        {
            while(true)
            {
                try{return new FileStream(full,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
                catch(IOException) when(Environment.TickCount64<until)
                {
                    mark??=Mark(full);
                    Thread.Sleep(PollMs);
                }
                catch(IOException error){throw busy(error);}
            }
        }
        finally{mark?.Dispose();}
    }

    /// <summary>A waiting mark younger than <see cref="StaleMark"/> beside the lock (called only while this client has none).</summary>
    public static bool OthersWaiting(string path)
    {
        var full=Path.GetFullPath(path);
        string[] marks;
        try{marks=Directory.GetFiles(Path.GetDirectoryName(full)!,Path.GetFileName(full)+MarkInfix+"*"+MarkEnd);}
        catch(IOException){return false;}
        catch(UnauthorizedAccessException){return false;}
        var now=DateTime.UtcNow;
        foreach(var mark in marks)
        {
            try{if(now-File.GetCreationTimeUtc(mark)<StaleMark)return true;}
            catch(IOException){}
            catch(UnauthorizedAccessException){}
        }
        return false;
    }

    // The mark stays open while this client waits; deleted on close (and by Windows when the process ends). A mark that cannot be made: the
    // client waits as before, unmarked (never a failed save because of the mark).
    private static FileStream? Mark(string full)
    {
        try
        {
            return new FileStream(full+MarkInfix+Guid.NewGuid().ToString("N")+MarkEnd,FileMode.CreateNew,FileAccess.Write,
                FileShare.ReadWrite|FileShare.Delete,1,FileOptions.DeleteOnClose);
        }
        catch(IOException){return null;}
        catch(UnauthorizedAccessException){return null;}
    }
}
