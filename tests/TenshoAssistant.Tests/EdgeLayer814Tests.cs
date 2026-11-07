// 0.814 (X-50, user report): the map's edge icons above the names' band and the tools.
internal static class EdgeLayer814Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Edge layer: a child window without input, drawn after the overlays, the cursor put back",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("ImGui.BeginChild(\"MapEdgeLayer\",size,false,ImGuiWindowFlags.NoInputs|ImGuiWindowFlags.NoBackground"),"layer");
            Check(map.Contains("edgeLabel=DrawEdgeMarkers(ImGui.GetWindowDrawList(),") && map.Contains("ImGui.SetCursorScreenPos(cursor);"),"drawn there, cursor back");
        }),
    ];
}
