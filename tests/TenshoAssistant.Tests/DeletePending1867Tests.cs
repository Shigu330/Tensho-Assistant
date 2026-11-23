using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// 0.1867 (X-862, review 0.1866 M2): since 0.1865 a lock file « delete pending » is « not yet » while the client waits, but the last open,
// past the wait, let its access denied through raw. A caller that tells « busy » from « failed » took it for a hard failure: the bank set
// its failed load and refused every write until a reload. Behaviour tests: the lock left « delete pending » past the wait gives the
// caller's busy exception; the bank says « busy », then loads and saves once the file is let go; a read-only lock still raises access denied.
internal static class DeletePending1867Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    [DllImport("kernel32.dll",SetLastError=true,CharSet=CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string name,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
    [DllImport("kernel32.dll",SetLastError=true)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle,int infoClass,ref byte info,int size);
    private const uint Delete=0x10000;
    private const uint ShareAll=7;
    private const uint OpenExisting=3;
    private const int FileDispositionInfo=4;

    // The file opened with the right to delete it and marked deleted the classic way: it stays « delete pending » while this handle lives.
    private static SafeFileHandle DeletePending(string path)
    {
        var handle=CreateFileW(path,Delete,ShareAll,IntPtr.Zero,OpenExisting,0,IntPtr.Zero);
        if(handle.IsInvalid)throw new Exception("open for delete failed: "+Marshal.GetLastPInvokeError());
        byte yes=1;
        if(!SetFileInformationByHandle(handle,FileDispositionInfo,ref yes,1)){handle.Dispose();throw new Exception("mark deleted failed: "+Marshal.GetLastPInvokeError());}
        return handle;
    }

    private static string Folder()=>Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),"tensho-1867-"+Guid.NewGuid().ToString("N"))).FullName;

    private static void Clean(string folder){try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Delete pending 1867: a lock still « delete pending » past the wait gives the caller's busy exception, not a raw access denied",() =>
        {
            var folder=Folder();
            try
            {
                var path=Path.Combine(folder,"1.lock");
                File.WriteAllText(path,"");
                using(DeletePending(path))
                {
                    Check(FairFileLock.DeletePending(Path.GetFullPath(path)),"a « delete pending » file is told apart");
                    Exception? raised=null;
                    try{using(FairFileLock.Hold(path,200,error=>new TimeoutException("busy",error))){}}
                    catch(Exception error){raised=error;}
                    Check(raised is TimeoutException { InnerException: IOException { InnerException: UnauthorizedAccessException } },
                        "busy, with Windows' refusal inside: "+raised?.GetType().Name+" / "+raised?.InnerException?.GetType().Name);
                }
            }
            finally{Clean(folder);}
        }),
        ("Delete pending 1867: the bank's lock « delete pending » past the wait: the bank says busy (no failed load), then loads and saves once it is let go",() =>
        {
            var root=Folder();
            try
            {
                var store=new PerCharacterBankStore(Path.Combine(root,"InventoryBank"),Path.Combine(root,"InventoryBank.json"),lockWaitMs:150);
                File.WriteAllText(store.LockPath,"");
                using(DeletePending(store.LockPath))
                {
                    Exception? raised=null;
                    try{store.Load();}catch(Exception error){raised=error;}
                    Check(raised is BankLockBusyException,"the bank says its lock is busy: "+raised?.GetType().Name+": "+raised?.Message);
                }
                Check(store.Load().Length==0,"loads once the file is let go");
                Exception? refused=null;
                try{store.Save([]);}catch(Exception error){refused=error;}
                Check(refused is null,"writes are not refused until a reload: "+refused?.Message);
            }
            finally{Clean(root);}
        }),
        ("Delete pending 1867: a read-only lock is not taken for « delete pending »: access denied still raised past the wait",() =>
        {
            var folder=Folder();
            var path=Path.Combine(folder,"2.lock");
            try
            {
                File.WriteAllText(path,"");
                File.SetAttributes(path,FileAttributes.ReadOnly);
                Check(!FairFileLock.DeletePending(Path.GetFullPath(path)),"a read-only file is there");
                Exception? raised=null;
                try{using(FairFileLock.Hold(path,150,error=>new TimeoutException("busy",error))){}}
                catch(Exception error){raised=error;}
                Check(raised is UnauthorizedAccessException,"access denied raised: "+raised?.GetType().Name);
            }
            finally
            {
                try{File.SetAttributes(path,FileAttributes.Normal);}catch(IOException){}
                Clean(folder);
            }
        }),
    ];
}
