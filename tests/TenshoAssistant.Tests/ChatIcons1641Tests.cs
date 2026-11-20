using TenshoAssistant.Chat;

// 0.1641 (X-447 B14, the user's choice of 15/11, after Prat): an item link's icon before its name in the chat window, a setting off by default.
internal static class ChatIcons1641Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,19,30,0,TimeSpan.Zero);
    private static readonly ChatLink Potion=new(ChatSegmentKind.ItemLink,ItemId:4551);
    private static readonly ChatLink Ether=new(ChatSegmentKind.ItemLink,ItemId:4555);
    private static ChatRecord Sale()=>new(1,At,10,ChatChannel.Say,"Ann Doe",[new ChatSegment("I sell "),new ChatSegment("Pot",ChatSegmentKind.ItemLink,Potion),
        new ChatSegment("ion",ChatSegmentKind.ItemLink,Potion),new ChatSegment(" and "),new ChatSegment("Ether",ChatSegmentKind.ItemLink,Ether),
        new ChatSegment(" and "),new ChatSegment("Mapped",ChatSegmentKind.MapLink,new ChatLink(ChatSegmentKind.MapLink,MapId:1))]);
    private static ChatSettings On=>new(){ShowTimestamps=false,ShowChannel=false,ItemIcons=true};
    private static uint Icons(ChatLink link)=>link.ItemId switch{4551=>20601,4555=>20602,_=>0};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat icons 1641: an item link's icon before its first piece only, for each item link; none for another link, an unknown item, the setting off, or the copy",()=>
        {
            var runs=ChatFormat.Runs(Sale(),On,_=>"",null,Icons);
            var icons=runs.Select((run,at)=>(run,at)).Where(entry=>entry.run.Role==ChatRole.ItemIcon).ToArray();
            Check(icons.Length==2 && icons[0].run.Link==Potion && icons[1].run.Link==Ether && icons.All(entry=>entry.run.Text.Length==0),"two icons: "+icons.Length);
            Check(runs[icons[0].at+1].Text=="Pot" && runs[icons[1].at+1].Text=="Ether","each before its link's first piece");
            Check(ChatFormat.Runs(Sale(),On,_=>"",null,link=>link.ItemId==4551 ? 0:Icons(link)).Count(run=>run.Role==ChatRole.ItemIcon)==1,"an item without a known icon: none");
            Check(ChatFormat.Runs(Sale(),new ChatSettings{ShowTimestamps=false,ShowChannel=false},_=>"",null,Icons).All(run=>run.Role!=ChatRole.ItemIcon),"off by default: none");
            Check(ChatFormat.Runs(Sale(),On,_=>"").All(run=>run.Role!=ChatRole.ItemIcon) && ChatFormat.Plain(Sale(),On,_=>"")=="Ann Doe: I sell Potion and Ether and Mapped","the copy: the text alone");
            var twice=new ChatRecord(2,At,10,ChatChannel.Say,"Ann Doe",[new ChatSegment("Pot",ChatSegmentKind.ItemLink,Potion),new ChatSegment(" "),new ChatSegment("Pot",ChatSegmentKind.ItemLink,Potion)]);
            Check(ChatFormat.Runs(twice,On,_=>"",null,Icons).Count(run=>run.Role==ChatRole.ItemIcon)==2,"the same item linked twice: two icons");
        }),
        ("Chat icons 1641: behaviour: the icon is one block of its width, never cut nor joined; it goes to the next line with no room left",()=>
        {
            // One unit a letter, the icon 3 wide.
            var runs=new[]{new ChatRun("ab ",ChatRole.Body),new ChatRun("",ChatRole.ItemIcon,Potion),new ChatRun("cd",ChatRole.Link,Potion)};
            var wide=ChatFormat.Wrap(runs,20,text=>text.Length,0,3);
            Check(wide.Count==1 && wide[0].Count==3 && wide[0][1].Role==ChatRole.ItemIcon,"one line: "+wide.Count);
            var narrow=ChatFormat.Wrap(runs,5,text=>text.Length,0,3);
            Check(narrow.Count==2 && narrow[0].Count==1 && narrow[1][0].Role==ChatRole.ItemIcon && narrow[1][1].Text=="cd","the icon starts the next line: "+string.Join(" | ",narrow.Select(line=>string.Join("+",line.Select(run=>run.Role+":"+run.Text)))));
            Check(ChatFormat.Wrap([new ChatRun("",ChatRole.ItemIcon,Potion)],2,text=>text.Length,0,3) is [[{ Role:ChatRole.ItemIcon }]],"wider than the line: alone, not lost");
            Check(ChatLook.IconWidth(18)==21 && ChatLook.IconWidth(12)==14,"a square and a sixth: "+ChatLook.IconWidth(18));
        }),
        ("Chat icons 1641: the setting lays the lines out again; off by default",()=>
        {
            Check(!new ChatSettings().ItemIcons,"off by default");
            Check(ChatFormat.LayoutKey(On)!=ChatFormat.LayoutKey(new ChatSettings{ShowTimestamps=false,ShowChannel=false}),"in the layout's key");
        }),
        ("Chat icons 1641: wiring: the window gives the item list's icons and draws them as game icons; the setting and its texts",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("ChatFormat.Runs(view.RecordAt(index),options,ChatChannelShort,ChatLinkName,ChatLinkIcon)",StringComparison.Ordinal),"icons given to the lines");
            Check(chat.Contains("if(run.Role==ChatRole.ItemIcon && run.Link is { } item)",StringComparison.Ordinal) && chat.Contains("DrawGameIcon(ChatLinkIcon(item),new Vector2(side,side));",StringComparison.Ordinal),"drawn as a game icon");
            Check(chat.Contains("BoolSetting(\"ChatWindow.ItemIcons\",\"ChatWindow.ItemIconsTip\"",StringComparison.Ordinal),"the setting");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"ItemIcons\":",StringComparison.Ordinal) && text.Contains("\"ItemIconsTip\":",StringComparison.Ordinal),language);
            }
        }),
    ];
}
