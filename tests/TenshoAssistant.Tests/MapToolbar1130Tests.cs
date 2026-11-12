using TenshoAssistant.Maps;

// 0.1130 (X-164, user request of 12/11 with a capture of the map's tool column): icons centred in their buttons, the zoom percentage typed
// with the keyboard, a « 100% » button under it.
internal static class MapToolbar1130Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map toolbar: a typed percentage becomes a zoom, clamped; anything else is ignored",() =>
        {
            Check(MapProjection.ZoomFromPercent("150")==1.5f && MapProjection.ZoomFromPercent("150%")==1.5f && MapProjection.ZoomFromPercent(" 87,5 % ")==0.875f,"150, 150% and 87,5 %");
            Check(MapProjection.ZoomFromPercent("1000")==4f && MapProjection.ZoomFromPercent("5")==0.15f,"clamped to 15–400 %");
            Check(MapProjection.ZoomFromPercent("")==null && MapProjection.ZoomFromPercent("abc")==null && MapProjection.ZoomFromPercent("0")==null && MapProjection.ZoomFromPercent(null)==null,"not a zoom: ignored");
        }),
        ("Map toolbar: typed percentage, « 100% » under it, icons centred on their ink",() =>
        {
            var view=Read("MainWindow.MapView.cs");
            var typed=view.IndexOf("ImGui.InputText(\"##MapZoomTyped\",ref typed,8,ImGuiInputTextFlags.CharsDecimal|ImGuiInputTextFlags.AutoSelectAll|ImGuiInputTextFlags.EnterReturnsTrue)",StringComparison.Ordinal);
            var hundred=view.IndexOf("if(TextToolButton(\"100%\",\"MapView.Zoom100\"))ZoomTo(1);",StringComparison.Ordinal);
            Check(typed>0 && hundred>typed,"the percentage is typed, « 100% » under it");
            Check(view.Contains("&& MapProjection.ZoomFromPercent(typed) is { } typedZoom)ZoomTo(typedZoom);"),"Enter applies it");
            var button=Read("UI/Components/ToolButton.cs");
            Check(button.Contains("var glyph=label.Length>0 ? font.FindGlyph(label[0]):null;") && button.Contains("at=(min+max-ink)/2-new Vector2(glyph->X0,glyph->Y0)*scale;"),"the glyph's ink box is centred");
            Check(button.IndexOf("Tip(tooltipKey);",StringComparison.Ordinal)>button.IndexOf("finally{ImGui.PopFont();}",StringComparison.Ordinal),"the tooltip is written with the text font");
        }),
    ];
}
