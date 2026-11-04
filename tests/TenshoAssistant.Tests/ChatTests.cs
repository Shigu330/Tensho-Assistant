using TenshoAssistant.Chat;

// CHAT-001 (DEC-029), phase 1: channels, store, formatting, wrapping, settings.
internal static class ChatTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static float Measure(string text)=>text.Length;
    private static string Text(IReadOnlyList<ChatRun> line)=>string.Concat(line.Select(run=>run.Text));
    private static ChatRecord Record(int kind,string sender,params string[] body)=>new(1,new DateTimeOffset(2026,11,4,12,5,9,TimeSpan.Zero),kind,ChatChannels.From(kind),sender,body.Select(text=>new ChatSegment(text)).ToArray());

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat channels: numbers of XivChatType; a kind not listed is Other, and combat lines have their own channel",() =>
        {
            Check(ChatChannels.From(10)==ChatChannel.Say && ChatChannels.From(30)==ChatChannel.Yell && ChatChannels.From(13)==ChatChannel.TellIn && ChatChannels.From(12)==ChatChannel.TellOut,"spoken");
            Check(ChatChannels.From(16)==ChatChannel.Ls1 && ChatChannels.From(23)==ChatChannel.Ls8 && ChatChannels.From(37)==ChatChannel.Cwls1 && ChatChannels.From(101)==ChatChannel.Cwls2 && ChatChannels.From(107)==ChatChannel.Cwls8,"linkshells");
            Check(ChatChannels.From(41)==ChatChannel.Combat && ChatChannels.From(49)==ChatChannel.Combat && ChatChannels.From(999)==ChatChannel.Other && ChatChannels.From(0)==ChatChannel.Other,"others");
            Check(ChatChannels.Spoken(ChatChannel.Novice) && !ChatChannels.Spoken(ChatChannel.System) && !ChatChannels.Spoken(ChatChannel.Echo),"spoken channels");
        }),
        ("Chat store: oldest lines dropped past the bounded cap; counts for the probe; a clear is seen by the window",() =>
        {
            var store=new ChatStore();
            for(var index=0;index<150;index++)store.Add(DateTimeOffset.UtcNow,10,"a",[],100,["Text"]);
            Check(store.Count==100 && store.LastId==150 && store.After(0)[0].Id==51,"cap");
            Check(store.After(149).Single().Id==150 && store.After(150).Count==0,"after");
            store.Add(DateTimeOffset.UtcNow,10,"a",[],1);Check(store.Count==100,"a cap below the minimum is bounded");
            Check(store.KindCounts().Single().Value==151 && store.PayloadCounts().Single().Value==150,"counts survive the cap");
            var clears=store.Clears;store.Clear();Check(store.Count==0 && store.Clears==clears+1 && store.LastId==151,"clear keeps the numbering");
        }),
        ("Chat format: timestamp, channel, sender by channel; game glyphs and control characters removed",() =>
        {
            var options=new ChatSettings{ShowTimestamps=false};
            string Line(ChatRecord record)=>string.Concat(ChatFormat.Runs(record,options,channel=>channel==ChatChannel.Say ? "S":"X").Select(run=>run.Text));
            Check(Line(Record(10,"Ann","hello"))=="[S] Ann: hello","say");
            Check(Line(Record(13,"Ann","hi"))=="[X] Ann >> hi" && Line(Record(12,"Ann","hi"))=="[X] >> Ann: hi","tells");
            Check(Line(Record(57,"","Message"))=="Message" && Line(Record(29,"Ann","Ann waves."))=="Ann waves." && Line(Record(28,"Ann","waves"))=="Ann waves","system and emotes");
            Check(Line(Record(10,"Ann","a\u0002bc"))=="[S] Ann: abc","cleaned");
            options.ShowChannel=false;Check(Line(Record(10,"Ann","x"))=="Ann: x","no channel");
            options.ShowTimestamps=true;options.Clock24=true;options.ShowSeconds=true;
            var runs=ChatFormat.Runs(Record(10,"Ann","x"),options,_=>"");
            Check(runs[0].Role==ChatRole.Timestamp && runs[0].Text.Length==9 && runs[0].Text[2]==':',"timestamp: "+runs[0].Text);
        }),
        ("Chat wrap: breaks between words, cuts a word wider than the line, keeps line breaks and joins runs of one role",() =>
        {
            var lines=ChatFormat.Wrap([new("one two three",ChatRole.Body)],8,Measure);
            Check(lines.Select(Text).SequenceEqual(["one two ","three"]),"words: "+string.Join("|",lines.Select(Text)));
            Check(ChatFormat.Wrap([new("abcdefghij",ChatRole.Body)],4,Measure).Select(Text).SequenceEqual(["abcd","efgh","ij"]),"long word");
            Check(ChatFormat.Wrap([new("a\nb",ChatRole.Body)],40,Measure).Select(Text).SequenceEqual(["a","b"]),"line break");
            var joined=ChatFormat.Wrap([new("a ",ChatRole.Body),new("b",ChatRole.Body),new("c",ChatRole.Sender)],40,Measure).Single();
            Check(joined.Count==2 && joined[0].Text=="a b" && joined[1].Role==ChatRole.Sender,"joined by role");
            Check(ChatFormat.Wrap([],40,Measure).Single().Count==0,"an empty message still takes a line");
            Check(ChatFormat.Wrap([new("abc def",ChatRole.Body)],0,Measure).Count>=1,"no width: no endless loop");
        }),
        ("Chat settings: off by default, bounded, two tabs at first use, colours by channel",() =>
        {
            var options=new ChatSettings();
            Check(!options.WindowOpen && options.ShowTimestamps,"off by default: no message is copied");
            options.Opacity=500;options.FontScale=1;options.MaxLines=9;
            Check(options.BoundedOpacity==100 && options.BoundedFontScale==60 && options.BoundedMaxLines==ChatStore.MinimumLines,"bounds");
            var tabs=options.EnsureTabs("All","Social");
            Check(tabs.Count==2 && !tabs[0].Shows(ChatChannel.Combat) && tabs[0].Shows(ChatChannel.System) && tabs[1].Shows(ChatChannel.Party) && !tabs[1].Shows(ChatChannel.System),"tabs");
            Check(ReferenceEquals(options.EnsureTabs("x","y"),tabs),"kept");
            options.Colors=new(){[ChatChannel.Say]=0xFF123456};
            Check(options.ColorOf(ChatChannel.Say)==0x123456 && options.ColorOf(ChatChannel.Party)==ChatChannels.DefaultColor(ChatChannel.Party),"colours");
        }),
        ("Chat wiring: reads only, sends nothing, the other plugins' filters kept, all cleaned up; Chat 2 is told about",() =>
        {
            var reader=Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("chat.ChatMessageUnhandled+=OnMessage;") && reader.Contains("chat.ChatMessageUnhandled-=OnMessage;") && reader.Contains("if(!current.WindowOpen)return;"),"reader");
            foreach(var file in new[]{"Chat/ChatReader.Dalamud.cs","MainWindow.Chat.cs"})Check(!Read(file).Contains("ProcessChatBox") && !Read(file).Contains("SendMessage"),"nothing sent: "+file);
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("new Chat.ChatReader(") && plugin.Contains("(\"Chat\",mainWindow.DrawChatWindow)") && plugin.Contains("mainWindow.ChatTwoLoaded="),"registered");
            Check(Read("MainWindow.Pages.cs").Contains("new(Page.Chat,\"World\",PageLabel(\"Navigation.Chat\"),DrawChat,"),"page");
        }),
    ];
}
