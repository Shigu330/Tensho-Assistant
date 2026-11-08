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
            Check(Read("MapViewWindow.cs").Contains("owner.MapBehindGame(at, lastSize)") && Read("PanelWindow.cs").Contains("owner.BarBehindGame(at, lastSize, panel.Name)"),"map and bars");
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
            Check(follower.Contains("if(wanted.IsEmpty)return;") && follower.Contains("now-asked>Stopwatch.Frequency") && follower.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate)"),"only what a panel asks, and cleaned");
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
        ("0.560: the gathering help says what the last chain still needs",() =>
        {
            TenshoAssistant.Inventory.ItemHoldingsSummary Stock(long total)=>new([],total,0,true,false,false,true);
            var result=new TenshoAssistant.Craft.CraftChainResult(1,1,[],[new(5,14,TenshoAssistant.Craft.CraftChainReason.RawMaterial)],new Dictionary<uint,TenshoAssistant.Inventory.ItemHoldingsSummary>{[5]=Stock(10)});
            Check(result.Remaining(5,10)==14 && result.Remaining(5,13)==11,"falls as you gather");
            Check(result.Remaining(5,99)==0 && result.Remaining(5,3)==14,"bounded");
            Check(result.Remaining(6,10) is null,"not needed");
        }),
        ("0.561: an item's menu copies its name, links it in your own chat and opens its pages",() =>
        {
            var menu=Read("MainWindow.DutyFinder.cs");
            foreach(var entry in new[]{"ItemMenu.CopyName","ItemMenu.LinkChat","ItemMenu.OpenOn","ExternalLinks.Universalis(item.Id)","ExternalLinks.Garland(item.Id)","ExternalLinks.Teamcraft("})Check(menu.Contains(entry),entry);
            Check(Read("Plugin.cs").Contains("chatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder().AddItemLink(id,false).BuiltString)"),"printed locally, nothing sent");
        }),
        ("0.562: an item's English name is shown only on the setting, and only when it differs",() =>
        {
            Check(!new TenshoAssistant.Configuration().ShowEnglishItemName,"off by default");
            Check(Read("MainWindow.MarketHover.cs").Contains("configuration.ShowEnglishItemName && item is {EnglishName.Length:>0} && !string.Equals(item.EnglishName,item.Name"),"rule");
            Check(Read("MainWindow.ItemSheet.cs").Contains("DrawEnglishItemName(item);"),"sheet");
        }),
        ("0.563: the weather table of several zones is computed once per period, and kept by reset all",() =>
        {
            var view=Read("MainWindow.Weather.cs");
            Check(view.Contains("period!=weatherFavoritesPeriod || weatherFavoritesGrid.Count!=favorites.Count") && view.Contains("WeatherFavoritePeriods=6,WeatherFavoriteZones=12"),"bounded, once per period");
            Check(Read("Configuration.cs").Contains("\"WeatherFavorites\","),"the player's data");
        }),
        ("0.565: the sort preview frames what would move; an unread place says nothing",() =>
        {
            var view=Read("MainWindow.SortPreview.cs");
            Check(view.Contains("shown.Stack.DisplayIndex>=0 && shown.Stack.DisplayIndex!=position") && view.Contains("stack.DisplayIndex>=0 && stack.DisplayIndex!=target"),"count and frame agree");
        }),
        ("0.566: a sort template goes to text and back, checked, with a new id; bad text is refused",() =>
        {
            var template=new TenshoAssistant.Inventory.InventorySortTemplate(Guid.NewGuid(),"Mine",4,35,[new(TenshoAssistant.Inventory.InventorySortField.ItemLevel,true)],[new("Zone",0,5,Query:"Ore")]);
            var text=TenshoAssistant.Inventory.InventorySortTemplates.Encode(template);
            var back=TenshoAssistant.Inventory.InventorySortTemplates.Decode(text);
            Check(back is not null && back.Name=="Mine" && back.Id!=template.Id && back.Zones.Count==1 && back.Zones[0].Query=="Ore" && back.Criteria.Count==1 && back.Criteria[0].Descending,"round trip");
            foreach(var bad in new[]{null,"","hello","TENSHO-SORT1:???","TENSHO-SORT1:e30="})Check(TenshoAssistant.Inventory.InventorySortTemplates.Decode(bad) is null,"refused: "+bad);
        }),
        ("0.567: the market page copies the lowest offer minus 1, without your retainers",() =>
        {
            var board=Read("MainWindow.MarketBoard.cs");
            Check(board.Contains("!ownRetainers.Contains(offer.Retainer)") && board.Contains("()=>lowest.Paste.ToString("),"only copied, own retainers left out");
        }),
        ("0.568: a fish's window and weather now: unknown stays unknown, and a window can cross midnight",() =>
        {
            Check(TenshoAssistant.Gathering.FishNow.InWindow(60,120,90)==true && TenshoAssistant.Gathering.FishNow.InWindow(60,120,120)==false,"plain window");
            Check(TenshoAssistant.Gathering.FishNow.InWindow(1320,60,1400)==true && TenshoAssistant.Gathering.FishNow.InWindow(1320,60,30)==true && TenshoAssistant.Gathering.FishNow.InWindow(1320,60,600)==false,"across midnight");
            Check(TenshoAssistant.Gathering.FishNow.InWindow(null,120,90) is null,"no window");
            Check(TenshoAssistant.Gathering.FishNow.WeatherFits([1,2],2)==true && TenshoAssistant.Gathering.FishNow.WeatherFits([1,2],3)==false,"weather");
            Check(TenshoAssistant.Gathering.FishNow.WeatherFits([1],null) is null && TenshoAssistant.Gathering.FishNow.WeatherFits([],1) is null,"unknown weather or no condition");
        }),
        ("0.570: retainer tasks one by one, with a bell, and one for the last back",() =>
        {
            var view=Read("MainWindow.Reminders.cs");
            Check(view.Contains("AlarmButton(\"Venture\"+name") && view.Contains("AlarmButton(\"VenturesAll\""),"two bells");
        }),
        ("0.571: the Mini Cactpot page shows the gains the advice uses",() =>
        {
            Check(TenshoAssistant.MiniGames.MiniCactpotSolver.RewardOf(6)==10000 && TenshoAssistant.MiniGames.MiniCactpotSolver.RewardOf(24)==3600,"ends");
            Check(TenshoAssistant.MiniGames.MiniCactpotSolver.RewardOf(5)==0 && TenshoAssistant.MiniGames.MiniCactpotSolver.RewardOf(25)==0,"out of range");
            Check(Read("MainWindow.MiniCactpot.cs").Contains("DrawMiniCactpotRewards();"),"drawn");
        }),
        ("0.572: the simulator finds its own recipe and runs once per change",() =>
        {
            var view=Read("MainWindow.CraftSimulation.cs");
            Check(view.Contains("if(key==simulationAutoKey)return false;") && view.Contains("ImGui.Button(localization.Get(\"CraftSequence.Simulate\")) || SimulationAutoDue()"),"once per change");
            Check(view.Contains("if(simulationRecipeQuery!=simulationRecipeShown)"),"the list is rebuilt only when the text changes");
            Check(new TenshoAssistant.CraftSearchSettings().AutoSimulate,"on by default");
        }),
        ("0.573: the macro's end line has its sound and can stay on the last page; the panel's cache key follows",() =>
        {
            var export=Read("Craft/CraftMacroExport.cs");
            Check(export.Contains("if(echo && (!echoLastOnly || index==chunks.Length-1))") && export.Contains("echoSound is >=1 and <=16"),"rule");
            Check(Read("Craft/CraftLogSession.cs").Contains("input.Echo,input.EchoSound,input.EchoLastOnly,"),"in the cache key");
            var settings=new TenshoAssistant.CraftSearchSettings();
            Check(settings.BoundedEchoSound==1 && !settings.EchoLastOnly,"as before until set");
            settings.EchoSound=99;Check(settings.BoundedEchoSound==16,"bounded");
        }),
        ("0.574: the preparation's cost can use the cheapest of the data center, naming the world",() =>
        {
            var now=DateTimeOffset.UtcNow;
            var quote=new TenshoAssistant.Market.MarketQuote(1,5,now,now,new(100,null,null),new(null,null,null),new(80,2,now));
            var item=new TenshoAssistant.Items.ItemDefinition(5,"Ore",false,0){IsMarketable=true};
            var demands=new[]{new TenshoAssistant.Market.PreparationCostDemand(5,3,3,false,false)};
            var world=TenshoAssistant.Market.PreparationCost.BuildDemands(demands,1,TenshoAssistant.Market.PreparationCostBasis.Missing,TenshoAssistant.Market.PreparationPriceQuality.Nq,_=>item,_=>quote,now);
            Check(world.Lines[0].UnitPrice==100 && world.Lines[0].BuyWorldId==0,"the world's price");
            var center=TenshoAssistant.Market.PreparationCost.BuildDemands(demands,1,TenshoAssistant.Market.PreparationCostBasis.Missing,TenshoAssistant.Market.PreparationPriceQuality.Nq,_=>item,_=>quote,now,dataCenter:true);
            Check(center.Lines[0].UnitPrice==80 && center.Lines[0].BuyWorldId==2 && center.KnownSubtotal==240,"the data center's, with its world");
        }),
        ("0.575: the preparation's cost takes an NPC's gil price when lower, also for what the market does not sell",() =>
        {
            var now=DateTimeOffset.UtcNow;
            var quote=new TenshoAssistant.Market.MarketQuote(1,5,now,now,new(100,null,null),new(null,null,null));
            var item=new TenshoAssistant.Items.ItemDefinition(5,"Ore",true,0){IsMarketable=true};
            var free=new TenshoAssistant.Items.ItemDefinition(6,"Oil",false,0);
            var demands=new[]{new TenshoAssistant.Market.PreparationCostDemand(5,3,3,false,false),new TenshoAssistant.Market.PreparationCostDemand(6,2,2,false,false)};
            TenshoAssistant.Market.PreparationCostEstimate Cost(TenshoAssistant.Market.PreparationPriceQuality quality,decimal price)=>TenshoAssistant.Market.PreparationCost.BuildDemands(demands,1,TenshoAssistant.Market.PreparationCostBasis.Missing,quality,id=>id==5 ? item:free,id=>id==5 ? quote:null,now,vendor:_=>price);
            var low=Cost(TenshoAssistant.Market.PreparationPriceQuality.Nq,40);
            Check(low.Lines[0].UnitPrice==40 && low.Lines[0].FromVendor && low.Lines[1].UnitPrice==40 && low.KnownSubtotal==200,"lower, and not marketable");
            var high=Cost(TenshoAssistant.Market.PreparationPriceQuality.Nq,400);
            Check(high.Lines[0].UnitPrice==100 && !high.Lines[0].FromVendor,"the market when lower");
            Check(!Cost(TenshoAssistant.Market.PreparationPriceQuality.Hq,40).Lines[0].FromVendor,"HQ asked: not an NPC's");
        }),
        ("0.576: one read can be erased, on two clicks, and a retainer gone from the bell is only said",() =>
        {
            var bank=Read("MainWindow.Bank.cs");
            Check(bank.Contains("if(!armed)bankForgetArmed=entry.Key;") && bank.Contains("bank.ForgetAsync(entry.Key)"),"two clicks");
            Check(bank.Contains("listed.All(known=>known.Id!=entry.Key.StorageId)"),"said when the bell does not list it");
            Check(Read("Inventory/BankService.cs").Contains("state.Entries.Where(e => e.Key != forgotten)"),"only that read");
        }),
        ("0.577: alarms in real time repeat by day, week or game reset",() =>
        {
            var at=new DateTimeOffset(2026,11,5,20,0,0,TimeSpan.Zero);
            TenshoAssistant.Alarms.TenshoAlarm Alarm(TenshoAssistant.Alarms.AlarmRepeat repeat)=>new(){Repeat=repeat,At=at};
            var daily=Alarm(TenshoAssistant.Alarms.AlarmRepeat.RealDaily);
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(daily,at.AddHours(-1))==at,"before its first time");
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(daily,at)==at.AddDays(1),"strictly after");
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(daily,at.AddDays(3).AddHours(1))==at.AddDays(4),"days later");
            var weekly=Alarm(TenshoAssistant.Alarms.AlarmRepeat.RealWeekly);
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(weekly,at.AddDays(1))==at.AddDays(7),"a week on");
            var reset=TenshoAssistant.Alarms.AlarmSchedule.Next(Alarm(TenshoAssistant.Alarms.AlarmRepeat.DailyReset),at);
            Check(reset==TenshoAssistant.Reminders.ResetSchedule.NextDaily(at,TenshoAssistant.Reminders.ResetSchedule.DailyHour),"the daily reset");
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(Alarm(TenshoAssistant.Alarms.AlarmRepeat.WeeklyReset),at)>at,"the weekly reset");
        }),
        ("0.579: all jobs (glamour) is on the gear page itself, the same setting as in its filters",() =>
        {
            var gear=Read("MainWindow.Gear.cs");
            var page=gear[gear.IndexOf("private void DrawGearPlanner()")..gear.IndexOf("DrawGearWear(catalog);")];
            Check(page.Contains("BoolSetting(\"Gear.AllJobs\""),"on the page");
            Check(gear.Split("configuration.GearAllJobs=value").Length==3,"one setting, shown twice");
        }),
        ("0.580: the gear page plans another job: no worn piece and no read value is lent to it",() =>
        {
            var gear=Read("MainWindow.Gear.cs");
            Check(gear.Contains("Where(piece=>!otherJob && catalog.Find(piece.ItemId) is not null)"),"no worn piece");
            Check(gear.Contains("Values=new Dictionary<string,int>(),Category=0,Role=0,Modifiers=default"),"no read value");
            Check(gear.Contains("levels?.TryGetValue(gearJob,out var known)==true"),"a job whose level is known");
        }),
        ("0.581: the week's outfit keeps one piece per slot, and another week's choice is dropped",() =>
        {
            var week=new DateTimeOffset(2026,11,3,8,0,0,TimeSpan.Zero);
            var outfit=TenshoAssistant.Fashion.FashionOutfit.Toggle(null,week,1,10);
            Check(TenshoAssistant.Fashion.FashionOutfit.Current(outfit,week)[1]==10,"chosen");
            outfit=TenshoAssistant.Fashion.FashionOutfit.Toggle(outfit,week,1,11);
            Check(TenshoAssistant.Fashion.FashionOutfit.Current(outfit,week)[1]==11 && outfit.Slots.Count==1,"one per slot");
            outfit=TenshoAssistant.Fashion.FashionOutfit.Toggle(outfit,week,1,11);
            Check(outfit.Slots.Count==0,"unticked");
            outfit=TenshoAssistant.Fashion.FashionOutfit.Toggle(outfit,week,2,12);
            Check(TenshoAssistant.Fashion.FashionOutfit.Current(outfit,week.AddDays(7)).Count==0,"another week");
        }),
        ("0.582: a quest is available when not started and its known prerequisites are done",() =>
        {
            TenshoAssistant.Quests.QuestEntry Of(uint id,TenshoAssistant.Quests.QuestProgress progress,params uint[] previous)=>new(id,"Q"+id,"C",0,progress,null){PreviousQuestIds=previous,PreviousQuestJoin=1};
            var done=Of(1,TenshoAssistant.Quests.QuestProgress.Complete);
            var open=Of(2,TenshoAssistant.Quests.QuestProgress.Unconfirmed,1);
            var locked=Of(3,TenshoAssistant.Quests.QuestProgress.Unconfirmed,2);
            var free=Of(4,TenshoAssistant.Quests.QuestProgress.Unconfirmed);
            var links=new TenshoAssistant.Quests.QuestLinks([done,open,locked,free]);
            Check(!TenshoAssistant.Quests.QuestList.Available(links,done),"done");
            Check(TenshoAssistant.Quests.QuestList.Available(links,open) && TenshoAssistant.Quests.QuestList.Available(links,free),"open");
            Check(!TenshoAssistant.Quests.QuestList.Available(links,locked),"locked");
        }),
        ("0.583: the way to a quest lists what is left, first to take first, and survives a long chain and a cycle",() =>
        {
            TenshoAssistant.Quests.QuestEntry Of(uint id,TenshoAssistant.Quests.QuestProgress progress,byte join,params uint[] previous)=>new(id,"Q"+id,"C",0,progress,null){PreviousQuestIds=previous,PreviousQuestJoin=join};
            var open=TenshoAssistant.Quests.QuestProgress.Unconfirmed;var done=TenshoAssistant.Quests.QuestProgress.Complete;
            var links=new TenshoAssistant.Quests.QuestLinks([Of(1,done,0),Of(2,open,1,1),Of(3,open,1,2),Of(4,open,1,3)]);
            Check(TenshoAssistant.Quests.QuestList.PathTo(links,4).Todo.Select(entry=>entry.Id).SequenceEqual(new uint[]{2,3}),"in order, without itself or what is done");
            var either=new TenshoAssistant.Quests.QuestLinks([Of(1,done,0),Of(2,open,0),Of(3,open,2,1,2)]);
            Check(TenshoAssistant.Quests.QuestList.PathTo(either,3).Todo.Count==0,"any of them, one done");
            var cycle=new TenshoAssistant.Quests.QuestLinks([Of(1,open,1,2),Of(2,open,1,1)]);
            Check(TenshoAssistant.Quests.QuestList.PathTo(cycle,1).Todo.Select(entry=>entry.Id).SequenceEqual(new uint[]{2}),"a cycle ends");
            var chain=Enumerable.Range(1,5000).Select(index=>Of((uint)index,open,1,index>1 ? [(uint)index-1]:[])).ToArray();
            var cut=TenshoAssistant.Quests.QuestList.PathTo(new TenshoAssistant.Quests.QuestLinks(chain),5000);
            Check(cut.Cut,"a very long chain is cut, no stack overflow");
            var unknown=new TenshoAssistant.Quests.QuestLinks([Of(1,open,9,2),Of(2,open,0)]);
            Check(TenshoAssistant.Quests.QuestList.PathTo(unknown,1).Unknown,"unknown join");
        }),
        ("0.585: job levels leave out the sheet's empty rows; the gear page's label comes before its list",() =>
        {
            Check(Read("Plugin.cs").Contains("Where(row=>row.ExpArrayIndex>=0 && !row.Name.IsEmpty)"),"rows 44 and 45 of ClassJob have no name, and share the first level slot");
            var gear=Read("MainWindow.Gear.cs");
            Check(gear.IndexOf("ImGui.TextUnformatted(localization.Get(\"Gear.PlanFor\"))")<gear.IndexOf("ImGui.BeginCombo(\"###GearJob\""),"label first");
        }),
        ("0.588: the gathering page lists the timed points open now or soon, once per Eorzea minute",() =>
        {
            var view=Read("MainWindow.GatheringBrowser.cs");
            Check(view.Contains("if(key!=gatheringNowKey && gatheringNowTask is null)") && view.Contains("DrawGatheringNow(sources);"),"once per Eorzea minute");
            Check(Read("Gathering/GatheringTimeline.cs").Contains("source.IsFishing || source.Timing.Kind!=GatheringScheduleKind.Limited"),"timed, no fish");
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
