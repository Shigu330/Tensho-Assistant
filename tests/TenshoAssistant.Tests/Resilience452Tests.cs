using TenshoAssistant.Craft;

// R-14 / A-14 (0.452): the crash class of the audit: overlays fail alone, hooks never throw into the game, native lists bounded, buffs per class.
// Native code is outside the test project: pinned in the source, like the earlier R-14 tests.
internal static class Resilience452Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
        ("Alarm chat link: one handler per item, kept; a chat failure leaves the sound and Discord",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(!plugin.Contains("RegisterAlarmLink"),"0.774: no link handler any more");
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
            Check(triad.Split("!Walkable(manager, depth)").Length==3 && triad.Contains("manager->NodeListCount <= manager->NodeListSize && manager->LoadedState == AtkLoadState.Loaded"),"walks");
            Check(triad.Split("TextureType").Length>=3 && !triad.Contains("asset == null || asset->AtkTexture.Resource == null"),"union");
            var tryOn=Read("Items/TryOn.Dalamud.cs");
            Check(tryOn.Contains("if (!characterReady) return false;") && tryOn.Contains("(nint)AgentTryon.MemberFunctionPointers.TryOn == 0") && Read("Plugin.cs").Contains("Items.TryOn.Item(item,player.IsLoaded,"),"try on");
            var live=Read("Craft/CraftLiveReader.Dalamud.cs");
            Check(live.Contains("if(statusList[index] is not { } status)") && !live.Contains("foreach(var status in local.StatusList)"),"statuses");
        }),
    ];
}
