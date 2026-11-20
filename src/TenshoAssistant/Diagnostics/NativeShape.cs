namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-590 A05 (0.1610, external audit of 18/11, R08): a count read in the game's memory (a vector's length) outside what the structure can
/// hold is a structure changed by a game update, never an empty list: it throws, so the caller keeps its last copy and says the part
/// « unavailable after a game update » (logged once), instead of the same state as a zone that is really empty.
/// </summary>
public sealed class UnexpectedGameStructureException(string what,long value,long max)
    :InvalidOperationException($"Unexpected game structure, maybe after a game update: {what} is {value} (expected 0 to {max}).");

public static class NativeShape
{
    /// <summary>The count when it is between 0 and max; else <see cref="UnexpectedGameStructureException"/> (never 0 in its place).</summary>
    public static int Count(long count,int max,string what)
        =>count<0 || count>max ? throw new UnexpectedGameStructureException(what,count,max):(int)count;

    /// <summary>X-665 M2 (0.1613, review of 0.1612): a list read on its own inside a larger part: its failure leaves it empty and is
    /// reported apart (<paramref name="failure"/>), never by the whole part (the map's events stayed frozen on another zone's copy).</summary>
    public static IReadOnlyList<T> Apart<T>(Func<IReadOnlyList<T>> read,out Exception? failure)
    {
        try{var result=read();failure=null;return result;}
        catch(Exception error){failure=error;return [];}
    }
}
