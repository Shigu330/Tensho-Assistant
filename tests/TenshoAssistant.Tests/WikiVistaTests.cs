using TenshoAssistant.Maps;

internal static class WikiVistaTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    // Rows of the wiki's raw Sightseeing Log page (read 2026-10-31), A Realm Reborn #1–3 and Heavensward #1.
    private const string Sample="""
==Unlock==
| 9 || not a vista table row before the vista sections
==A Realm Reborn Vistas==
| 1 || [[Sightseeing Log 1: Barracuda Piers|Barracuda Piers]] || [[Limsa Lominsa Upper Decks]] || {{coords|9.5, 7.8}} || width="125px"| {{weather icon|fair skies}} [[Fair Skies]] <br> {{weather icon|clear skies}} [[Clear Skies]] || 08:00-12:00 || width="80px"| {{action icon|Lookout}} || Located northwest out of [[The Bismarck]], there are two sets of bricks set into the wall there.
|-
| 2 || [[Sightseeing Log 2: The Astalicia|The Astalicia]] || [[Limsa Lominsa Lower Decks]] || {{coords|7.1, 15.1}} || {{weather icon|fair skies}} [[Fair Skies]] <br> {{weather icon|clear skies}} [[Clear Skies]] || 18:00-05:00 || {{action icon|Lookout}} || Climb to the top of ''[[The Astalicia]]'' (the ship there).
|-
| 3 || [[Sightseeing Log 3: Seasong Grotto|Seasong Grotto]] || [[Middle La Noscea]] || {{coords|20.3, 19.1}} || {{weather icon|rain}} [[Rain]] <br> {{weather icon|showers}} [[Showers]] || 05:00-08:00 || {{action icon|Pray}} || In the [[Seasong Grotto]], by the entrance.
==Heavensward Vistas==
=== Coerthas Western Highlands ===
|1 || [[Heavensward Sightseeing Log 1: Falcon's Nest|Falcon's Nest]] || [[Coerthas Western Highlands]] || {{coords|32, 36, 2.4}} || {{action icon|Lookout}} || On top of the tower. (Requires flying.)
""";
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Wiki vistas parse weathers, time, emote and comment per expansion section",()=>
        {
            var document=WikiVistaParser.Parse(Sample,DateTimeOffset.UnixEpoch);
            Check(document.Rows.Count==4,"four rows, the row before the vista sections ignored");
            var first=document.Rows[0];
            Check(first.Section==0 && first.Number==1 && first.Name=="Barracuda Piers" && first.Zone=="Limsa Lominsa Upper Decks" && first.X==9.5f && first.Y==7.8f,"identity and coordinates");
            Check(first.Weathers.SequenceEqual(new[]{"Fair Skies","Clear Skies"}) && first.Time=="08:00-12:00" && first.Emote=="Lookout","weathers despite the width attribute, time, emote");
            Check(first.Comment.StartsWith("Located northwest out of The Bismarck",StringComparison.Ordinal),"comment without wiki links");
            Check(document.Rows[1].Comment=="Climb to the top of The Astalicia (the ship there).","italics and links removed");
            Check(document.Rows[2].Weathers.SequenceEqual(new[]{"Rain","Showers"}) && document.Rows[2].Emote=="Pray","rain row");
            var later=document.Rows[3];
            Check(later.Section==1 && later.Number==1 && later.Weathers.Count==0 && later.Time.Length==0 && later.Emote=="Lookout" && later.X==32f && later.Y==36f,"Heavensward row has no weather");
        }),
        ("Wiki vistas match game vistas by coordinates and ARR number, never by guess",()=>
        {
            var rows=WikiVistaParser.Parse(Sample,DateTimeOffset.UnixEpoch).Rows;
            // Game coordinates from docs/evidence/0.129.0-sightseeing-probe.log (#001 9.6/7.8, #002 7.1/15.1, #003 20.3/19.1).
            var matched=WikiVistaParser.Match(rows,[(2162688u,0,9.6f,7.8f),(2162689u,1,7.1f,15.1f),(2162690u,2,20.3f,19.1f),(2162800u,112,32.1f,35.9f)]);
            Check(matched[2162688].Number==1 && matched[2162690].Weathers[0]=="Rain" && matched[2162800].Section==1,"ARR by number + coordinates, later by coordinates");
            var shifted=WikiVistaParser.Match(rows,[(2162688u,5,9.6f,7.8f),(2162689u,1,9f,9f)]);
            Check(!shifted.ContainsKey(2162688) && !shifted.ContainsKey(2162689),"wrong number or distant coordinates stay unmatched");
            var ambiguous=WikiVistaParser.Match([rows[3]],[(1u,200,32f,36f),(2u,201,32.1f,36.1f)]);
            Check(ambiguous.Count==0,"two candidates = no match");
        }),
        ("Wiki vistas reject a page without vista rows",()=>
        {
            var failed=false;
            try{WikiVistaParser.Parse("==Unlock==\nnothing here",DateTimeOffset.UnixEpoch);}catch(InvalidDataException){failed=true;}
            Check(failed,"rejected");
        }),
    };
}
