using TenshoAssistant.UI;

// 0.535 (user requests): the map without its title bar; collapse and close on what has a title bar; the sort button under a window; Balanced not choosable when greyed.
internal static class Interfaces535Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game windows (0.537): what touches an interface covers it; its own game window and what is behind that do not; a screen layer does not",() =>
        {
            GameWindow Of(string name,float x,float y,float w,float h,int rank)=>new(name,x,y,w,h,rank);
            var inventory=Of("InventoryExpansion",100,100,400,600,4989);var grid=Of("InventoryGrid0E",120,120,200,370,4993);var log=Of("MonsterNote",150,500,500,500,4998);var behind=Of("MonsterNote",150,500,500,500,4900);
            // The captures of 0.536: the button at the foot of the inventory, under the Hunting log, or over it.
            Check(GameWindows.Covers([inventory,grid,log],110,620,80,24,1920,1080,["Inventory"]),"a window in front of the inventory");
            Check(!GameWindows.Covers([inventory,grid,behind],110,620,80,24,1920,1080,["Inventory"]),"a window behind it");
            Check(!GameWindows.Covers([inventory,grid],110,620,80,24,1920,1080,["Inventory"]),"its own parts");
            Check(!GameWindows.Covers([inventory,log],700,100,80,24,1920,1080,["Inventory"]),"beside it");
            Check(GameWindows.Covers([log],200,600,300,100,1920,1080),"a free interface: whatever touches it");
            Check(!GameWindows.Covers([log],900,100,80,24,1920,1080),"not touching");
            Check(!GameWindows.Covers([Of("Big",0,0,1920,1080,9000)],200,600,300,100,1920,1080),"a screen layer");
            Check(!GameWindows.Covers([Of("NoSize",0,0,0,0,9000)],0,0,300,100,1920,1080),"no size");
            Check(GameWindows.Covers([log],200,600,300,100,1920,1080,["Gathering"]),"its game window is not open: whatever touches it");
            foreach(var name in new[]{"_ActionBar","NamePlate","CastBarEnemy","ChatLog","ChatLogPanel_0","ScenarioTree","CursorAddon","AreaMap"})Check(GameWindows.Ignored(name),"not a window: "+name);
            foreach(var name in new[]{"MonsterNote","InventoryExpansion","Character","RecipeNote","ItemDetail","ActionDetail","Tooltip","ContextMenu"})Check(!GameWindows.Ignored(name),"a window: "+name);
            var host=Read("AttachedPanels.cs");
            Check(host.Contains("[\"TenshoInventorySort\"] = [\"Inventory\"]") && host.Contains("id == HoverPanel || !owner.HidePanelsBehindGame && id != SortButton"),"each panel has its window; the sort button leaves whatever the setting");
            Check(Read("MapViewWindow.cs").Contains("owner.MapBehindGame(at, lastSize)") && Read("PanelWindow.cs").Contains("owner.BarBehindGame(at, lastSize)"),"map and bars");
            var configuration=new TenshoAssistant.Configuration();
            Check(configuration.HidePanelsBehindGame && configuration.HideBarsBehindGame && !configuration.MapView.HideBehindGame,"panels and bars on, the map off");
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
