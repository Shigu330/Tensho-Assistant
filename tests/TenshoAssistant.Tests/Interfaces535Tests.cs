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
        ("0.539: panels follow their game window every tick; the map leaves for them and comes back after a moment",() =>
        {
            var aside=new StepAside(TimeSpan.FromSeconds(1));
            Check(!aside.Hidden(false,TimeSpan.Zero),"nothing in front");
            Check(aside.Hidden(true,TimeSpan.FromSeconds(5)),"leaves at once");
            Check(aside.Hidden(false,TimeSpan.FromSeconds(5.5)),"stays a moment");
            Check(aside.Hidden(true,TimeSpan.FromSeconds(5.9)) && aside.Hidden(false,TimeSpan.FromSeconds(6.5)),"another tooltip starts the moment again");
            Check(!aside.Hidden(false,TimeSpan.FromSeconds(7)),"comes back");
            Check(GameWindows.Touch(0,0,10,10,5,5,10,10) && !GameWindows.Touch(0,0,10,10,10,0,10,10),"touching");
            var follower=Read("UI/GameWindowFollower.Dalamud.cs");
            Check(follower.Contains("if(wanted.IsEmpty)return;") && follower.Contains("now-asked>Stopwatch.Frequency") && follower.Contains("framework.Update-=OnUpdate"),"only what a panel asks, and cleaned");
            foreach(var (file,addon) in new[]{("MainWindow.Character.cs","Character"),("MainWindow.GatheringPanel.cs","Gathering"),("MainWindow.CollectablePanel.cs","GatheringMasterpiece"),("MainWindow.SpearfishingPanel.cs","SpearFishing"),("MainWindow.DutyFinder.cs","ContentsFinder"),("MainWindow.TriadRequest.cs","TripleTriadRequest"),("MainWindow.CraftLog.cs","RecipeNote"),("MainWindow.FashionWeek.cs","FashionCheck")})
                Check(Read(file).Contains($"WindowMoved(\"{addon}\","),"follows: "+addon);
            Check(Read("BankImportOverlay.cs").Contains("follow(context.AddonName"),"bank panel follows");
            Check(Read("MainWindow.TrackerWindow.cs").Contains("Panels?.Over(at,size)==true") && Read("AttachedPanels.cs").Contains("window.Key == HoverPanel || Anchors.ContainsKey(window.Key)"),"the map leaves for attached panels, not for free ones");
            Check(!Read("MainWindow.cs").Contains("BehindGame") && !Read("MainWindow.cs").Contains("Panels?.Show") && !Read("AttachedPanels.cs").Contains("\"TenshoAssistant"),"the main Tensho window neither hides the map nor is hidden");
            Check(Read("MapViewWindow.cs").Contains("aside.Hidden(lastPosition") && Read("MapViewWindow.cs").Contains("owner.MapBehindGame(at, lastSize)"),"the map uses the delay");
        }),
        ("0.541: a tool's item list shows prices: lowest of NQ and HQ, unknown never free, asked only on the button",() =>
        {
            Check(TenshoAssistant.MyTools.ToolItemCost.Lowest(100,80)==80 && TenshoAssistant.MyTools.ToolItemCost.Lowest(null,80)==80 && TenshoAssistant.MyTools.ToolItemCost.Lowest(100,null)==100 && TenshoAssistant.MyTools.ToolItemCost.Lowest(null,null) is null,"lowest");
            var cost=TenshoAssistant.MyTools.ToolItemCost.Missing([(2,10m),(0,null),(3,null),(null,5m)]);
            Check(cost==(20m,2),"known 2 x 10; nothing missing is free; no price or no stock is unknown");
            var tools=Read("MainWindow.MyTools.cs");
            Check(tools.Contains("toolPrices.StartListAsync(market,character,worldId,unpriced.Take(ToolItemCost.MaximumAsked))") && tools.Contains("SourceButton(localization.Format(\"MyToolsUi.AskPrices\""),"on the button, within the session's limit");
            Check(TenshoAssistant.MyTools.ToolItemCost.MaximumAsked==32,"the session's limit");
        }),
        ("0.550: the quest list has a level range and sorted columns",() =>
        {
            Check(TenshoAssistant.Quests.QuestList.InLevels(0,0,0) && TenshoAssistant.Quests.QuestList.InLevels(50,0,0),"no limit: everything");
            Check(!TenshoAssistant.Quests.QuestList.InLevels(0,10,0),"unknown level is not level 0");
            Check(TenshoAssistant.Quests.QuestList.InLevels(50,50,50) && !TenshoAssistant.Quests.QuestList.InLevels(49,50,0) && !TenshoAssistant.Quests.QuestList.InLevels(51,0,50),"bounds");
            TenshoAssistant.Quests.QuestEntry Of(uint id,string name,int level,string zone)=>new(id,name,"Cat",0,TenshoAssistant.Quests.QuestProgress.Unconfirmed,null){Level=(ushort)level,Zone=zone};
            var list=new[]{Of(1,"B",10,"Z"),Of(2,"A",20,"Y"),Of(3,"C",10,"X")};
            Check(TenshoAssistant.Quests.QuestList.Sorted(list,TenshoAssistant.Quests.QuestList.ColumnLevel,true).First().Id==2,"by level, descending");
            Check(TenshoAssistant.Quests.QuestList.Sorted(list,TenshoAssistant.Quests.QuestList.ColumnLevel,false).Select(entry=>entry.Id).SequenceEqual(new uint[]{1,3,2}),"then by name");
            Check(TenshoAssistant.Quests.QuestList.Sorted(list,TenshoAssistant.Quests.QuestList.ColumnZone,false).First().Id==3,"by zone");
        }),
        ("0.551: a monster's map button places the flag at its known position, and says where it comes from",() =>
        {
            var monsters=Read("MainWindow.Monsters.cs");
            Check(monsters.Contains("if (monster.X is { } x && monster.Y is { } y)") && monsters.Contains("FlagFromMap(chosen,x,y);") && monsters.Contains("mapX=20.5f"),"known position, else as before");
        }),
        ("0.552: the crafting log tries sequences saved for other recipes, when the setting asks",() =>
        {
            var session=Read("Craft/CraftLogSession.cs");
            Check(session.Contains("!own && !input.TryAllPresets") && session.Contains("own ? 0:preset.ResultItemId"),"only on the setting, and it says for which recipe");
            Check(new TenshoAssistant.Configuration().CraftLogTryAllPresets,"on by default");
            Check(Read("MainWindow.CraftLog.cs").Contains("TryAllPresets:configuration.CraftLogTryAllPresets"),"passed to the session");
        }),
        ("0.555: item commands in French and English, added beside wiki and lodestone",() =>
        {
            var links=Read("MainWindow.ExternalLinks.cs");
            foreach(var word in new[]{"\"objet\" or \"item\"","\"prix\" or \"price\"","\"recolte\" or \"récolte\" or \"gather\"","\"recette\" or \"recipe\""})Check(links.Contains(word),word);
            Check(Read("Plugin.cs").Contains("MainWindow.CommandRoute(parts[0]) is { } route"),"the command uses them");
        }),
        ("0.556: an alarm can ring again on time, once, after really ringing ahead",() =>
        {
            var at=new DateTimeOffset(2026,11,5,12,0,0,TimeSpan.Zero);
            TenshoAssistant.Alarms.TenshoAlarm Alarm()=>new(){Repeat=TenshoAssistant.Alarms.AlarmRepeat.Once,At=at,LeadMinutes=5};
            var alarm=Alarm();
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(alarm,at) is null,"it has not rung ahead");
            alarm.LastRung=at;
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(alarm,at.AddMinutes(-1)) is null,"not before its time");
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(alarm,at.AddSeconds(5))==at,"at its time");
            alarm.LastOnTime=at;
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(alarm,at.AddSeconds(6)) is null,"once");
            var noLead=Alarm();noLead.LeadMinutes=0;noLead.LastRung=at;
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(noLead,at.AddSeconds(5)) is null,"no lead: the first ring was on time");
            Check(!new TenshoAssistant.Alarms.AlarmSettings().RingOnTime,"off by default");
        }),
        ("0.557: an alarm can be put off and edited; the original stays",() =>
        {
            var now=new DateTimeOffset(2026,11,5,12,0,0,TimeSpan.Zero);
            var alarm=new TenshoAssistant.Alarms.TenshoAlarm{Label="Mandragora",Group="G",ItemId=4,Repeat=TenshoAssistant.Alarms.AlarmRepeat.EorzeaDaily,EtMinute=60};
            var later=TenshoAssistant.Alarms.AlarmSchedule.Snooze(alarm,now,5);
            Check(later.Label=="Mandragora" && later.Group=="G" && later.ItemId==4 && later.Repeat==TenshoAssistant.Alarms.AlarmRepeat.Once && later.At==now.AddMinutes(5) && later.Id!=alarm.Id,"a new one-off alarm");
            Check(alarm.Repeat==TenshoAssistant.Alarms.AlarmRepeat.EorzeaDaily,"the original stays");
            Check(TenshoAssistant.Alarms.AlarmSchedule.Snooze(alarm,now,0).At==now.AddMinutes(1),"at least a minute");
            Check(Read("MainWindow.Alarms.cs").Contains("if(alarm.AutoKey.Length==0 && ImGui.SmallButton(localization.Get(\"Alarms.Edit\")"),"an automatic alarm is not edited");
        }),
        ("0.558: a reminder ticks itself only when the game reads its counter at 0",() =>
        {
            Check(TenshoAssistant.Reminders.ReminderCounts.DoneByGame(0,0).SequenceEqual(["Allied","Deliveries"]),"both at 0");
            Check(TenshoAssistant.Reminders.ReminderCounts.DoneByGame(null,null).Count==0,"unknown says nothing");
            Check(TenshoAssistant.Reminders.ReminderCounts.DoneByGame(3,1).Count==0,"still to do");
            Check(new TenshoAssistant.Configuration().RemindersFromGame,"on by default");
            Check(Read("Alarms/ReminderDtrEntry.Dalamud.cs").Contains("fromGame?.Invoke(timers);"),"also from the server bar");
        }),
        ("0.559: a material's tooltip says which storage holds it, from the rows already computed",() =>
        {
            Check(Read("MainWindow.CraftPlan.cs").Contains("DrawStorageDetail(material.Holdings);") && Read("MainWindow.CraftChain.cs").Contains("DrawStorageDetail(stored);"),"both tables");
            Check(Read("MainWindow.CraftPlan.cs").Contains("row.IsCached && row.ObservedAt is { } seen"),"a saved read shows its date");
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
