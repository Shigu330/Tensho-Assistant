using System.Text;
using TenshoAssistant.Chat;

// 0.1586 (X-518, review of 0.1494, minor findings): the consent sentence no longer says « older ones are removed » with both « No limit »;
// unticking « No limit » asks first; the log line of a history failure carries its scrubbed call stack; reading stops after a run of messages
// older than the days (behaviour test on a real file: the old lines are never decoded); the history's settings have a reference picture.
internal static class ChatHistoryReview1586Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Now=new(2026,11,20,7,0,0,TimeSpan.Zero);
    private const ulong Ann=18014398509481985UL;

    private static string Root()
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);
        while(dir!=null && !Directory.Exists(Path.Combine(dir.FullName,"src","TenshoAssistant")))dir=dir.Parent;
        return dir!.FullName;
    }
    private static string Text(string relative)=>File.ReadAllText(Path.Combine(Root(),relative)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat history review 1586: M1, older ones are removed only while a bound is set, in both languages",() =>
        {
            Check(ChatHistory.RemovesOlder(false,false) && ChatHistory.RemovesOlder(true,false) && ChatHistory.RemovesOlder(false,true),"a bound: removed");
            Check(!ChatHistory.RemovesOlder(true,true),"both « No limit »: nothing removed");
            foreach(var language in new[]{"en","fr"})
            {
                var json=Text($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(json.Contains("(settings below){3}.") || json.Contains("(réglages ci-dessous){3}."),$"{language}: the end of the sentence is a placeholder");
                Check(json.Contains("\"HistoryNothingRemoved\"") && json.Contains("\"HistoryOlderRemoved\""),$"{language}: both ends");
            }
        }),
        ("Chat history review 1586: M2, unticking « No limit » while the history is kept asks first",() =>
        {
            var ui=Text("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(ui.Contains("if(!value && configuration.Chat.KeepHistory)chatHistoryLimitAsk=1;else configuration.Chat.HistoryMessagesUnlimited=value;"),"messages");
            Check(ui.Contains("if(!value && configuration.Chat.KeepHistory)chatHistoryLimitAsk=2;else configuration.Chat.HistoryDaysUnlimited=value;"),"days");
            Check(ui.Contains("DrawChatHistoryLimitAsk();") && ui.Contains("###ChatHistoryLimitYes"),"the question is drawn, with its answer");
        }),
        ("Chat history review 1586: M3, the log line carries the call stack, the character's number scrubbed",() =>
        {
            Exception caught;
            try{throw new IOException($"Could not find '{Ann}.jsonl'.");}catch(IOException error){caught=error;}
            var line=ChatHistory.Report(caught,"The chat history could not be written.");
            Check(line.Contains(nameof(ChatHistoryReview1586Tests)) && !line.Contains(Ann.ToString()),"stack kept, number scrubbed: "+line);
            Check(ChatHistory.Report(new IOException("x"),"m")=="m (IOException: x)","an error never thrown has no stack line");
        }),
        ("Chat history review 1586: M4, reading stops after a run of messages older than the days",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-chat-review-"+Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(folder);
                var path=Path.Combine(folder,"history.jsonl");
                using(var output=new StreamWriter(path,false,new UTF8Encoding(false)))
                {
                    output.Write(ChatHistory.Header(Ann)+"\n");
                    // 3 000 messages of 20 days ago, then 10 of today.
                    for(var index=0;index<3000;index++)
                        output.Write(ChatHistory.Encode(new ChatRecord(0,Now.AddDays(-20).AddSeconds(index),10,ChatChannels.From(10),"Ann Example",[new ChatSegment("old "+index)],1,2))+"\n");
                    for(var index=0;index<10;index++)
                        output.Write(ChatHistory.Encode(new ChatRecord(0,Now.AddMinutes(-10+index),10,ChatChannels.From(10),"Ann Example",[new ChatSegment("new "+index)],1,2))+"\n");
                }
                var read=ChatHistory.Read(path,Ann,Now,ChatHistory.NoLimit,7);
                Check(read.Records.Count==10 && read.Trim,$"the 10 of today, a trim asked ({read.Records.Count})");
                Check(read.Lines==10+ChatHistory.OldRunEnd,$"the old lines are read no further than a run of {ChatHistory.OldRunEnd} ({read.Lines} lines read)");
            }
            finally{try{Directory.Delete(folder,true);}catch(IOException){}}
        }),
        ("Chat history review 1586: M5, the history's settings have their reference picture",() =>
        {
            var plan=Text("src/TenshoAssistant.Desktop/ReferencePlan.cs");
            Check(plan.Contains("(\"ChatHistory\",\"Panels\",\"ChatHistory\",1280)"),"Settings-ChatHistory pictured");
        }),
    ];
}
