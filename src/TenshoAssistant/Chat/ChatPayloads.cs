namespace TenshoAssistant.Chat;

/// <summary>X-479 (0.1468): the kinds of a message's payloads as the reader tells them apart.</summary>
/// <remarks>X-474 (0.1477): Dalamud, a link of Dalamud or of a plugin (DalamudLinkPayload).</remarks>
public enum ChatPayloadKind { Text,Item,Quest,Map,LinkEnd,Icon,Raw,Other,Dalamud }

/// <summary>
/// X-479 (0.1468): one payload of a message, without Dalamud's types: what ChatReader.Dalamud makes of each of the game's payloads (an item,
/// quest or map link's start, the link terminator, a text, an icon of the game's font, a raw payload by its size, the others by their type's
/// name). What follows (the pieces, the developer-mode line) is pure, and the tests replay it on the payloads the game sent
/// (docs/evidence/0.1468-chat-link-pieces.md).
/// </summary>
/// <param name="Name">The payload's type name, as the developer-mode line writes it (« TextPayload », « UIForegroundPayload »).</param>
/// <param name="Link">What a link's start points to; null for a map link that could not be read (its texts then stay plain text).</param>
public readonly record struct ChatPayload(ChatPayloadKind Kind,string Name,string? Text=null,ChatLink? Link=null,int Size=0,uint Icon=0);

/// <summary>X-479 (0.1468): a message's payloads made into its pieces and into the developer-mode line (kinds and lengths, never the words). Pure.</summary>
public static class ChatPayloads
{
    /// <summary>The pieces of a message, in order: a link's start (item, quest, map), its end (the game's link terminator), the texts; the other payloads are left out.</summary>
    public static IEnumerable<ChatPiece> Pieces(IEnumerable<ChatPayload> payloads)
    {
        foreach(var payload in payloads)
            switch(payload.Kind)
            {
                case ChatPayloadKind.Item or ChatPayloadKind.Quest or ChatPayloadKind.Map or ChatPayloadKind.Dalamud when payload.Link is { } link:yield return new(null,link);break;
                case ChatPayloadKind.LinkEnd:yield return new(null,End:true);break;
                case ChatPayloadKind.Text:yield return new(payload.Text);break;
                // X-472 (0.1471): an icon of the game's font keeps its place, written readably (the world icon « @ »).
                case ChatPayloadKind.Icon:yield return new(ChatGlyphs.Icon(payload.Icon));break;
            }
    }

    /// <summary>X-472 (0.1471): the plain text of payloads (a sender's name): the texts, and each icon of the game's font written readably
    /// (« Name@World »); before, the sender's TextValue left the world icon out (« NameWorld »).</summary>
    public static string Text(IEnumerable<ChatPayload> payloads)
        =>string.Concat(payloads.Select(payload=>payload.Kind switch{ChatPayloadKind.Text=>payload.Text ?? "",ChatPayloadKind.Icon=>ChatGlyphs.Icon(payload.Icon),_=>""}));

    /// <summary>The segments the store keeps for these payloads (ChatPieces.Segments of their pieces).</summary>
    public static IReadOnlyList<ChatSegment> Segments(IEnumerable<ChatPayload> payloads)=>ChatPieces.Segments(Pieces(payloads));

    /// <summary>X-496 M1 (0.1478): the last payload of the link that starts at <paramref name="start"/>: its end (the link terminator, kept as
    /// Dalamud keeps it), or the message's last payload when the link is not closed. The payloads Dalamud passes to a link's handler.</summary>
    public static int LinkLast(IReadOnlyList<ChatPayload> payloads,int start)
    {
        for(var index=start+1;index<payloads.Count;index++)
            if(payloads[index].Kind==ChatPayloadKind.LinkEnd)return index;
        return payloads.Count-1;
    }

    /// <summary>Whether a message holds a link of the game (item, quest or map).</summary>
    public static bool HasLink(IEnumerable<ChatPayload> payloads)=>payloads.Any(payload=>payload.Kind is ChatPayloadKind.Item or ChatPayloadKind.Quest or ChatPayloadKind.Map or ChatPayloadKind.Dalamud);

    /// <summary>The links of a body that show no text of their own (ChatPieces kept their place empty; the window writes the name of their target).</summary>
    public static int Bare(IReadOnlyList<ChatSegment> body)=>body.Count(segment=>segment.Text.Length==0 && segment.Link is not null);

    /// <summary>The developer-mode line's list of payloads: kinds, an item's row, a text's length and whether it holds a glyph of the game's font; never the words.</summary>
    public static string Describe(IEnumerable<ChatPayload> payloads)=>string.Join(", ",payloads.Select(Describe));

    public static string Describe(ChatPayload payload)=>payload.Kind switch
    {
        ChatPayloadKind.Item=>$"Item {payload.Link?.ItemId ?? 0}{(payload.Link?.Hq==true ? " HQ":"")}",
        ChatPayloadKind.Quest=>"Quest",
        ChatPayloadKind.Map=>"MapLink",
        ChatPayloadKind.Dalamud=>"DalamudLink",
        ChatPayloadKind.LinkEnd=>"LinkEnd",
        ChatPayloadKind.Text=>$"{payload.Name} ({payload.Text?.Length ?? 0} characters{(Glyph(payload.Text) ? ", game glyph":"")})",
        ChatPayloadKind.Raw=>$"Raw ({payload.Size} bytes)",
        _=>payload.Name,
    };

    /// <summary>Whether a text holds a glyph of the game's font (private use area U+E000 to U+F8FF).</summary>
    public static bool Glyph(string? text)=>text?.Any(letter=>letter is >= '' and <= '')==true;
}

/// <summary>
/// X-479 (0.1468): how many developer-mode lines « pieces of a message with a link » a session writes: a few per kind of message, so that a run
/// of loot or system messages no longer uses them all before a message of a player (0.1463: twenty for the session, all taken by loot lines on
/// 11/15 before any message said by a player). Not thread-safe: the reader calls it on the game's thread.
/// </summary>
public sealed class ChatPieceLog(int perKind=2,int total=30)
{
    private readonly Dictionary<int,int> said=[];
    private int count;

    /// <summary>Whether a line for a message of this kind is written now (and counted).</summary>
    public bool Take(int kind)
    {
        if(count>=total || said.GetValueOrDefault(kind)>=perKind)return false;
        said[kind]=said.GetValueOrDefault(kind)+1;
        count++;
        return true;
    }
}
