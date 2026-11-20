using TenshoAssistant.Chat;

// 0.1623 (X-447 B15, the user's choice of 15/11, R25): capture mode, the players' names replaced by a stable stand-in.
internal static class ChatCapture1623Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,16,0,0,TimeSpan.Zero);
    private static ChatRecord Said(ChatChannel channel,string sender="Ann Doe@Moogle")=>new(1,At,10,channel,sender,[new ChatSegment("hello")]);
    private static string Line(ChatRecord record,ChatSettings settings)=>string.Concat(ChatFormat.Runs(record,settings,_=>"").Select(run=>run.Text));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat capture 1623: the stand-in is stable, its world and case aside, five letters or digits, and tells players apart",()=>
        {
            var ann=ChatFormat.StandIn("Ann Doe@Moogle");
            Check(ann==ChatFormat.StandIn("ann doe") && ann==ChatFormat.StandIn(" Ann Doe ") && System.Text.RegularExpressions.Regex.IsMatch(ann,"^#[0-9A-F]{5}$"),ann);
            Check(!ann.Contains("Ann",StringComparison.OrdinalIgnoreCase) && ann!=ChatFormat.StandIn("Bob Roe"),"nothing of the name; another player, another stand-in");
        }),
        ("Chat capture 1623: behaviour: in capture mode no player's name is written, in any spoken channel, tells and brackets included; a story character keeps its name",()=>
        {
            var capture=new ChatSettings{ShowTimestamps=false,ShowChannel=false,CaptureNames=true};
            var stand=ChatFormat.StandIn("Ann Doe");
            foreach(var channel in new[]{ChatChannel.Say,ChatChannel.Party,ChatChannel.TellIn,ChatChannel.TellOut,ChatChannel.FreeCompany})
            {
                var line=Line(Said(channel),capture);
                Check(!line.Contains("Ann",StringComparison.Ordinal) && !line.Contains("Moogle",StringComparison.Ordinal) && line.Contains(stand,StringComparison.Ordinal),channel+": "+line);
            }
            Check(Line(Said(ChatChannel.Say),new ChatSettings{ShowTimestamps=false,ShowChannel=false,CaptureNames=true,SenderBrackets=true})==$"[{stand}]: hello","brackets");
            Check(Line(Said(ChatChannel.Npc,"Cid Moe"),capture)=="Cid Moe: hello","a character of the story");
            Check(ChatFormat.Plain(Said(ChatChannel.Say),capture,_=>"").Contains(stand,StringComparison.Ordinal),"the copy too");
            Check(!new ChatSettings().CaptureNames && ChatFormat.LayoutKey(new ChatSettings())!=ChatFormat.LayoutKey(capture),"off by default, laid out again");
        }),
    ];
}
