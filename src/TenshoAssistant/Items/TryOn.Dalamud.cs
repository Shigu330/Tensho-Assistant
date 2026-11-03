using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace TenshoAssistant.Items;

/// <summary>
/// 0.310 (user request: « try on » gear, everywhere fashion matters): opens the game's Try On window with an item, as the game's
/// own « Try On » menu entry does (AgentTryon.TryOn). Called from the UI draw (game thread). 0.321 (review 0.316, point 4): a failure
/// (the game function moved after an update) returns false instead of leaving the draw; it is reported once, then the caller turns the
/// button off (<see cref="Failed"/>).
/// </summary>
public static class TryOn
{
    /// <summary>Set after the first failure: the Try On buttons stay greyed for the session.</summary>
    public static bool Failed { get; private set; }

    public static unsafe bool Item(uint itemId, bool characterReady, Action<Exception>? report = null)
    {
        if (Failed) return false;
        // R-14 (0.452): the game's Try On previews the loaded character: with none (title, character select, loading) no call is made.
        if (!characterReady) return false;
        if ((nint)AgentTryon.MemberFunctionPointers.TryOn == 0)
        {
            Failed = true;
            report?.Invoke(new InvalidOperationException("AgentTryon.TryOn did not resolve."));
            return false;
        }
        try { return AgentTryon.TryOn(0, itemId, 0, 0, 0, false); }
        catch (Exception error) { Failed = true; report?.Invoke(error); return false; }
    }
}
