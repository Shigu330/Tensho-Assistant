namespace TenshoAssistant.Maps;

/// <summary>
/// X-09 (0.444): a place of the directory — a residential address: name, world, district (TerritoryType of a residential area,
/// docs/evidence/0.444-housing.md), ward, plot or apartment room, and a note. Kept in the configuration (100 at most).
/// </summary>
public sealed record PlaceAddress(Guid Id,string Name,string World,uint District,int Ward,int Plot,bool Apartment=false,string Note="");

public static class PlaceDirectory
{
    public const int Capacity=100,TextLimit=60;

    /// <summary>The address cleaned and checked (null when a field is missing or out of bounds): name and world 1-60 characters,
    /// ward 1-99, plot or room 1-999.</summary>
    public static PlaceAddress? Clean(PlaceAddress address)
    {
        string? Text(string? value,bool required)
        {
            var text=(value ?? "").Trim();
            if(text.Any(char.IsControl) || text.Length>TextLimit || required && text.Length==0)return null;
            return text;
        }
        if(Text(address.Name,true) is not { } name || Text(address.World,true) is not { } world || Text(address.Note,false) is not { } note
           || address.District==0 || address.Ward is < 1 or > 99 || address.Plot is < 1 or > 999)return null;
        return address with {Name=name,World=world,Note=note,Id=address.Id==Guid.Empty ? Guid.NewGuid():address.Id};
    }

    public static IReadOnlyList<PlaceAddress>? Add(IReadOnlyList<PlaceAddress> places,PlaceAddress address)
        =>places.Count>=Capacity || Clean(address) is not { } clean ? null:[..places,clean];

    public static IReadOnlyList<PlaceAddress> Remove(IReadOnlyList<PlaceAddress> places,Guid id)=>places.Where(place=>place.Id!=id).ToArray();

    /// <summary>« Name — World · District · Ward 5 · Plot 12 » (or « Apartment 12 »), with the labels given in the plugin's language.</summary>
    public static string Text(PlaceAddress place,string district,string ward,string plot,string apartment)
        =>$"{place.Name} — {place.World} · {district} · {ward} {place.Ward} · {(place.Apartment ? apartment:plot)} {place.Plot}";
}
