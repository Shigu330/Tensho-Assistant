using System.Text;
using TenshoAssistant.Craft;

// 0.423: A-09 Teamcraft import link; user report « Crafting : item » opening the Crafting page with no recipe.
internal static class Teamcraft423Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Teamcraft: rows « item,recipe|null,quantity » joined by « ; », base64 then URL-encoded",() =>
        {
            var link=TeamcraftExport.Link([(20545,null,3),(17962,32308,1),(20247,0,1),(5,9,0)])!;
            Check(link.StartsWith(TeamcraftExport.ImportUrl,StringComparison.Ordinal),"import page");
            var decoded=Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(link[TeamcraftExport.ImportUrl.Length..])));
            // Example string of Teamcraft's import page comment.
            Check(decoded=="20545,null,3;17962,32308,1;20247,null,1","rows; zero quantity left out");
            Check(TeamcraftExport.Link([]) is null && TeamcraftExport.Link([(0,1,1)]) is null,"nothing to import");
        }),
        ("Crafting link from an item opens its recipe on Preparation; no recipe chosen = the item's own",() =>
        {
            Check(Read("MainWindow.SourceLinks.cs").Contains("ForResult(craftId) is { Count:>0 } own)OpenCraftPreparation(PreferredRecipe(own));"),"link opens the recipe");
            Check(Read("MainWindow.CraftSimulation.cs").Contains("if(simulationRecipe is null && itemId>0 && recipeCatalog.State.Catalog?.ForResult((uint)itemId) is { Count:>0 } own)OpenCraftPreparation(PreferredRecipe(own));"),
                "fallback on the item in context");
        }),
    ];
}
