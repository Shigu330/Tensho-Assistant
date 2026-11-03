using TenshoAssistant.Items;

namespace TenshoAssistant;

// g, X-07 (0.349): the item's page on the FFXIV Console Games Wiki (English name) and on the Lodestone database (Tensho's language),
// for the game's right-click menu, the item sheet and the « /tensho wiki | lodestone <item> » commands. Opened in the browser on the
// player's click or command only (Dalamud Util.OpenLink); nothing is fetched.
internal sealed partial class MainWindow
{
    internal IReadOnlyList<(string Label,Action Open)> ExternalLinksFor(uint id)
    {
        if(itemCatalog.State.Catalog?.Find(id) is not { } item)return [];
        var links=new List<(string,Action)>();
        if(item.EnglishName.Trim().Length>0)
        {
            var wiki=ExternalLinks.Wiki(item.EnglishName);
            links.Add((localization.Get("ExternalLinks.Wiki"),()=>Dalamud.Utility.Util.OpenLink(wiki)));
        }
        var lodestone=ExternalLinks.Lodestone(localization.CurrentCode,item.Name);
        links.Add((localization.Get("ExternalLinks.Lodestone"),()=>Dalamud.Utility.Util.OpenLink(lodestone)));
        return links;
    }

    /// <summary>« /tensho wiki | lodestone &lt;item&gt; »: opens the page of the item named; returns what to tell the player, or null when opened.</summary>
    internal string? OpenExternal(bool wiki,string text)
    {
        if(text.Trim().Length==0)return localization.Get("ExternalLinks.Usage");
        if(itemCatalog.State.Catalog is not { } catalog)return localization.Get("ExternalLinks.Loading");
        if(ExternalLinks.Find(catalog.Search(text,100),text) is not { } item)return localization.Format("ExternalLinks.NoItem",text.Trim());
        if(wiki && item.EnglishName.Trim().Length==0)return localization.Get("ExternalLinks.NoEnglish");
        Dalamud.Utility.Util.OpenLink(wiki ? ExternalLinks.Wiki(item.EnglishName):ExternalLinks.Lodestone(localization.CurrentCode,item.Name));
        return null;
    }
}
