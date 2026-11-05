// R-14 (0.453): second batch of the audit: an unknown value stays unknown (never zero, « none » or « no category »). Pinned in the source.
internal static class Resilience453Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
