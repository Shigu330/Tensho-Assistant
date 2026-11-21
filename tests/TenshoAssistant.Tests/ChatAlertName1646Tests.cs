using TenshoAssistant.Chat;

// 0.1646 (X-447 B7, the user's choice of 15/11, after Prat and WIM): your first name as an alert, the alert's sound and a sound per channel.
internal static class ChatAlertName1646Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset Start=new(2026,11,20,21,0,0,TimeSpan.Zero);
    private static ChatRecord Said(ChatChannel channel,string text,string sender="Bob Roe",byte source=0)=>new(1,Start,10,channel,sender,[new ChatSegment(text)],source);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat alert name 1646: your first name, a whole word, capitals ignored, said by a player; never a loot or system line, what you send, a combat line",()=>
        {
            Check(ChatAlert.FirstName("Ann Doe")=="Ann" && ChatAlert.FirstName("  Ann  ")=="Ann" && ChatAlert.FirstName(" ") is null && ChatAlert.FirstName(null) is null && ChatAlert.FirstName("A Doe") is null,"the first name");
            Check(ChatAlert.WholeWord("hi ann!","Ann") && ChatAlert.WholeWord("Ann's turn","Ann") && ChatAlert.WholeWord("ANN","Ann") && ChatAlert.WholeWord("hi Y’shtola","Y'shtola"),"a whole word, apostrophes folded");
            Check(!ChatAlert.WholeWord("anything","") && !ChatAlert.WholeWord("","Ann"),"nothing to look for, nothing to look in");
            Check(!ChatAlert.WholeWord("Annie is here","Ann") && !ChatAlert.WholeWord("joann","Ann") && !ChatAlert.WholeWord("ann2","Ann"),"never inside a longer word");
            Check(ChatAlert.NameIn(Said(ChatChannel.Say,"Ann, come here"),"Ann Doe") && ChatAlert.NameIn(Said(ChatChannel.Party,"thanks ann"),"Ann Doe"),"said by a player");
            Check(!ChatAlert.NameIn(Said(ChatChannel.Loot,"Ann Doe obtains a potion."),"Ann Doe") && !ChatAlert.NameIn(Said(ChatChannel.System,"Ann Doe joins."),"Ann Doe"),"a loot or system line naming you");
            Check(!ChatAlert.NameIn(Said(ChatChannel.TellOut,"Ann here"),"Ann Doe") && !ChatAlert.NameIn(Said(ChatChannel.Say,"I am Ann","Ann Doe",ChatAlert.You),"Ann Doe"),"what you send");
            Check(!ChatAlert.NameIn(Said(ChatChannel.Combat,"Ann uses Cure."),"Ann Doe") && !ChatAlert.NameIn(Said(ChatChannel.Say,"Ann"),null),"a combat line; no name known");
            Check(ChatAlert.Holds(Said(ChatChannel.Say,"Ann?"),[],"Ann Doe") && ChatAlert.Holds(Said(ChatChannel.Say,"raid"),["raid"],null) && !ChatAlert.Holds(Said(ChatChannel.Say,"Ann?"),[],null),"words or name");
            Check(new ChatSettings().AlertOwnName && new ChatSettings().AlertSound==0 && new ChatSettings().Sounds is null,"defaults: name on, no sound");
        }),
        ("Chat alert name 1646: behaviour: a tab counts your name as an alert with the setting on, from the name the store holds; the marks follow the setting",()=>
        {
            var store=new ChatStore();
            var tab=new ChatTabSettings{Name="Say",Channels=[ChatChannel.Say]};
            var on=new ChatSettings();var off=new ChatSettings{AlertOwnName=false};
            var view=new ChatTabView();
            view.Refresh(store,tab,100,null,on);
            store.Add(Start,10,"Bob Roe",[new ChatSegment("Ann, are you there?")],100);
            view.Refresh(store,tab,100,null,on);
            Check(view.Alerts==0,"no name known yet: no alert");
            store.OwnName="Ann Doe";
            store.Add(Start.AddSeconds(1),10,"Bob Roe",[new ChatSegment("ann?")],100);
            view.Refresh(store,tab,100,null,on);
            Check(view.Alerts==1 && view.AlertAt(1,null),"your name alerts, the line stands out");
            view.Refresh(store,tab,100,null,off);
            Check(!view.AlertAt(1,null) && !view.AlertAt(0,null),"setting off: the marks made again without it");
            store.Add(Start.AddSeconds(2),10,"Bob Roe",[new ChatSegment("Ann!")],100);
            view.Refresh(store,tab,100,null,off);
            Check(view.Alerts==1,"setting off: no new alert");
        }),
        ("Chat alert name 1646: the sound: the alert's first, else the channel's; none for what you send, a combat line, a hidden player, a number out of 1 to 16",()=>
        {
            var settings=new ChatSettings{AlertSound=5,Sounds=new(){[ChatChannel.Party]=3,[ChatChannel.Say]=40}};
            Check(ChatSound.For(Said(ChatChannel.Party,"raid"),settings,true)==5 && ChatSound.For(Said(ChatChannel.Party,"hello"),settings,false)==3,"the alert's, else the channel's");
            Check(ChatSound.For(Said(ChatChannel.Shout,"hello"),settings,false)==0 && ChatSound.For(Said(ChatChannel.Say,"hello"),settings,false)==0,"no sound chosen; a number out of range");
            Check(ChatSound.For(Said(ChatChannel.Party,"hi",source:ChatAlert.You),settings,true)==0 && ChatSound.For(Said(ChatChannel.Combat,"Ann"),settings,true)==0,"what you send; a combat line");
            Check(ChatSound.For(Said(ChatChannel.Party,"raid"),settings,true,ChatTextFilter.Words("Bob Roe"))==0,"a player you hid");
            Check(ChatSound.For(Said(ChatChannel.Party,"raid"),new ChatSettings{Sounds=new(){[ChatChannel.Party]=3}},true)==3,"an alert without its sound: the channel's");
            var gate=new ChatSoundGate();
            Check(gate.Take(0) && !gate.Take(1999) && gate.Take(2000) && !gate.Take(2500) && gate.Take(10_000),"at most one every 2 s, the first one plays");
        }),
        ("Chat alert name 1646: wiring: the reader plays on the game's thread while logged in, through the gate; the plugin gives the game's sound; settings and texts",()=>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs"));
            Check(reader.Contains("if(PlaySound is { } play && client.IsLoggedIn)",StringComparison.Ordinal) && reader.Contains("soundGate.Take(Environment.TickCount64))play(sound);",StringComparison.Ordinal)
                && reader.Contains("if(!string.Equals(store.OwnName,name,StringComparison.Ordinal))store.OwnName=name;",StringComparison.Ordinal),"the reader");
            Check(reader.IndexOf("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;",StringComparison.Ordinal) is var cut and >0 && cut<reader.IndexOf("play(sound)",StringComparison.Ordinal),"never during a competitive cut");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Chat.cs")).Contains("chatReader.PlaySound=sound=>Alarms.AlarmService.PlayChatSound(sound,log);",StringComparison.Ordinal),"the game's sound");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("BoolSetting(\"ChatWindow.AlertOwnName\",\"ChatWindow.AlertOwnNameTip\"",StringComparison.Ordinal) && window.Contains("IntSetting(\"ChatWindow.AlertSound\",\"ChatWindow.AlertSoundTip\"",StringComparison.Ordinal)
                && window.Contains("if(channel!=ChatChannel.TellOut)DrawChatChannelSound(options,channel);",StringComparison.Ordinal),"the settings");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach(var key in new[]{"AlertOwnName","AlertOwnNameTip","AlertSound","AlertSoundTip","NoSound","SoundNumber","ChannelSoundTip"})Check(text.Contains($"\"{key}\":",StringComparison.Ordinal),language+" "+key);
            }
        }),
    ];
}
