using System.Globalization;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-403 phase 2 (0.1453): the web addresses in a message's plain text (the game sends them as text, not as a link): words that start with
/// http://, https:// or www. (opened as https), at the start of the text or after a space or an opening bracket or quote. Trailing
/// punctuation, a closing quote and a closing bracket not opened in the address are left out. Only http and https with a host holding a
/// dot, at most <see cref="MaxLength"/> characters; anything else stays text. Pure.
/// X-451 (0.1456): an address with a « @ » before its host (« https://www.mogstation.com@evil.org/ » opens evil.org) stays text, and the
/// address kept has its host as the browser reaches it (letters of other alphabets in their « xn--… » form): what is shown is where it leads.
/// </summary>
public static class ChatWeb
{
    public const int MaxLength=2048;
    private const string Trailing=".,;:!?'\"»>";

    /// <summary>The absolute address a word names (https:// put before www.), escaped for the browser, its host in ASCII form; null when it is
    /// not an http or https address with a host, or when a user part comes before the host (X-451).</summary>
    public static string? Address(string word)
    {
        if(word.Length is 0 or >MaxLength)return null;
        var absolute=word.StartsWith("www.",StringComparison.OrdinalIgnoreCase) ? "https://"+word:word;
        if(!Uri.TryCreate(absolute,UriKind.Absolute,out var uri))return null;
        if(uri.Scheme!=Uri.UriSchemeHttp && uri.Scheme!=Uri.UriSchemeHttps || !uri.Host.Contains('.') || uri.Host.StartsWith('.') || uri.Host.EndsWith('.'))return null;
        // X-451 (0.1456): a user part is only there to look like another site (« https://site-connu@autre-site/ »); « https://@evil.org »
        // writes an empty one, so the « @ » itself is looked for before the host.
        if(uri.UserInfo.Length>0 || Authority(absolute).Contains('@'))return null;
        // X-451 (0.1456): AbsoluteUri kept the look-alike letters of the host; IdnHost is the name the browser asks for.
        var port=uri.IsDefaultPort ? "":":"+uri.Port.ToString(CultureInfo.InvariantCulture);
        return uri.Scheme+"://"+uri.IdnHost+port+uri.PathAndQuery+uri.Fragment;
    }

    /// <summary>X-451 (0.1456): the site an address made by <see cref="Address"/> leads to: its host, and how the host reads when it holds
    /// letters of other alphabets (a label « xn--… »), a name that may imitate a known site. Null when the address cannot be read.</summary>
    public static WebSite? Site(string url)
    {
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri) || uri.IdnHost.Length==0)return null;
        var host=uri.IdnHost;
        if(!host.Split('.').Any(label=>label.StartsWith("xn--",StringComparison.OrdinalIgnoreCase)))return new WebSite(url,host,null);
        string reads;
        try{reads=new IdnMapping().GetUnicode(host);}
        catch(ArgumentException){reads=host;}
        return new WebSite(url,host,reads);
    }

    /// <summary>The part between « :// » and the path, query or fragment (where a user part and the host are written).</summary>
    private static string Authority(string absolute)
    {
        var start=absolute.IndexOf("://",StringComparison.Ordinal)+3;
        var end=absolute.IndexOfAny(['/','?','#','\\'],start);
        return end<0 ? absolute[start..]:absolute[start..end];
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

/// <summary>X-451 (0.1456): the site of a web address: its host as the browser reaches it, and <paramref name="Reads"/>, how it reads, when it
/// holds letters of other alphabets (null otherwise).</summary>
public sealed record WebSite(string Url,string Host,string? Reads)
{
    public bool Foreign=>Reads is not null;
}
