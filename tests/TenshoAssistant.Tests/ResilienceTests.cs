// Test audit of 2026-11-09, batch 2 (0.1021): Resilience452Tests, Resilience453Tests, Resilience454Tests, Resilience507Tests merged into ResilienceTests; every case is kept.
using TenshoAssistant.Craft;
using TenshoAssistant.MiniGames;
internal static class ResilienceTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static string Code(string file)=>File.ReadAllText(Path.Combine(Path.GetFullPath("src/TenshoAssistant"),file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Resilience452Tests: R-14 / A-14 (0.452): the crash class of the audit: overlays fail alone, hooks never throw into the game, native lists bounded, buffs per class.
        // Native code is outside the test project: pinned in the source, like the earlier R-14 tests.
        ("Plugin draw: each overlay fails alone, logged once; sheets read at load turn their own feature off",() =>
        {
            var plugin=Read("Plugin.cs");
            var draw=plugin[plugin.IndexOf("private void Draw()",StringComparison.Ordinal)..];
            Check(draw.Contains("try{draw();}") && draw.Contains("brokenOverlays.Add(name);log.Error(error,") && draw.Contains("if(brokenOverlays.Contains(name))continue;"),"isolated");
            foreach(var overlay in new[]{"DrawCraftLogHelper","DrawItemHover","DrawGatheringPanel","DrawFishingPanel","ObserveGarden","WatchPlots","ObserveHuntTrain"})
                Check(draw.Contains(",mainWindow."+overlay+")"),"in the list: "+overlay);
            Check(!draw.Contains("mainWindow.DrawFishingPanel();"),"no direct call left");
            foreach(var sheet in new[]{"ClassJob","ContentRoulette","ContentsNote"})
                Check(plugin.Contains("LoadSheet(\""+sheet+"\",()=>data.GetExcelSheet<Lumina.Excel.Sheets."+sheet+">()"),"guarded at load: "+sheet);
        }),
        ("Alarm chat line: a chat failure leaves the sound and Discord (the links: AlarmLinks414Tests)",() =>
        {
            Check(Read("Alarms/AlarmService.Dalamud.cs").Contains("catch (Exception error) { log.Warning(error, \"Alarm chat line could not be written.\"); }"),"chat guarded");
        }),
        ("Hooks (A-14): the game's function always runs, nothing is thrown back, the game's 1-byte bool is kept",() =>
        {
            var recorder=Read("Craft/CraftRecorder.Dalamud.cs");
            Check(recorder.Contains("Hook<ActionManager.Delegates.UseAction>") && !recorder.Contains("private delegate bool"),"ClientStructs' delegate");
            Check(recorder.Contains("var result=hook!.OriginalDisposeSafe(") && !recorder.Contains("hook=null;"),"safe after dispose");
            var macros=Read("Macros/MacroChainBridge.Dalamud.cs");
            Check(macros.Contains("executeHook!.OriginalDisposeSafe(shell, macro);") && !macros.Contains("executeHook!.Original(shell, macro);"),"macro: safe original");
            var detour=macros[macros.IndexOf("private void OnExecute",StringComparison.Ordinal)..macros.IndexOf("private ulong ReadOwner",StringComparison.Ordinal)];
            Check(detour.IndexOf("catch (Exception error)",StringComparison.Ordinal)<detour.IndexOf("executeHook!.OriginalDisposeSafe",StringComparison.Ordinal) && detour.Contains("!shell->MacroLocked"),"observation guarded before the original; a refused macro does not stop the chain");
            Check(macros.Contains("if ((nint)RaptureShellModule.MemberFunctionPointers.ExecuteMacro == 0)"),"resolved first");
        }),
        ("Recorder: an action is noted when it runs, not when it is queued, and once",() =>
        {
            Check(CraftRecording.Runs(true,false,false),"runs now");
            Check(!CraftRecording.Runs(true,false,true),"only queued");
            Check(CraftRecording.Runs(true,true,true) && CraftRecording.Runs(true,true,false),"replayed from the queue");
            Check(!CraftRecording.Runs(false,false,false),"not a crafting type");
        }),
        ("Crafting buffs: each job gets its own Action row (docs/evidence/0.452-craft-buff-rows.md); a missing row fails the catalog",() =>
        {
            var loader=Read("Craft/CraftActionCatalogLoader.Dalamud.cs");
            Check(loader.Contains("copied.Add(new(spec,job,own.RowId,own.Name.ExtractText(),own.Icon,") && !loader.Contains("copied.Add(new(spec,job,row.RowId,row.Name.ExtractText(),row.Icon,description)"),"own row");
            Check(loader.Contains("has no row for job"),"strict");
            Check(File.ReadAllText(Path.GetFullPath("docs/evidence/0.452-craft-buff-rows.md")).Contains("19297"),"evidence");
        }),
        ("Native lists: the BGM vector and the Triad node walks are bounded; texture union checked; Try On needs a loaded character; statuses read by slot",() =>
        {
            var bgm=Read("Collections/BgmReader.Dalamud.cs");
            Check(bgm.Contains("count is > 0 and <= 32 && count==system->NumScenes") && !bgm.Contains("foreach(ref var scene"),"BGM");
            var triad=Read("MiniGames/TriadObservationReader.Dalamud.cs");
            Check(triad.Split("!Walkable(manager, depth)").Length==3 && triad.Contains("=> depth <= 4 && UiNodes.Walkable(manager);")
                && Read("UI/UiNodes.Dalamud.cs").Contains("manager->NodeListCount<=manager->NodeListSize && manager->LoadedState==AtkLoadState.Loaded"),"walks");
            Check(triad.Split("TextureType").Length>=3 && !triad.Contains("asset == null || asset->AtkTexture.Resource == null"),"union");
            var tryOn=Read("Items/TryOn.Dalamud.cs");
            Check(tryOn.Contains("if (!characterReady) return false;") && tryOn.Contains("(nint)AgentTryon.MemberFunctionPointers.TryOn == 0") && Read("Plugin.cs").Contains("Items.TryOn.Item(item,player.IsLoaded,"),"try on");
            var live=Read("Craft/CraftLiveReader.Dalamud.cs");
            Check(live.Contains("if(statusList[index] is not { } status)") && !live.Contains("foreach(var status in local.StatusList)"),"statuses");
        }),
        // From Resilience453Tests: R-14 (0.453): second batch of the audit: an unknown value stays unknown (never zero, « none » or « no category »). Pinned in the source.
        ("Gil: read once the Currency container is loaded, each function checked",() =>
        {
            var gil=Read("Inventory/GilWatcher.Dalamud.cs");
            Check(gil.Contains("inventory->GetInventoryContainer(InventoryType.Currency) is var wallet && wallet!=null && wallet->IsLoaded)newGil=inventory->GetGil();"),"loaded first");
            Check(gil.Contains("MemberFunctionPointers.GetInventoryContainer!=0") && gil.Contains("RetainerManager.StaticAddressPointers.pInstance!=0"),"resolved first");
        }),
        ("Undercut: own retainers unknown gives no price, a failed read forgets them",() =>
        {
            var watcher=Read("Market/UndercutWatcher.Dalamud.cs");
            Check(watcher.Contains("private IReadOnlySet<ulong>? retainers;") && watcher.Contains("if(own is null)return;") && watcher.Contains("catch(Exception error){lock(gate)retainers=null;"),"unknown");
        }),
        ("Fashion Report: a missing slot value fails the read instead of reading « no category », logged once",() =>
        {
            var reader=Read("Fashion/FashionCheckReader.Dalamud.cs");
            Check(reader.Contains("index >= unit->AtkValuesCount) throw new InvalidOperationException(") && !reader.Contains("categories[slot] = \"\"; continue;"),"throws");
            Check(reader.Contains("if (!warned) { warned = true; log.Warning(error, \"Fashion Report window unavailable.\"); }"),"once");
        }),
        ("Squadron: a failed read is said as such, not « go to the barracks »; texts in both languages",() =>
        {
            Check(Read("Reminders/SquadronReader.Dalamud.cs").Contains("(nint)GcArmyManager.StaticAddressPointers.ppInstance==0)throw"),"address");
            Check(Read("Plugin.cs").Contains("log.Warning(error,\"Squadron unreadable (game update?).\");throw;"),"propagated");
            Check(Read("MainWindow.Squadron.cs").Contains("if(squadronReadFailed)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get(\"Squadron.Unavailable\"));"),"shown");
            Check(Read("Localization/Languages/en.json").Contains("\"Unavailable\": \"The squadron could not be read") && Read("Localization/Languages/fr.json").Contains("\"Unavailable\": \"L'escouade n'a pas pu"),"texts");
        }),
        ("Teleport list: unreadable fails (TeleportCostsFailed) instead of « none attuned »",() =>
        {
            var map=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(map.Contains("if(list==null || list->LongCount is <0 or >512)throw new InvalidOperationException(") && map.Contains("if(telepo==null)throw new InvalidOperationException("),"throws");
        }),
        ("Unlock readers wait for the loaded character: IUnlockState answers false before it",() =>
        {
            foreach(var file in new[]{"Leves/LeveProgressReader.Dalamud.cs","Collections/ExtraCollectionReader.Dalamud.cs","Gear/ChocoboReader.Dalamud.cs"})
                Check(Read(file).Contains("if(!client.IsLoggedIn || !player.IsLoaded)"),file);
        }),
        ("Reminders: no counter before the character is loaded; each one guarded on its own",() =>
        {
            var timers=Read("Reminders/GameTimers.Dalamud.cs");
            Check(timers.Contains("characterLoaded=loadedState!=null && loadedState->IsLoaded;") && timers.Contains("if(characterLoaded && roulettes is { Count:>0 })"),"gate");
            foreach(var guard in new[]{"catch(Exception){allied=null;leves=null;nextLeves=null;}","catch(Exception){deliveries=null;}","catch(Exception){ventures=null;}"})
                Check(timers.Contains(guard),guard);
        }),
        ("Currencies: CurrencyManager unknown gives an unknown balance, not a count in the bags",() =>
        {
            var watcher=Read("Inventory/CurrencyWatcher.Dalamud.cs");
            Check(watcher.Contains("if(!loaded || managed is null){balances.Add(new(definition,null,cap));continue;}") && watcher.Contains("? (currency->HasItem(definition.ItemId) ? 1:0):null);"),"unknown");
        }),
        ("Macro writer: slot snapshot guarded, hotbar checked, pages already replaced are saved",() =>
        {
            var writer=Read("Macros/MacroWriter.Dalamud.cs");
            Check(writer.Contains("try { Snapshot(); }") && writer.Contains("if (hotbar != null) hotbar->ReloadMacroSlots(") && writer.Contains("finally { if (wrote) module->SetSavePendingFlag(true, book); }"),"writer");
        }),
        // From Resilience454Tests: R-14 (0.454): last confirmed findings of the audit: « unavailable » instead of « reading » for ever, one lost sheet stays local, functions checked.
        ("Title by job: only a title this character owns is sent",() =>
        {
            var changer=Read("Collections/TitleChanger.Dalamud.cs");
            Check(changer.Contains("if(send!=0 && (!ui->TitleList.UnlockedTitlesBitArray.TryGet(send,out var owned) || !owned))"),"checked");
            Check(changer.IndexOf("UnlockedTitlesBitArray",StringComparison.Ordinal)<changer.IndexOf("SendTitleIdUpdate(send)",StringComparison.Ordinal),"before sending");
        }),
        ("Collections: a lost linked sheet leaves one card or location, not every category",() =>
        {
            var reader=Read("Collections/CollectionReader.Dalamud.cs");
            Check(reader.Contains("try { linked = details.TripleTriadCardRarity.IsValid && details.TripleTriadCardType.IsValid; }"),"cards");
            Check(reader.Contains("catch (Exception) { location = \"\"; blueIssue ="),"locations");
        }),
        ("Collectables and notebooks: a failure says « unavailable », a bag not loaded publishes nothing incomplete",() =>
        {
            var reader=Read("Gathering/CollectableReader.Dalamud.cs");
            Check(reader.Contains("catch(Exception error){CatalogFailed=true;") && reader.Contains("HoldingsUnavailable=true;") && reader.Contains("if(container==null || !container->IsLoaded)return;"),"collectables");
            Check(Read("MainWindow.CollectableTable.cs").Contains("reader.CatalogFailed ? \"CollectablePanel.Unavailable\"") && Read("MainWindow.CollectablePanel.cs").Contains("Collectables.CatalogFailed ? \"CollectablePanel.Unavailable\""),"pages");
            Check(new TenshoAssistant.Gathering.NotebookState([],DateTimeOffset.UnixEpoch).GatheringUnavailable==false,"available by default");
            var notebook=Read("Gathering/NotebookReader.Dalamud.cs");
            Check(notebook.Contains("new NotebookState(entries,now,!gatheringReadable)") && notebook.Contains("Volatile.Write(ref unavailable,true);"),"notebook");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Read($"Localization/Languages/{language}.json");
                foreach(var key in new[]{"\"HeldUnavailable\"","\"GatheringUnavailable\""})Check(text.Contains(key),language+" "+key);
            }
        }),
        ("Community names: a lost sheet is remembered, each table built alone",() =>
        {
            var names=Read("Items/GameNames.Lumina.cs");
            Check(names.Contains("catch(Exception error){Warn(error);garland[(type,id)]=null;return null;}") && names.Contains("var monsters=Safe(()=>") && names.Contains("var duties=Safe(()=>"),"names");
        }),
        ("Functions checked: retainer sorter (map node move: Map328Tests), treasure map said once; Triad data plausible; no French in the code",() =>
        {
            Check(Read("Inventory/InventoryNativeReader.Dalamud.cs").Contains("(nint)ItemOrderModule.MemberFunctionPointers.GetActiveRetainerSorter != 0") && Read("Inventory/InventoryVisualSortBridge.Dalamud.cs").Contains("(nint)ItemOrderModule.MemberFunctionPointers.GetActiveRetainerSorter!=0"),"sorter");
            Check(Read("Maps/TreasureMapReader.Dalamud.cs").Contains("The treasure map could not be read: game function not resolved"),"treasure");
            Check(Read("MiniGames/TriadNpcLoader.Dalamud.cs").Contains("if (cardOfItem.Count == 0 || npcOfMatch.Count == 0) throw new InvalidOperationException("),"triad");
            Check(!Read("Gathering/GatheringObservationReader.Dalamud.cs").Contains("GetAsAtkTextNode") && Read("Craft/CraftLiveObservation.cs").Contains("\"Native text too long.\""),"cast, English");
        }),
        // From Resilience507Tests: 0.507 (R-14): the findings of the saved audit, read one by one against the code: a sheet or a function lost after a game update costs its own value only,
        // and what cannot be read says so. These reads need the game: the code is pinned here.
        ("R-14: an optional sheet costs only its own value",() =>
        {
            Check(Code("Character/CharacterStatsReader.Dalamud.cs").Contains("catch(Exception error){paramGrowFailed=true;"),"stats keep going without ParamGrow");
            Check(Code("Inventory/BankInventoryBridge.Dalamud.cs").Contains("homeWorldFailed = true"),"storages keep going without the World sheet");
            Check(Code("Maps/MapLiveReader.Dalamud.cs").Contains("\"Hunt mark data unavailable: marks are not shown on the map.\""),"one guard per sheet on the map");
            var gear=Code("Gear/GearLoader.Dalamud.cs");
            Check(gear.Contains("Optional(\"overmeld rates\"") && gear.Contains("Optional(\"crafter overmeld rates\"") && gear.Contains("Optional(\"bardings\""),"gear keeps going without its optional sheets");
            Check(Code("Items/ItemHoverReader.Dalamud.cs").Contains("stackFailed=true;"),"the hover keeps price and uses without the stack");
            var overview=Code("Maps/MapMarkerSource.Dalamud.cs");
            Check(overview.Contains("OverviewsFailed=true") && overview.Contains("overviews.Clear();"),"overview maps: all or nothing");
        }),
        ("R-14: a dangling link skips its row, not the catalogue",() =>
        {
            Check(Code("Craft/RecipeCatalogLoader.Dalamud.cs").Contains("levelSheet.TryGetRow(row.RecipeLevelTable.RowId,out var table)"),"recipes");
            var duties=Code("Duties/DutyCatalogLoader.Dalamud.cs");
            Check(duties.Contains("row.ContentFinderCondition.ValueNullable is not { } finder") && duties.Contains("finder.ContentType.ValueNullable"),"duties");
            Check(Code("Npcs/NpcVendorLoader.Dalamud.cs").Contains("map.PlaceName.ValueNullable?.Name.ExtractText()"),"vendors");
        }),
        ("R-14: a lost function is refused or logged, an old reading is not kept as current",() =>
        {
            Check(Code("Collections/AchievementProgressWatcher.Dalamud.cs").Contains("MemberFunctionPointers.RequestAchievementProgress==0)return false;"),"achievement request refused");
            Check(Code("Collections/AchievementProgressWatcher.Dalamud.cs").Contains("failedUntil=now.AddSeconds(30);"),"no retry on every frame");
            Check(Code("Craft/CraftLogReader.Dalamud.cs").Contains("OpenRecipeByRecipeIdInternal==0"),"crafting log opening");
            Check(Code("Craft/CraftLiveReader.Dalamud.cs").Contains("MemberFunctionPointers.GetActionStatus!=0"),"action status");
            var recipes=Code("Craft/RecipeLogReader.Dalamud.cs");
            Check(recipes.Contains("function not resolved") && recipes.Split("Volatile.Write(ref state,null);").Length>=3,"crafted recipes");
            Check(Code("Leves/LeveProgressReader.Dalamud.cs").Contains("pending=null;building=[];Unavailable=true;nextRead=now.AddSeconds(30);Volatile.Write(ref state,null);"),"leves");
            var hunting=Code("Hunting/HuntingLogReader.Dalamud.cs");
            Check(hunting.Contains("!loaded->IsLoaded") && hunting.Contains("nextRead=now.AddSeconds(30);\n            Volatile.Write(ref state,null);".Replace("\n",Environment.NewLine)) || hunting.Contains("!loaded->IsLoaded") && hunting.Contains("nextRead=now.AddSeconds(30);\n            Volatile.Write(ref state,null);"),"hunting log");
            Check(Code("Inventory/BankInventoryBridge.Dalamud.cs").Contains("Cabinet.MemberFunctionPointers.IsItemInCabinet != 0"),"armoire");
            Check(Code("Maps/MapLiveReader.Dalamud.cs").Contains("teleportFailed=true"),"a failed teleport no longer blanks the map");
        }),
        ("R-14: caches follow Dalamud too, and window texts stay on one line",() =>
        {
            Check(Code("Caching/GameCacheIdentity.Dalamud.cs").Contains("typeof(IDataManager).Assembly.GetName().Version"),"a Dalamud with other sheet definitions does not serve old values");
            Check(Code("MiniGames/TriadObservationReader.Dalamud.cs").Contains("text.ReplaceLineEndings(\" \")"),"a capture is no longer cut at a newline");
        }),
        ("Catalog loaders: an empty copy is a failure, a failed gear read says so (R-14)",()=>
        {
            var root=Path.GetFullPath("src/TenshoAssistant");
            string Code(string file)=>File.ReadAllText(Path.Combine(root,file));
            Check(Code("Craft/RecipeCatalogLoader.Dalamud.cs").Contains("if (rows.Count == 0) throw new InvalidDataException"),"recipes");
            Check(Code("Duties/DutyCatalogLoader.Dalamud.cs").Contains("if (copied.Count == 0) throw new InvalidDataException"),"duties");
            Check(Code("Npcs/NpcVendorLoader.Dalamud.cs").Contains("if(vendors.Length==0 || rows.Length==0)throw new InvalidDataException"),"vendors");
            Check(Code("Gear/GearLoader.Dalamud.cs").Contains("catalogFailed=true;") && Code("MainWindow.Gear.cs").Contains("Gear.CatalogFailed") && Code("MainWindow.ChocoboBarding.cs").Contains("GearNotReady"),"gear catalog");
            Check(Code("Gear/GameGearsetReader.Dalamud.cs").Contains("unavailable=true;") && Code("MainWindow.Gearsets.cs").Contains("source.Unavailable"),"gear sets");
        }),
        ("Map and dresser readers: what is not read stays unknown (R-14)",()=>
        {
            var root=Path.GetFullPath("src/TenshoAssistant");
            var map=File.ReadAllText(Path.Combine(root,"Maps/MapLiveReader.Dalamud.cs"));
            Check(map.Contains("if(player==null)return (null,null);") && map.Contains("adventureRows.Length>bits.Length*8)return (unlocked,null);") && map.Contains("\"Sightseeing rows unavailable.\""),"unknown collected sets");
            var bank=File.ReadAllText(Path.Combine(root,"Inventory/BankInventoryBridge.Dalamud.cs"));
            Check(bank.Contains("mirage->IsSetSlotUnlocked(index, column)") && bank.Contains("held.Any(value => value) ? held : null") && bank.Contains("Dresser outfit {0}: slots held {1}."),"outfit pieces by slot, nothing held = as before, logged");
            var plugin=File.ReadAllText(Path.Combine(root,"Plugin.cs"));
            Check(plugin.Contains("outfit.Ring.RowId }") && !plugin.Contains("outfit.Ring.RowId }.Where"),"columns kept in order");
        }),
    ];
}
