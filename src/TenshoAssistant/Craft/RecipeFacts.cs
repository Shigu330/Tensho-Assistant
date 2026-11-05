namespace TenshoAssistant.Craft;

/// <summary>0.600 (C14): what a recipe asks and allows, from the game's Recipe sheet. Key: the end of a RecipeFacts language key.</summary>
public static class RecipeFacts
{
    public static IReadOnlyList<(string Key,string Value)> Of(CraftRecipeParameters p,Func<uint,string?> itemName)
    {
        ArgumentNullException.ThrowIfNull(p);
        var facts=new List<(string,string)>{(p.CanHq ? "Hq":"NoHq","")};
        if(p.QuickSynth)facts.Add(p.QuickSynthCraftsmanship>0 || p.QuickSynthControl>0 ? ("QuickFrom",$"{p.QuickSynthCraftsmanship}|{p.QuickSynthControl}"):("Quick",""));
        if(p.MasterBook.Length>0)facts.Add(("Book",p.MasterBook));
        if(p.Specialist)facts.Add(("Specialist",""));
        if(p.Expert)facts.Add(("Expert",""));
        if(p.RequiredItem!=0)facts.Add(("Item",itemName(p.RequiredItem) ?? $"#{p.RequiredItem}"));
        if(p.RequiredStatus.Length>0)facts.Add(("Status",p.RequiredStatus));
        return facts;
    }
}
