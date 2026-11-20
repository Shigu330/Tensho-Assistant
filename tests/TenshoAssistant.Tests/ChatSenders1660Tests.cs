using TenshoAssistant.Chat;

// 0.1660 (X-679, the user's report of 20/11: right-click on a player's name in the Tensho chat window, none of the game's entries, even for a
// new message; X-681, the user's request of 20/11: « Tell, can't it be done with the name? »). Behaviour tests (R28.14, other players' data):
// the identifiers are the character's, so another message of the same player (same name and home world) finds them; never another player's;
// a message with no source of its own still gives its player, by the name its text gives; the entries are greyed with the reason, never left
// out; the tell needs only the name and home world. docs/evidence/0.1660-tell-by-name.md.
internal static class ChatSenders1660Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static ChatLogLine Line(string name,ushort world,ushort kind=10)=>new(kind,name,world);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat senders 1660: a message without its own identifiers finds those given to the same player, never another player's",()=>
        {
            var sources=new ChatSources();
            sources.Expect(5,"Aya Lune",73,30);
            Check(sources.Resolve(1001,2001,73,30,Line("Aya Lune",73,30),9)==ChatSourceMatch.Given,"given to the yell");
            // A message from the history on disk (no source), a repeat folded into its first copy, a number changed: by name and world.
            Check(sources.Find(2,"Aya Lune",73) is ({ ContentId:1001,AccountId:2001 },ChatSourceFound.Player),"same player: found");
            Check(sources.Find(2,"Aya Lune",74) is ({ ContentId:0,Name:"Aya Lune",World:74 },ChatSourceFound.Named),"same name, another world: another player, no identifiers");
            Check(sources.Find(2,"Bo Ciel",73) is ({ ContentId:0 },ChatSourceFound.Named),"another name: no identifiers");
            sources.Expect(6,"Aya Lune",73,10);
            sources.Skip();
            Check(sources.Find(6,"Aya Lune",73) is ({ ContentId:1001 },ChatSourceFound.Player),"own source without identifiers: the player's");
            Check(sources.Find(5,"",0) is ({ ContentId:1001 },ChatSourceFound.Message),"own identifiers first");
            sources.Expect(7,"Cy Vent",73,10);
            sources.Skip();
            Check(sources.Find(7,"",0) is ({ ContentId:0,Name:"Cy Vent" },ChatSourceFound.MessageOnly),"own source, no identifiers known");
            Check(sources.Find(8,"",0) is (null,ChatSourceFound.None) && sources.Find(8,"Dee",0) is (null,ChatSourceFound.None),"no name or no world: nothing");
        }),
        ("Chat senders 1660: a new message of each spoken channel gets its identifiers (say, shout, yell, tell, party, alliance, company, linkshells, novice)",()=>
        {
            // Dalamud's XivChatType numbers of the channels (ChatChannels.From reads them): 10 say, 11 shout, 30 yell, 13 tell, 14 party,
            // 15 alliance, 24 company, 16 and 23 linkshells 1 and 8, 37 cross-world linkshell 1, 27 novice network.
            var sources=new ChatSources();
            var id=0L;
            foreach(ushort kind in new ushort[]{10,11,30,13,14,15,24,16,23,37,27})
            {
                Check(ChatChannels.Spoken(ChatChannels.From(kind)),"spoken: kind "+kind);
                id++;
                sources.Expect(id,"Aya Lune",73,kind);
                Check(sources.Resolve(1000+(ulong)kind,2001,73,(ushort)(kind|0x500),Line("Aya Lune",73,kind),9)==ChatSourceMatch.Given
                      && sources.Find(id,"",0) is ({ ContentId:var given },ChatSourceFound.Message) && given==1000+(ulong)kind,"given: kind "+kind);
            }
        }),
        ("Chat senders 1660: the players' identifiers are bounded, cleared with the messages and never kept for your own character",()=>
        {
            var sources=new ChatSources();
            for(var id=0;id<ChatSources.MaximumKept+5;id++)
            {
                sources.Expect(id,"P"+id,1,10);
                sources.Resolve((ulong)(1000+id),1,1,10,Line("P"+id,1),9);
            }
            Check(sources.Find(-1,"P0",1).How==ChatSourceFound.Named && sources.Find(-1,"P"+(ChatSources.MaximumKept+4),1).How==ChatSourceFound.Player,"the oldest player dropped");
            sources.Expect(1,"Me Myself",1,10);
            Check(sources.Resolve(9,9,1,10,Line("Me Myself",1),9)==ChatSourceMatch.Own && sources.Find(-1,"Me Myself",1).How==ChatSourceFound.Named,"your own: never kept");
            sources.Clear();
            Check(sources.Find(-1,"P"+(ChatSources.MaximumKept+4),1).How==ChatSourceFound.Named,"cleared (logout, window off, unload)");
        }),
        ("Chat senders 1660: renumbered lines keep their players' identifiers found by name too",()=>
        {
            var sources=new ChatSources();
            sources.Expect(3,"Aya Lune",73,11);
            sources.Resolve(1001,2001,73,11,Line("Aya Lune",73,11),9);
            sources.Renumber(new Dictionary<long,long>{[3]=40});
            Check(sources.Find(40,"",0) is ({ ContentId:1001 },ChatSourceFound.Message) && sources.Find(3,"Aya Lune",73).How==ChatSourceFound.Player,"followed, and by name");
        }),
        ("Chat senders 1660: no sender, every entry greyed with why; known by name, the tell and its reply work, the rest says why",()=>
        {
            var none=ChatPlayerMenu.Entries(null,ChatChannel.Yell,false,false);
            Check(none.Count>=4 && none.All(e=>!e.Enabled && e.Why==ChatPlayerWhy.NoSender) && none.Any(e=>e.Action==ChatPlayerAction.Tell),"greyed, never empty");
            var named=ChatPlayerMenu.Entries(new ChatSource("Aya Lune",73),ChatChannel.Yell,false,false);
            Check(named.Single(e=>e.Action==ChatPlayerAction.Tell) is { Enabled:true,Why:ChatPlayerWhy.None },"tell by name");
            Check(named.Single(e=>e.Action==ChatPlayerAction.Plate) is { Enabled:false,Why:ChatPlayerWhy.NoIdentifiers }
                  && named.Single(e=>e.Action==ChatPlayerAction.Mute) is { Enabled:false,Why:ChatPlayerWhy.NoIdentifiers }
                  && named.Single(e=>e.Action==ChatPlayerAction.Target) is { Enabled:false,Why:ChatPlayerWhy.NotInRange },"plate, mute and target say why");
            var noAccount=ChatPlayerMenu.Entries(new ChatSource("Aya Lune",73,1001,0),ChatChannel.Say,true,true);
            Check(noAccount.Single(e=>e.Action==ChatPlayerAction.Mute) is { Enabled:false,Why:ChatPlayerWhy.NoAccount } && noAccount.Where(e=>e.Action!=ChatPlayerAction.Mute).All(e=>e.Enabled && e.Why==ChatPlayerWhy.None),"no account: the mute list says so");
            Check(ChatPlayerMenu.Entries(new ChatSource("Aya Lune",73),ChatChannel.TellIn,false,false).Single(e=>e.Action==ChatPlayerAction.Reply).Enabled,"reply to a tell by name");
        }),
        ("Chat senders 1660: the player a message names, from its sender's text (glyphs off, « Name@World »)",()=>
        {
            Check(ChatPlayerMenu.Named("Aya Lune@Moogle")==("Aya Lune","Moogle"),"another world");
            Check(ChatPlayerMenu.Named("Aya Lune")==("Aya Lune",""),"your world: none written");
            Check(ChatPlayerMenu.Named("Aya Lune")==("Aya Lune",""),"a party number glyph taken off");
            Check(ChatPlayerMenu.Named("")==("","") && ChatPlayerMenu.Named(null)==("",""),"nothing");
        }),
        ("Chat senders 1660: the menu finds the sender by message, player or name, says why in tooltips, never on your own lines nor in a cut",()=>
        {
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var at=window.IndexOf("private void DrawChatPlayerGameEntries(ChatRecord said)",StringComparison.Ordinal);
            var body=window[at..window.IndexOf("private static string ChatPlayerWhyKey",at,StringComparison.Ordinal)];
            Check(body.Contains("senders.Find(said.Id,") && !body.Contains("ChatSenders?.For(said.Id) is not { } source)return;"),"found by message, player or name");
            Check(body.Contains("if(said.Channel!=ChatChannel.TellOut && Chat.ChatAlert.Sent(said))return;"),"not your own lines");
            Check(body.Contains("if(ChatPlayerWhyKey(entry.Why) is { Length:>0 } why)Tip(why);") && body.Contains("&& entry.Enabled && source is not null"),"why in a tooltip; a greyed entry does nothing");
            foreach(var key in new[]{"PlayerWhyNoSender","PlayerWhyNoIdentifiers","PlayerWhyNoAccount","PlayerWhyNotInRange"})
                Check(window.Contains("\"ChatWindow."+key+"\"") && Read("src/TenshoAssistant/Localization/Languages/en.json").Contains("\""+key+"\":")
                      && Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("\""+key+"\":"),key);
            var actions=Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(actions.Contains("if(failed || TenshoAssistant.Competitive.CompetitiveGate.Cut)return;")
                  && actions.Contains("if(!client.IsLoggedIn || objects.LocalPlayer is null || TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),"never during a cut (R03.2)");
            Check(actions.Contains("if(source.Name.Length==0 || source.World==0)return;") && !actions.Contains("if(source.ContentId==0)return;\n                var world="),"tell by name, nothing sent");
            Check(actions.Contains("SetContextTellTarget(name,worldName,source.World,source.AccountId,source.ContentId,0,true)") && !actions.Contains("ProcessChatBoxEntry"),"the typing box only");
        }),
        ("Chat senders 1660: the hook is set when Tensho loads, not later; its first call is measured in the log",()=>
        {
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            var hooked=plugin.IndexOf("registrations.Register(()=>mainWindow.ChatPlayers=chatPlayers=new Chat.ChatPlayerActions(",StringComparison.Ordinal);
            Check(hooked>0 && plugin.IndexOf("public Plugin(",StringComparison.Ordinal) is var ctor && ctor>0 && ctor<hooked,"registered in the plugin's constructor");
            var actions=Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            var made=actions.IndexOf("public ChatPlayerActions(",StringComparison.Ordinal);
            Check(made>0 && actions.IndexOf("hook.Enable();",made,StringComparison.Ordinal) is var enabled && enabled>made && actions.IndexOf("public bool Available",made,StringComparison.Ordinal)>enabled,"enabled in its constructor");
            Check(actions.Contains("first call {0:0.0} s after the load") && actions.Contains("a player's menu found its sender: {0} (kind {1})."),"measured and said in the log");
        }),
    ];
}
