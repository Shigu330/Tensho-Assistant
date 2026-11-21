namespace TenshoAssistant.Chat;

/// <summary>X-479 (0.1468): the kinds of a message's payloads as the reader tells them apart.</summary>
/// <remarks>X-474 (0.1477): Dalamud, a link of Dalamud or of a plugin (DalamudLinkPayload).</remarks>
/// <remarks>X-473 (0.1715): Status, a status the game links (StatusPayload). X-473 (0.1720): Colour, a text colour of the game on (its key in
/// Size) or off (Size 0) (UIForegroundPayload).</remarks>
public enum ChatPayloadKind { Text,Item,Quest,Map,LinkEnd,Icon,Raw,Other,Dalamud,Status,Colour }

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
    /// <summary>The pieces of a message, in order: a link's start (item, quest, map), its end (the game's link terminator), the texts; the other payloads are left out.
    /// X-473 (0.1720): in a combat line (<paramref name="combat"/>), outside a link, a coloured text alone (<see cref="ColouredName"/>) is an
    /// action's name (ChatSegmentKind.ActionName): the game links no action (docs/evidence/0.1720-chat-action-names.md).</summary>
    public static IEnumerable<ChatPiece> Pieces(IEnumerable<ChatPayload> payloads,bool combat=false)
    {
        var list=payloads as IReadOnlyList<ChatPayload> ?? payloads.ToList();
        var inLink=false;
        for(var index=0;index<list.Count;index++)
        {
            var payload=list[index];
            switch(payload.Kind)
            {
                case ChatPayloadKind.Item or ChatPayloadKind.Quest or ChatPayloadKind.Map or ChatPayloadKind.Dalamud or ChatPayloadKind.Status when payload.Link is { } link:inLink=true;yield return new(null,link);break;
                // X-635 (bench X-631, review of 0.1441, M1): a link whose target could not be read (MapLink null) ends a link still open: its
                // text stays plain, never joined to the link before.
                case ChatPayloadKind.Item or ChatPayloadKind.Quest or ChatPayloadKind.Map or ChatPayloadKind.Dalamud or ChatPayloadKind.Status:inLink=false;yield return new(null,End:true);break;
                case ChatPayloadKind.LinkEnd:inLink=false;yield return new(null,End:true);break;
                case ChatPayloadKind.Text:yield return new(payload.Text);break;
                // X-472 (0.1471): an icon of the game's font keeps its place, written readably (the world icon « @ »).
                case ChatPayloadKind.Icon:yield return new(ChatGlyphs.Icon(payload.Icon));break;
                case ChatPayloadKind.Colour when combat && !inLink && payload.Size!=0 && ColouredName(list,index) is { } coloured:
                    yield return new(null,new ChatLink(ChatSegmentKind.ActionName,Action:coloured.Name));
                    for(var at=index+1;at<coloured.End;at++)if(list[at].Kind==ChatPayloadKind.Text)yield return new(list[at].Text);
                    yield return new(null,End:true);
                    index=coloured.End;
                    break;
            }
        }
    }

    /// <summary>X-473 (0.1720): the coloured text that starts at <paramref name="start"/> (a colour on): its texts, a glow of the game around them
    /// allowed, up to the colour off (its index); null when anything else comes first (an icon, a link, another colour) or the colour never
    /// ends, or when it holds no letter.</summary>
    public static (string Name,int End)? ColouredName(IReadOnlyList<ChatPayload> payloads,int start)
    {
        var name=new System.Text.StringBuilder();
        for(var index=start+1;index<payloads.Count;index++)
        {
            var payload=payloads[index];
            if(payload.Kind==ChatPayloadKind.Colour)return payload.Size==0 && name.ToString().Trim() is { Length:>0 } text && text.Any(char.IsLetter) ? (text,index):null;
            if(payload.Kind==ChatPayloadKind.Text)name.Append(payload.Text);
            else if(payload.Kind!=ChatPayloadKind.Other || payload.Name!="UIGlowPayload")return null;
        }
        return null;
    }

    /// <summary>X-472 (0.1471): the plain text of payloads (a sender's name): the texts, and each icon of the game's font written readably
    /// (« Name@World »); before, the sender's TextValue left the world icon out (« NameWorld »). X-524 M2 (0.1501): the decorations before
    /// the name (novice network, class and job) are left out here only (ChatGlyphs.SenderIcon); a message's text keeps them as « · » (Pieces).</summary>
    public static string Text(IEnumerable<ChatPayload> payloads)
        =>string.Concat(payloads.Select(payload=>payload.Kind switch{ChatPayloadKind.Text=>payload.Text ?? "",ChatPayloadKind.Icon=>ChatGlyphs.SenderIcon(payload.Icon),_=>""}));

    /// <summary>X-722 (0.1724, the probe): a sender's pieces for the developer's log, never a name: icons by number, the game's glyphs by code,
    /// a word of two to four capitals as it is (a job's abbreviation), any other word by its length, a player's link said as such.</summary>
    public static string DescribeSender(IEnumerable<ChatPayload> payloads)
        =>string.Join(" | ",payloads.Select(payload=>payload.Kind switch
        {
            ChatPayloadKind.Icon=>"icon "+payload.Icon.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ChatPayloadKind.Text=>(payload.Name=="PlayerPayload" ? "player link: ":"text: ")+Masked(payload.Text),
            _=>payload.Name,
        }));

    private static string Masked(string? text)=>string.Join(" ",(text ?? "").Split(' ').Select(word=>
    {
        var glyphs=0;
        while(glyphs<word.Length && ChatGlyphs.IsGlyph(word[glyphs]))glyphs++;
        var codes=string.Concat(word[..glyphs].Select(letter=>string.Create(System.Globalization.CultureInfo.InvariantCulture,$"U+{(int)letter:X4}")));
        var rest=word[glyphs..];
        var said=rest.Length==0 ? "":rest.Length is >= 2 and <= 4 && rest.All(char.IsUpper) ? "'"+rest+"'":string.Create(System.Globalization.CultureInfo.InvariantCulture,$"({rest.Length} letters)");
        return codes+said;
    }));

    /// <summary>X-525 M3 (0.1503): the icon codes in a sender's name, in order (codes only, never a letter of the name), with what the
    /// window does with each: « 88 kept », « 128 left out » (ChatGlyphs.IsSenderDecoration). Empty when the name has no icon.</summary>
    public static IReadOnlyList<string> SenderIcons(IEnumerable<ChatPayload> payloads)
        =>payloads.Where(payload=>payload.Kind==ChatPayloadKind.Icon).Select(payload=>$"{payload.Icon} {(ChatGlyphs.IsSenderDecoration(payload.Icon) ? "left out" : "kept")}").ToList();

    /// <summary>The segments the store keeps for these payloads (ChatPieces.Segments of their pieces).</summary>
    public static IReadOnlyList<ChatSegment> Segments(IEnumerable<ChatPayload> payloads,bool combat=false)=>ChatPieces.Segments(Pieces(payloads,combat));

    /// <summary>X-496 M1 (0.1478): the last payload of the link that starts at <paramref name="start"/>: its end (the link terminator, kept as
    /// Dalamud keeps it), or the message's last payload when the link is not closed. The payloads Dalamud passes to a link's handler.</summary>
    public static int LinkLast(IReadOnlyList<ChatPayload> payloads,int start)
    {
        for(var index=start+1;index<payloads.Count;index++)
            if(payloads[index].Kind==ChatPayloadKind.LinkEnd)return index;
        return payloads.Count-1;
    }

    /// <summary>X-509 (review of 0.1490): what the link that starts at <paramref name="start"/> shows: its texts up to its end, as the window
    /// writes them (the link glyph left out), and whether its end (the game's link terminator) comes before another link starts or the message
    /// ends. The self-check reads with it the place link Tensho's alarms send: a place and its coordinates, closed.</summary>
    public static (string Shown,bool Closed) LinkShows(IReadOnlyList<ChatPayload> payloads,int start)
    {
        var shown=new System.Text.StringBuilder();
        for(var index=start+1;index<payloads.Count;index++)
        {
            var payload=payloads[index];
            if(payload.Kind==ChatPayloadKind.LinkEnd)return (shown.ToString().Trim(),true);
            if(payload.Kind is ChatPayloadKind.Item or ChatPayloadKind.Quest or ChatPayloadKind.Map or ChatPayloadKind.Dalamud or ChatPayloadKind.Status)break;
            if(payload.Kind==ChatPayloadKind.Text)shown.Append(ChatFormat.Clean(payload.Text));
        }
        return (shown.ToString().Trim(),false);
    }

    /// <summary>X-509: the self-check's verdict on a place link Tensho sends (the alarms' ring): its start, then a place's name and coordinates
    /// (more than one letter, with a digit), then its end. Null when it is whole; else what is missing, in the log's words.</summary>
    public static string? PlaceLinkFault(IReadOnlyList<ChatPayload> payloads)
    {
        var start=-1;
        for(var index=0;index<payloads.Count && start<0;index++)if(payloads[index].Kind==ChatPayloadKind.Map)start=index;
        if(start<0)return "no place link";
        var (shown,closed)=LinkShows(payloads,start);
        if(shown.Length<=1 || !shown.Any(char.IsDigit))return $"the place link shows {shown.Length} characters, no place and coordinates";
        return closed ? null:"the place link has no end";
    }

    /// <summary>Whether a message holds a link of the game (item, quest, map, status) or of Dalamud.</summary>
    public static bool HasLink(IEnumerable<ChatPayload> payloads)=>payloads.Any(payload=>payload.Kind is ChatPayloadKind.Item or ChatPayloadKind.Quest or ChatPayloadKind.Map or ChatPayloadKind.Dalamud or ChatPayloadKind.Status);

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
        ChatPayloadKind.Status=>$"Status {payload.Link?.StatusId ?? 0}",
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
