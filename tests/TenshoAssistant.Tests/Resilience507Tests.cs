// 0.507 (R-14): the findings of the saved audit, read one by one against the code: a sheet or a function lost after a game update costs its own value only,
// and what cannot be read says so. These reads need the game: the code is pinned here.
internal static class Resilience507Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Code(string file)=>File.ReadAllText(Path.Combine(Path.GetFullPath("src/TenshoAssistant"),file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
