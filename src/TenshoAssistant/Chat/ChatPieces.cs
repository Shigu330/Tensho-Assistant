namespace TenshoAssistant.Chat;

/// <summary>X-403 phase 2 (0.1441): one piece of a message as the reader sees it: a text, the start of a link (what it points to), or a link's end.</summary>
public readonly record struct ChatPiece(string? Text,ChatLink? Start=null,bool End=false);

/// <summary>
/// X-403 phase 2 (0.1441): a message's pieces made into its segments. The texts between a link's start and its end (the game's link terminator)
/// are that link's (its visible text: a place and coordinate, an item's name); a link left open runs to the end of the message; a new start
/// replaces an open one; an end without a start changes nothing; empty texts are left out. Pure.
/// </summary>
public static class ChatPieces
{
    public static IReadOnlyList<ChatSegment> Segments(IEnumerable<ChatPiece> pieces)
    {
        var body=new List<ChatSegment>();
        ChatLink? open=null;
        foreach(var piece in pieces)
        {
            if(piece.Start is { } start){open=start;continue;}
            if(piece.End){open=null;continue;}
            if(piece.Text is not { Length:>0 } text)continue;
            if(open is not null){body.Add(new ChatSegment(text,open.Kind,open));continue;}
            // X-403 phase 2 (0.1453): web addresses in the plain text (outside a game link), each its own segment.
            foreach(var (part,url) in ChatWeb.Split(text))
                body.Add(url is null ? new ChatSegment(part):new ChatSegment(part,ChatSegmentKind.WebLink,new ChatLink(ChatSegmentKind.WebLink,Url:url)));
        }
        return body;
    }
}
