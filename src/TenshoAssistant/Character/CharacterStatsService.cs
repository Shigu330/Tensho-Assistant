using System.Collections.Frozen;

namespace TenshoAssistant.Character;

public sealed record CharacterStatsState(ulong CharacterId, string Name, uint JobId, string Job, int Level,
    DateTimeOffset? ObservedAt, IReadOnlyDictionary<string, int> Values,uint? CurrentGp=null,uint? MaximumGp=null);

public sealed class CharacterStatsService
{
    private CharacterStatsState state = Empty();
    public CharacterStatsState State => Volatile.Read(ref state);
    public void Publish(ulong character, string name, uint jobId, string job, int level, IReadOnlyDictionary<string, int> values,uint? currentGp=null,uint? maximumGp=null)
    {
        if (character == 0) throw new ArgumentOutOfRangeException(nameof(character));
        if(currentGp.HasValue!=maximumGp.HasValue || currentGp>maximumGp) throw new ArgumentException("Invalid observed GP pair.");
        Interlocked.Exchange(ref state, new(character, name, jobId, job, level, DateTimeOffset.UtcNow,
            values.ToFrozenDictionary(StringComparer.Ordinal),currentGp,maximumGp));
    }
    public void Clear() => Interlocked.Exchange(ref state, Empty());
    private static CharacterStatsState Empty() => new(0, "", 0, "", 0, null, new Dictionary<string, int>().ToFrozenDictionary());
}
