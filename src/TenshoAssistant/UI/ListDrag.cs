namespace TenshoAssistant.UI;

/// <summary>
/// X-605 (0.1702, the user's request of 18/11: « and if possible the side bar's entries »): a list of rows the player reorders with the mouse
/// (the side bar's groups; ImGui has no tab bar there). Each frame the rows tell where they were drawn (<see cref="Row"/>); a row pressed and
/// dragged is the one moved; while the mouse is held, <see cref="Line"/> says where it would go (from the mouse's height); when it is
/// released, <see cref="Drop"/> gives the new order (null when nothing moved). Pure.
/// </summary>
public sealed class ListDrag
{
    public readonly List<(string Id,float Left,float Top,float Right,float Bottom)> Places=[];
    /// <summary>The row being dragged, null otherwise (a click on a row does nothing while it is set).</summary>
    public string? Dragged{get;private set;}

    /// <summary>A new frame: the rows are told again.</summary>
    public void Clear()=>Places.Clear();

    /// <summary>A row drawn this frame; <paramref name="dragging"/>: pressed and dragged now (it starts the drag when none is under way).</summary>
    public void Row(string id,float left,float top,float right,float bottom,bool dragging)
    {
        Places.Add((id,left,top,right,bottom));
        if(dragging && Dragged is null)Dragged=id;
    }

    /// <summary>Where the row dragged would go: the index of the row it would come before (the number of rows: at the end).</summary>
    public int Target(float y)
    {
        for(var index=0;index<Places.Count;index++)if(y<(Places[index].Top+Places[index].Bottom)/2)return index;
        return Places.Count;
    }

    /// <summary>While the mouse is held: the line where the row would go (left, height, right); null when no drag or no row.</summary>
    public (float Left,float Y,float Right)? Line(float y)
    {
        if(Dragged is null || Places.Count==0)return null;
        var at=Target(y);
        var height=at<Places.Count ? Places[at].Top:Places[^1].Bottom;
        return (Places[0].Left,height,Places[0].Right);
    }

    /// <summary>The mouse released: the new order of the rows (null when the row would stay where it was, or no drag); the drag ends.</summary>
    public List<string>? Drop(float y)
    {
        var dragged=Dragged;
        Dragged=null;
        if(dragged is null)return null;
        var order=Places.Select(place=>place.Id).ToList();
        var from=order.IndexOf(dragged);
        if(from<0)return null;
        var to=Target(y);
        if(to==from || to==from+1)return null;
        order.RemoveAt(from);
        order.Insert(to>from ? to-1:to,dragged);
        return order;
    }
}
