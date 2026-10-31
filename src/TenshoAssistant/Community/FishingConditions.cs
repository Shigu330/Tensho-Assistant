using System.Text.Json;
using TenshoAssistant.Gathering;

namespace TenshoAssistant.Community;

/// <summary>Conditions explicitly listed for one community source variant, not current availability.</summary>
public sealed record FishingConditions(GatheringSchedule Timing,IReadOnlyList<string>? Weather,
    IReadOnlyList<string>? PreviousWeather,string? Error=null);

public static class FishingConditionParser
{
    public static FishingConditions Parse(JsonElement spot)
    {
        // A malformed conditions block must not discard otherwise usable bait routes.
        try
        {
            if(spot.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Invalid community source conditions.");
            var timing=GatheringSchedule.Unknown;
            if(spot.TryGetProperty("during",out var during) && during.ValueKind!=JsonValueKind.Null)
            {
                if(during.ValueKind!=JsonValueKind.Object
                    || !during.TryGetProperty("start",out var start) || !during.TryGetProperty("end",out var end)
                    || start.ValueKind!=JsonValueKind.Number || !start.TryGetInt32(out var first)
                    || end.ValueKind!=JsonValueKind.Number || !end.TryGetInt32(out var last)
                    || first is <0 or >23 || last is <0 or >24)
                    throw new InvalidDataException("Invalid community ET hours.");
                // Source uses integer ET hours, inclusive start and exclusive end, wrapping midnight.
                timing=GatheringSchedule.FromEphemeral((ushort)(first*100),(ushort)(last*100));
            }
            return new(timing,Names(spot,"weather"),Names(spot,"transition"));
        }
        catch(InvalidDataException error){return new(GatheringSchedule.Unknown,null,null,error.Message);}
    }

    private static IReadOnlyList<string>? Names(JsonElement spot,string property)
    {
        if(!spot.TryGetProperty(property,out var list) || list.ValueKind==JsonValueKind.Null)return null;
        if(list.ValueKind!=JsonValueKind.Array || list.GetArrayLength()>32)throw new InvalidDataException("Invalid community weather list.");
        var names=new List<string>();
        foreach(var value in list.EnumerateArray())
        {
            if(value.ValueKind!=JsonValueKind.String || value.GetString() is not { } name
                || string.IsNullOrWhiteSpace(name) || name.Length>64 || name.Any(char.IsControl))
                throw new InvalidDataException("Invalid community weather name.");
            names.Add(name);
        }
        return Array.AsReadOnly(names.ToArray());
    }
}
