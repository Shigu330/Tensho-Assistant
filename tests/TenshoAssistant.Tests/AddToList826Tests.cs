// 0.826 (X-58, user request): Tensho's right-click menus add an item's recipe to a crafting list.
internal static class AddToList826Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Add to list: items with a recipe only, one entry per list, in Tensho's menu and the game's",() =>
        {
            var lists=Read("MainWindow.CraftLists.cs");
            Check(lists.Contains("recipeCatalog.State.Catalog?.ForResult(itemId) is not { Count:>0 })return [];") && lists.Contains("CraftLists.Add(current,itemId,1)"),"recipe, one more");
            Check(Read("MainWindow.DutyFinder.cs").Contains("if(CraftListAdds(item.Id) is { Count:>0 } adds"),"Tensho's menu");
            Check(Read("Plugin.cs").Contains("Links=id=>[..mainWindow.CraftListAdds(id),..(mainWindow.ExternalLinksFor(id) ?? [])]"),"the game's menu");
        }),
    ];
}
