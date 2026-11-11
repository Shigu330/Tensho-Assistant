namespace TenshoAssistant.UI;

/// <summary>
/// X-122, decision D-112 (user, 2026-11-10; 0.1043): every deletion of saved data asks for a second click. The first click arms the
/// button (its label becomes « Click again to delete »), a second click on the same button within <see cref="WindowMs"/> deletes; a click
/// on another delete button arms that one instead. Kept out of ImGui so it can be tested.
/// </summary>
public sealed class DeleteConfirm
{
    /// <summary>How long an armed button waits for its second click, in milliseconds.</summary>
    public const long WindowMs=5000;

    private uint armed;
    private long armedAt;

    /// <summary>True while <paramref name="id"/> waits for its second click.</summary>
    public bool IsArmed(uint id,long now)=>armed!=0 && armed==id && now-armedAt>=0 && now-armedAt<=WindowMs;

    /// <summary>A click on <paramref name="id"/>: true when it confirms an armed button (the deletion runs), false when it only arms it.</summary>
    public bool Click(uint id,long now)
    {
        if(IsArmed(id,now)){armed=0;return true;}
        armed=id;armedAt=now;
        return false;
    }
}
