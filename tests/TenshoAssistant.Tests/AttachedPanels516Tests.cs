// Issue #38: the panels attached to a game window go through the Dalamud window system.
internal static class AttachedPanels516Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Windows: no ImGui window of our own but the Fashion Report's invisible click zones",() =>
        {
            foreach(var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories))
            {
                if(file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))continue;
                var count=File.ReadAllText(file).Split("ImGui.Begin(").Length-1;
                if(Path.GetFileName(file)=="MainWindow.FashionWeek.cs")Check(count==1 && File.ReadAllText(file).Contains("ImGui.Begin($\"###TenshoFashionSlot{slot}\",ImGuiWindowFlags.NoDecoration|ImGuiWindowFlags.NoBackground"),"the click zones only");
                else Check(count==0,"a window outside the window system: "+Path.GetFileName(file));
            }
        }),
        ("Attached panels: they ask first, the windows are drawn after, and each request lasts one frame",() =>
        {
            var plugin=Read("Plugin.cs");
            var overlays=plugin.IndexOf("for(var index=0;index<overlays.Length;index++)",StringComparison.Ordinal);
            var apply=plugin.IndexOf("attachedPanels?.Apply();",StringComparison.Ordinal);var draw=plugin.IndexOf("windows.Draw();",StringComparison.Ordinal);var end=plugin.IndexOf("attachedPanels?.EndFrame();",StringComparison.Ordinal);
            Check(overlays>=0 && overlays<apply && apply<draw && draw<end,"the order");
            Check(plugin.Split("windows.Draw();").Length==2,"drawn once");
            Check(plugin.Contains("bankOverlay.Panels = attachedPanels") && plugin.Contains("attachedPanels?.Dispose()"),"owned and cleaned up");
        }),
        ("Attached panels: the same ids, position set just before the window, the close button answered, one failure stays alone",() =>
        {
            var host=Read("AttachedPanels.cs");
            Check(host.Contains("request.Prepare?.Invoke();") && host.Contains("if (request.Close is { } close) close();"),"prepared and closed in the frame");
            Check(host.Contains("RespectCloseHotkey = false;") && host.Contains("AllowPinning = false;"),"Escape closes nothing; a placed panel is not pinned");
            Check(host.Contains("catch (Exception error) { host.TurnOff(id, error); }") && host.Contains("if (broken.Contains(id)) return;"),"fails alone");
            foreach(var (file,id) in new[]{("BankImportOverlay.cs","$\"TenshoBankImport{id}\""),("MainWindow.Character.cs","\"TenshoCharacterPanel\""),("MainWindow.CollectablePanel.cs","\"TenshoCollectablePanel\""),("MainWindow.CraftLog.cs","\"TenshoCraftLog\""),("MainWindow.CraftProjection.cs","\"TenshoCraftLivePanel\""),("MainWindow.DutyFinder.cs","\"TenshoDutyFinder\""),("MainWindow.FashionWeek.cs","\"TenshoFashionSlotWindow\""),("MainWindow.FishingPanel.cs","\"TenshoFishingPanel\""),("MainWindow.GatheringPanel.cs","\"TenshoGatheringPanel\""),("MainWindow.InventorySortButton.cs","\"TenshoInventorySort\""),("MainWindow.MarketHover.cs","\"TenshoItemHover\""),("MainWindow.OceanVoyagePanel.cs","\"TenshoOceanVoyage\""),("MainWindow.RetainerSales.cs","\"TenshoRetainerSales\""),("MainWindow.SpearfishingPanel.cs","\"TenshoSpearfishingPanel\""),("MainWindow.TriadRequest.cs","\"TenshoTriadRequest\"")})
                Check(Read(file).Contains("Panels?.Show("+id),file+": asks by its id");
            Check(Read("MainWindow.CraftProjection.cs").Contains("()=>{configuration.CraftLivePanel=false;craftLive.SetEnabled(CraftLiveWanted(configuration));SaveReminders();}"),"its cross turns its setting off");
            Check(Read("MainWindow.FishingPanel.cs").Contains("fishingPanelManual ? ()=>fishingPanelManual=false:null"),"a cross only when opened by hand");
            Check(Read("MainWindow.FashionWeek.cs").Contains("()=>fashionOpenSlot=-1"),"the slot's cross");
        }),
    ];
}
