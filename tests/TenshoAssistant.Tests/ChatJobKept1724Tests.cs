using TenshoAssistant.Chat;

// X-723 (0.1724): a line keeps its sender's job (colour and icon) after the party and after a reload (behaviour test, R28.14: the party
// disbanded, the plugin reloaded and the line read back from the chat history). X-722 (0.1724): the party line's sender « [1] [SAG] [Name] »,
// the job's icon inside the brackets, the copy readable, capture mode replacing the name only; the probe never writes a name.
internal static class ChatJobKept1724Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 40, 0, TimeSpan.Zero);
    private const uint Sage = 40;
    // The sender's text as the chat history kept it on 21/11 (kind 14): the game's party number 1, the abbreviation, the name.
    private const string PartySender = "SAG Ann Doe";

    private static ChatSources Sources()
    {
        var sources = new ChatSources();
        sources.Jobs.SetRoles([(Sage, (byte)4), (19u, (byte)1)]);
        return sources;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat job kept 1724: a line written in a party keeps its colour and icon after the party and after a reload; a later line has none", () =>
        {
            var settings = new ChatSettings { RoleColors = true, JobIcons = true };
            var sources = Sources();
            sources.Jobs.Replace([("Ann Doe", (ushort)73, Sage)]);
            var store = new ChatStore();
            var record = store.Add(Now, 14, PartySender, [new ChatSegment("hello")], 1000, job: sources.Jobs.Find("Ann Doe", 73));
            sources.Expect(record.Id, "Ann Doe", 73, 14);
            Check(record.Job == Sage, "kept with the line when it arrived");
            sources.Jobs.Replace([]);
            var job = ChatJobs.JobOf(record, sources.For(record.Id));
            Check(job == Sage && sources.Jobs.ColourFor(settings, record.Channel, job) == ChatJobs.GameHealer && ChatJobs.IconFor(settings, record.Channel, job) == ChatJobs.IconBase + Sage, "the party disbanded: the same colour and icon");
            var back = ChatHistory.Decode(ChatHistory.Encode(record), 7)!;
            var reloaded = Sources();
            var again = ChatJobs.JobOf(back, reloaded.For(back.Id));
            Check(back.Job == Sage && again == Sage && reloaded.Jobs.ColourFor(settings, back.Channel, again) == ChatJobs.GameHealer, "a reload: read back from the history with its job");
            Check(!ChatHistory.Encode(store.Add(Now, 10, "Bob", [new ChatSegment("x")], 1000)).Contains("\"job\"", StringComparison.Ordinal), "no job known: nothing written");
            var later = store.Add(Now.AddMinutes(1), 14, "Ann Doe", [new ChatSegment("bye")], 1000, job: sources.Jobs.Find("Ann Doe", 73));
            Check(later.Job == 0 && ChatJobs.JobOf(later, null) == 0 && sources.Jobs.ColourFor(settings, later.Channel, 0) is null, "written after the party, the player no longer shown: no colour (R06)");
            Check(ChatJobs.JobOf(new ChatRecord(1, Now, 14, ChatChannel.Party, "A", []), new ChatSource("A", 1, Job: 19)) == 19, "a line without its job: the sender's kept in memory");
            Check(ChatHistory.Decode(ChatHistory.Encode(record).Replace("\"job\":40", "\"job\":9999", StringComparison.Ordinal), 8)!.Job == 0, "a job read back that is no ClassJob row: not kept");
        }),
        ("Chat job kept 1724: the party line's sender: [1] [icon SAG] [Name], the copy readable, capture mode replaces the name only", () =>
        {
            Check(ChatFormat.SenderParts(PartySender) == ("1", "SAG", "Ann Doe"), "number, abbreviation, name");
            Check(ChatFormat.SenderParts("Ann Doe@Moogle") == ("", "", "Ann Doe@Moogle") && ChatFormat.SenderParts("Ann Doe") == ("1", "", "Ann Doe"), "no glyph: the whole name; a glyph without abbreviation: number and name");
            Check(ChatFormat.SenderParts("Ab Cd") == ("1", "", "Ab Cd") && ChatFormat.SenderParts("SAG") == ("1", "", "SAG"), "a name is never cut (a capital and small letters; no name after)");
            var record = new ChatRecord(1, Now, 14, ChatChannel.Party, PartySender, [new ChatSegment("hello")], Job: Sage);
            var settings = new ChatSettings { ShowTimestamps = false, ShowChannel = false, SenderBrackets = true, JobIcons = true };
            var runs = ChatFormat.WithJobIcon(ChatFormat.Runs(record, settings, _ => ""), ChatJobs.IconFor(settings, record.Channel, record.Job));
            Check(runs.Select(run => run.Role).SequenceEqual([ChatRole.Channel, ChatRole.JobIcon, ChatRole.Sender, ChatRole.Sender, ChatRole.Body]), string.Join(",", runs.Select(run => run.Role + ":" + run.Text)));
            Check(string.Concat(runs.Select(run => run.Text)) == "[1] [SAG] [Ann Doe]: hello", "« [1] [icon SAG] [Ann Doe]: hello »");
            Check(ChatFormat.Plain(record, settings, _ => "") == "[1] [SAG] [Ann Doe]: hello", "the copy: readable, no icon");
            var noBrackets = new ChatSettings { ShowTimestamps = false, ShowChannel = false, SenderBrackets = false };
            Check(ChatFormat.Plain(record, noBrackets, _ => "") == "[1] [SAG] Ann Doe: hello", "the name's brackets follow the setting; the number and the job keep theirs");
            var capture = new ChatSettings { ShowTimestamps = false, ShowChannel = false, CaptureNames = true };
            var shown = ChatFormat.Plain(record, capture, _ => "");
            Check(shown.StartsWith("[1] [SAG] #", StringComparison.Ordinal) && !shown.Contains("Ann", StringComparison.Ordinal) && ChatFormat.ShownSender(record, true) == "[1] [SAG] " + ChatFormat.StandIn("Ann Doe"), "capture mode: the number and the job stay, the name replaced: " + shown);
            Check(ChatFormat.ShownSender(record, false) == "[1] [SAG] Ann Doe", "the search: as shown (X-730 M2)");
            var unknown = ChatFormat.WithJobIcon(ChatFormat.Runs(record with { Job = 0 }, settings, _ => ""), 0);
            Check(!unknown.Any(run => run.Role == ChatRole.JobIcon) && string.Concat(unknown.Select(run => run.Text)) == "[1] [SAG] [Ann Doe]: hello", "job not known: no icon, no empty brackets");
            var plain = new ChatRecord(2, Now, 10, ChatChannel.Say, "Ann Doe", [new ChatSegment("hi")], Job: Sage);
            Check(string.Concat(ChatFormat.WithJobIcon(ChatFormat.Runs(plain, settings, _ => ""), ChatJobs.IconFor(settings, plain.Channel, plain.Job)).Select(run => run.Text)) == "[Ann Doe]: hi", "a line without the game's number: as before");
        }),
        ("Chat job kept 1724: the probe writes a sender's pieces, never a name; wiring", () =>
        {
            var said = ChatPayloads.DescribeSender([new ChatPayload(ChatPayloadKind.Icon, "IconPayload", Icon: 167), new ChatPayload(ChatPayloadKind.Text, "TextPayload", Text: "SAG "), new ChatPayload(ChatPayloadKind.Text, "PlayerPayload", Text: "Ann Doe")]);
            Check(said == "icon 167 | text: U+E090'SAG'  | player link: (3 letters) (3 letters)" && !said.Contains("Ann", StringComparison.Ordinal), said);
            var reader = Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("var job=sources is not null && sentBy is not null ? sources.Jobs.Find(sentBy.PlayerName,(ushort)sentBy.World.RowId):0u;", StringComparison.Ordinal)
                && reader.Contains("current.BoundedMaxLines,source,(byte)message.TargetKind,job);", StringComparison.Ordinal), "the job taken when the line arrives");
            Check(reader.Contains("if(developer?.Invoke()==true && senderPiecesSaid.Take((int)message.LogKind))", StringComparison.Ordinal), "the probe, developer mode only");
            var window = Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("Chat.ChatJobs.JobOf(record,ChatSenders?.For(record.Id))", StringComparison.Ordinal) && window.Contains("Chat.ChatJobs.JobOf(record,senders.For(record.Id))", StringComparison.Ordinal), "icon and colour from the line's job");
        }),
    ];
}
