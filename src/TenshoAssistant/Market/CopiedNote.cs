namespace TenshoAssistant.Market;

/// <summary>
/// X-767 M2 (review of 0.1756): the « N sale(s) copied » note of a CSV copy, shown only next to what was copied: the key of the view at the
/// copy (the market board's item, filters and order; the bank's tab, filters and data). Another item, filter, order or newer data and the
/// note is gone for good (it no longer says the lines shown were copied); a new copy sets it again. Pure (tests).
/// </summary>
public sealed class CopiedNote<TKey>
{
    private TKey? copied;
    private string? text;

    /// <summary>The view <paramref name="key"/> was just copied; <paramref name="note"/> says how many lines.</summary>
    public void Copied(TKey key,string note){copied=key;text=note;}

    /// <summary>The note while <paramref name="key"/> is the view copied; null otherwise, and forgotten.</summary>
    public string? For(TKey key)
    {
        if(text is not null && !EqualityComparer<TKey>.Default.Equals(copied,key)){text=null;copied=default;}
        return text;
    }
}
