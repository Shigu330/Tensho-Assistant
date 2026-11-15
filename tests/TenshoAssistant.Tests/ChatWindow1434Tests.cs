using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Chat;

// X-403 phase 1 (0.1434, D-166 of 15/11; the 0.471 ChatTests brought back): the Tensho chat window, beside the game's chat, reading only. Here: the
// game's kinds grouped into channels, the bounded store and its order, the clear at logout that drops every copy (tabs included), the unread count
// of each tab, the format of a line (glyphs removed, time), the wrapping, the settings (off by default, bounded, never the messages), and the
// wiring: not read during a competitive cut, nothing sent, nothing written to disk, cleaned up at unload.
internal static class ChatWindow1434Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static float Measure(string text)=>text.Length;
    private static string Text(IReadOnlyList<ChatRun> line)=>string.Concat(line.Select(run=>run.Text));
    private static readonly DateTimeOffset Noon=new(2026,11,15,12,5,9,TimeSpan.Zero);
    private static ChatRecord Record(int kind,string sender,params string[] body)=>new(1,Noon,kind,ChatChannels.From(kind),sender,body.Select(text=>new ChatSegment(text)).ToArray());
    private static void Say(ChatStore store,int kind,int cap=2000)=>store.Add(Noon,kind,"Ann",[new ChatSegment("hello")],cap);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat window 1434: the game's kinds (XivChatType, API 15) grouped into channels as the game's log filters group them; an unknown kind is Other",() =>
        {
            Check(ChatChannels.From(10)==ChatChannel.Say && ChatChannels.From(11)==ChatChannel.Shout && ChatChannels.From(30)==ChatChannel.Yell,"say, shout, yell");
            Check(ChatChannels.From(13)==ChatChannel.TellIn && ChatChannels.From(12)==ChatChannel.TellOut,"tells");
            Check(ChatChannels.From(14)==ChatChannel.Party && ChatChannels.From(32)==ChatChannel.Party && ChatChannels.From(15)==ChatChannel.Alliance && ChatChannels.From(24)==ChatChannel.FreeCompany,"groups");
            Check(ChatChannels.From(16)==ChatChannel.Ls1 && ChatChannels.From(23)==ChatChannel.Ls8 && ChatChannels.From(37)==ChatChannel.Cwls1 && ChatChannels.From(101)==ChatChannel.Cwls2 && ChatChannels.From(107)==ChatChannel.Cwls8,"linkshells");
            Check(ChatChannels.From(28)==ChatChannel.Emote && ChatChannels.From(29)==ChatChannel.Emote && ChatChannels.From(27)==ChatChannel.Novice && ChatChannels.From(56)==ChatChannel.Echo,"emotes, novice, echo");
            Check(ChatChannels.From(57)==ChatChannel.System && ChatChannels.From(58)==ChatChannel.Combat && ChatChannels.From(73)==ChatChannel.System && ChatChannels.From(74)==ChatChannel.System,"58 is a battle system message, in the game's Battle tab since 0.1458 (X-445); signs and /random with the system messages");
            Check(ChatChannels.From(60)==ChatChannel.Error && ChatChannels.From(61)==ChatChannel.Npc && ChatChannels.From(65)==ChatChannel.Loot && ChatChannels.From(66)==ChatChannel.Crafting && ChatChannels.From(67)==ChatChannel.Gathering,"errors, NPC, loot, synthesis, gathering");
            Check(ChatChannels.From(71)==ChatChannel.Retainer && ChatChannels.From(70)==ChatChannel.Announcement && ChatChannels.From(64)==ChatChannel.Progress,"retainers, announcements, progression");
            Check(ChatChannels.From(41)==ChatChannel.Combat && ChatChannels.From(49)==ChatChannel.Combat && ChatChannels.From(999)==ChatChannel.Other && ChatChannels.From(0)==ChatChannel.Other,"combat apart, others");
            Check(ChatChannels.Spoken(ChatChannel.Novice) && ChatChannels.Spoken(ChatChannel.Cwls8) && !ChatChannels.Spoken(ChatChannel.Emote) && !ChatChannels.Spoken(ChatChannel.System),"spoken channels");
            Check(!ChatChannels.CountsAsUnread(ChatChannel.TellOut) && ChatChannels.CountsAsUnread(ChatChannel.TellIn) && ChatChannels.CountsAsUnread(ChatChannel.System),"what you send is never unread");
            Check(ChatChannels.Pack(1,0.5f,0)==0xFF8000 && ChatChannels.Unpack(0xFF8000).Red==1 && ChatChannels.Pack(2,-1,float.NaN)==0xFF0000,"colours packed and bounded");
        }),
        ("Chat window 1434: the store keeps the newest lines in order, bounded between 100 and 10 000; a line asked again is not copied twice",() =>
        {
            var store=new ChatStore();
            for(var index=0;index<150;index++)Say(store,10,100);
            var kept=store.After(0);
            Check(store.Count==100 && store.LastId==150 && kept[0].Id==51 && kept[^1].Id==150,"the oldest dropped first");
            Check(kept.Zip(kept.Skip(1)).All(pair=>pair.Second.Id==pair.First.Id+1),"in order");
            Check(store.After(149).Single().Id==150 && store.After(150).Count==0 && store.After(10).Count==100,"after a number");
            Say(store,10,1);Check(store.Count==100,"a cap below the minimum is bounded");
            var big=new ChatStore();
            for(var index=0;index<ChatStore.MaximumLines+5;index++)Say(big,10,int.MaxValue);
            Check(big.Count==ChatStore.MaximumLines && big.After(0)[0].Id==6,"a cap above the maximum is bounded");
            Check(store.KindCounts().Single().Key==10 && store.KindCounts().Single().Value==151,"kinds counted, no text");
        }),
        ("Chat window 1434: the clear of a logout (or of the window turned off, or of the unload) drops every copy, the tabs' lines included",() =>
        {
            var store=new ChatStore();var tab=new ChatTabSettings{Channels=[ChatChannel.Say]};
            Say(store,10);Say(store,10);
            var view=store.ViewOf(tab);view.Refresh(store,tab,2000);
            Check(view.Count==2 && ReferenceEquals(store.ViewOf(tab),view),"the tab's view is kept between frames");
            var clears=store.Clears;
            store.Clear();
            Check(store.Count==0 && store.After(0).Count==0 && store.Clears==clears+1 && store.LastId==2,"nothing kept, the numbering goes on");
            var after=store.ViewOf(tab);after.Refresh(store,tab,2000);
            Check(!ReferenceEquals(after,view) && after.Count==0,"the tab's copies are dropped with the lines");
            Say(store,10);after.Refresh(store,tab,2000);
            Check(after.Count==1 && after.RecordAt(0).Id==3 && after.Unread==1,"the next session starts empty");
            var reader=Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("private void OnLogout(int type,int code)=>store.Clear();") && reader.Contains("client.Logout+=OnLogout"),"cleared at logout");
            Check(Regex.IsMatch(reader,@"public void Dispose\(\)\s*\{[^}]*registrations\.Dispose\(\);\s*store\.Clear\(\);"),"cleared at unload, after the subscriptions are released");
            Check(reader.Contains("if(!current.WindowOpen){if(store.Count>0)store.Clear();return;}"),"nothing copied, and nothing kept, while the window is off");
        }),
        ("Chat window 1434: unread counts per tab: a line in a tab not shown counts, showing the tab clears it, what you send never counts",() =>
        {
            var store=new ChatStore();var options=new ChatSettings();
            var tabs=options.EnsureTabs("General","Battle","Event","Social").Where(tab=>tab.Name is "General" or "Social").ToList();
            Say(store,10);Say(store,57);
            var general=store.ViewOf(tabs[0]);var social=store.ViewOf(tabs[1]);
            general.Refresh(store,tabs[0],2000);social.Refresh(store,tabs[1],2000);
            Check(general.Count==2 && social.Count==1 && general.Unread==0 && social.Unread==0,"a tab made while lines are kept takes them as read");
            Say(store,10);Say(store,57);Say(store,12);Say(store,41);
            general.Refresh(store,tabs[0],2000);social.Refresh(store,tabs[1],2000);
            Check(general.Unread==2 && social.Unread==1,$"new lines counted where they are shown: {general.Unread}, {social.Unread}");
            Check(general.Count==5 && social.Count==3,"the tell you sent is shown, the combat line only where its channel is");
            general.MarkSeen();
            Say(store,13);general.Refresh(store,tabs[0],2000);social.Refresh(store,tabs[1],2000);
            general.MarkSeen();
            Check(general.Unread==0 && social.Unread==2,"the tab shown is read; the other still counts");
            Check(ChatFormat.TabLabel("Social",true,social.Unread)=="Social (2)" && ChatFormat.TabLabel("Social",false,2)=="Social" && ChatFormat.TabLabel("General",true,0)=="General" && ChatFormat.TabLabel("",true,0)=="-","the count in the tab's name, when it shows it");
            store.Restart(tabs[1]);
            var rebuilt=store.ViewOf(tabs[1]);rebuilt.Refresh(store,tabs[1],2000);
            Check(!ReferenceEquals(rebuilt,social) && rebuilt.Count==4 && rebuilt.Unread==0,"a tab whose channels change is built again from the lines kept, as read");
            for(var index=0;index<150;index++)Say(store,10);
            rebuilt.Refresh(store,tabs[1],100);
            Check(rebuilt.Count==100 && rebuilt.RecordAt(0).Id==store.LastId-99,"a tab keeps as many lines as the store");
        }),
        ("Chat window 1434: only the rows in view are drawn: each message's row, the message on a row, the newest laid out first",() =>
        {
            var store=new ChatStore();var tab=new ChatTabSettings{Channels=[ChatChannel.Say]};
            for(var index=0;index<4;index++)Say(store,10);
            var view=store.ViewOf(tab);view.Refresh(store,tab,2000);
            Check(view.RowCount==4 && view.RowOf(3)==3 && view.IndexAtRow(2)==2,"a message not laid out counts one row");
            Check(view.NextToLayOut()==3,"the newest first");
            IReadOnlyList<IReadOnlyList<ChatRun>> three=[[new("a",ChatRole.Body)],[new("b",ChatRole.Body)],[new("c",ChatRole.Body)]];
            view.SetLines(1,three);
            Check(view.RowCount==6 && view.RowOf(2)==4 && view.IndexAtRow(3)==1 && view.IndexAtRow(4)==2 && view.IndexAtRow(99)==3 && view.IndexAtRow(-1)==0,"rows follow the laid-out lines");
            foreach(var index in new[]{0,2,3})view.SetLines(index,[[new("x",ChatRole.Body)]]);
            Check(view.NextToLayOut()==-1,"all laid out");
            Check(!view.LaidOutFor(300,"k"),"a first width");
            view.Relayout(300,"k");
            Check(view.LaidOutFor(300.2f,"k") && !view.LaidOutFor(320,"k") && !view.LaidOutFor(300,"other") && view.NextToLayOut()==3 && view.RowCount==4,"a new width lays everything out again");
            Check(new ChatTabView().IndexAtRow(5)==0 && new ChatTabView().RowCount==0,"an empty tab");
        }),
        ("Chat window 1434: a line reads time, channel and sender by channel; the game's glyphs and control characters are removed",() =>
        {
            var options=new ChatSettings{ShowTimestamps=false};
            string Line(ChatRecord record)=>string.Concat(ChatFormat.Runs(record,options,channel=>channel==ChatChannel.Say ? "S":"X").Select(run=>run.Text));
            Check(Line(Record(10,"Ann","hello"))=="[S] Ann: hello","say");
            Check(Line(Record(13,"Ann","hi"))=="[X] Ann >> hi" && Line(Record(12,"Ann","hi"))=="[X] >> Ann: hi","tells");
            Check(Line(Record(57,"","Message"))=="Message" && Line(Record(29,"Ann","Ann waves."))=="Ann waves." && Line(Record(28,"Ann","waves"))=="Ann waves","system and emotes");
            Check(Line(Record(10,"AnnShiva","a\u0002bc"))=="[S] AnnShiva: abc","game glyphs and control characters removed");
            Check(ChatFormat.Clean("a\nb")=="a\nb" && ChatFormat.Clean(null)=="","line breaks kept");
            options.ShowChannel=false;Check(Line(Record(10,"Ann","x"))=="Ann: x","no channel");
            options.ShowTimestamps=true;options.Clock24=true;options.ShowSeconds=true;
            var runs=ChatFormat.Runs(Record(10,"Ann","x"),options,_=>"");
            Check(runs[0].Role==ChatRole.Timestamp && runs[0].Text.Length==9 && runs[0].Text[2]==':' && runs[0].Text[5]==':',"timestamp: "+runs[0].Text);
            options.Clock24=false;options.ShowSeconds=false;
            var twelve=ChatFormat.Runs(Record(10,"Ann","x"),options,_=>"")[0].Text;
            Check(twelve.EndsWith("AM ") || twelve.EndsWith("PM "),"12-hour clock: "+twelve);
            Check(ChatFormat.LayoutKey(options)!=ChatFormat.LayoutKey(new ChatSettings()),"a change of format lays the lines out again");
            Check(ChatFormat.When(Noon.ToUnixTimeSeconds()-30,Noon)==Noon.AddSeconds(-30) && ChatFormat.When(0,Noon)==Noon && ChatFormat.When(1,Noon)==Noon && ChatFormat.When(long.MaxValue,Noon)==Noon,"the game's time stamp when it is plausible, else now");
        }),
        ("Chat window 1434: wrapping breaks between words, cuts a word wider than the line, keeps line breaks and joins runs of one role",() =>
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
        ("Chat window 1434: settings off by default and bounded, two tabs at first use, colours by channel; never the messages",() =>
        {
            var options=new ChatSettings();
            Check(!options.WindowOpen && !options.HideInCombat && options.ShowTimestamps && options.ShowChannel,"off by default: no message is copied");
            options.Opacity=500;options.FontScale=1;options.MaxLines=9;
            Check(options.BoundedOpacity==100 && options.BoundedFontScale==60 && options.BoundedMaxLines==ChatStore.MinimumLines,"bounds");
            var tabs=options.EnsureTabs("All","Battle","Event","Social");
            Check(tabs.Count==4 && !tabs[0].Shows(ChatChannel.Combat) && tabs[0].Shows(ChatChannel.System) && tabs[3].Shows(ChatChannel.Party) && !tabs[3].Shows(ChatChannel.System),"tabs");
            Check(ReferenceEquals(options.EnsureTabs("x","y","z","w"),tabs),"kept");
            options.Colors=new(){[ChatChannel.Say]=0xFF123456};
            Check(options.ColorOf(ChatChannel.Say)==0x123456 && options.ColorOf(ChatChannel.Party)==ChatChannels.DefaultColor(ChatChannel.Party),"colours");
            var configuration=new Configuration();
            Check(!configuration.Chat.WindowOpen && ConfigurationReset.Reset.Contains("Chat") && !ConfigurationReset.Kept.Contains("Chat"),"a setting: « restore every setting » turns the window off");
            configuration.Chat=null!;Check(configuration.Chat is { WindowOpen:false },"« \"Chat\": null » in a hand-edited file becomes the defaults");
            // Nothing of a message can be saved: no saved setting holds a record, a segment or the store.
            var saved=new[]{typeof(Configuration),typeof(ChatSettings),typeof(ChatTabSettings)}.SelectMany(type=>type.GetProperties()).Select(property=>property.PropertyType.ToString()).ToArray();
            Check(saved.Length>100 && !saved.Any(type=>type.Contains("ChatRecord") || type.Contains("ChatSegment") || type.Contains("ChatStore") || type.Contains("ChatTabView")),"no message among the saved settings");
        }),
        ("Chat window 1434: not read during a competitive cut, nothing sent, nothing on disk, every part cleaned up, the window off in a cut",() =>
        {
            var reader=Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),"the chat is not read during a cut");
            Check(reader.Contains("registrations.Register(()=>chat.ChatMessageUnhandled+=OnMessage,()=>chat.ChatMessageUnhandled-=OnMessage);"),"after the other plugins (what they hide stays hidden), released at unload");
            var window=Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("!TenshoAssistant.Competitive.CompetitiveGate.Cut") && window.Contains("!gameGui.GameUiHidden"),"the window is not drawn during a cut, nor with the game's interface hidden");
            foreach(var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant/Chat"),"*.cs").Select(path=>"Chat/"+Path.GetFileName(path)).Append("MainWindow.ChatWindow.cs"))
            {
                var code=Read(file);
                foreach(var gone in new[]{"ProcessChatBox","SendMessage","chatGui.Print","AtomicFile","File.Write","BackgroundWriter","StreamWriter",".Update+=",".Update +="})
                    Check(!code.Contains(gone,StringComparison.Ordinal),$"{file}: {gone} (it only reads, keeps nothing on disk, and has no framework subscription)");
            }
            Check(Read("MainWindow.TrackerWindow.cs").Contains("new(this,ChatPanelDefinition),"),"a panel window: fails alone, its close button written back");
            Check(window.Contains("DrawChatWindowContents,CloseChatWindow,") && window.Contains("ChatMessages.Clear();"),"closing the window drops what it kept");
            var plugin=Read("Plugin.cs");
            // 0.1443 (X-153): the chat family moved to Plugin.Chat.cs.
            Check(Read("Plugin.Chat.cs").Contains("new Chat.ChatReader(chatGui,client,log,()=>configuration.Chat,mainWindow.ChatMessages),()=>chatReader?.Dispose());"),"the reader owned by the plugin's registrations");
            Check(plugin.Contains("is \"chat\") { mainWindow?.ToggleChatWindow(); return; }"),"« /tensho chat »");
            Check(Read("MainWindow.Settings.cs").Contains("DrawChatWindowSettings();"),"its settings in Settings › Panels beside the game");
            Check(window.Contains("ImGui.SetScrollHereY(1);view.MarkSeen();") && window.Contains("view.IndexAtRow((int)(scroll/height))"),"only the rows in view are drawn; the bottom reached clears the count");
        }),
    ];
}
