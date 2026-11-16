namespace TenshoAssistant.UI;

/// <summary>
/// X-507 (the user's bug of 16/11): choosing a result of the settings search must reach the setting itself, not only its page. The state of that
/// journey, kept out of ImGui so it can be tested: the page asks for a setting (<see cref="Request"/>), the sections around it stay open while it
/// is looked for (<see cref="Opening"/>), each setting row says it was drawn (<see cref="Reached"/>), the first row of that key is scrolled into
/// view once and highlighted for <see cref="HighlightSeconds"/>, fading out. A setting that is not drawn (its parent option is off) is given up
/// after <see cref="SearchFrames"/> frames; two rows with the same key (one per page, one per chat tab) count once, the first.
/// </summary>
public sealed class SettingFocus
{
    public const double HighlightSeconds=2;
    public const int SearchFrames=8;

    private string? key;
    private bool found;
    private int framesLeft;
    private double foundAt;
    private int frame=-1,rowsThisFrame,foundRow;

    /// <summary>Asks for the setting whose label key this is.</summary>
    public void Request(string labelKey)
    {
        key=labelKey;found=false;framesLeft=SearchFrames;frame=-1;rowsThisFrame=0;foundRow=-1;
    }

    /// <summary>Once per frame of the page: a setting not drawn in time is given up.</summary>
    public void Tick()
    {
        if(key is null || found)return;
        if(--framesLeft<=0)key=null;
    }

    /// <summary>True while the setting is looked for: the headers and tree nodes open so that it is drawn.</summary>
    public bool Opening=>key is not null && !found;

    /// <summary>
    /// Called right after a setting row: Scroll is true once, on the frame the setting is first drawn; Strength goes from 1 to 0 over the highlight
    /// (0 for any other row and once it is over).
    /// </summary>
    public (bool Scroll,float Strength) Reached(string labelKey,int frameNumber,double now)
    {
        if(key is null || !string.Equals(key,labelKey,StringComparison.Ordinal))return (false,0);
        if(frameNumber!=frame){frame=frameNumber;rowsThisFrame=0;}
        var row=rowsThisFrame++;
        if(!found){found=true;foundRow=row;foundAt=now;return (true,1);}
        if(row!=foundRow)return (false,0);
        var elapsed=now-foundAt;
        if(elapsed>=HighlightSeconds){key=null;return (false,0);}
        return (false,(float)(1-elapsed/HighlightSeconds));
    }
}
