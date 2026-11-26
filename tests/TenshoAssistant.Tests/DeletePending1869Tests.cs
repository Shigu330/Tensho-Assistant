using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// 0.1869 (X-863, review 0.1868 M1): since 0.1867 a lock that even refuses a read was taken for « delete pending », so a lock really there
// whose rights (ACL) refuse reading and writing was « busy » for ever (the bank tried again every 5 s, « waits for another program to let
// it go »). Behaviour tests: such a lock is not « delete pending »; past the wait it raises access denied; the bank's load fails clearly
// (not « busy ») and its writes are refused; a lock really « delete pending » is still told apart, also when its rights refuse a read.
internal static class DeletePending1869Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    [DllImport("kernel32.dll",SetLastError=true,CharSet=CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string name,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
    [DllImport("kernel32.dll",SetLastError=true)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle,int infoClass,ref byte info,int size);

    // The file opened with the right to delete it and marked deleted the classic way: it stays « delete pending » while this handle lives.
    private static SafeFileHandle MakeDeletePending(string path)
    {
        var handle=CreateFileW(path,0x10000,7,IntPtr.Zero,3,0,IntPtr.Zero);
        if(handle.IsInvalid)throw new Exception("open for delete failed: "+Marshal.GetLastPInvokeError());
        byte yes=1;
        if(!SetFileInformationByHandle(handle,4,ref yes,1)){handle.Dispose();throw new Exception("mark deleted failed: "+Marshal.GetLastPInvokeError());}
        return handle;
    }

    // Everyone refused reading and writing the file's data (the owner keeps the right to change the rights back).
    private static void Refuse(string path,bool refuse)
    {
        if(!OperatingSystem.IsWindows())throw Skip.Because("file rights (ACL) are Windows' own");
        var info=new FileInfo(path);
        var security=info.GetAccessControl(AccessControlSections.Access);
        var rule=new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid,null),FileSystemRights.ReadData|FileSystemRights.WriteData|FileSystemRights.AppendData,AccessControlType.Deny);
        if(refuse)security.AddAccessRule(rule);else security.RemoveAccessRuleSpecific(rule);
        info.SetAccessControl(security);
    }

    private static string Folder()=>Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),"tensho-1869-"+Guid.NewGuid().ToString("N"))).FullName;

    private static void Clean(string folder){try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}

    // A lock whose rights refuse reading and writing, given back (rights restored, folder deleted) at the end.
    private static void WithRefusedLock(string path,Action run)
    {
        File.WriteAllText(path,"");
        Refuse(path,true);
        try
        {
            var read=false;
            try{using(File.OpenRead(path)){}read=true;}catch(UnauthorizedAccessException){}
            Check(!read,"the rights really refuse a read (the case under test)");
            run();
        }
        finally{try{Refuse(path,false);}catch(Exception){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Delete pending 1869: a lock there whose rights refuse reading and writing is not « delete pending »; past the wait, access denied, never busy",() =>
        {
            var folder=Folder();
            try
            {
                var path=Path.Combine(folder,"1.lock");
                WithRefusedLock(path,() =>
                {
                    Check(!FairFileLock.DeletePending(Path.GetFullPath(path)),"a refused lock is there, not « delete pending »");
                    Exception? raised=null;
                    try{using(FairFileLock.Hold(path,150,error=>new TimeoutException("busy",error))){}}
                    catch(Exception error){raised=error;}
                    Check(raised is UnauthorizedAccessException,"access denied raised, not busy: "+raised?.GetType().Name);
                });
            }
            finally{Clean(folder);}
        }),
        ("Delete pending 1869: the bank's lock refused by its rights: a clear failed load (not « busy »), writes refused",() =>
        {
            var root=Folder();
            try
            {
                var store=new PerCharacterBankStore(Path.Combine(root,"InventoryBank"),Path.Combine(root,"InventoryBank.json"),lockWaitMs:150);
                WithRefusedLock(store.LockPath,() =>
                {
                    Exception? raised=null;
                    try{store.Load();}catch(Exception error){raised=error;}
                    Check(raised is UnauthorizedAccessException,"the load fails with access denied, not busy: "+raised?.GetType().Name+": "+raised?.Message);
                    Exception? refused=null;
                    try{store.Save([]);}catch(Exception error){refused=error;}
                    Check(refused is InvalidOperationException,"no write after the failed load: "+refused?.GetType().Name);
                });
            }
            finally{Clean(root);}
        }),
        ("Delete pending 1869: a lock really « delete pending » is still told apart, also when its rights refuse a read; gone counts too",() =>
        {
            var folder=Folder();
            try
            {
                var plain=Path.Combine(folder,"2.lock");
                File.WriteAllText(plain,"");
                Check(!FairFileLock.DeletePending(plain),"a plain file is there");
                Check(FairFileLock.OpenStatus(plain)==0,"a plain file opens to read its attributes");
                using(MakeDeletePending(plain))
                {
                    Check(FairFileLock.DeletePending(plain),"« delete pending » told apart: status "+FairFileLock.OpenStatus(plain).ToString("X8"));
                    Check(FairFileLock.OpenStatus(plain)==0xC0000056,"Windows says STATUS_DELETE_PENDING");
                }
                Check(FairFileLock.DeletePending(plain),"gone once let go: « not yet », the next open creates it");
                var refused=Path.Combine(folder,"3.lock");
                WithRefusedLock(refused,() =>
                {
                    using(MakeDeletePending(refused))Check(FairFileLock.DeletePending(refused),"« delete pending » with refused rights still told apart");
                });
                Check(!File.Exists(refused),"the refused lock is gone once let go");
            }
            finally{Clean(folder);}
        }),
    ];
}
