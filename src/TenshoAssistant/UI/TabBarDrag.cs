namespace TenshoAssistant.UI;

/// <summary>
/// X-605 (0.1698, the user's request of 18/11): what a reorderable tab bar remembers between frames: where each tab was drawn this frame (its
/// left edge), the bar's number (a new bar shows the tabs in the order they are drawn), the tab shown, a drag under way. After the bar,
/// <see cref="After"/> gives the order to keep when a drag just ended; a bar showing another order than the one asked, outside a drag (a tab
/// came or went), is made again (a new number, the tab shown selected once). Pure (docs/evidence/0.1697-reorderable-tabs.md).
/// </summary>
public sealed class TabBarDrag
{
    public readonly List<(string Id,float X)> Places=[];
    public int Generation{get;private set;}
    /// <summary>The tab open in the last frame (selected again on a new bar).</summary>
    public string? Shown{get;set;}
    private bool fresh,dragging;

    /// <summary>A new bar: the tab shown is selected once in it.</summary>
    public bool TakeFresh(){var was=fresh;fresh=false;return was;}

    /// <summary>The bar made again (another order to show, or « put back in order »).</summary>
    public void Renew(){Generation++;fresh=true;}

    /// <summary>After the bar: the order to keep when a drag just ended, else null (a new bar when the order shown is not the one asked).</summary>
    public List<string>? After(bool dragged,IReadOnlyList<string>? kept)
    {
        if(dragged){dragging=true;return null;}
        var shown=TabOrder.Shown(Places);
        var same=shown.SequenceEqual(Places.Select(place=>place.Id));
        if(dragging)
        {
            dragging=false;
            return same ? null:TabOrder.Keep(shown,kept);
        }
        if(!same)Renew();
        return null;
    }
}
