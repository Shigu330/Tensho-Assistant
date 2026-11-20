using System.Text;
using TenshoAssistant.Chat;

// 0.1588 (X-644, review of 0.1586, minor findings): M1, with « No limit » on the messages and the days, a kind that rarely comes no longer
// makes the reading decode the file back to its 64 MB budget (behaviour on a real file); M2, the zone names in four languages are read off
// the frame, not in the click of « Import » (R07).
internal static class HistoryAndZoneNames1588Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Now=new(2026,11,20,8,0,0,TimeSpan.Zero);
    private const ulong Ann=18014398509481985UL;

    private static string Root()
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);
        while(dir!=null && !Directory.Exists(Path.Combine(dir.FullName,"src","TenshoAssistant")))dir=dir.Parent;
        return dir!.FullName;
    }

    // A file of `talk` messages of the others' kind, `fights` combat lines among the oldest ones; every `broken`-th of the others (0: none)
    // has a body that cannot be decoded, its time and kind still readable.
    private static string Write(string folder,int talk,int fights,int broken)
    {
        var path=Path.Combine(folder,"history.jsonl");
        using var output=new StreamWriter(path,false,new UTF8Encoding(false));
        output.Write(ChatHistory.Header(Ann)+"\n");
        var start=Now.AddDays(-1);
        for(var fight=0;fight<fights;fight++)
            output.Write(ChatHistory.Encode(new ChatRecord(0,start,41,ChatChannels.From(41),"",[new ChatSegment("hit "+fight)],1,2))+"\n");
        for(var index=0;index<talk;index++)
        {
            var line=ChatHistory.Encode(new ChatRecord(0,start.AddSeconds(index+1),10,ChatChannels.From(10),"Ann Example",[new ChatSegment("say "+index)],1,2));
            if(broken>0 && index%broken==0 && index<talk-ChatHistory.MaximumMessages)line=line[..line.IndexOf("\"body\"",StringComparison.Ordinal)]+"\"body\":[{\"text\":}";
            output.Write(line+"\n");
        }
        return path;
    }

    private static void InFolder(Action<string> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-history-1588-"+Guid.NewGuid().ToString("N"));
        try{Directory.CreateDirectory(folder);run(folder);}
        finally{try{Directory.Delete(folder,true);}catch(IOException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("History and zone names 1588: M1, a line's time and kind are peeked as Encode writes them",() =>
        {
            foreach(var (kind,time) in new[]{(10,Now),(41,Now.AddDays(-400)),(2105,Now.AddMilliseconds(-1))})
            {
                var line=ChatHistory.Encode(new ChatRecord(0,time,kind,ChatChannels.From(kind),"Ann Example",[new ChatSegment("x")],1,2));
                Check(ChatHistory.Peek(line) is { } peek && peek.Kind==kind && peek.Time==time,$"kind {kind}: peeked as written ({line})");
            }
            foreach(var line in new[]{"","{}","{\"kind\":10,\"time\":5,\"body\":[]}","{\"time\":-5,\"kind\":10,\"body\":[]}","{\"time\":5,\"kind\":x,","{\"time\":5,\"kind\":10","not json"})
                Check(ChatHistory.Peek(line) is null,"not peeked: "+line);
        }),
        ("History and zone names 1588: M1, with every « No limit », the lines of a full kind are not decoded, the window gets the same",() =>
        InFolder(folder =>
        {
            var max=ChatHistory.MaximumMessages;
            // 3 × the window's messages of the others, every 7th of the older ones unreadable past its kind, 5 combat lines at the start.
            var path=Write(folder,3*max,5,7);
            var read=ChatHistory.Read(path,Ann,Now,ChatHistory.NoLimit,ChatHistory.NoLimit);
            Check(read.Bad==0,$"the older lines of the full kind are peeked, never decoded ({read.Bad} decoded and found unreadable)");
            Check(read.Lines==3*max+5,$"the whole file walked for the rare kind ({read.Lines})");
            Check(read.Records.Count==max+5 && read.Records.Count(record=>record.Channel==ChatChannel.Combat)==5,$"the last {max} of the others and the 5 combat lines ({read.Records.Count})");
            Check(read.Records[^1].Body[0].Text=="say "+(3*max-1) && read.Records[5].Body[0].Text=="say "+(2*max),"the newest of the others, in order");
            Check(!read.Trim,"« No limit »: no trim asked");
        })),
        ("History and zone names 1588: M1, a bounded history and the days still behave as before",() =>
        InFolder(folder =>
        {
            var path=Write(folder,3000,5,0);
            var read=ChatHistory.Read(path,Ann,Now,ChatHistory.MinimumMessages,ChatHistory.NoLimit);
            Check(read.Records.Count==ChatHistory.MinimumMessages+5 && read.Trim,"bounded: the last of the others, the 5 combat lines, the trim asked");
            // Every line is of yesterday: a bound of 1 day keeps the ones of the last 24 hours (all), 2 days too.
            var days=ChatHistory.Read(path,Ann,Now.AddDays(1).AddSeconds(1500),ChatHistory.NoLimit,1);
            Check(days.Records.All(record=>record.Time>=Now.AddSeconds(1500)) && days.Trim,"the days: the older ones are left, the trim asked");
        })),
        ("History and zone names 1588: M2, the zone names are read off the frame, the import waits for them",() =>
        {
            var ui=File.ReadAllText(Path.Combine(Root(),"src","TenshoAssistant","MainWindow.MapBookmarks.cs")).Replace("\r\n","\n");
            Check(!ui.Contains("ZoneNamesLoad?.Invoke()") && !ui.Contains("ZoneNamesLoad()"),"never read in the drawing");
            Check(ui.Contains("zoneNamesTask = Task.Run(loadNames);"),"read by a task");
            Check(ui.Contains("bookmarkPaste.Start(bookmarkImport, ImGui.GetFrameCount());") && ui.Contains("ImportPastedCoordinates(pastedText);"),"a click before they are read waits (X-645: with the clicked text)");
            foreach(var language in new[]{"en","fr"})
                Check(File.ReadAllText(Path.Combine(Root(),"src","TenshoAssistant","Localization","Languages",language+".json")).Contains("\"PastedReading\""),language+": the waiting line");
        }),
    ];
}
