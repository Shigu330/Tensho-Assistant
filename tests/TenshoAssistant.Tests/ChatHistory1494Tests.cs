using System.Text;
using Newtonsoft.Json;
using TenshoAssistant;
using TenshoAssistant.Chat;

// 0.1494 (X-513, the user's request of 16/11, choice (a): « aucune limite de message et juste 7j impossible »): « No limit » for the messages and
// for the days, each apart; 30 days by default; the place the character's history takes on disk shown; a big history read back at once.
// Behaviour tests (R28.14) on real files in a folder of their own: nothing removed with « No limit », a limit then « No limit » then a limit
// again, a big file read from its end only, a trim past the read budget that loses nothing, the size measured; a saved setting keeps its value.
internal static class ChatHistory1494Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
    private const ulong Ann = 18014398509481985UL;
    private const int No = ChatHistory.NoLimit;

    private static ChatRecord Said(string text, double minutesAgo = 1, int kind = 10)
        => new(0, Now.AddMinutes(-minutesAgo), kind, ChatChannels.From(kind), "Ann Example", [new ChatSegment(text)], 1, 2);

    private static string Folder() => Path.Combine(Path.GetTempPath(), "tensho-chat-history-" + Guid.NewGuid().ToString("N"));

    private static void Clean(string folder) { try { if (Directory.Exists(folder)) Directory.Delete(folder, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

    private static ChatHistoryWriter Writer(string folder) => new(folder, (_, work) => work(), (_, _) => { });

    private static ChatHistory.Loaded Open(ChatHistoryWriter writer, int messages, int days)
    {
        ChatHistory.Loaded? got = null;
        writer.Open(Ann, Now, messages, days, read => got = read);
        Check(got is not null, "the history read back is handed over");
        return got!;
    }

    private static List<string> Texts(IEnumerable<ChatRecord> records) => [.. records.Select(record => string.Concat(record.Body.Select(segment => segment.Text)))];

    private static int Lines(string path) => File.ReadAllText(path).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>A history file written at once: its first line, then <paramref name="count"/> messages, one in ten a combat line, the last the newest.</summary>
    private static void Big(string path, int count, double minutesApart = 0.01)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = new StreamWriter(path, false, new UTF8Encoding(false));
        output.Write(ChatHistory.Header(Ann) + "\n");
        for (var index = 0; index < count; index++)
            output.Write(ChatHistory.Encode(Said((index % 10 == 9 ? "hit " : "talk ") + index, (count - index) * minutesApart, index % 10 == 9 ? 41 : 10)) + "\n");
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat history 1494 (X-513): « No limit » on both: nothing is removed, however many and however old; the window gets the last of each kind", () =>
        {
            var folder = Folder();
            try
            {
                var writer = Writer(folder);
                Check(Open(writer, No, No).Records.Count == 0, "no file yet");
                writer.Keep(Said("from last spring", 200 * 24 * 60));
                for (var index = 0; index < 450; index++) writer.Keep(Said("talk " + index, 450 - index));
                writer.Flush(Now, No, No);
                var path = ChatHistory.PathOf(folder, Ann);
                Check(Lines(path) == 1 + 451, "every line written: " + Lines(path));
                var again = Open(Writer(folder), No, No);
                Check(again.Records.Count == 451 && !again.Trim && Texts(again.Records)[0] == "from last spring", "read back whole, nothing to trim");
                Check(Lines(path) == 1 + 451, "the file untouched by the open");
                var read = ChatHistory.Read(path, Ann, Now, No, 30);
                Check(read.Trim && !Texts(read.Records).Contains("from last spring") && read.Records.Count == 450, "days 30 with messages « No limit »: the old line only is past the bound");
                ChatHistory.Trim(path, Ann, Now, No, 30);
                Check(Lines(path) == 1 + 450 && !ChatHistory.Read(path, Ann, Now, No, 30).Trim, "trimmed of it alone");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1494 (X-513): a limit, then « No limit », then the limit again: kept while unlimited, trimmed when the limit is back", () =>
        {
            var folder = Folder();
            try
            {
                var path = ChatHistory.PathOf(folder, Ann);
                var writer = Writer(folder);
                Open(writer, 100, 30);
                for (var index = 0; index < 250; index++) writer.Keep(Said("first " + index, 1000 - index));
                writer.Flush(Now, 100, 30);
                Check(Lines(path) <= 1 + 100, "the limit: trimmed once past twice the bound: " + Lines(path));
                // « No limit » ticked: the next open trims nothing, 250 more lines are all kept, whatever the count since the open.
                var unlimited = Writer(folder);
                Check(!Open(unlimited, No, 30).Trim, "nothing past the bounds");
                for (var index = 0; index < 25_000; index++)
                {
                    unlimited.Keep(Said("second " + index, 700 - index * 0.01));
                    if (index % 1000 == 999) unlimited.Flush(Now, No, 30);   // once a second, as the game's side does
                }
                var all = Lines(path) - 1;
                Check(all == 25_000 + 100, "« No limit »: every line kept (" + all + ")");
                var shown = ChatHistory.Read(path, Ann, Now, No, 30);
                Check(shown.Records.Count == ChatHistory.MaximumMessages && !shown.Trim && Texts(shown.Records)[^1] == "second 24999", "the window gets the last 10,000 of that kind, nothing trimmed");
                // The limit back: the next open trims to it.
                var limited = Writer(folder);
                var back = Open(limited, 100, 30);
                Check(back.Records.Count == 100 && Texts(back.Records)[^1] == "second 24999" && Lines(path) == 1 + 100, "the limit back: trimmed to the last 100: " + Lines(path));
                Check(!ChatHistory.Read(path, Ann, Now, 100, 30).Trim, "exactly the bound: no trim at each open");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1494 (X-513): a big history is read back from its end only, and a trim past the read budget loses nothing", () =>
        {
            var folder = Folder();
            try
            {
                var path = ChatHistory.PathOf(folder, Ann);
                Big(path, 120_000);
                var size = new FileInfo(path).Length;
                var read = ChatHistory.Read(path, Ann, Now, 2000, No);
                Check(read.Records.Count == 4000 && read.Lines < 25_000 && read.Trim, $"the last 2,000 of each kind, from the end only ({read.Lines} lines read of 120,000)");
                Check(Texts(read.Records)[^1] == "hit 119999" && Texts(read.Records).Count(text => text.StartsWith("hit", StringComparison.Ordinal)) == 2000, "the newest kept");
                Check(read.Records.Select(record => record.Id).SequenceEqual(Enumerable.Range(1, 4000).Select(index => (long)index)), "numbered from 1, in order");
                // « No limit » with a small read budget: the window gets what the budget reached, the file is never cut to it.
                var tail = ChatHistory.Read(path, Ann, Now, No, No, 64 * 1024);
                Check(tail.Records.Count > 0 && tail.Records.Count < 2000 && !tail.Trim && new FileInfo(path).Length == size, "budget reached: nothing to trim, the file whole");
                var writer = Writer(folder);
                Check(!Open(writer, No, No).Trim && new FileInfo(path).Length == size, "opened with « No limit »: the file whole");
                // A limit with the budget reached: the trim goes through the whole file (the oldest past the bound go, not what the budget missed).
                Check(ChatHistory.Read(path, Ann, Now, 100, No, 64 * 1024).Trim, "past the budget with a limit: trimmed");
                ChatHistory.Trim(path, Ann, Now, 100, No);
                var after = ChatHistory.Read(path, Ann, Now, 100, No);
                Check(Lines(path) == 1 + 200 && Texts(after.Records)[^1] == "hit 119999" && Texts(after.Records)[0] == "hit 119009"
                      && Texts(after.Records).Count(text => text.StartsWith("talk", StringComparison.Ordinal)) == 100 && Texts(after.Records).Contains("talk 119888")
                      && !Texts(after.Records).Contains("talk 119887"), "the last 100 of each kind of the whole file: " + Texts(after.Records)[0]);
                // The days through the whole file too: a file made long ago, a minute apart.
                Big(path, 50_000, 1);
                ChatHistory.Trim(path, Ann, Now, No, 30);
                var days = ChatHistory.Read(path, Ann, Now, No, No);
                Check(Lines(path) - 1 == 30 * 24 * 60 && days.Records.All(record => record.Time >= Now.AddDays(-30)), "older than 30 days: gone, line by line: " + (Lines(path) - 1));
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1494 (X-513): the place on disk is measured after each read, write and erase; shown in KB, MB or GB", () =>
        {
            var folder = Folder();
            try
            {
                var writer = Writer(folder);
                Check(writer.Bytes == -1, "nothing open: not shown");
                Open(writer, 2000, 30);
                Check(writer.Bytes == 0, "no file yet: 0");
                writer.Keep(Said("a line"));
                writer.Flush(Now, 2000, 30);
                var path = ChatHistory.PathOf(folder, Ann);
                Check(writer.Bytes == new FileInfo(path).Length && writer.Bytes > 0, "measured after the write: " + writer.Bytes);
                writer.Erase();
                Check(writer.Bytes == 0, "erased: 0");
                writer.Drop();
                Check(writer.Bytes == -1, "turned off: not shown");
                Check(ChatHistory.Size(0) == (0, "KB") && ChatHistory.Size(1) == (1, "KB") && ChatHistory.Size(1536) == (2, "KB")
                      && ChatHistory.Size(5L * 1024 * 1024) == (5, "MB") && ChatHistory.Size(3L * 1024 * 1024 * 1024) == (3, "GB"), "units");
            }
            finally { Clean(folder); }
        }),
        ("Chat history 1494 (X-513): 30 days by default; « No limit » off by default; a saved setting keeps its value", () =>
        {
            var settings = new ChatSettings();
            Check(settings.HistoryDays == 30 && settings.HistoryMessages == 2000 && !settings.HistoryMessagesUnlimited && !settings.HistoryDaysUnlimited, "the defaults");
            Check(settings.BoundedHistoryDays == 30 && settings.BoundedHistoryMessages == 2000, "bounded as before");
            settings.HistoryMessagesUnlimited = true; settings.HistoryDaysUnlimited = true;
            Check(settings.BoundedHistoryMessages == No && settings.BoundedHistoryDays == No && settings.HistoryMessages == 2000, "« No limit »: the number kept beside");
            settings.HistoryMessages = int.MaxValue;
            settings.HistoryMessagesUnlimited = false;
            Check(settings.BoundedHistoryMessages == ChatHistory.MaximumMessages, "a hand-edited number is bounded, never taken as « No limit »");
            var json = new JsonSerializerSettings { TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple, TypeNameHandling = TypeNameHandling.Objects };
            var saved = JsonConvert.SerializeObject(new Configuration { Chat = new ChatSettings { KeepHistory = true, HistoryDays = 7, HistoryMessages = 500 } }, json);
            var loaded = JsonConvert.DeserializeObject<Configuration>(saved, json)!;
            Check(loaded.Chat.HistoryDays == 7 && loaded.Chat.HistoryMessages == 500 && loaded.Chat.KeepHistory && !loaded.Chat.HistoryDaysUnlimited, "a saved configuration keeps its values (7 days stays 7)");
            var older = saved.Replace(",\"HistoryMessagesUnlimited\":false", "", StringComparison.Ordinal).Replace(",\"HistoryDaysUnlimited\":false", "", StringComparison.Ordinal);
            Check(!older.Contains("HistoryMessagesUnlimited", StringComparison.Ordinal) && !older.Contains("HistoryDaysUnlimited", StringComparison.Ordinal) && JsonConvert.DeserializeObject<Configuration>(older, json)!.Chat is { HistoryDays: 7, HistoryMessagesUnlimited: false }, "a file of 0.1492 (without the boxes) reads back");
        }),
        ("Chat history 1494 (X-513): the window: « No limit » beside each number, the number greyed while ticked, the place on disk, the question says it", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("ImGui.BeginDisabled(configuration.Chat.HistoryMessagesUnlimited);", StringComparison.Ordinal)
                  && window.Contains("BoolSetting(\"ChatWindow.HistoryMessagesUnlimited\",\"ChatWindow.HistoryMessagesUnlimitedTip\"", StringComparison.Ordinal)
                  && window.Contains("ImGui.BeginDisabled(configuration.Chat.HistoryDaysUnlimited);", StringComparison.Ordinal)
                  && window.Contains("BoolSetting(\"ChatWindow.HistoryDaysUnlimited\",\"ChatWindow.HistoryDaysUnlimitedTip\"", StringComparison.Ordinal), "a box each, its number greyed");
            Check(window.Contains("()=>ChatHistory.BoundedMessages(configuration.Chat.HistoryMessages)", StringComparison.Ordinal)
                  && window.Contains("()=>ChatHistory.BoundedDays(configuration.Chat.HistoryDays)", StringComparison.Ordinal), "the number shown is the one kept, never « No limit »'s");
            Check(window.Contains("ChatHistoryBytes?.Invoke() is >=0 and var size", StringComparison.Ordinal) && window.Contains("localization.Format(\"ChatWindow.HistorySize\"+unit,amount)", StringComparison.Ordinal), "the place on disk");
            Check(window.Contains("ChatOptions.HistoryMessagesUnlimited ? localization.Get(\"ChatWindow.HistoryNoLimit\")", StringComparison.Ordinal), "the question says « no limit »");
            Check(Read("src/TenshoAssistant/Plugin.Chat.cs").Contains("mainWindow.ChatHistoryBytes=()=>history.Bytes;", StringComparison.Ordinal), "measured by the game's side");
            Check(window.Contains("if(ImGui.Button(localization.Get(\"ChatWindow.HistoryErase\")", StringComparison.Ordinal), "« Erase the history » stays");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "HistoryMessagesUnlimited", "HistoryMessagesUnlimitedTip", "HistoryDaysUnlimited", "HistoryDaysUnlimitedTip", "HistorySize", "HistorySizeKB", "HistorySizeMB", "HistorySizeGB", "HistoryNoLimit", "HistoryNumber" })
                    Check(text.Contains($"\"{key}\": \"", StringComparison.Ordinal), $"{language}: {key}");
                Check(text.Contains(language == "en" ? "from 1 to 90 days.\\nDefault: 30.\"" : "de 1 à 90 jours.\\nPar défaut : 30.\"", StringComparison.Ordinal), language + ": 30 days by default, said");
            }
        }),
    ];
}
