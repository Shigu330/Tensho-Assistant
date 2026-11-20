using TenshoAssistant.Chat;

// 0.1621 (X-447 B4 and B5, the user's choices of 15/11): a short name chosen per channel, the players' names in brackets and in a colour
// taken from the name.
internal static class ChatNames1621Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,16,0,0,TimeSpan.Zero);
    private static ChatRecord Said(ChatChannel channel,string sender="Ann Doe@Moogle")=>new(1,At,10,channel,sender,[new ChatSegment("hello")]);
    private static string Line(ChatRecord record,ChatSettings settings)=>string.Concat(ChatFormat.Runs(record,settings,channel=>channel==ChatChannel.Say ? "S":"P").Select(run=>run.Text));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat names 1621: B4, a chosen short name replaces the game's, bounded, empty is the game's",()=>
        {
            var settings=new ChatSettings{ShowTimestamps=false,ChannelNames=new(){[ChatChannel.Say]="Dire"}};
            Check(Line(Said(ChatChannel.Say),settings).StartsWith("[Dire] ",StringComparison.Ordinal),"chosen: "+Line(Said(ChatChannel.Say),settings));
            Check(Line(Said(ChatChannel.Party),settings).StartsWith("[P] ",StringComparison.Ordinal),"another channel keeps the game's");
            Check(new ChatSettings{ChannelNames=new(){[ChatChannel.Say]="  "}}.ShortNameOf(ChatChannel.Say) is null,"blank: none");
            Check(new ChatSettings{ChannelNames=new(){[ChatChannel.Say]="Abcdefghijk"}}.ShortNameOf(ChatChannel.Say)=="Abcdefgh","8 letters at most");
            Check(ChatFormat.LayoutKey(new ChatSettings())!=ChatFormat.LayoutKey(settings),"a name chosen lays the lines out again");
        }),
        ("Chat names 1621: B5, names in brackets when chosen, tells included; a story character's name as it is",()=>
        {
            var settings=new ChatSettings{ShowTimestamps=false,ShowChannel=false,SenderBrackets=true};
            Check(Line(Said(ChatChannel.Say),settings)=="[Ann Doe@Moogle]: hello",Line(Said(ChatChannel.Say),settings));
            Check(Line(Said(ChatChannel.TellIn),settings).StartsWith("[Ann Doe@Moogle] >> ",StringComparison.Ordinal) && Line(Said(ChatChannel.TellOut),settings).StartsWith(">> [Ann Doe@Moogle]: ",StringComparison.Ordinal),"tells");
            Check(Line(Said(ChatChannel.Npc,"Cid Moe"),settings)=="Cid Moe: hello","a character of the story");
            Check(Line(Said(ChatChannel.Say),new ChatSettings{ShowTimestamps=false,ShowChannel=false})=="Ann Doe@Moogle: hello","off by default");
            Check(ChatFormat.LayoutKey(new ChatSettings())!=ChatFormat.LayoutKey(new ChatSettings{SenderBrackets=true}),"laid out again");
        }),
        ("Chat names 1621: B5, a name's colour is stable, its own, light, the world and case aside",()=>
        {
            var ann=ChatLook.NameColour("Ann Doe");
            Check(ann==ChatLook.NameColour("ann doe@Moogle") && ann==ChatLook.NameColour(" Ann Doe "),"the same name, the same colour");
            Check(ann!=ChatLook.NameColour("Bob Roe") || ChatLook.NameColour("Bob Roe")!=ChatLook.NameColour("Cid Moe"),"names told apart");
            foreach(var name in new[]{"Ann Doe","Bob Roe","Cid Moe","",null})
            {
                var (r,g,b)=ChatLook.NameColour(name);
                Check(new[]{r,g,b}.All(part=>part>=0.5f && part<=1f) && Math.Max(r,Math.Max(g,b))>=0.94f,"light enough: "+name);
            }
            Check(!new ChatSettings().NameColors && !new ChatSettings().SenderBrackets,"off by default");
        }),
        ("Chat names 1621: wiring: the run's colour, the short name's box, the settings",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("ImGui.TextColored(ChatRunColour(run,record,color,options),run.Text);") && chat.Contains("var (r,g,b)=ChatLook.NameColour(record.Sender);"),"the colour");
            Check(chat.Contains("ImGui.InputTextWithHint(\"###ChatChannelName\",ChatChannelShort(channel),ref shortName,ChatSettings.MaximumChannelName)"),"the short name's box");
            Check(chat.Contains("BoolSetting(\"ChatWindow.SenderBrackets\",") && chat.Contains("BoolSetting(\"ChatWindow.NameColors\","),"settings");
        }),
    ];
}
