namespace TenshoAssistant.Gathering;

/// <summary>X-701 (0.1685): the last cast, as the game's line state tells it: when it started, when the fish bit and how hard, and whether Chum was on.</summary>
/// <param name="Cast">When the line turned to CastingOut (1).</param>
/// <param name="Bite">When it turned to Bite (5) after that cast; null while nothing bit.</param>
/// <param name="Strength">1 « ! », 2 « !! », 3 « !!! » (FishingHookStrength 292 to 294, the player's animation slot 7); 0 unknown.</param>
/// <param name="Chum">Status 763 (« Amorçage », Chum) on the player when the cast began; null when it could not be read.</param>
public sealed record FishingCast(DateTimeOffset Cast, DateTimeOffset? Bite, int Strength, bool? Chum);

/// <summary>
/// X-701 (0.1685, user request of 21/11): the line's state of the game (FishingEventHandler.State, docs/evidence/0.887-fishing-bite.md: 1
/// CastingOut at each cast, mooch included, lures leave it; 5 Bite; then 6 Hooking, 2 PullingPoleIn, 4 PoleReady) turned into the last cast.
/// The bite belongs to the cast it follows and stays until the next cast, caught or not; nothing is guessed: no state 5, no bite.
/// Fed on the framework thread; <see cref="Last"/> is an immutable record swapped whole, read from any thread.
/// </summary>
public sealed class FishingLine
{
    public const int CastingOut = 1, Bite = 5;
    private int state = -1;
    private FishingCast? last;

    public FishingCast? Last => Volatile.Read(ref last);

    /// <summary>The bite strength from the player's animation slot 7 at the bite (FishingHookStrength 292 !, 293 !!, 294 !!!); 0 unknown.</summary>
    public static int Strength(ushort timeline) => (int)Discoveries.ItemGains.BiteStrength(timeline);

    /// <summary>
    /// The line's state now (each frame). <paramref name="chum"/> is asked at a cast only, <paramref name="timeline"/> at the bite and on each
    /// frame after while its strength is unknown (0.1095: the animation can come a frame after the state).
    /// </summary>
    public void See(int now, DateTimeOffset at, Func<bool?> chum, Func<ushort> timeline)
    {
        if (now == state)
        {
            if (now == Bite && last is { Bite: not null, Strength: 0 } bitten && Strength(timeline()) is var later and not 0)
                Volatile.Write(ref last, bitten with { Strength = later });
            return;
        }
        state = now;
        if (now == CastingOut) Volatile.Write(ref last, new FishingCast(at, null, 0, chum()));
        else if (now == Bite && last is { Bite: null } cast) Volatile.Write(ref last, cast with { Bite = at, Strength = Strength(timeline()) });
    }
}
