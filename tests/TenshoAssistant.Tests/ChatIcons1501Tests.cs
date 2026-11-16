using TenshoAssistant.Chat;

// X-523 and X-524 M2 (review of 0.1498 to 0.1500): MentorProblem is 108 in Dalamud 15 (127 is RolePlaying, 168 WaitingForDutyFinder, 169 Tural:
// docs/evidence/0.1501-bitmapfonticon-codes.md); the decorations before a sender's name are left out of the name only, and an icon in a
// message's text keeps its « · ».
internal static class ChatIcons1501Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly DateTime Noon = new(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat icons 1501: the codes are Dalamud 15's (MentorProblem 108), and the evidence says so",() =>
        {
            Check(ChatGlyphs.MentorProblem==108,"MentorProblem is 108");
            foreach(var icon in new uint[]{77,78,79,80,81,95,108,128,146,167,170,171})Check(ChatGlyphs.IsSenderDecoration(icon),$"icon {icon} is a decoration");
            foreach(var icon in new uint[]{1,76,82,88,94,96,107,109,126,127,168,169,172})Check(!ChatGlyphs.IsSenderDecoration(icon),$"icon {icon} is not a decoration");
            var evidence=File.ReadAllText(Path.GetFullPath("docs/evidence/0.1501-bitmapfonticon-codes.md"));
            Check(evidence.Contains("| 108 | MentorProblem |",StringComparison.Ordinal) && evidence.Contains("| 127 | RolePlaying |",StringComparison.Ordinal)
                && evidence.Contains("| 168 | WaitingForDutyFinder |",StringComparison.Ordinal) && evidence.Contains("| 169 | Tural |",StringComparison.Ordinal),"the probe's lines");
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatGlyphs.cs"));
            Check(source.Contains("docs/evidence/0.1501-bitmapfonticon-codes.md",StringComparison.Ordinal),"the code cites the evidence");
        }),
        ("Chat icons 1501: a struggling mentor's name reads as it is; role-playing, the duty finder and Tural keep « · »",() =>
        {
            Check(ChatPayloads.Text([new(ChatPayloadKind.Icon,"IconPayload",Icon:108),new(ChatPayloadKind.Text,"TextPayload","Name")])=="Name","108 left out of the name");
            foreach(var icon in new uint[]{127,168,169})
                Check(ChatPayloads.Text([new(ChatPayloadKind.Icon,"IconPayload",Icon:icon),new(ChatPayloadKind.Text,"TextPayload","Name")])=="·Name",$"{icon} kept as « · »");
            Check(ChatPayloads.Text([new(ChatPayloadKind.Text,"TextPayload","Name"),new(ChatPayloadKind.Icon,"IconPayload",Icon:ChatGlyphs.CrossWorld),new(ChatPayloadKind.Text,"TextPayload","World")])=="Name@World","world icon kept");
        }),
        ("Chat icons 1501: a class or novice network icon in a message's text keeps its « · », shown and copied",() =>
        {
            var payloads=new ChatPayload[]{new(ChatPayloadKind.Text,"TextPayload","looking for "),new(ChatPayloadKind.Icon,"IconPayload",Icon:146),
                new(ChatPayloadKind.Text,"TextPayload"," and "),new(ChatPayloadKind.Icon,"IconPayload",Icon:78)};
            var record=new ChatRecord(1,Noon,10,ChatChannels.From(10),ChatPayloads.Text([new(ChatPayloadKind.Icon,"IconPayload",Icon:78),new(ChatPayloadKind.Text,"TextPayload","Mentor")]),ChatPayloads.Segments(payloads));
            Check(record.Sender=="Mentor","the sender's decoration left out");
            Check(string.Concat(record.Body.Select(segment=>segment.Text))=="looking for · and ·","the body keeps a trace of each icon");
            Check(ChatGlyphs.Icon(146)=="·" && ChatGlyphs.Icon(108)=="·" && ChatGlyphs.SenderIcon(146)=="","Icon for a text, SenderIcon for a name");
        }),
    ];
}
