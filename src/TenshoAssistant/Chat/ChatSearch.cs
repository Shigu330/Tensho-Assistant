using System.Globalization;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-447 B8 (the user's choice of 15/11, after Prat's search): what the chat window's search finds: the messages of the tab shown whose sender
/// or text holds what was typed, case and accents aside (« fete » finds « Fête »). The text typed lives in memory only; nothing is saved.
/// X-670 (review of 0.1638): gone through off the game's thread (ChatStore.Searched); the sender as shown (its stand-in in capture mode).
/// Pure (tests).
/// </summary>
public static class ChatSearch
{
    /// <summary>The longest text the search box takes.</summary>
    public const int MaximumText=100;

    /// <summary>The text searched: what was typed, trimmed; empty: no search (the tab as it is).</summary>
    public static string Query(string? typed)=>(typed ?? "").Trim();

    /// <summary>The message holds <paramref name="query"/>: in its sender as the window shows it or in its text (its pieces put together, so a
    /// word split over two pieces is found), case and accents aside. An empty query finds every message. X-670 M3 (review of 0.1638): with
    /// <paramref name="capture"/>, a player's stand-in (« #A3F27 », as shown), not the name: what is typed is what is seen.</summary>
    public static bool Matches(ChatRecord record,string query,bool capture=false)
    {
        if(query.Length==0)return true;
        var compare=CultureInfo.InvariantCulture.CompareInfo;
        const CompareOptions options=CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace;
        // X-730 M2 (0.1729): the sender as shown (« [1] [SAG] Name ») or without its brackets (« 1 SAG Name »).
        if(compare.IndexOf(ChatFormat.ShownSender(record,capture),query,options)>=0 || compare.IndexOf(ChatFormat.SenderWords(record,capture),query,options)>=0)return true;
        return compare.IndexOf(string.Concat(record.Body.Select(segment=>segment.Text)),query,options)>=0;
    }
}
