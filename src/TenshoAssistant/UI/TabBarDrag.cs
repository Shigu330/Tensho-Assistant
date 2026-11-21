namespace TenshoAssistant.UI;

/// <summary>
/// X-605 (0.1698, the user's request of 18/11): what a reorderable tab bar remembers between frames: where each tab was drawn this frame (its
/// left edge), the bar's number (a new bar shows the tabs in the order they are drawn), the tab shown, a drag under way. After the bar,
/// <see cref="After"/> gives the order to keep when a drag just ended. Pure (docs/evidence/0.1697-reorderable-tabs.md).
/// X-709 (0.1700, review of 0.1698, M1): a bar showing another order than the one drawn, outside a drag (a tab came: a reorderable bar adds
/// it at the end), is no longer made again with a new id (new child windows each time, never freed, the scroll lost, a frame in the wrong
/// order then an empty one): it is drawn once without Reorderable (<see cref="Settle{T}"/>), and ImGui sorts a bar's tabs in the order they
/// were drawn when that flag changes, in the same bar (docs/evidence/0.1700-tab-bar-settle.md). A tab that was not drawn in the last frame
/// settles the bar before it is drawn. A new bar only to put the tabs back in order, or when settling twice did not bring the order back.
/// </summary>
public sealed class TabBarDrag
{
    public readonly List<(string Id,float X)> Places=[];
    public int Generation{get;private set;}
    /// <summary>The tab open in the last frame (selected again on a new bar).</summary>
    public string? Shown{get;set;}
    private bool fresh,dragging,settle,settling;
    private int misses;

    /// <summary>A new bar: the tab shown is selected once in it.</summary>
    public bool TakeFresh(){var was=fresh;fresh=false;return was;}

    /// <summary>The bar made again (« put back in order », or settling did not help).</summary>
    public void Renew(){Generation++;fresh=true;settle=false;misses=0;}

    /// <summary>
    /// Before the bar, with the tabs drawn in this frame, in order (<see cref="Places"/> still holds the last frame's): true when the bar is
    /// drawn without Reorderable in this frame: the last frame showed another order than the one drawn, or a tab comes that was not drawn in
    /// the last frame (outside a drag). Never during a drag.
    /// </summary>
    public bool Settle<T>(IReadOnlyList<T> tabs,Func<T,string> id)
    {
        settling=!dragging && (settle || Places.Count>0 && Comes(tabs,id));
        settle=false;
        return settling;
    }

    private bool Comes<T>(IReadOnlyList<T> tabs,Func<T,string> id)
    {
        foreach(var tab in tabs)
        {
            var key=id(tab);
            var seen=false;
            foreach(var place in Places)if(place.Id==key){seen=true;break;}
            if(!seen)return true;
        }
        return false;
    }

    /// <summary>After the bar: the order to keep when a drag just ended, else null (another order shown than the one drawn: the bar settles
    /// in the next frame; a new bar after two settles that did not bring it back).</summary>
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
        // A settling frame: a tab that comes is not drawn in its first frame (ImGui adds it with an empty place), the order shows next frame.
        if(settling)return null;
        if(same)misses=0;
        else if(++misses>2)Renew();
        else settle=true;
        return null;
    }
}
