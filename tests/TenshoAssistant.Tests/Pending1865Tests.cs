using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;
using TenshoAssistant;

// 0.1865 (X-859): the CI stayed red from 0.1864. 1) Replace1864Tests looked for a snippet with « \n » in a file read raw; the Windows
// runner checks the files out with CRLF, so it failed there only. A ratchet over the tests' sources now refuses a file read raw and then
// searched for a « \n » snippet without its « \r\n » form. 2) X-844 failed once on CI: the chat sweep deleted a lock file another program
// still held, the file stayed « delete pending », and the client waiting for it got access denied. Behaviour test: a lock file made
// « delete pending » by hand (the old way of deleting, kept open), the client waits, then gets the lock once it is let go.
internal static class Pending1865Tests
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

    private static string Folder()=>Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),"tensho-1865-"+Guid.NewGuid().ToString("N"))).FullName;

    private static string FindRepositoryFile(string relative)
    {
        for(var folder=new DirectoryInfo(AppContext.BaseDirectory);folder is not null;folder=folder.Parent)
        {
            var candidate=Path.Combine(folder.FullName,relative);
            if(Directory.Exists(candidate) || File.Exists(candidate))return candidate;
        }
        return Path.GetFullPath(relative);
    }

    // A variable read by File.ReadAllText without its line endings made one form, then searched for a snippet holding « \n »: the statement
    // (its line and the next) must also accept « \r\n ».
    internal static IReadOnlyList<string> RawNewlineSearches(string source)
    {
        var found=new List<string>();
        foreach(Match read in Regex.Matches(source,@"var\s+(\w+)\s*=\s*File\.ReadAllText\(([^;]*?)\)\s*;"))
        {
            if(read.Groups[2].Value.Contains("Replace",StringComparison.Ordinal))continue;
            var name=read.Groups[1].Value;
            var rest=source[(read.Index+read.Length)..];
            var next=Regex.Match(rest,@"\bvar\s+"+Regex.Escape(name)+@"\s*=");
            var lines=(next.Success ? rest[..next.Index]:rest).Split('\n');
            for(var i=0;i<lines.Length;i++)
            {
                if(!Regex.IsMatch(lines[i],@"\b"+Regex.Escape(name)+@"\.Contains\(""(?:[^""\\]|\\.)*\\n"))continue;
                var statement=lines[i]+(i+1<lines.Length ? lines[i+1]:"");
                if(!statement.Contains(@"\r\n",StringComparison.Ordinal))found.Add(name+": "+lines[i].Trim());
            }
        }
        return found;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Pending 1865: no test reads a file raw then looks for a « \\n » snippet without its « \\r\\n » form (CRLF on the CI runner)",() =>
        {
            Check(RawNewlineSearches("var tool=File.ReadAllText(Path.GetFullPath(\"a.py\"));\nCheck(tool.Contains(\"a:\\n    b\",StringComparison.Ordinal),\"x\");\n").Count==1,
                "the ratchet finds a raw read searched with « \\n »");
            Check(RawNewlineSearches("var tool=File.ReadAllText(Path.GetFullPath(\"a.py\")).ReplaceLineEndings(\"\\n\");\nCheck(tool.Contains(\"a:\\n    b\"),\"x\");\n").Count==0
                && RawNewlineSearches("var tool=File.ReadAllText(Path.GetFullPath(\"a.py\"));\nCheck(tool.Contains(\"a:\\n b\")\n    || tool.Contains(\"a:\\r\\n b\"),\"x\");\n").Count==0,
                "a normalised read, or a search that takes both forms, passes");
            var folder=FindRepositoryFile("tests/TenshoAssistant.Tests");
            var bad=new List<string>();
            foreach(var file in Directory.EnumerateFiles(folder,"*.cs"))
                foreach(var line in RawNewlineSearches(File.ReadAllText(file)))bad.Add(Path.GetFileName(file)+": "+line);
            Check(bad.Count==0,"raw reads searched with « \\n »: "+string.Join(" | ",bad));
        }),
        ("Pending 1865: a lock file left « delete pending » by another program: the client waits, then holds the lock once it is let go",() =>
        {
            var folder=Folder();
            try
            {
                var path=Path.Combine(folder,"1.jsonl.lock");
                File.WriteAllText(path,"");
                var pending=DeletePending(path);
                Exception? direct=null;
                try{using(new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){}}
                catch(Exception error){direct=error;}
                Check(direct is UnauthorizedAccessException,"Windows refuses a « delete pending » file with access denied: "+direct?.GetType().Name);
                var holding=Task.Run(()=>FairFileLock.Hold(path,10_000,error=>error));
                Check(!holding.Wait(300),"the client waits while the file is « delete pending »");
                pending.Dispose();
                Check(holding.Wait(10_000),"the client holds the lock once the other program let the file go");
                holding.Result.Dispose();
            }
            finally{try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }),
        ("Pending 1865: a lock file that really cannot be opened still raises access denied, once the wait is over",() =>
        {
            var folder=Folder();
            var path=Path.Combine(folder,"2.jsonl.lock");
            try
            {
                File.WriteAllText(path,"");
                File.SetAttributes(path,FileAttributes.ReadOnly);
                var started=Environment.TickCount64;
                Exception? raised=null;
                try{using(FairFileLock.Hold(path,200,error=>error)){}}
                catch(Exception error){raised=error;}
                Check(raised is UnauthorizedAccessException,"access denied raised: "+raised?.GetType().Name);
                Check(Environment.TickCount64-started>=150,"raised after the wait, not at once");
            }
            finally
            {
                try{File.SetAttributes(path,FileAttributes.Normal);}catch(IOException){}
                try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}
            }
        }),
    ];
}
