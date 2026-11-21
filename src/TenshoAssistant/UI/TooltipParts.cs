namespace TenshoAssistant.UI;

/// <summary>
/// X-684 (0.1696, M3 of the review X-700, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the parts of Tensho's
/// own tooltip bodies that can be turned off, by kind (as the four parts of the item panel under the game's tooltip), named « Kind.Part ».
/// One table for the settings (Settings › General › Other tooltips), the bodies and the test; every part is shown by default.
/// </summary>
public static class TooltipParts
{
    public static readonly (string Kind,string[] Parts)[] All=
    [
        ("Place",["Region","Coordinates","Aetheryte","Weather"]),
        ("Quest",["Category","Level","Zone","Expansion","Progress"]),
        ("Monster",["Level","Zone","Notes"]),
        ("Action",["Detail","Description"]),
        ("Fate",["Kind","Place","Now","Description","Participants"]),
        ("Npc",["Places","Shops","Notes"]),
        ("Duty",["Kind","Zone","Status","Unlock"]),
        ("Recipe",["Stats","Minimum","Facts","Crafted","Ingredients"]),
        ("Player",["Job"]),
        ("Gathering",["Detail","Place","Opening","Items"]),
    ];

    /// <summary>Every part's name, « Kind.Part ».</summary>
    public static IEnumerable<string> Names=>All.SelectMany(kind=>kind.Parts.Select(part=>kind.Kind+"."+part));

    /// <summary>Whether a part is shown: not in the list of parts turned off (none read: all shown).</summary>
    public static bool Shown(IReadOnlyCollection<string>? hidden,string part)=>hidden is not { Count:>0 } || !hidden.Contains(part);

    /// <summary>A new list with the part shown or turned off (each name once; the list is replaced whole, never changed in place).</summary>
    public static List<string> With(IReadOnlyCollection<string>? hidden,string part,bool shown)
    {
        var kept=(hidden ?? []).Where(name=>name!=part).Distinct().ToList();
        if(!shown)kept.Add(part);
        return kept;
    }
}
