// 0.808 (X-44, user report): the Tensho entry in the crafting log's right-click menu.
internal static class RecipeMenu808Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Recipe menu: the crafting log's windows take the hovered item; other default menus still skip characters",() =>
        {
            var bridge=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/ItemContextMenuBridge.Dalamud.cs"));
            var windows=bridge[bridge.IndexOf("ItemWindows=",StringComparison.Ordinal)..];
            windows=windows[..windows.IndexOf("};",StringComparison.Ordinal)];
            Check(new[]{"RecipeNote","RecipeMaterialList","RecipeTree","RecipeProductList"}.All(name=>windows.Contains("\""+name+"\"",StringComparison.Ordinal)),"windows (0.852: quest windows after them)");
            Check(bridge.Contains("|| !itemWindow && !chatItem && (target.TargetContentId!=0 || target.TargetObject is not null || !string.IsNullOrEmpty(target.TargetName)))return;"),"characters still skipped elsewhere");
            Check(bridge.Contains("raw=gui.HoveredItem;") && bridge.Contains("if(raw==0)return;"),"the hovered item, none: nothing");
        }),
    ];
}
