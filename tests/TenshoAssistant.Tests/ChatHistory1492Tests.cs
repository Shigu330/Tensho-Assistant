using System.Text;
using System.Text.Json;
using TenshoAssistant.Chat;

// 0.1492 (X-475, the user's remark of 15/11 and 16/11: the chat has no memory between two loads or game restarts; X-447 B10: kept on disk, off
// by default, with the player's consent). Behaviour tests (R28.14) on real files in a folder of their own: the format (R02.3), two characters
// never mixed, a write that fails (nothing lost, said once), a line cut short, the bounds (messages, days, a file that grew), « Erase », the
// history turned off, and the messages read back put before this session's lines without a line twice. Then the wiring (consent first, PvP).
internal static class ChatHistory1492Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
    private const ulong Ann = 18014398509481985UL, Bob = 18014398509481986UL;

    private static ChatRecord Said(string text, int minutesAgo = 1, int kind = 10, string sender = "Ann Example", IReadOnlyList<ChatSegment>? body = null)
        => new(0, Now.AddMinutes(-minutesAgo), kind, ChatChannels.From(kind), sender, body ?? [new ChatSegment(text)], 1, 2);

    private static string Folder() => Path.Combine(Path.GetTempPath(), "tensho-chat-history-" + Guid.NewGuid().ToString("N"));

    private static void Clean(string folder) { try { if (Directory.Exists(folder)) Directory.Delete(folder, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

    /// <summary>A writer whose work runs at once (as the background writer would, in order).</summary>
    private static (ChatHistoryWriter Writer, List<string> Warned) Writer(string folder)
    {
        var warned = new List<string>();
        return (new ChatHistoryWriter(folder, (_, work) => work(), (_, message) => warned.Add(message)), warned);
    }

    private static ChatHistory.Loaded Open(ChatHistoryWriter writer, ulong who, int messages = 2000, int days = 7)
    {
        ChatHistory.Loaded? got = null;
        writer.Open(who, Now, messages, days, read => got = read);
        Check(got is not null, "the history read back is handed over");
        return got!;
    }

    private static List<string> Texts(IEnumerable<ChatRecord> records) => [.. records.Select(record => string.Concat(record.Body.Select(segment => segment.Text)))];

    private static string[] Lines(string path) => File.ReadAllText(path).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat history 1492: format 1 (R02.3): a file per character, a first line that says what it is, one message per line, read back whole", () =>
        {
            Check(ChatHistory.FileName(Ann) == "18014398509481985.jsonl" && ChatHistory.FolderName == "ChatHistory", "ChatHistory/<the character's number>.jsonl");
            Check(ChatHistory.Header(Ann) == "{\"format\":\"tensho-chat-history\",\"version\":1,\"character\":18014398509481985}", "the first line: " + ChatHistory.Header(Ann));
            var map = new ChatLink(ChatSegmentKind.MapLink, 140, 20, 26.1f, 24.4f, "Thanalan occidental");
            var item = new ChatLink(ChatSegmentKind.ItemLink, ItemId: 7766, Hq: true);
            var quest = new ChatLink(ChatSegmentKind.QuestLink, QuestId: 65_575);
            var web = new ChatLink(ChatSegmentKind.WebLink, Url: "https://example.org/a?b=1");
            var plugin = new ChatLink(ChatSegmentKind.DalamudLink, Plugin: "Dalamud", CommandId: 3, Data: [2, 39, 7, 1, 3]);
            var body = new List<ChatSegment>
            {
                new("« Salut », dit-elle \"vite\"\nà la ligne "), new(ChatGlyphs.LinkMarker + "Ul'dah", ChatSegmentKind.MapLink, map), new(" "),
                new("Terreau", ChatSegmentKind.ItemLink, item), new("Quête", ChatSegmentKind.QuestLink, quest), new("example.org", ChatSegmentKind.WebLink, web),
                new("Learn more", ChatSegmentKind.DalamudLink, plugin), new(""),
            };
            var record = Said("", 3, 10, "Ann Example@World", body);
            var line = ChatHistory.Encode(record);
            Check(!line.Contains('\n') && !line.Contains('\r'), "one line: " + line);
            using (var json = JsonDocument.Parse(line))
            {
                var root = json.RootElement;
                Check(root.GetProperty("time").GetInt64() == record.Time.ToUnixTimeMilliseconds() && root.GetProperty("kind").GetInt32() == 10
                      && root.GetProperty("sender").GetString() == "Ann Example@World" && root.GetProperty("source").GetInt32() == 1 && root.GetProperty("target").GetInt32() == 2, "time, kind, sender, who and to whom");
                Check(root.GetProperty("body")[1].GetProperty("link").GetProperty("type").GetString() == "MapLink", "a link says its type");
                Check(!root.GetProperty("body")[0].TryGetProperty("link", out _) && !root.GetProperty("body")[7].TryGetProperty("text", out _), "an empty value is left out");
            }
            var back = ChatHistory.Decode(line, 5)!;
            Check(back.Id == 5 && back.Time == record.Time && back.Kind == 10 && back.Channel == ChatChannel.Say && back.Sender == record.Sender && back.Source == 1 && back.Target == 2, "read back: " + back);
            Check(back.Body.Count == body.Count, "every piece");
            for (var index = 0; index < body.Count; index++)
            {
                var (was, got) = (body[index], back.Body[index]);
                // 0.1493 (X-514, review of 0.1492 I1): a link of Dalamud or of a plugin is kept as its text only (ChatHistory1493Tests).
                if (was.Kind == ChatSegmentKind.DalamudLink) was = new ChatSegment(was.Text);
                Check(was.Text == got.Text && was.Kind == got.Kind, $"piece {index}: « {got.Text} » {got.Kind}");
                Check(was.Link is null ? got.Link is null : got.Link is { } link && link with { Data = null } == was.Link with { Data = null }
                      && (was.Link.Data ?? []).SequenceEqual(link.Data ?? []), $"piece {index}: its link {got.Link}");
            }
            Check(ChatHistory.Decode("{\"time\":1,\"kind\":10,\"body\":[{\"text\":\"x\",\"link\":{\"type\":\"Hologram\"}}]}", 1)?.Body[0] is { Link: null, Text: "x" }, "a link type this version does not know stays text");
            foreach (var bad in new[] { "", "{", "{\"time\":5", "[1,2]", "{\"kind\":10}", "{\"time\":-3,\"body\":[]}", "{\"time\":99999999999999999,\"body\":[]}" })
                Check(ChatHistory.Decode(bad, 1) is null, "not a message: " + bad);
        }),
        ("Chat history 1492: a message kept is read back after a reload (a new writer), in order, with its time", () =>
        {
            var folder = Folder();
            try
            {
                var (writer, warned) = Writer(folder);
                Check(Open(writer, Ann).Records.Count == 0, "no file yet: nothing");
                foreach (var (text, ago) in new[] { ("first", 30), ("second", 20), ("third", 10) }) writer.Keep(Said(text, ago));
                writer.Flush(Now, 2000, 7);
                writer.Close(Now, 2000, 7);
                var path = ChatHistory.PathOf(folder, Ann);
                Check(Lines(path) is [var header, _, _, _] && header == ChatHistory.Header(Ann), "the first line, then three messages");
                var (again, _) = Writer(folder);
                var read = Open(again, Ann);
                Check(Texts(read.Records).SequenceEqual(["first", "second", "third"]) && read.Records[0].Time == Now.AddMinutes(-30) && read.Bad == 0 && read.Ours, "read back: " + string.Join(",", Texts(read.Records)));
                Check(read.Records.Select(record => record.Id).SequenceEqual([1L, 2L, 3L]), "numbered from 1");
                Check(warned.Count == 0 && writer.Failures == 0, "nothing failed");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1492: two characters never mixed: each its file, a line queued before the switch goes to its own character", () =>
        {
            var folder = Folder();
            try
            {
                var (writer, _) = Writer(folder);
                Open(writer, Ann);
                writer.Keep(Said("Ann's line"));
                Open(writer, Bob);            // switched before the flush: the queued line is still Ann's
                writer.Keep(Said("Bob's line"));
                writer.Flush(Now, 2000, 7);
                var (fresh, _) = Writer(folder);
                Check(Texts(Open(fresh, Ann).Records).SequenceEqual(["Ann's line"]), "Ann: her line only");
                Check(Texts(Open(fresh, Bob).Records).SequenceEqual(["Bob's line"]), "Bob: his line only");
                // A file holding another character (renamed by hand) is neither read nor written to.
                var bobPath = ChatHistory.PathOf(folder, Bob);
                var annPath = ChatHistory.PathOf(folder, Ann);
                File.Copy(annPath, bobPath, true);
                var before = File.ReadAllBytes(bobPath);
                var (third, warned) = Writer(folder);
                var foreign = Open(third, Bob);
                Check(!foreign.Ours && foreign.Records.Count == 0 && warned.Count == 1, "Ann's file under Bob's name: not read, said");
                third.Keep(Said("Bob again"));
                third.Flush(Now, 2000, 7);
                Check(File.ReadAllBytes(bobPath).SequenceEqual(before), "and never written to");
                Check(third.Pending == 0, "its lines are not kept for a retry that would never work");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1492: a write that fails loses nothing: the lines wait, the failure is said once, the next flush writes them in order", () =>
        {
            var folder = Folder();
            try
            {
                var (writer, warned) = Writer(folder);
                Open(writer, Ann);
                writer.Keep(Said("before"));
                writer.Flush(Now, 2000, 7);
                var path = ChatHistory.PathOf(folder, Ann);
                var before = File.ReadAllBytes(path);
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))   // another program holds the file
                {
                    writer.Keep(Said("during 1"));
                    writer.Flush(Now, 2000, 7);
                    writer.Keep(Said("during 2"));
                    writer.Flush(Now, 2000, 7);
                    Check(writer.Pending == 2 && writer.Failures == 2 && warned.Count == 1, $"kept for later ({writer.Pending}), said once ({warned.Count} of {writer.Failures})");
                }
                Check(File.ReadAllBytes(path).SequenceEqual(before), "the file as it was");
                writer.Keep(Said("after"));
                writer.Flush(Now, 2000, 7);
                Check(writer.Pending == 0, "written");
                var (fresh, _) = Writer(folder);
                Check(Texts(Open(fresh, Ann).Records).SequenceEqual(["before", "during 1", "during 2", "after"]), "nothing lost, in order, nothing twice");
                // The folder cannot be made (a file where it should be): nothing thrown, kept for later.
                var blocked = Folder();
                File.WriteAllText(blocked, "not a folder");
                try
                {
                    var (stuck, said) = Writer(blocked);
                    Open(stuck, Ann);
                    stuck.Keep(Said("waiting"));
                    stuck.Flush(Now, 2000, 7);
                    Check(stuck.Pending == 1 && stuck.Failures >= 1 && said.Count == 1, "no folder: kept for later, said");
                }
                finally { File.Delete(blocked); }
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1492: a line cut short (a crash mid-write) is skipped alone; the next line is not spoilt; the file is trimmed of it", () =>
        {
            var folder = Folder();
            try
            {
                var (writer, _) = Writer(folder);
                Open(writer, Ann);
                writer.Keep(Said("whole"));
                writer.Flush(Now, 2000, 7);
                var path = ChatHistory.PathOf(folder, Ann);
                File.AppendAllText(path, "{\"time\":17632", new UTF8Encoding(false));   // cut short, no line break
                writer.Keep(Said("next"));
                writer.Flush(Now, 2000, 7);
                var (fresh, _) = Writer(folder);
                var read = Open(fresh, Ann);
                Check(Texts(read.Records).SequenceEqual(["whole", "next"]) && read.Bad == 1 && read.Trim, $"the cut line alone skipped ({read.Bad})");
                Check(Lines(path).Length == 3, "written again without it: " + Lines(path).Length);
                // A first line cut short: the file is still Tensho's, its messages are read, the first line made again.
                File.WriteAllText(path, "{\"form\n" + ChatHistory.Encode(Said("kept")) + "\n", new UTF8Encoding(false));
                var (third, _) = Writer(folder);
                Check(Texts(Open(third, Ann).Records).SequenceEqual(["kept"]) && Lines(path)[0] == ChatHistory.Header(Ann), "a damaged first line made again");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1492: the bounds: the last messages (combat lines and the others each), the days, and a file that grew is trimmed", () =>
        {
            var folder = Folder();
            try
            {
                var (writer, _) = Writer(folder);
                Open(writer, Ann, 100, 7);
                for (var index = 0; index < 150; index++) writer.Keep(Said("talk " + index, 600 - index));
                for (var index = 0; index < 30; index++) writer.Keep(Said("hit " + index, 300 - index, 41));
                writer.Keep(Said("too old", 8 * 24 * 60));
                writer.Flush(Now, 100, 7);
                var path = ChatHistory.PathOf(folder, Ann);
                Check(Lines(path).Length == 1 + 181, "everything written first: " + Lines(path).Length);
                var (fresh, _) = Writer(folder);
                var read = Open(fresh, Ann, 100, 7);
                var texts = Texts(read.Records);
                Check(texts.Count(text => text.StartsWith("talk", StringComparison.Ordinal)) == 100 && texts.Contains("talk 149") && !texts.Contains("talk 49"), "the last 100 spoken lines");
                Check(texts.Count(text => text.StartsWith("hit", StringComparison.Ordinal)) == 30, "the combat lines have their own bound: a raid never pushes a conversation out");
                Check(!texts.Contains("too old"), "older than 7 days: gone");
                Check(Lines(path).Length == 1 + 130 && read.Trim, "the file trimmed to the bounds: " + Lines(path).Length);
                Check(ChatHistory.BoundedMessages(5) == 100 && ChatHistory.BoundedMessages(50_000) == 10_000 && ChatHistory.BoundedDays(0) == 1 && ChatHistory.BoundedDays(500) == 90, "bounded settings");
                // Written past twice the bound since read: trimmed at the next flush.
                for (var index = 0; index < 201; index++) fresh.Keep(Said("later " + index, 60 - index % 60));
                fresh.Flush(Now, 100, 7);
                Check(Lines(path).Length <= 1 + 100 + 30, "trimmed once it grew past twice the bound: " + Lines(path).Length);
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1492: turned off, nothing queued is written; « Erase » deletes every character's file, even queued work is void", () =>
        {
            var folder = Folder();
            try
            {
                var queued = new List<Action>();
                var writer = new ChatHistoryWriter(folder, (_, work) => queued.Add(work), (_, _) => { });
                writer.Open(Ann, Now, 2000, 7, _ => { });
                writer.Keep(Said("kept"));
                writer.Flush(Now, 2000, 7);
                foreach (var work in queued) work();
                queued.Clear();
                writer.Keep(Said("not kept"));
                writer.Flush(Now, 2000, 7);   // handed over...
                writer.Drop();                // ...then the history is turned off before it runs
                foreach (var work in queued) work();
                queued.Clear();
                var (reader, _) = Writer(folder);
                Check(Texts(Open(reader, Ann).Records).SequenceEqual(["kept"]), "turned off: the queued line is not written");
                writer.Keep(Said("closed"));
                Check(writer.Pending == 0 && writer.Character == 0, "nothing is queued once off");
                reader.Keep(Said("Ann later"));
                reader.Flush(Now, 2000, 7);
                Open(reader, Bob);
                reader.Keep(Said("Bob's"));
                reader.Flush(Now, 2000, 7);
                Check(File.Exists(ChatHistory.PathOf(folder, Ann)) && File.Exists(ChatHistory.PathOf(folder, Bob)), "two files");
                bool? all = null;
                reader.Keep(Said("queued before erase"));
                reader.Erase(done => all = done);
                Check(all == true && !Directory.Exists(folder), "every file deleted, and the empty folder");
                reader.Flush(Now, 2000, 7);
                Check(!Directory.Exists(folder), "what was queued before « Erase » is not written after it");
                reader.Keep(Said("after erase"));
                reader.Flush(Now, 2000, 7);
                Check(Texts(ChatHistory.Read(ChatHistory.PathOf(folder, Bob), Bob, Now, 2000, 7).Records).SequenceEqual(["after erase"]), "the history stays on: the next messages are kept again");
                using (new FileStream(ChatHistory.PathOf(folder, Bob), FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    reader.Erase(done => all = done);
                    Check(all == false, "a file held by another program: said");
                }
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1492: read back, the messages go before this session's lines, never twice, numbered after, each tab taken as read", () =>
        {
            var store = new ChatStore();
            var tab = new ChatTabSettings { Name = "General", Channels = [ChatChannel.Say] };
            // 0.1493 (X-515 M4): the session's line as the history writes it (same sender, who and to whom): a line of the same second is told
            // apart by them now (ChatHistory1493Tests).
            store.Add(Now.AddMinutes(-5), 10, "Ann Example", [new("this session")], 2000, 1, 2);
            var view = store.ViewOf(tab);
            view.Refresh(store, tab, 2000);
            var older = new List<ChatRecord> { Said("yesterday", 24 * 60), Said("an hour ago", 60), Said("this session", 5) };
            Check(store.Restore(older, 2000) == 2, "only those older than the oldest line kept (the session's line is not twice)");
            var records = store.After(0);
            Check(Texts(records).SequenceEqual(["yesterday", "an hour ago", "this session"]), "the old ones before: " + string.Join(",", Texts(records)));
            Check(records.Zip(records.Skip(1)).All(pair => pair.First.Id < pair.Second.Id) && records[0].Id > 1, "numbered again, after the last number given");
            var rebuilt = store.ViewOf(tab);
            Check(!ReferenceEquals(rebuilt, view), "each tab's view made again");
            rebuilt.Refresh(store, tab, 2000);
            Check(rebuilt.Count == 3 && rebuilt.Unread == 0, "every line shown, taken as read");
            Check(store.Restore(older, 2000) == 0, "read back twice: nothing added");
            var full = new ChatStore();
            Check(full.Restore([.. Enumerable.Range(0, 150).Select(index => Said("old " + index, 500 - index))], 100) == 150 && full.Count == 100, "the window's bound kept");
        }),
        ("Chat history 1492: off by default; ticking it asks first (what, where, how long, how to erase); only « Keep the history » turns it on", () =>
        {
            var settings = new ChatSettings();
            // 0.1494 (X-513, the user's request of 16/11): 30 days by default (ChatHistory1494Tests).
            Check(!settings.KeepHistory && settings.BoundedHistoryMessages == 2000 && settings.BoundedHistoryDays == 30, "off, 2,000 messages, 30 days");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("value=>{if(value)chatHistoryAsk=true;else configuration.Chat.KeepHistory=false;}", StringComparison.Ordinal), "ticking only asks");
            Check(window.Contains("if(chatHistoryAsk){chatHistoryAsk=false;ImGui.OpenPopup(\"###ChatHistoryConsent\");}", StringComparison.Ordinal)
                  && window.Contains("localization.Format(\"ChatWindow.HistoryConsentText\",ChatHistoryFolder ?? \"-\",", StringComparison.Ordinal)
                  && window.Contains("localization.Format(\"ChatWindow.HistoryNumber\",ChatOptions.BoundedHistoryMessages)", StringComparison.Ordinal)
                  && window.Contains("localization.Format(\"ChatWindow.HistoryNumber\",ChatOptions.BoundedHistoryDays)", StringComparison.Ordinal), "the question says the folder and the bounds (0.1494, X-513: « no limit » in words)");
            Check(window.Split("configuration.Chat.KeepHistory=true;").Length == 2 && window.Contains("\"###ChatHistoryYes\")){configuration.Chat.KeepHistory=true;", StringComparison.Ordinal), "only its « yes » turns it on");
            Check(window.Contains("ImGui.OpenPopup(\"###ChatHistoryEraseAsk\")", StringComparison.Ordinal) && window.Contains("EraseChatHistory?.Invoke(", StringComparison.Ordinal), "« Erase » asks first");
            foreach (var language in new[] { "en", "fr" })
            {
                using var json = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
                var chat = json.RootElement.GetProperty("ChatWindow");
                var text = chat.GetProperty("HistoryConsentText").GetString() ?? "";
                Check(text.Contains("{0}") && text.Contains("{1}") && text.Contains("{2}"), language + ": where, how many, how long");
                foreach (var key in new[] { "KeepHistory", "KeepHistoryTip", "HistoryMessages", "HistoryMessagesTip", "HistoryDays", "HistoryDaysTip", "HistoryErase", "HistoryEraseTip", "HistoryConsentTitle", "HistoryConsentYes", "HistoryCancel", "HistoryEraseTitle", "HistoryEraseText", "HistoryEraseYes", "HistoryErased", "HistoryEraseLeft" })
                    Check(chat.TryGetProperty(key, out var value) && value.GetString() is { Length: > 0 }, $"{language}: {key}");
                Check(chat.GetProperty("KeepHistoryTip").GetString()!.Contains(language == "en" ? "Default: off." : "Par défaut : désactivé.", StringComparison.Ordinal), language + ": off by default, said");
            }
        }),
        ("Chat history 1492: the game's side: written off the game's thread, stopped in PvP, dropped when turned off, opened per character", () =>
        {
            var keeper = Read("src/TenshoAssistant/Chat/ChatHistory.Dalamud.cs");
            Check(keeper.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);", StringComparison.Ordinal) && keeper.Contains("CompetitiveGate.Remove(framework,OnUpdate);", StringComparison.Ordinal), "through the competitive gate (nothing in PvP)");
            Check(keeper.Contains("new ChatHistoryWriter(Path.Combine(pluginFolder,ChatHistory.FolderName),background.Enqueue,", StringComparison.Ordinal), "a background writer, in order");
            Check(keeper.Contains("if(!current.KeepHistory)", StringComparison.Ordinal) && keeper.Contains("writer.Drop();", StringComparison.Ordinal), "turned off: dropped");
            Check(keeper.Contains("player.ContentId:0UL", StringComparison.Ordinal) && keeper.Contains("current.WindowOpen && client.IsLoggedIn && player.IsLoaded", StringComparison.Ordinal), "per character, while the window shows");
            Check(keeper.Contains("store.Restore(back.Records,current.BoundedMaxLines)", StringComparison.Ordinal), "read back before this session's lines");
            Check(!keeper.Contains("Http", StringComparison.Ordinal) && !Read("src/TenshoAssistant/Chat/ChatHistory.cs").Contains("Http", StringComparison.Ordinal), "never sent anywhere");
            var reader = Read("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("var record=store.Add(", StringComparison.Ordinal) && reader.Contains("history?.Keep(record);", StringComparison.Ordinal) && reader.Contains("history?.Dispose();", StringComparison.Ordinal),
                  "the reader hands each copied message over, and releases the history");
            Check(reader.IndexOf("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;", StringComparison.Ordinal) < reader.IndexOf("history?.Keep(record);", StringComparison.Ordinal), "not read, so not kept, in PvP");
            Check(Read("src/TenshoAssistant/Plugin.Chat.cs").Contains("new Chat.ChatHistoryKeeper(framework,client,player,log,()=>configuration.Chat,mainWindow.ChatMessages,pluginFolder)", StringComparison.Ordinal), "made with the reader");
            Check(!Read("src/TenshoAssistant/MainWindow.Caching.cs").Contains("ChatHistory", StringComparison.Ordinal) && !Read("src/TenshoAssistant/Link/LocalLink.cs").Contains("ChatHistory", StringComparison.Ordinal), "in no copy and no link to the desktop app");
        }),
    ];
}
