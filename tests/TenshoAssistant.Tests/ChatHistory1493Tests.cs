using System.Globalization;
using System.Text;
using TenshoAssistant.Chat;

// 0.1493 (X-514 and X-515, review of 0.1492). Behaviour tests (R28.14) on real files in a folder of their own: a link of Dalamud or of a
// plugin is never a link once read back from disk (its command number is valid in its session only); « Erase » right after a read back is
// not undone; « off » or « Erase » while a write is under way takes that write back; the lines of the same second as this session's first are
// read back, the session's own not twice; the log never carries the character's number. Then the desktop app's settings (greyed, said why).
internal static class ChatHistory1493Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
    private const ulong Ann = 18014398509481985UL, Bob = 18014398509481986UL;
    private static readonly ChatSettings Bare = new() { ShowTimestamps = false, ShowChannel = false, GameLinks = false };

    private static ChatRecord Said(string text, int minutesAgo = 1, int kind = 10, string sender = "Ann Example", IReadOnlyList<ChatSegment>? body = null)
        => new(0, Now.AddMinutes(-minutesAgo), kind, ChatChannels.From(kind), sender, body ?? [new ChatSegment(text)], 1, 2);

    private static ChatRecord At(DateTimeOffset time, int kind, string text, string sender = "")
        => new(0, time, kind, ChatChannels.From(kind), sender, [new ChatSegment(text)]);

    private static string Folder() => Path.Combine(Path.GetTempPath(), "tensho-chat-history-" + Guid.NewGuid().ToString("N"));

    private static void Clean(string folder) { try { if (Directory.Exists(folder)) Directory.Delete(folder, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

    private static (ChatHistoryWriter Writer, List<string> Warned) Writer(string folder)
    {
        var warned = new List<string>();
        return (new ChatHistoryWriter(folder, (_, work) => work(), (_, message) => warned.Add(message)), warned);
    }

    private static ChatHistory.Loaded Open(ChatHistoryWriter writer, ulong who)
    {
        ChatHistory.Loaded? got = null;
        writer.Open(who, Now, 2000, 7, read => got = read);
        Check(got is not null, "the history read back is handed over");
        return got!;
    }

    private static List<string> Texts(IEnumerable<ChatRecord> records) => [.. records.Select(record => string.Concat(record.Body.Select(segment => segment.Text)))];

    /// <summary>Yesterday's line on disk for <paramref name="who"/>.</summary>
    private static void Yesterday(string folder, ulong who)
    {
        var (writer, _) = Writer(folder);
        writer.Open(who, Now, 2000, 7, _ => { });
        writer.Keep(Said("yesterday", 24 * 60));
        writer.Flush(Now, 2000, 7);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat history 1493 (X-514): a link of Dalamud or of a plugin is read back as its text, never a link a click could run", () =>
        {
            var folder = Folder();
            try
            {
                var plugin = new ChatLink(ChatSegmentKind.DalamudLink, Plugin: "SomePlugin", CommandId: 7, Data: [2, 39, 7, 1, 3]);
                var place = new ChatLink(ChatSegmentKind.MapLink, 130, 14, 10.5f, 11.2f, "Ul'dah");
                var record = Said("", 2, body: [new("Teleport "), new("[Click here]", ChatSegmentKind.DalamudLink, plugin), new(" "), new(ChatGlyphs.LinkMarker + "Ul'dah", ChatSegmentKind.MapLink, place)]);
                var (writer, _) = Writer(folder);
                Open(writer, Ann);
                writer.Keep(record);
                writer.Flush(Now, 2000, 7);
                var path = ChatHistory.PathOf(folder, Ann);
                var file = File.ReadAllText(path);
                Check(!file.Contains("DalamudLink", StringComparison.Ordinal) && !file.Contains("SomePlugin", StringComparison.Ordinal) && !file.Contains("\"command\"", StringComparison.Ordinal),
                      "never written as a link (plugin, command, data): " + file);
                Check(file.Contains("[Click here]", StringComparison.Ordinal) && file.Contains("\"MapLink\"", StringComparison.Ordinal), "its text kept, the other links too");
                // A line written by 0.1492, with such a link whole: read back as its text.
                var old = "{\"time\":" + Now.AddMinutes(-1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
                          + ",\"kind\":10,\"body\":[{\"text\":\"[Run]\",\"link\":{\"type\":\"DalamudLink\",\"plugin\":\"SomePlugin\",\"command\":7,\"data\":\"AicHAQM=\"}}]}";
                File.AppendAllText(path, old + "\n", new UTF8Encoding(false));
                var (fresh, _) = Writer(folder);
                var read = Open(fresh, Ann);
                Check(read.Records.Count == 2 && read.Bad == 0, $"both read back ({read.Records.Count}, {read.Bad} bad)");
                Check(Texts(read.Records)[0] == "Teleport [Click here] " + ChatGlyphs.LinkMarker + "Ul'dah" && Texts(read.Records)[1] == "[Run]", "their text: " + string.Join(" | ", Texts(read.Records)));
                foreach (var back in read.Records)
                {
                    Check(back.Body.All(piece => piece.Kind != ChatSegmentKind.DalamudLink && piece.Link is not { Kind: ChatSegmentKind.DalamudLink }), "no link of a plugin read back");
                    var runs = ChatFormat.Runs(back, Bare, _ => "");
                    Check(!runs.Any(run => run.Link is { Kind: ChatSegmentKind.DalamudLink }), "the window has nothing to click that would run a plugin's command");
                }
                Check(read.Records[0].Body.Any(piece => piece.Link is { Kind: ChatSegmentKind.MapLink, TerritoryId: 130 }), "a place stays a link (it runs no command)");
                // Trimmed and written again, the file no longer holds the old link at all.
                ChatHistory.Trim(path, Ann, Now, 2000, 7);
                Check(!File.ReadAllText(path).Contains("SomePlugin", StringComparison.Ordinal), "written again: gone from the disk");
                // This session's line keeps its link (the command is valid now).
                Check(record.Body[1].Link is { Kind: ChatSegmentKind.DalamudLink, CommandId: 7 }, "the session's own message unchanged");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1493 (X-515 M1): « Erase » right after a read back not yet shown: nothing comes back into the window", () =>
        {
            var folder = Folder();
            try
            {
                var queued = new List<Action>();
                void RunAll() { while (queued.Count > 0) { var work = queued[0]; queued.RemoveAt(0); work(); } }
                var writer = new ChatHistoryWriter(folder, (_, work) => queued.Add(work), (_, _) => { });
                var store = new ChatStore();
                // What the game's thread does once a second: the read back, if any, put before this session's lines.
                int Second() => writer.Take() is { } back ? store.Restore(back.Records, 2000) : 0;
                // The normal case: read back once.
                Yesterday(folder, Ann);
                writer.Open(Ann, Now, 2000, 7);
                RunAll();
                store.Add(Now, 10, "Ann", [new("this session")], 2000);
                Check(Second() == 1 && Texts(store.After(0)).SequenceEqual(["yesterday", "this session"]) && writer.Take() is null, "read back and shown once");
                // The read is done, not yet shown; the player confirms « Erase » (the files, what is queued, the window's lines).
                store.Clear();
                Yesterday(folder, Ann);
                writer.Open(Ann, Now, 2000, 7);
                RunAll();
                writer.Erase();
                store.Clear();
                RunAll();
                Check(Second() == 0 && store.Count == 0, "the next second puts nothing back: " + string.Join(",", Texts(store.After(0))));
                Check(!File.Exists(ChatHistory.PathOf(folder, Ann)), "and nothing is on disk");
                // « Erase » while the read is still queued: nothing either.
                Yesterday(folder, Ann);
                writer.Open(Ann, Now, 2000, 7);
                writer.Erase();
                RunAll();
                Check(Second() == 0 && store.Count == 0, "a read that ran after « Erase » shows nothing");
                // Turned off after the read: nothing comes back.
                Yesterday(folder, Ann);
                writer.Open(Ann, Now, 2000, 7);
                RunAll();
                writer.Drop();
                Check(Second() == 0, "turned off: the read back is dropped");
                // Another character opened before the read is shown: Ann's lines never go to Bob's window.
                writer.Open(Ann, Now, 2000, 7);
                writer.Open(Bob, Now, 2000, 7);
                RunAll();
                Check(writer.Take() is { Records.Count: 0 }, "only Bob's read back (none) waits");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1493 (X-515 M3): turned off or erased while a write is under way: that write is taken back, nothing after it", () =>
        {
            var folder = Folder();
            try
            {
                ChatHistoryWriter? writer = null;
                Action? during = null;
                var appends = 0;
                writer = new ChatHistoryWriter(folder, (_, work) => work(), (_, _) => { }, (path, who, lines) =>
                {
                    appends++;
                    var before = ChatHistory.Append(path, who, lines);
                    during?.Invoke();   // the player acts while these lines are being written
                    return before;
                });
                writer.Open(Ann, Now, 2000, 7, _ => { });
                writer.Keep(Said("kept before"));
                writer.Flush(Now, 2000, 7);
                var annPath = ChatHistory.PathOf(folder, Ann);
                var before = File.ReadAllBytes(annPath);
                // Turned off during the write: the file as it was before it; a second character's lines, in the same work, not written.
                writer.Keep(Said("written while unticked"));
                writer.Open(Bob, Now, 2000, 7, _ => { });
                writer.Keep(Said("Bob's line"));
                during = () => writer!.Drop();
                appends = 0;
                writer.Flush(Now, 2000, 7);
                Check(File.ReadAllBytes(annPath).SequenceEqual(before), "the write the « off » overtook is taken back");
                Check(appends == 1 && !File.Exists(ChatHistory.PathOf(folder, Bob)), "nothing written after it");
                Check(writer.Pending == 0, "and nothing kept for later");
                // A file made by the overtaken write is removed.
                during = null;
                writer.Open(Bob, Now, 2000, 7, _ => { });
                writer.Keep(Said("first of Bob"));
                during = () => writer!.Drop();
                writer.Flush(Now, 2000, 7);
                Check(!File.Exists(ChatHistory.PathOf(folder, Bob)), "made by that write: deleted");
                // « Erase » during the write: the files go all the same.
                during = null;
                writer.Open(Ann, Now, 2000, 7, _ => { });
                writer.Keep(Said("before erase"));
                during = () => writer!.Erase();
                writer.Flush(Now, 2000, 7);
                Check(!Directory.Exists(folder), "erased, the folder too");
                // Nothing overtaken: written as before.
                during = null;
                writer.Keep(Said("after"));
                writer.Flush(Now, 2000, 7);
                Check(Texts(ChatHistory.Read(annPath, Ann, Now, 2000, 7).Records).SequenceEqual(["after"]), "the history stays on: written");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1493 (X-515 M4): the lines of the same second as this session's first are read back, the session's own not twice", () =>
        {
            var store = new ChatStore();
            var second = Now.AddMinutes(-1);
            store.Add(second, 41, "", [new("You hit the dummy.")], 2000);   // this session's first line, in that second
            store.Add(Now, 10, "Ann", [new("after")], 2000);
            var older = new List<ChatRecord>
            {
                At(Now.AddMinutes(-5), 10, "before", "Ann"),
                At(second, 41, "You hit the dummy."),     // the same line, written before the reload
                At(second, 41, "You hit the dummy."),     // this session's own, written by it
                At(second, 62, "You obtain a potion."),   // loot of that second, before the reload
            };
            Check(store.Restore(older, 2000) == 3, "three put back");
            Check(Texts(store.After(0)).SequenceEqual(["before", "You hit the dummy.", "You obtain a potion.", "You hit the dummy.", "after"]),
                  "the lines of that second are not lost, the session's own is not twice: " + string.Join(",", Texts(store.After(0))));
            Check(store.Restore(older, 2000) == 0, "read back twice: nothing added");
            var lone = new ChatStore();
            lone.Add(second, 41, "", [new("You hit the dummy.")], 2000);
            Check(lone.Restore([At(second, 41, "You hit the dummy.")], 2000) == 0, "the session's own line alone: never twice");
        }),
        ("Chat history 1493 (X-515 M5): a failure of the history never puts the character's number in the log", () =>
        {
            var folder = Folder();
            try
            {
                var errors = new List<(Exception Error, string Message)>();
                var writer = new ChatHistoryWriter(folder, (_, work) => work(), (error, message) => errors.Add((error, message)));
                writer.Open(Ann, Now, 2000, 7, _ => { });
                writer.Keep(Said("x"));
                writer.Flush(Now, 2000, 7);
                var path = ChatHistory.PathOf(folder, Ann);
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    writer.Keep(Said("held"));
                    writer.Flush(Now, 2000, 7);
                }
                Check(errors.Count == 1, "said once: " + errors.Count);
                var number = Ann.ToString(CultureInfo.InvariantCulture);
                var (error, message) = errors[0];
                var line = ChatHistory.Report(error, message);
                Check(!line.Contains(number, StringComparison.Ordinal) && line.StartsWith(message, StringComparison.Ordinal) && line.Contains(error.GetType().Name, StringComparison.Ordinal), "the log line: " + line);
                var made = new IOException($"Could not find a part of the path '{path}'. Also '{path}.tmp', line 12.");
                var scrubbed = ChatHistory.Report(made, "The chat history could not be written.");
                Check(made.Message.Contains(number, StringComparison.Ordinal) && !scrubbed.Contains(number, StringComparison.Ordinal)
                      && scrubbed.Contains("<character>.jsonl'", StringComparison.Ordinal) && scrubbed.Contains("<character>.jsonl.tmp", StringComparison.Ordinal)
                      && scrubbed.Contains("line 12", StringComparison.Ordinal), "the number replaced, the rest kept: " + scrubbed);
                var keeper = Read("src/TenshoAssistant/Chat/ChatHistory.Dalamud.cs");
                Check(!keeper.Contains("log.Warning(error", StringComparison.Ordinal) && keeper.Contains("log.Warning(ChatHistory.Report(error,message))", StringComparison.Ordinal)
                      && keeper.Contains("new BackgroundWriter(Warn)", StringComparison.Ordinal) && keeper.Contains("background.Enqueue,Warn)", StringComparison.Ordinal),
                      "the game's side logs the scrubbed line only, the background writer's failures too");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1493 (X-515 M2): outside the game (the desktop app) the history's settings are greyed, with why, and never ask", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("var here=ChatHistoryFolder is not null && EraseChatHistory is not null;", StringComparison.Ordinal)
                  && window.Contains("ImGui.BeginDisabled(!here);", StringComparison.Ordinal)
                  && window.Contains("ImGui.BeginDisabled(!configuration.Chat.KeepHistory || !here);", StringComparison.Ordinal), "greyed outside the game");
            Check(window.Contains("chatHistoryAsk=false;\n            ImGui.TextColored(Muted,localization.Get(\"ChatWindow.HistoryGameOnly\"));", StringComparison.Ordinal), "said why; the question (with its folder) never opens there");
            foreach (var language in new[] { "en", "fr" })
                Check(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").Contains("\"HistoryGameOnly\": \"", StringComparison.Ordinal), language + ": the reason");
            Check(!Read("src/TenshoAssistant.Desktop/Host.cs").Contains("ChatHistoryFolder", StringComparison.Ordinal), "the desktop app gives no folder (it never keeps a history)");
        }),
    ];
}
