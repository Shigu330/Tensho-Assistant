using System.Globalization;
using System.Text.Json;

namespace TenshoAssistant.Community;

public sealed record FishingSourceData(uint? SpotId,IReadOnlyList<IReadOnlyList<uint>>? BaitChains,FishingConditions? Conditions=null);
public sealed record FishingSources(bool SpotsKnown,IReadOnlyList<FishingSourceData> Spots);

/// <summary>Only explicit bait routes from community item data. Missing conditions are never inferred.</summary>
public static class FishingSourceParser
{
    public static FishingSources? Parse(JsonElement item,CancellationToken cancellation=default)
    {
        cancellation.ThrowIfCancellationRequested();
        if(!item.TryGetProperty("fish",out var fish) || fish.ValueKind==JsonValueKind.Null)return null;
        if(fish.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Invalid fishing object.");
        if(!fish.TryGetProperty("spots",out var list) || list.ValueKind==JsonValueKind.Null)return new(false,Array.AsReadOnly(Array.Empty<FishingSourceData>()));
        if(list.ValueKind!=JsonValueKind.Array || list.GetArrayLength()>256)throw new InvalidDataException("Invalid fishing source list.");
        var spots=new List<FishingSourceData>();var totalSteps=0;
        foreach(var spot in list.EnumerateArray())
        {
            cancellation.ThrowIfCancellationRequested();
            if(spot.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Invalid fishing source.");
            uint? spotId=spot.TryGetProperty("spot",out var id) && id.ValueKind!=JsonValueKind.Null ? Identity(id):null;
            IReadOnlyList<IReadOnlyList<uint>>? chains=null;
            if(spot.TryGetProperty("baits",out var baits) && baits.ValueKind!=JsonValueKind.Null)
            {
                if(baits.ValueKind!=JsonValueKind.Array || baits.GetArrayLength()>64)throw new InvalidDataException("Invalid bait route list.");
                var routes=new List<IReadOnlyList<uint>>();
                foreach(var route in baits.EnumerateArray())
                {
                    cancellation.ThrowIfCancellationRequested();
                    if(route.ValueKind!=JsonValueKind.Array || route.GetArrayLength() is <1 or >8)throw new InvalidDataException("Invalid bait chain.");
                    var copied=new List<uint>();
                    foreach(var step in route.EnumerateArray())
                    {
                        if(++totalSteps>4096)throw new InvalidDataException("Too many bait references.");
                        copied.Add(Identity(step));
                    }
                    routes.Add(Array.AsReadOnly(copied.ToArray()));
                }
                chains=Array.AsReadOnly(routes.ToArray());
            }
            spots.Add(new(spotId,chains,FishingConditionParser.Parse(spot)));
        }
        return new(true,Array.AsReadOnly(spots.ToArray()));
    }
    private static uint Identity(JsonElement value)
    {
        if(value.ValueKind==JsonValueKind.Number && value.TryGetUInt32(out var id) && id!=0)return id;
        if(value.ValueKind==JsonValueKind.String && uint.TryParse(value.GetString(),NumberStyles.None,CultureInfo.InvariantCulture,out id) && id!=0)return id;
        throw new InvalidDataException("Invalid fishing Item/spot identity.");
    }
}
