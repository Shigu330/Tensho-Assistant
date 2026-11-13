namespace TenshoAssistant.UI;

/// <summary>
/// X-178 (0.1256): what to show in place of a game icon that does not come. An icon without id: its name at once. A texture still loading
/// (Dalamud gives it a few frames later; a missing file never comes): an empty square for 60 frames, then the name. The memory is per icon
/// id and bounded. Not thread-safe: one caller, the draw.
/// </summary>
public sealed class IconWait
{
    public const int Frames=60,Capacity=256;
    private readonly Dictionary<uint,int> missingSince=[];

    public int Count=>missingSince.Count;

    /// <param name="drawn">The icon was drawn this frame.</param>
    /// <param name="frame">The current frame number (it only grows).</param>
    /// <returns>True when the name is written instead of the icon.</returns>
    public bool ShowText(uint iconId,bool drawn,int frame)
    {
        if(iconId==0)return true;
        if(drawn){missingSince.Remove(iconId);return false;}
        if(!missingSince.TryGetValue(iconId,out var since))
        {
            if(missingSince.Count>=Capacity)missingSince.Clear();
            missingSince[iconId]=since=frame;
        }
        // A frame counter that went back (another context): the wait starts again.
        if(frame<since){missingSince[iconId]=frame;return false;}
        return frame-since>=Frames;
    }
}
