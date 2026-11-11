namespace TenshoAssistant.Alarms;

/// <summary>0.1097: a macro's sound effect, <c>&lt;se.1&gt;</c> to <c>&lt;se.16&gt;</c> as the game's macros write it; 0 means none.</summary>
public static class MacroSound
{
    public const int First=1,Last=16;
    public static bool Valid(int sound)=>sound is >= First and <= Last;
}
