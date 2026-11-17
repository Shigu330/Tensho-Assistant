using TenshoAssistant.Chat;

// X-525 (0.1503, review of 0.1502): M1 the layout line no longer calls the focus node « focused » (it is not a sign of typing); M2 the
// evidence no longer states the nested nodes' boxes as facts; M3 the sender's icons are written in developer mode, codes only, to prove the set.
internal static class ChatReview1503Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat review 1503 M1: the focus node is not called typing",() =>
        {
            var window=new ChatLogBox(0,0,100,100);
            var line=new ChatLogLayout(true,1,window,new ChatLogBox(0,80,100,20),null,null,null,true,[]).Line();
            Check(line.Contains("inside focus node;",StringComparison.Ordinal) && !line.Contains("focused",StringComparison.Ordinal) && !line.Contains("typing",StringComparison.Ordinal),line);
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatLogLayout.cs"));
            Check(!source.Contains("(the player is typing)",StringComparison.Ordinal) && source.Contains("not a sign of typing",StringComparison.Ordinal),"the parameter says what the flag is");
        }),
        ("Chat review 1503 M2: the evidence narrows the nested nodes' boxes",() =>
        {
            var evidence=File.ReadAllText(Path.GetFullPath("docs/evidence/0.1502-game-chat-input.md"));
            Check(!evidence.Contains("the row read for the first tab is under it",StringComparison.Ordinal),"no more stated as a fact");
            Check(evidence.Contains("Correction (0.1503, X-525 M2",StringComparison.Ordinal) && evidence.Contains("are not verified",StringComparison.Ordinal),"the correction");
        }),
        ("Chat review 1503 M3: the sender's icons, codes only, and what the window does with each",() =>
        {
            var payloads=new[]{new ChatPayload(ChatPayloadKind.Icon,"IconPayload",Icon:128),new ChatPayload(ChatPayloadKind.Text,"TextPayload",Text:"Secret Name"),
                new ChatPayload(ChatPayloadKind.Icon,"IconPayload",Icon:ChatGlyphs.CrossWorld),new ChatPayload(ChatPayloadKind.Text,"TextPayload",Text:"World")};
            var icons=ChatPayloads.SenderIcons(payloads);
            Check(icons.SequenceEqual(["128 left out","88 kept"]),string.Join("|",icons));
            Check(!string.Join(", ",icons).Contains("Secret",StringComparison.Ordinal),"no letter of the name");
            Check(ChatPayloads.SenderIcons([new ChatPayload(ChatPayloadKind.Text,"TextPayload",Text:"Name")]).Count==0,"no icon, nothing");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs"));
            Check(reader.Contains("if(developer?.Invoke()==true && message.Sender.Payloads",StringComparison.Ordinal) && reader.Contains("senderIconsSaid=new(1,20)",StringComparison.Ordinal),"developer mode, bounded");
        }),
    ];
}
