namespace TenshoAssistant.Link;

/// <summary>
/// X-592 (external audit A22 of 18/11): the desktop application reads a copy of the plugin's files off the frame; each reading begins a
/// generation, and only the newest one is shown. Showing runs under the slot's lock, after the generation is checked, so an older reading
/// that finishes late never replaces a newer one, nor a « copy missing » set in between (a copy removed between two refreshes clears what
/// was shown, never leaves the old reading).
/// </summary>
public sealed class DesktopCopySlot
{
    private readonly object gate=new();
    private int generation;

    /// <summary>A new reading begins: every reading begun before it is now too old to be shown.</summary>
    public int Begin(){lock(gate)return ++generation;}

    /// <summary>Runs <paramref name="show"/> when <paramref name="reading"/> is still the newest; false (nothing shown) otherwise.</summary>
    public bool Publish(int reading,Action show)
    {
        lock(gate)
        {
            if(reading!=generation)return false;
            show();
            return true;
        }
    }
}
