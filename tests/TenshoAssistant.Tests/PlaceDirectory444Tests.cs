using TenshoAssistant.Maps;

// X-09 (0.444): place directory — residential addresses, checked and bounded.
internal static class PlaceDirectory444Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Place directory: fields cleaned and bounded, add, remove, text",() =>
        {
            var place=new PlaceAddress(Guid.Empty,"  Chez Tensho ","Moogle",339,5,12,false," thé ");
            var clean=PlaceDirectory.Clean(place)!;
            Check(clean.Name=="Chez Tensho" && clean.Note=="thé" && clean.Id!=Guid.Empty,"cleaned, id given");
            Check(PlaceDirectory.Clean(place with {Name=""}) is null && PlaceDirectory.Clean(place with {World=" "}) is null && PlaceDirectory.Clean(place with {District=0}) is null
                && PlaceDirectory.Clean(place with {Ward=0}) is null && PlaceDirectory.Clean(place with {Plot=1000}) is null && PlaceDirectory.Clean(place with {Name="a\nb"}) is null,"refused");
            var list=PlaceDirectory.Add([],place)!;
            Check(list.Count==1 && PlaceDirectory.Remove(list,list[0].Id).Count==0,"add and remove");
            var full=Enumerable.Range(0,PlaceDirectory.Capacity).Aggregate((IReadOnlyList<PlaceAddress>)[],(items,_)=>PlaceDirectory.Add(items,place)!);
            Check(PlaceDirectory.Add(full,place) is null,"100 at most");
            Check(PlaceDirectory.Text(clean,"Brumée","Secteur","Parcelle","Appartement")=="Chez Tensho — Moogle · Brumée · Secteur 5 · Parcelle 12"
                && PlaceDirectory.Text(clean with {Apartment=true},"Brumée","Secteur","Parcelle","Appartement").EndsWith("Appartement 12",StringComparison.Ordinal),"text");
        }),
    ];
}
