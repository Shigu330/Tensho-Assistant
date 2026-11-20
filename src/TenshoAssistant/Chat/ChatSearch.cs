using System.Globalization;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-447 B8 (the user's choice of 15/11, after Prat's search): what the chat window's search finds: the messages of the tab shown whose sender
/// or text holds what was typed, case and accents aside (« fete » finds « Fête »). The text typed lives in memory only; nothing is saved.
/// Pure (tests).
/// </summary>
public static class ChatSearch
{
    /// <summary>The longest text the search box takes.</summary>
    public const int MaximumText=100;

    /// <summary>The text searched: what was typed, trimmed; empty: no search (the tab as it is).</summary>
    public static string Query(string? typed)=>(typed ?? "").Trim();

    /// <summary>The message holds <paramref name="query"/>: in its sender's name or in its text (its pieces put together, so a word split over
    /// two pieces is found), case and accents aside. An empty query finds every message.</summary>
    public static bool Matches(ChatRecord record,string query)
    {
        if(query.Length==0)return true;
        var compare=CultureInfo.InvariantCulture.CompareInfo;
        const CompareOptions options=CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace;
        if(compare.IndexOf(ChatFormat.Clean(record.Sender),query,options)>=0)return true;
        return compare.IndexOf(string.Concat(record.Body.Select(segment=>segment.Text)),query,options)>=0;
    }
}
