using System.Text;
using TenshoAssistant.Chat;

// 0.1648 (X-447 B16, the user's choice of 15/11, after Chattynator and ElvUI): the chat window's look copied as a code, and pasted.
internal static class ChatTheme1648Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Code(string json)=>ChatTheme.Prefix+Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat theme 1648: behaviour: a look copied then pasted gives the same look; the tabs, players, words, sounds and the rest are kept; the code names no player nor word",()=>
        {
            var mine=new ChatSettings{Opacity=55,TextOpacity=70,FontScale=120,MarginX=10,MarginY=2,Rounding=8,Border=0,LineSpacing=3,FadeAfter=30,Borderless=true,
                HideTabs=true,TabsAtBottom=false,ThinScrollbar=false,GameLinks=false,GameTabs=false,WrapIndent=false,TextOutline=false,ShowTimestamps=false,
                Clock24=false,ShowSeconds=true,HourLeadingZero=true,ShowChannel=false,SenderBrackets=true,NameColors=true,ItemIcons=true,
                Colors=new(){[ChatChannel.Party]=0x112233,[ChatChannel.Say]=0xFFFFFF},ChannelNames=new(){[ChatChannel.Party]="Grp"},
                HiddenPlayers="Ann Doe",HighlightedPlayers="Bob Roe",AlertWords="raid",Sounds=new(){[ChatChannel.Party]=3}};
            var code=ChatTheme.Share(mine);
            var json=Encoding.UTF8.GetString(Convert.FromBase64String(code[ChatTheme.Prefix.Length..]));
            Check(code.StartsWith(ChatTheme.Prefix,StringComparison.Ordinal) && code.Length<ChatTheme.MaxCode,"a code");
            Check(!json.Contains("Ann",StringComparison.Ordinal) && !json.Contains("Bob",StringComparison.Ordinal) && !json.Contains("raid",StringComparison.Ordinal) && !json.Contains("Sound",StringComparison.Ordinal),"no player, no word, no sound: "+json);
            var theirs=new ChatSettings{HiddenPlayers="Cid",AlertWords="loot",Locked=true,Tabs=[new ChatTabSettings{Name="Mine"}],Sounds=new(){[ChatChannel.Say]=2}};
            Check(ChatTheme.Apply(code,theirs),"pasted");
            Check(theirs.Opacity==55 && theirs.TextOpacity==70 && theirs.FontScale==120 && theirs.MarginX==10 && theirs.MarginY==2 && theirs.Rounding==8 && theirs.Border==0
                && theirs.LineSpacing==3 && theirs.FadeAfter==30 && theirs.Borderless && theirs.HideTabs && !theirs.TabsAtBottom && !theirs.ThinScrollbar && !theirs.GameLinks
                && !theirs.GameTabs && !theirs.WrapIndent && !theirs.TextOutline && !theirs.ShowTimestamps && !theirs.Clock24 && theirs.ShowSeconds && theirs.HourLeadingZero
                && !theirs.ShowChannel && theirs.SenderBrackets && theirs.NameColors && theirs.ItemIcons,"the same look");
            Check(theirs.ColorOf(ChatChannel.Party)==0x112233 && theirs.ColorOf(ChatChannel.Say)==0xFFFFFF && theirs.ColorOf(ChatChannel.Shout)==ChatChannels.DefaultColor(ChatChannel.Shout)
                && theirs.ShortNameOf(ChatChannel.Party)=="Grp" && theirs.ShortNameOf(ChatChannel.Say) is null,"the colours and short names");
            Check(theirs.HiddenPlayers=="Cid" && theirs.AlertWords=="loot" && theirs.Locked && theirs.Tabs is [{ Name:"Mine" }] && theirs.SoundOf(ChatChannel.Say)==2 && theirs.SoundOf(ChatChannel.Party)==0,"the rest kept");
            var plain=ChatTheme.Share(new ChatSettings());
            Check(ChatTheme.Apply(plain,theirs) && theirs.Colors is null && theirs.ChannelNames is null && theirs.Opacity==ChatSettings.DefaultOpacity,"a default look puts the colours back");
        }),
        ("Chat theme 1648: behaviour: a code's values bounded, unknown or numbered channels left out, a value missing takes its default; a text that is not a code changes nothing",()=>
        {
            var settings=new ChatSettings();
            Check(ChatTheme.Apply(Code("{\"Opacity\":500,\"FontScale\":5,\"Border\":-3,\"FadeAfter\":99999,\"Colors\":{\"Party\":4294967295,\"Nowhere\":1,\"5\":2},\"Names\":{\"Party\":\"AVeryLongName\",\"Loot\":\"L\"}}"),settings),"read");
            Check(settings.Opacity==100 && settings.FontScale==60 && settings.Border==0 && settings.FadeAfter==600 && settings.TextOpacity==new ChatSettings().TextOpacity,"bounded; missing: default");
            Check(settings.Colors is { Count:1 } colors && colors[ChatChannel.Party]==0xFFFFFF,"one known channel, no alpha");
            Check(settings.ShortNameOf(ChatChannel.Party)=="AVeryLon" && settings.ChannelNames!.Count==1,"a name cut; a channel without a short name left out");
            var before=ChatTheme.Share(settings);
            foreach(var bad in new string?[]{null,"","hello",ChatTheme.Prefix+"%%%",Code("[1,2]"),Code("{\"Opacity\":\"x\"}"),"TENSHO-ARENA1:"+Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")),
                     ChatTheme.Prefix+new string('A',ChatTheme.MaxCode)})
                Check(!ChatTheme.Apply(bad,settings),"refused: "+(bad?.Length>40 ? bad[..40]:bad));
            Check(ChatTheme.Share(settings)==before,"nothing changed");
        }),
        ("Chat theme 1648: wiring: the two buttons after the look settings, the paste saved; texts",()=>
        {
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("ImGui.SetClipboardText(ChatTheme.Share(configuration.Chat));",StringComparison.Ordinal)
                && window.Contains("if(ChatTheme.Apply(ImGui.GetClipboardText(),configuration.Chat)){chatLookMessage=localization.Get(\"ChatWindow.LookPasted\");TrySave(",StringComparison.Ordinal)
                && window.Contains("defaults.MaxLines);\n        DrawChatLookCode();".Replace("\n",Environment.NewLine.Length==2 && window.Contains("\r\n") ? "\r\n":"\n"),StringComparison.Ordinal),"the buttons");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach(var key in new[]{"CopyLook","CopyLookTip","PasteLook","PasteLookTip","LookCopied","LookPasted","LookNotACode"})Check(text.Contains($"\"{key}\":",StringComparison.Ordinal),language+" "+key);
            }
        }),
    ];
}
