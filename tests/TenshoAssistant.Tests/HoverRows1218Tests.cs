// 0.1218 (X-01 C5, slowness analysis of 12/11): the item tooltip's panel sorted the storages holding the item at each frame (« hover stock »);
// they are now sorted once, when the holdings are built again (item, inventory, bank or character changed). The wiring is checked here.
internal static class HoverRows1218Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Hover rows 1218: the storages of the hovered item are sorted once per holdings built, never at each frame",() =>
        {
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketHover.cs")).Replace("\r\n","\n");
            var built=source.IndexOf("hoverHoldingsKey=(id,live,bankState,character);\n            hoverHoldingRows=hoverHoldings.Rows.Where(row=>row.Nq+row.Hq>0).OrderByDescending(row=>row.Nq+row.Hq).ToArray();",StringComparison.Ordinal);
            Check(built>0,"sorted where the holdings are built");
            Check(source.Contains("        var rows=hoverHoldingRows;\n",StringComparison.Ordinal) && source.Split("OrderByDescending(row=>row.Nq+row.Hq)").Length==2,"the frame reads the sorted rows only");
        }),
    ];
}
