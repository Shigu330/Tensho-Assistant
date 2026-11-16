namespace TenshoAssistant.Chat;

/// <summary>X-403 phase 2 (0.1441): one piece of a message as the reader sees it: a text, the start of a link (what it points to), or a link's end.</summary>
public readonly record struct ChatPiece(string? Text,ChatLink? Start=null,bool End=false);

/// <summary>
/// X-403 phase 2 (0.1441): a message's pieces made into its segments. The texts between a link's start and its end (the game's link terminator)
/// are that link's (its visible text: a place and coordinate, an item's name); a link left open runs to the end of the message; a new start
/// replaces an open one; an end without a start changes nothing; empty texts are left out. X-461 (0.1463): a link that ends with no visible text
/// (only the game's link glyph, or nothing) keeps its place as an empty segment of that link, which the window writes with the name of what it
/// points to (ChatFormat.Runs): it no longer vanishes from the message. X-506 (0.1487, user's capture and game log of 11/16): a link whose
/// only visible piece is the game's link glyph (the place link of Tensho's alarms, followed by an item link) shows that glyph as an arrow
/// (ChatGlyphs.LinkArrow), as the game does, and no name: the zone's name was written glued to the item's (« Thanalan occidentalTerreau… »).
/// The name of the target is written only for a link that would otherwise show nothing at all.
/// X-509 (review of 0.1490, Dalamud 15.0.4.0 read): that alarm message was not the game's way: Tensho sent a bare place link (no arrow, no
/// name, no end; docs/evidence/0.1491-alarm-place-link.md), and the glyph shown was the item link's own arrow, which Dalamud's item link puts
/// before the link (the game's item link template, outside it). A glyph alone outside any link (with spaces only) is now the arrow of the link
/// that follows it, as the game draws it: part of that link, not lost. Pure.
/// </summary>
public static class ChatPieces
{
    public static IReadOnlyList<ChatSegment> Segments(IEnumerable<ChatPiece> pieces)
    {
        var body=new List<ChatSegment>();
        ChatLink? open=null;
        var shown=false;
        var mark=-1;   // X-506: the segment of the open link holding the game's link glyph
        var before=-1; // X-509: a segment outside any link holding the game's link glyph alone: the arrow of the next link
        void Close()
        {
            if(open is not null && !shown)
            {
                if(mark>=0)body[mark]=body[mark] with{Text=body[mark].Text.Replace(ChatGlyphs.LinkMarker.ToString(),ChatGlyphs.LinkArrow,StringComparison.Ordinal)};
                else body.Add(new ChatSegment("",open.Kind,open));
            }
            open=null;shown=false;mark=-1;
        }
        foreach(var piece in pieces)
        {
            if(piece.Start is { } start)
            {
                Close();open=start;
                // X-509: the glyph just before the link (spaces only between) is that link's arrow.
                if(before>=0){body[before]=body[before] with{Kind=start.Kind,Link=start};mark=before;before=-1;}
                continue;
            }
            if(piece.End){Close();continue;}
            if(piece.Text is not { Length:>0 } text)continue;
            if(open is not null)
            {
                if(mark<0 && text.Contains(ChatGlyphs.LinkMarker,StringComparison.Ordinal))mark=body.Count;
                body.Add(new ChatSegment(text,open.Kind,open));
                if(ChatFormat.Clean(text).Trim().Length>0)shown=true;
                continue;
            }
            if(ChatFormat.Clean(text).Trim().Length==0)
            {
                if(text.Contains(ChatGlyphs.LinkMarker,StringComparison.Ordinal))before=body.Count;
                body.Add(new ChatSegment(text));
                continue;
            }
            before=-1;
            // X-403 phase 2 (0.1453): web addresses in the plain text (outside a game link), each its own segment.
            foreach(var (part,url) in ChatWeb.Split(text))
                body.Add(url is null ? new ChatSegment(part):new ChatSegment(part,ChatSegmentKind.WebLink,new ChatLink(ChatSegmentKind.WebLink,Url:url)));
        }
        Close();
        return body;
    }
}
