// 0.900: the Fashion Report slot list dims the pieces you do not own; the icon's tooltip and right-click menu are drawn without the dimming.
internal static class FashionIconMenu900Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fashion slot list: only the icon's image is dimmed, its menu is drawn after the dimming is removed",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FashionWeek.cs"));
            var image=code.IndexOf("DrawItemImage(item,new Vector2(compact ? 22:26));");var pop=code.IndexOf("if(dim)ImGui.PopStyleVar();",image);
            var menu=code.IndexOf("ItemLinksMenu(UniqueMenuId($\"FashionWeekIcon{hint}/{id}\"),item);",image);
            Check(image>0 && pop>image && menu>pop,"order");
            Check(!code.Contains("DrawItemIcon(item,new Vector2(compact ? 22:26));"),"no menu inside the dimming");
        }),
    ];
}
