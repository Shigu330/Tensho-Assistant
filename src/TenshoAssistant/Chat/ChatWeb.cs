namespace TenshoAssistant.Chat;

/// <summary>
/// X-403 phase 2 (0.1453): the web addresses in a message's plain text (the game sends them as text, not as a link): words that start with
/// http://, https:// or www. (opened as https), at the start of the text or after a space or an opening bracket or quote. Trailing
/// punctuation, a closing quote and a closing bracket not opened in the address are left out. Only http and https with a host holding a
/// dot, at most <see cref="MaxLength"/> characters; anything else stays text. Pure.
/// </summary>
public static class ChatWeb
{
    public const int MaxLength=2048;
    private const string Trailing=".,;:!?'\"»>";

    /// <summary>The absolute address a word names (https:// put before www.), escaped for the browser; null when it is not an http or https
    /// address with a host.</summary>
    public static string? Address(string word)
    {
        if(word.Length is 0 or >MaxLength)return null;
        var absolute=word.StartsWith("www.",StringComparison.OrdinalIgnoreCase) ? "https://"+word:word;
        if(!Uri.TryCreate(absolute,UriKind.Absolute,out var uri))return null;
        if(uri.Scheme!=Uri.UriSchemeHttp && uri.Scheme!=Uri.UriSchemeHttps || !uri.Host.Contains('.') || uri.Host.StartsWith('.') || uri.Host.EndsWith('.'))return null;
        return uri.AbsoluteUri;
    }

    /// <summary>The text cut into its plain parts (Url null) and its addresses (the text as written, and the address to open), in order.</summary>
    public static IReadOnlyList<(string Text,string? Url)> Split(string text)
    {
        var parts=new List<(string,string?)>();
        var plain=0;
        var at=0;
        while(at<text.Length)
        {
            if(!Starts(text,at)){at++;continue;}
            var end=at;
            while(end<text.Length && !char.IsWhiteSpace(text[end]))end++;
            var stop=end;
            while(stop>at)
            {
                var last=text[stop-1];
                if(Trailing.Contains(last) || last==')' && Count(text,at,stop,')')>Count(text,at,stop,'(') || last==']' && Count(text,at,stop,']')>Count(text,at,stop,'['))stop--;
                else break;
            }
            if(stop>at && Address(text[at..stop]) is { } url)
            {
                if(at>plain)parts.Add((text[plain..at],null));
                parts.Add((text[at..stop],url));
                plain=stop;
            }
            at=Math.Max(stop,at+1);
        }
        if(plain<text.Length)parts.Add((text[plain..],null));
        return parts;
    }

    private static bool Starts(string text,int at)=>(at==0 || char.IsWhiteSpace(text[at-1]) || text[at-1] is '(' or '[' or '<' or '"' or '\'' or '«')
        && (Word(text,at,"https://") || Word(text,at,"http://") || Word(text,at,"www."));

    private static bool Word(string text,int at,string start)=>string.Compare(text,at,start,0,start.Length,StringComparison.OrdinalIgnoreCase)==0 && text.Length-at>start.Length;

    private static int Count(string text,int from,int to,char letter)
    {
        var count=0;
        for(var index=from;index<to;index++)if(text[index]==letter)count++;
        return count;
    }
}
