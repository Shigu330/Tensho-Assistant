using TenshoAssistant.Chat;

// 0.1602 (X-654, the user's request of 20/11 and choice « by the game's menu »): the game's entries of a player's menu in the Tensho chat
// window. Behaviour tests (R28.14, other players' data): a message's identifiers are given to its sender only (same world, the message just
// copied, once), never to another player; they stay in memory (never in the chat history on disk); the entries that need them are greyed
// without them. docs/evidence/0.1602-chat-player-menu.md.
internal static class ChatPlayerMenu1602Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat player menu 1602: the identifiers that follow a message go to its sender, once, same world only",()=>
        {
            var sources=new ChatSources();
            // X-660 (0.1603): Resolve also takes the kind, the game's log line and your own id (ChatSourceMatch1603Tests for those).
            ChatLogLine Line(string name,ushort world)=>new(10,name,world);
            sources.Expect(1,"Aya Lune",73,10);
            Check(sources.Resolve(1001,2001,73,10,Line("Aya Lune",73),9)==ChatSourceMatch.Given && sources.For(1) is {ContentId:1001,AccountId:2001,Name:"Aya Lune",World:73},"given to the message just copied");
            Check(sources.Resolve(1002,2002,73,10,Line("Aya Lune",73),9)!=ChatSourceMatch.Given && sources.For(1)!.ContentId==1001,"once: a second call gives nothing");
            sources.Expect(2,"Bo Ciel",74,10);
            Check(sources.Resolve(1003,2003,73,10,Line("Bo Ciel",74),9)!=ChatSourceMatch.Given && sources.For(2)!.ContentId==0,"another world: never given (another player)");
            sources.Expect(3,"Cy Vent",73,10);
            sources.Skip();
            Check(sources.Resolve(1004,2004,73,10,Line("Cy Vent",73),9)!=ChatSourceMatch.Given && sources.For(3)!.ContentId==0,"a line between (no sender, Skip): the next identifiers are not its");
            sources.Expect(4,"",0,10);
            Check(sources.Resolve(1005,2005,0,10,Line("",0),9)!=ChatSourceMatch.Given && sources.For(4) is null,"a message without a player: nothing kept");
            sources.Expect(5,"Aya Lune",73,10);
            Check(sources.Resolve(0,0,73,10,Line("Aya Lune",73),9)==ChatSourceMatch.NoIdentifiers && sources.For(5)!.ContentId==0,"no identifier: nothing");
        }),
        ("Chat player menu 1602: bounded, cleared with the messages",()=>
        {
            var sources=new ChatSources();
            for(var id=0L;id<ChatSources.MaximumKept+10;id++)sources.Expect(id,"P",1,10);
            Check(sources.Count==ChatSources.MaximumKept && sources.For(0) is null && sources.For(ChatSources.MaximumKept+9) is not null,"the oldest dropped");
            sources.Clear();
            Check(sources.Count==0 && sources.Resolve(1,1,1,10,new ChatLogLine(10,"P",1),9)==ChatSourceMatch.NotWaiting,"cleared");
        }),
        ("Chat player menu 1602: the entries, greyed without the identifiers; the game's menu only in range; reply only where the mode is known",()=>
        {
            Check(ChatPlayerMenu.Entries(null,ChatChannel.Say,true,true).Count==0,"no sender: no entry");
            var bare=new ChatSource("Aya Lune",73);
            var entries=ChatPlayerMenu.Entries(bare,ChatChannel.Say,false,true);
            Check(entries.Single(e=>e.Action==ChatPlayerAction.Tell) is {Enabled:false,AddonRow:50} && entries.Single(e=>e.Action==ChatPlayerAction.Plate) is {Enabled:false,AddonRow:15083}
                && entries.Single(e=>e.Action==ChatPlayerAction.Mute) is {Enabled:false,AddonRow:14833},"tell, plate, mute greyed without identifiers");
            Check(entries.Single(e=>e.Action==ChatPlayerAction.Reply) is {Enabled:true,AddonRow:368} && entries.Single(e=>e.Action==ChatPlayerAction.Invite) is {Enabled:true,AddonRow:54},"reply in say, invite");
            Check(entries.Single(e=>e.Action==ChatPlayerAction.Target) is {Enabled:false,AddonRow:8320} && entries.All(e=>e.Action!=ChatPlayerAction.GameMenu),"not in range: no target, no game menu");
            var known=bare with{ContentId=1001,AccountId=2001};
            var near=ChatPlayerMenu.Entries(known,ChatChannel.Ls1,true,false);
            Check(near.All(e=>e.Enabled) && near.Any(e=>e.Action==ChatPlayerAction.GameMenu) && near.All(e=>e.Action is not (ChatPlayerAction.Reply or ChatPlayerAction.Invite)),"in range, known: all on; linkshell: no reply; cannot invite: none");
            Check(ChatPlayerMenu.Entries(bare,ChatChannel.TellIn,false,false).Single(e=>e.Action==ChatPlayerAction.Reply).Enabled==false,"reply to a tell needs the identifiers");
            Check(ChatPlayerMenu.ReplyChannel(ChatChannel.Say)==1 && ChatPlayerMenu.ReplyChannel(ChatChannel.Party)==2 && ChatPlayerMenu.ReplyChannel(ChatChannel.Alliance)==3 && ChatPlayerMenu.ReplyChannel(ChatChannel.FreeCompany) is null,"modes of FFXIVClientStructs' ChatChannel");
        }),
        ("Chat player menu 1602: identifiers in memory only; the hook calls the game first; each entry on the click, at the next tick, nothing automatic",()=>
        {
            string Read(string path)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+path));
            Check(!Read("Chat/ChatHistory.cs").Contains("ContentId") && !Read("Chat/ChatHistory.Dalamud.cs").Contains("ChatSources") && !Read("Chat/ChatStore.cs").Contains("ContentId"),"never on disk, not in the records");
            var actions=Read("Chat/ChatPlayerActions.Dalamud.cs");
            Check(actions.Replace("\r\n","\n").Contains("hook!.Original(module,contentId,accountId,messageIndex,worldId,chatType);\n        // X-662 M2") && actions.Contains("try{Said(sources.ResolveRead("),"the game first, then the copy");
            Check(actions.Contains("MemberFunctionPointers.AddMsgSourceEntry==0") && actions.Contains("framework.RunOnTick(") && actions.Contains("client.Logout+=OnLogout") && !actions.Contains("ProcessChatBoxEntry"),"pointer checked, next tick, cleared at logout, no command sent");
            var window=Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("DrawChatPlayerGameEntries(said);") && window.Contains("actions.Run(entry.Action,source,said.Channel);"),"only from the click in the menu");
            var reader=Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("sources?.Skip();") && reader.Contains("sources.Expect(record.Id,player.PlayerName,(ushort)player.World.RowId,(ushort)message.LogKind);") && reader.Contains("if(!current.WindowOpen)sources?.Clear();"),"the reader notes each sender, skips the others, clears when off");
            foreach(var language in new[]{"en","fr"})
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).Contains("\"MenuGameMenu\": \""),language);
        }),
    ];
}
