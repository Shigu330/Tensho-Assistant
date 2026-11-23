using TenshoAssistant;
using TenshoAssistant.Chat;

// 0.1853 (X-844, review 0.1844 M2). Behaviour tests (R28.14) on real files in folders of their own: the sweep of another character's
// chat history and a second client that plays that character at the same time. Each case acts as the second client while the sweep is
// between its read of the file and the file replaced (or deleted): before, the lines it added there were written over and lost.
internal static class ChatSweepLock1853Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Folder(string name)=>Path.Combine(Path.GetTempPath(),"tensho-1853-"+name+"-"+Guid.NewGuid().ToString("N"));
    private static void Clean(string folder){try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}

    private static readonly DateTimeOffset Now=new(2026,11,23,12,0,0,TimeSpan.Zero);
    private const ulong Ann=18014398509481985UL,Bob=18014398509481986UL;
    private static ChatRecord Said(string text,double daysAgo)
        =>new(0,Now.AddDays(-daysAgo),10,ChatChannels.From(10),"Other Player",[new ChatSegment(text)],1,2);
    private static string WriteHistory(string folder,ulong who,DateTimeOffset written,params ChatRecord[] records)
    {
        Directory.CreateDirectory(folder);
        var path=ChatHistory.PathOf(folder,who);
        File.WriteAllText(path,ChatHistory.Header(who)+"\n"+string.Concat(records.Select(record=>ChatHistory.Encode(record)+"\n")));
        File.SetLastWriteTimeUtc(path,written.UtcDateTime);
        return path;
    }

    // In each case the second client starts adding a line while the sweep is about to replace the file, and the sweep goes on 300 ms
    // later: the line must still be waiting for the lock then (written before the file is replaced, it would be lost).
    public static readonly (string Name,Action Run)[] Cases=
    [
        ("X-844: a client adds lines while another client's sweep trims the file: they wait for the lock and are kept",()=>
        {
            var folder=Folder("trim");
            try
            {
                var path=WriteHistory(folder,Bob,Now.AddDays(-2),Said("bob old",31),Said("bob recent",2));
                Task? second=null;var waited=false;
                var done=ChatHistory.Sweep(folder,Ann,Now,2000,30,replacing:file=>
                {
                    second=Task.Run(()=>ChatHistory.Append(file,Bob,[ChatHistory.Encode(Said("bob new",0))]));
                    waited=!second.Wait(300);
                });
                Check(done==1,"Bob's file trimmed: "+done);
                Check(second is not null && waited,"the second client's line waited for the sweep's lock");
                Check(second!.Wait(10_000),"the second client's line written once the sweep let the lock go");
                var text=File.ReadAllText(path);
                Check(!text.Contains("bob old",StringComparison.Ordinal),"the old message removed");
                Check(text.Contains("bob recent",StringComparison.Ordinal),"the recent message kept");
                Check(text.Contains("bob new",StringComparison.Ordinal),"the line the second client added during the sweep kept");
                var read=ChatHistory.Read(path,Bob,Now,2000,30);
                Check(read.Ours && read.Records.Count==2,"the file reads as Bob's, two messages: "+read.Records.Count);
                Check(ChatHistory.Erase(folder) && !Directory.Exists(folder),"« Erase » removes the lock files too, then the folder");
            }
            finally{Clean(folder);}
        }),
        ("X-844: a client adds lines while another client's sweep deletes the file: they wait for the lock and make the file again",()=>
        {
            var folder=Folder("delete");
            try
            {
                var path=WriteHistory(folder,Bob,Now.AddDays(-40),Said("bob old",40));
                Task<long>? second=null;var waited=false;
                var done=ChatHistory.Sweep(folder,Ann,Now,2000,30,replacing:file=>
                {
                    second=Task.Run(()=>ChatHistory.Append(file,Bob,[ChatHistory.Encode(Said("bob new",0))]));
                    waited=!second.Wait(300);
                });
                Check(done==1,"Bob's file deleted: "+done);
                Check(second is not null && waited,"the second client's line waited for the sweep's lock");
                Check(second!.Wait(10_000),"the second client's line written once the sweep let the lock go");
                var read=ChatHistory.Read(path,Bob,Now,2000,30);
                Check(read.Ours && read.Records.Count==1 && File.ReadAllText(path).Contains("bob new",StringComparison.Ordinal),"the file made again with the new line only");
            }
            finally{Clean(folder);}
        }),
        ("X-844: a file whose lock another client holds is left as it is, and swept at a later load once free",()=>
        {
            var folder=Folder("held");
            try
            {
                var path=WriteHistory(folder,Bob,Now.AddDays(-2),Said("bob old",31),Said("bob recent",2));
                var before=File.ReadAllText(path);
                using(FairFileLock.Hold(ChatHistory.LockOf(path),1000,error=>error))
                    Check(ChatHistory.Sweep(folder,Ann,Now,2000,30)==0,"held by another client: not swept");
                Check(File.ReadAllText(path)==before,"the file left whole");
                File.SetLastWriteTimeUtc(path,Now.AddDays(-2).UtcDateTime);
                Check(ChatHistory.Sweep(folder,Ann,Now,2000,30)==1 && !File.ReadAllText(path).Contains("bob old",StringComparison.Ordinal),"once free, swept");
            }
            finally{Clean(folder);}
        }),
        ("X-844: a write taken back (« off » during a write) holds the lock too, and a busy lock makes a write fail without losing its lines",()=>
        {
            var folder=Folder("undo");
            try
            {
                Directory.CreateDirectory(folder);
                var path=ChatHistory.PathOf(folder,Bob);
                ChatHistory.Append(path,Bob,[ChatHistory.Encode(Said("first",0))]);
                var length=new FileInfo(path).Length;
                using(FairFileLock.Hold(ChatHistory.LockOf(path),1000,error=>error))
                {
                    var failed=false;
                    var start=Environment.TickCount64;
                    try{ChatHistory.Append(path,Bob,[ChatHistory.Encode(Said("second",0))]);}
                    catch(IOException){failed=true;}
                    Check(failed && Environment.TickCount64-start>=ChatHistory.LockWaitMs/2,"a write fails (IOException, its lines kept by the writer) after waiting for the lock");
                    Check(new FileInfo(path).Length==length,"nothing written while another client held the lock");
                }
                var before=ChatHistory.Append(path,Bob,[ChatHistory.Encode(Said("third",0))]);
                ChatHistory.Undo(path,before);
                Check(new FileInfo(path).Length==length,"the write taken back");
            }
            finally{Clean(folder);}
        }),
    ];
}
