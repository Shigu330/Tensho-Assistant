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

    // X-521 M1 (review of 0.1497): while the setting is looked for, every header and tree node of the category is opened so that it is drawn.
    // Once it is found, only the sections on its path stay open: the others that were folded before the search are folded back (once, the
    // next time they are drawn). A setting given up folds back every section the search opened.
    private readonly Dictionary<uint,bool> foldedBefore=[];
    private readonly List<(uint Id,bool Header,float X)> openedThisFrame=[];
    private int openedFrame=-1;
    private readonly HashSet<uint> foldBack=[];

    /// <summary>Asks for the setting whose label key this is.</summary>
    public void Request(string labelKey)
    {
        key=labelKey;found=false;framesLeft=SearchFrames;frame=-1;rowsThisFrame=0;foundRow=-1;
        foldedBefore.Clear();openedThisFrame.Clear();openedFrame=-1;foldBack.Clear();
    }

    /// <summary>Once per frame of the page: a setting not drawn in time is given up.</summary>
    public void Tick()
    {
        if(key is null || found)return;
        if(--framesLeft<=0){key=null;FoldBackAllBut([]);}
    }

    /// <summary>
    /// Called before a header (<paramref name="header"/>) or a tree node is drawn, with its ImGui id, whether it is open now and the left edge
    /// where it is drawn. True when it must be opened (the setting is looked for); its former state is kept to fold it back after.
    /// </summary>
    public bool Open(uint id,bool openNow,bool header,float x,int frameNumber)
    {
        if(!Opening)return false;
        if(frameNumber!=openedFrame){openedFrame=frameNumber;openedThisFrame.Clear();}
        foldedBefore.TryAdd(id,!openNow);
        openedThisFrame.Add((id,header,x));
        return true;
    }

    /// <summary>True once for a section the search opened that is not on the setting's path and was folded before: fold it.</summary>
    public bool FoldBack(uint id)=>foldBack.Remove(id);

    /// <summary>
    /// The sections around a row drawn at <paramref name="rowX"/>, from those opened before it this frame (in drawing order): going back, the
    /// tree nodes whose edge lies left of the last one kept (they hold it), up to the first header (headers follow each other, the last one
    /// holds the row).
    /// </summary>
    public static HashSet<uint> PathTo(IReadOnlyList<(uint Id,bool Header,float X)> opened,float rowX)
    {
        var path=new HashSet<uint>();
        var edge=rowX;
        for(var i=opened.Count-1;i>=0;i--)
        {
            var (id,header,x)=opened[i];
            if(header){path.Add(id);break;}
            if(x<edge){path.Add(id);edge=x;}
        }
        return path;
    }

    private void FoldBackAllBut(HashSet<uint> path)
    {
        foreach(var (id,folded) in foldedBefore)if(folded && !path.Contains(id))foldBack.Add(id);
        foldedBefore.Clear();openedThisFrame.Clear();
    }

    /// <summary>True while the setting is looked for: the headers and tree nodes open so that it is drawn.</summary>
    public bool Opening=>key is not null && !found;

    /// <summary>
    /// Called right after a setting row: Scroll is true once, on the frame the setting is first drawn; Strength goes from 1 to 0 over the highlight
    /// (0 for any other row and once it is over).
    /// </summary>
    public (bool Scroll,float Strength) Reached(string labelKey,int frameNumber,double now,float rowX=0)
    {
        if(key is null || !string.Equals(key,labelKey,StringComparison.Ordinal))return (false,0);
        if(frameNumber!=frame){frame=frameNumber;rowsThisFrame=0;}
        var row=rowsThisFrame++;
        if(!found)
        {
            found=true;foundRow=row;foundAt=now;
            FoldBackAllBut(openedFrame==frameNumber ? PathTo(openedThisFrame,rowX):[]);
            return (true,1);
        }
        if(row!=foundRow)return (false,0);
        var elapsed=now-foundAt;
        if(elapsed>=HighlightSeconds){key=null;return (false,0);}
        return (false,(float)(1-elapsed/HighlightSeconds));
    }
}
