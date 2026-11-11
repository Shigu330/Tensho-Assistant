using TenshoAssistant.Maps;

// 0.1098 (user report, 11/11: after each teleport the Tensho map went back to an old target that no longer existed; dalamud.log from 15:40 on:
// every opening said « type FlagMarker, map 700, selected 700 », after teleports to other zones too): a request the game kept follows the player.
internal static class StaleMapRequest1098Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Stale map request 1098: an old flag request or a reopening after a teleport follows the player",() =>
        {
            Check(StaleMapRequests.FollowPlayer("FlagMarker",700,0,false,false),"flag removed: the request asks for nothing");
            Check(StaleMapRequests.FollowPlayer("FlagMarker",700,412,false,false),"flag moved to another map");
            Check(StaleMapRequests.FollowPlayer("FlagMarker",700,700,true,false),"the same flag request kept by the game (M again)");
            Check(!StaleMapRequests.FollowPlayer("FlagMarker",700,700,false,false),"a new flag request opens on it");
            Check(StaleMapRequests.FollowPlayer("QuestLog",700,0,false,true),"reopened by Tensho after a teleport to another zone: whatever the game kept");
            Check(!StaleMapRequests.FollowPlayer("QuestLog",700,0,true,false) && !StaleMapRequests.FollowPlayer("SharedFate",433,0,true,false),"other kinds keep their own rules (0.627)");
            var kept=new GameMapRequest(700,700,961,11,QuestLog:true,Target:new System.Numerics.Vector2(-375,-161));
            Check(kept.Plain() is { Selected:11, Requested:11, Pointed:false },"followed: the player's map");
        }),
        ("Stale map request 1098: wiring in the map replacer",() =>
        {
            var replacer=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs"));
            Check(replacer.Contains("StaleMapRequests.FollowPlayer(staleType,staleRequested,FlagMapNow(),signature==before,moved)") && replacer.Contains("request=request.Plain();"),"applied when the game map appears");
            Check(replacer.Contains("again->Show();reopenedAfterEvent=true;") && replacer.Contains("closedByEventIn=ClosedByEvent() ? client.TerritoryType:0u;"),"the teleport's zones are known");
            Check(replacer.Contains("agent->FlagMarkerCount==0 ? 0:agent->FlagMapMarkers[0].MapId"),"the flag now");
        }),
    ];
}
