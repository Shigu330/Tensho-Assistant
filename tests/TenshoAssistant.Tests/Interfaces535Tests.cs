using TenshoAssistant.Inventory;

// 0.535 (user requests): the map without its title bar; collapse and close on what has a title bar; the sort button under a window; Balanced not choosable when greyed.
internal static class Interfaces535Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sort button: a window over it hides it; beside it, or a layer as large as the screen, does not",() =>
        {
            var filters=new InventoryWindowRect(100,100,400,300);
            Check(InventoryCover.Covers(filters,150,150,80,24,1920,1080),"over it");
            Check(InventoryCover.Covers(filters,90,390,80,24,1920,1080),"a corner is enough");
            Check(!InventoryCover.Covers(filters,600,150,80,24,1920,1080),"beside it");
            Check(!InventoryCover.Covers(filters,150,450,80,24,1920,1080),"below it");
            Check(!InventoryCover.Covers(new(0,0,1920,1080),150,150,80,24,1920,1080),"a screen layer");
            Check(!InventoryCover.Covers(new(100,100,0,0),150,150,80,24,1920,1080),"no size");
            Check(InventoryCover.Covers(new(0,0,1920,200),150,150,80,24,1920,1080),"wide but not the whole screen");
            var button=Read("MainWindow.InventorySortButton.cs");
            Check(button.Contains("InventoryWindow.Above.Any(cover=>Inventory.InventoryCover.Covers("),"used");
            var reader=Read("Inventory/InventoryWindowReader.Dalamud.cs");
            Check(reader.Contains("name[0]==\x27_\x27 || name.StartsWith(\"Inventory\"") && reader.Contains("Math.Min((int)list.Count,list.Entries.Length)"),"HUD and its own grids left out; the list bounded");
        }),
        ("Panels: collapse and close on all that have a title bar; closed until the game window goes away",() =>
        {
            var host=Read("AttachedPanels.cs");
            Check(host.Contains("Flags = request.Flags & ~ImGuiWindowFlags.NoCollapse;") && host.Contains("ShowCloseButton = (request.Flags & ImGuiWindowFlags.NoTitleBar) == 0;"),"on all");
            Check(host.Contains("else host.dismissed[id] =") && host.Contains("dismissed[id] = System.Diagnostics.Stopwatch.GetTimestamp(); return;") && host.Contains("TotalSeconds >= DismissForgetSeconds"),"stays closed, comes back with the window");
            Check(!Read("MainWindow.TrackerWindow.cs").Contains("NoCollapse") && !Read("MainWindow.PersonalTasks.cs").Contains("NoCollapse"),"free panels too");
            Check(!Read("BankImportOverlay.cs").Contains("NoDecoration"),"the bank panel has its title bar");
        }),
        ("Map: its title bar on a setting, off by default; Balanced greyed is not choosable",() =>
        {
            Check(!new TenshoAssistant.MapViewSettings().HideTitleBar,"off by default");
            Check(Read("MainWindow.MapView.cs").Contains("(settings.HideTitleBar ? ImGuiWindowFlags.NoTitleBar:0)") && Read("MainWindow.MapView.cs").Contains("SettingBox(\"MapView.HideTitleBar\""),"setting");
            var panel=Read("MainWindow.CollectablePanel.cs");
            Check(panel.Contains("if(same)ImGui.BeginDisabled();") && panel.Contains("if(same)ImGui.EndDisabled();") && panel.Contains("if(balancedOff && risk==50)risk=CollectableRisk.Safe;"),"disabled, shown as Safe");
            Check(panel.Contains("ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)"),"its tooltip still says why");
        }),
    ];
}
