namespace TenshoAssistant;

/// <summary>
/// X-310 (0.1309): the lock file shared by the clients of one profile, fair to the clients that wait: first come, first served. The lock
/// itself is the file opened without sharing (as before). A client that finds the lock taken, or other clients waiting, leaves a waiting
/// mark beside it (« name.wait-TICKS-guid.lock »: its arrival, in UTC ticks; deleted when it gets the lock or gives up, and by Windows if the
/// process ends: delete on close; a mark older than <see cref="StaleMark"/> is ignored), and tries the lock only once no older mark is left.
/// Before, the client that saved in a row released the lock and took it again within microseconds while the other tried only every 50 ms:
/// it could wait past its limit and its save was refused (CI run 43546325155: « Bank save failed: The bank lock is held by another program »).
/// </summary>
public static class FairFileLock
{
    public const int PollMs=20;
    /// <summary>A waiting mark older than this is ignored (a client stopped without closing it on a disk that keeps it).</summary>
    public static readonly TimeSpan StaleMark=TimeSpan.FromSeconds(60);
    private const string MarkInfix=".wait-";
    // Ends in .lock: the link to the desktop application sends no change for it (LocalLink.ChangeCategory), like the lock itself.
    private const string MarkEnd=".lock";

    /// <summary>
    /// The lock held (dispose to release). No client waiting: tried at once. Otherwise this client takes its place in the line (its mark) and
    /// tries every <see cref="PollMs"/> ms once the clients before it are served; past half its wait, or without a mark of its own, it tries
    /// anyway (a mark never makes it refuse a free lock); past <paramref name="waitMs"/>, <paramref name="busy"/>'s exception.
    /// X-171 (0.1333): <paramref name="cancel"/> ends the wait (OperationCanceledException), so a plugin that unloads never sleeps on a lock.
    /// </summary>
    public static FileStream Hold(string path,int waitMs,Func<IOException,Exception> busy,CancellationToken cancel=default)
    {
        cancel.ThrowIfCancellationRequested();
        var full=Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var start=Environment.TickCount64;
        var until=start+Math.Max(0,waitMs);
        var lineUntil=start+Math.Max(0,waitMs)/2;
        if(Waiting(full,null).Count==0 && TryOpen(full) is { } quick)return quick;
        var mark=Mark(full,out var own);
        try
        {
            while(true)
            {
                var served=own is null || Environment.TickCount64>=lineUntil || Waiting(full,own).Count==0;
                if(served && TryOpen(full) is { } held)return held;
                if(Environment.TickCount64>=until)
                {
                    try{return new FileStream(full,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
                    catch(IOException error){throw busy(error);}
                    // X-862 (0.1867): still « delete pending » past the wait: busy too (the caller tries again, as for a held lock), never a
                    // hard failure (the bank would refuse its writes until a reload). A file that is there and refused (read only, rights)
                    // still raises access denied.
                    catch(UnauthorizedAccessException error) when(DeletePending(full))
                    {throw busy(new IOException("The lock file is still being deleted by another program (access denied past the wait).",error));}
                }
                if(!cancel.CanBeCanceled)Thread.Sleep(PollMs);
                else if(cancel.WaitHandle.WaitOne(PollMs))cancel.ThrowIfCancellationRequested();
            }
        }
        finally{mark?.Dispose();}
    }

    /// <summary>The waiting marks younger than <see cref="StaleMark"/> that came before <paramref name="own"/> (every one when null), oldest first.</summary>
    public static IReadOnlyList<string> Waiting(string path,string? own)
    {
        var full=Path.GetFullPath(path);
        var prefix=Path.GetFileName(full)+MarkInfix;
        string[] marks;
        try{marks=Directory.GetFiles(Path.GetDirectoryName(full)!,prefix+"*"+MarkEnd);}
        catch(IOException){return [];}
        catch(UnauthorizedAccessException){return [];}
        var ownName=own is null ? null:Path.GetFileName(own);
        var now=DateTime.UtcNow;
        var found=new List<string>();
        foreach(var mark in marks)
        {
            var name=Path.GetFileName(mark);
            if(ownName is not null && string.CompareOrdinal(name,ownName)>=0)continue;
            try{if(now-File.GetCreationTimeUtc(mark)<StaleMark)found.Add(name);}
            catch(IOException){}
            catch(UnauthorizedAccessException){}
        }
        found.Sort(StringComparer.Ordinal);
        return found;
    }

    /// <summary>The name of a waiting mark: the arrival in UTC ticks (fixed width, so the names sort in arrival order), then a guid.</summary>
    public static string MarkName(string path,DateTime arrival,Guid id)=>
        Path.GetFileName(Path.GetFullPath(path))+MarkInfix+arrival.ToUniversalTime().Ticks.ToString("D19",System.Globalization.CultureInfo.InvariantCulture)
        +"-"+id.ToString("N")+MarkEnd;

    // X-859 (0.1865): a lock file being deleted is « delete pending », and opening it is then refused with access denied, not a sharing
    // violation (CI run 45147821072, after the chat sweep deleted a history's lock). X-861 (0.1868): the sweep's own File.Delete is enough:
    // between setting the deletion and closing its handle the file is « delete pending » for an instant, which a client trying every 20 ms
    // can hit (reproduced under load on the old code: 4 failures in 30); another program holding the file (antivirus, indexer) would make it
    // last, not proven. Both are « not yet »: tried again until the wait ends; past it, see Hold (X-862).
    private static FileStream? TryOpen(string full)
    {
        try{return new FileStream(full,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
        catch(IOException){return null;}
        catch(UnauthorizedAccessException){return null;}
    }

    // X-862 (0.1867): a « delete pending » file refuses every open, even to read it with all sharing allowed (access denied; not found once
    // it is gone), while a file really there that refuses the lock's write (read only) still opens that way. Its attributes do not tell:
    // Windows still shows them for a « delete pending » file.
    internal static bool DeletePending(string full)
    {
        try{using(File.OpenHandle(full,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){}return false;}
        catch(FileNotFoundException){return true;}
        catch(UnauthorizedAccessException){return true;}
        catch(IOException){return false;}
    }

    // The mark stays open while this client waits; deleted on close (and by Windows when the process ends). A mark that cannot be made: the
    // client tries every poll as before 0.1309, out of the line (never a failed save because of the mark).
    private static FileStream? Mark(string full,out string? own)
    {
        own=Path.Combine(Path.GetDirectoryName(full)!,MarkName(full,DateTime.UtcNow,Guid.NewGuid()));
        try{return new FileStream(own,FileMode.CreateNew,FileAccess.Write,FileShare.ReadWrite|FileShare.Delete,1,FileOptions.DeleteOnClose);}
        catch(IOException){own=null;return null;}
        catch(UnauthorizedAccessException){own=null;return null;}
    }
}
