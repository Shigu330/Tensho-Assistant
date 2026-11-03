namespace TenshoAssistant.Items;

/// <summary>
/// g, X-07 (0.349): addresses of an item on the FFXIV Console Games Wiki (English names only: the wiki page is the English item name)
/// and on the Lodestone database search of the Tensho language (fr, de, jp, otherwise na). Opened in the player's browser on their
/// click or command only; nothing is fetched by Tensho.
/// </summary>
public static class ExternalLinks
{
    public static string Wiki(string englishName)
        =>"https://ffxiv.consolegameswiki.com/wiki/"+Uri.EscapeDataString(englishName.Trim().Replace(' ','_'));

    public static string Lodestone(string languageCode,string name)
    {
        var host=languageCode switch
        {
            "fr"=>"https://fr.finalfantasyxiv.com",
            "de"=>"https://de.finalfantasyxiv.com",
            "ja"=>"https://jp.finalfantasyxiv.com",
            _=>"https://na.finalfantasyxiv.com",
        };
        return host+"/lodestone/playguide/db/search/?q="+Uri.EscapeDataString(name.Trim());
    }

    /// <summary>The item a command names: an exact name (any case) first, else the shortest name containing the text; null if none.</summary>
    public static ItemDefinition? Find(IEnumerable<ItemDefinition> items,string text)
    {
        var wanted=text.Trim();
        if(wanted.Length==0)return null;
        ItemDefinition? best=null;
        foreach(var item in items)
        {
            if(string.Equals(item.Name,wanted,StringComparison.CurrentCultureIgnoreCase))return item;
            if(item.Name.Contains(wanted,StringComparison.CurrentCultureIgnoreCase) && (best is null || item.Name.Length<best.Name.Length))best=item;
        }
        return best;
    }
}
