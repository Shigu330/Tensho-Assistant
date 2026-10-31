using System.Collections.Frozen;

namespace TenshoAssistant.Character;

public sealed record CharacterStatsState(ulong CharacterId, string Name, uint JobId, string Job, int Level,
    DateTimeOffset? ObservedAt, IReadOnlyDictionary<string, int> Values,uint? CurrentGp=null,uint? MaximumGp=null,
    int Category=0,int Role=0,LevelModifiers Modifiers=default);
/// <summary>Screen rectangle of the game's Character window (0.182), for the panel beside it.</summary>
public readonly record struct CharacterWindowRect(float X,float Y,float Width,float Height);

public sealed class CharacterStatsService
{
    private CharacterStatsState state = Empty();
    public CharacterStatsState State => Volatile.Read(ref state);
    private CharacterWindowRect? window;
    public CharacterWindowRect? Window { get { lock (this) return window; } }
    internal void SetWindow(CharacterWindowRect? rect) { lock (this) window = rect; }
    public void Publish(ulong character, string name, uint jobId, string job, int level, IReadOnlyDictionary<string, int> values,uint? currentGp=null,uint? maximumGp=null,
        int category=0,int role=0,LevelModifiers modifiers=default)
    {
        if (character == 0) throw new ArgumentOutOfRangeException(nameof(character));
        if(currentGp.HasValue!=maximumGp.HasValue || currentGp>maximumGp) throw new ArgumentException("Invalid observed GP pair.");
        Interlocked.Exchange(ref state, new(character, name, jobId, job, level, DateTimeOffset.UtcNow,
            values.ToFrozenDictionary(StringComparer.Ordinal),currentGp,maximumGp,category,role,modifiers));
    }
    public void Clear() => Interlocked.Exchange(ref state, Empty());
    private static CharacterStatsState Empty() => new(0, "", 0, "", 0, null, new Dictionary<string, int>().ToFrozenDictionary());
}
