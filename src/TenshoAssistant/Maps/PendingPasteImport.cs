namespace TenshoAssistant.Maps;

/// <summary>X-645 (review of 0.1588, M1): an import of pasted « Place ( x , y ) » lines waiting for the zone names, read off the frame.
/// It leaves with the text of the click only: if the field's text changes, or the share area is not drawn for a frame (another tab,
/// the window closed), the wait is dropped and nothing is imported; the player clicks again.</summary>
internal sealed class PendingPasteImport
{
    private string? text;
    private int lastFrame;

    public bool Waiting => text is not null;

    /// <summary>The click of « Import »: wait with this text, from this frame.</summary>
    public void Start(string clicked,int frame){text=clicked;lastFrame=frame;}

    /// <summary>Each frame the share area is drawn: the text to import now (the names are read and the field still holds the clicked
    /// text), or Cancelled when the wait is dropped (text changed or emptied, a frame without the share area).</summary>
    public (string? Import,bool Cancelled) Poll(string current,bool namesRead,int frame)
    {
        if(text is null)return (null,false);
        var away=frame-lastFrame>1;
        lastFrame=frame;
        if(away || !string.Equals(current,text,StringComparison.Ordinal)){text=null;return (null,true);}
        if(!namesRead)return (null,false);
        var clicked=text;
        text=null;
        return (clicked,false);
    }
}
