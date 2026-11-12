using TenshoAssistant.Maps;

// 0.1166 (X-138, user capture of 2026-11-11, Central Shroud): in the map's right-click menu, Aetherytes tab, some aetherytes had their icon and no
// name. Their MapMarker row has no subtext (Central Shroud, range 5, sub-row 16: icon 60453, DataType 3, Aetheryte 3, PlaceNameSubtext 0): the game
// draws the place's name with another marker. The aetheryte's own name (Aetheryte sheet) is now shown in menus, tooltips and search, never drawn on the map.
internal static class AetheryteNames1166Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Aetheryte names 1166: a marker shows the game's label, else its aetheryte's name", () =>
        {
            Check(MapLabels.MenuName("Camp de Bentbranch", "Bentbranch") == "Camp de Bentbranch", "the game's label first");
            Check(MapLabels.MenuName("", "Lisière de ronces") == "Lisière de ronces", "no label: the aetheryte's name");
            Check(MapLabels.MenuName("", "") == "", "neither: nothing invented");
        }),
        ("Aetheryte names 1166: wiring (the title filled for aetheryte markers only, used by the menus, search, tooltips and teleport; the map's drawn names unchanged)", () =>
        {
            var source = Read("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs");
            Check(source.Contains("row.DataType==3 && label.Length==0 ? AetheryteName(row.DataKey.RowId):\"\"") && source.Contains("public string Shown=>MapLabels.MenuName(Label,Title);"), "the title of a nameless aetheryte marker");
            Check(source.Contains("Copy(range).Where(marker=>marker.Shown.Length>0)"), "search reads named aetherytes too");
            Check(Read("src/TenshoAssistant/MainWindow.MapLocalLists.cs").Contains("ImGui.Selectable($\"{marker.Shown}##Aetheryte{marker.AetheryteId}\")"), "the menu's Aetherytes tab");
            var view = Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(view.Contains("var label=marker.Shown.Length>0 ? marker.Shown:null;") && view.Contains("mapTeleportTarget=(aetheryte.AetheryteId,aetheryte.Shown)") && view.Contains("mapTeleportTarget=(marker.AetheryteId,marker.Shown)"), "tooltips and teleport");
            Check(view.Contains("ImGui.CalcTextSize(named.Label)") && !view.Contains("CalcTextSize(named.Shown)"), "the names drawn on the map stay the game's");
            Check(Read("src/TenshoAssistant/MainWindow.MapSearch.cs").Contains("marker.Shown,map.Zone") && Read("src/TenshoAssistant/MainWindow.ChatMapLinks.cs").Contains(".Shown;"), "search and chat links");
        }),
    ];
}
