namespace TenshoAssistant.UI;

/// <summary>
/// X-600 (0.1573): keeps a tab bar and the page shown in step. The page can change from anywhere (the side bar, a link, a command), and
/// ImGui applies a forced selection one frame late: during that frame the bar still reports its former tab as open, which must not be read
/// as a click (it would send the player back to the page they just left). Pure: the window asks <see cref="Force"/> for each tab, tells
/// <see cref="Chosen"/> which tab ImGui reports as open, and ends the frame with <see cref="Shown"/>.
/// </summary>
public struct PageTabSync<T> where T:struct
{
    private T? shown;

    /// <summary>No bar is drawn: the next bar starts from the page, not from what ImGui remembers.</summary>
    public void Reset()=>shown=null;

    /// <summary>The page changed since the bar was last drawn (or the bar is new): what the bar reports during this frame is not a click.</summary>
    public readonly bool Pending(T current)=>shown is not { } last || !EqualityComparer<T>.Default.Equals(last,current);

    /// <summary>This tab must be selected by force: it is the page's, and the bar does not know it yet.</summary>
    public readonly bool Force(T current,T tab)=>Pending(current) && EqualityComparer<T>.Default.Equals(tab,current);

    /// <summary>The page to move to when ImGui reports <paramref name="open"/> as the open tab; null when nothing changes.</summary>
    public readonly T? Chosen(T current,T open)=>Pending(current) || EqualityComparer<T>.Default.Equals(open,current) ? null:open;

    /// <summary>End of the frame: the bar was drawn for this page.</summary>
    public void Shown(T current)=>shown=current;
}
