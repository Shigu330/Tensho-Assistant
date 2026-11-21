namespace TenshoAssistant.UI;

/// <summary>
/// X-605 (0.1697, the user's request of 18/11: « arrange the tabs of every page by dragging them with the mouse »): the order of a tab bar the
/// player arranged. ImGui's bar moves a dragged tab itself (ImGuiTabBarFlags.Reorderable) but keeps that order only while the game runs; the
/// order the bar shows is read from where each tab is drawn (its left edge), kept by tab id, and given back as the order the tabs are drawn in
/// (a new bar shows them in that order). Pure, as the chat's tabs (X-687). What ImGui allows: docs/evidence/0.1697-reorderable-tabs.md.
/// </summary>
public static class TabOrder
{
    /// <summary>The tabs in the order kept: the ids kept that are drawn now first, in the kept order, then the others in the list's order.</summary>
    public static List<T> Arrange<T>(IReadOnlyList<T> natural,IReadOnlyList<string>? kept,Func<T,string> id)
    {
        if(kept is not { Count:>0 })return [..natural];
        var rank=new Dictionary<string,int>(StringComparer.Ordinal);
        for(var index=0;index<kept.Count;index++)rank.TryAdd(kept[index],index);
        return natural.Select((tab,index)=>(Tab:tab,Rank:rank.TryGetValue(id(tab),out var at) ? at:kept.Count+index)).OrderBy(pair=>pair.Rank).Select(pair=>pair.Tab).ToList();
    }

    /// <summary>The ids as the bar shows them, from left to right.</summary>
    public static List<string> Shown(IEnumerable<(string Id,float X)> places)=>places.OrderBy(place=>place.X).Select(place=>place.Id).ToList();

    /// <summary>The order to keep after a drag: the ids shown, then the ids kept before that are not drawn now (a tab out of reach for now
    /// keeps a place, at the end).</summary>
    public static List<string> Keep(IReadOnlyList<string> shown,IReadOnlyList<string>? kept)
        =>[..shown,..(kept ?? []).Where(id=>!shown.Contains(id)).Distinct()];
}
