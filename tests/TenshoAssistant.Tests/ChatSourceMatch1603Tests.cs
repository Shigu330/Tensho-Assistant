using TenshoAssistant.Chat;

// 0.1603 (X-660, review of the block 0.1599 to 0.1602, I1): the game's identifiers of a chat message (content id, account id) were given to the
// last message copied by order and world only. Behaviour tests (R28.14, other players' data): a message hidden by another plugin, a tell you
// send (its link is the recipient's), another kind of message or another sender on the game's log line: never given; given only when the
// kind, the log line at the game's index (kind, name, home world), the world agree and the ids are not your own.
// docs/evidence/0.1602-chat-player-menu.md (section X-660).
internal static class ChatSourceMatch1603Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private const ushort Say=10,TellOut=12,TellIn=13,Party=14;
    private const ulong Own=5000;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat source match 1603: a message hidden by another plugin: its identifiers never go to the line before",()=>
        {
            // Aya's line is copied; Bo's message (same world) is hidden by another plugin: no Expect for it. The game then gives Bo's ids with
            // the index of Bo's line: the log line says Bo, not Aya: refused.
            var sources=new ChatSources();
            sources.Expect(1,"Aya Lune",73,Say);
            Check(sources.Resolve(2002,3002,73,Say,new ChatLogLine(Say,"Bo Ciel",73),Own)==ChatSourceMatch.Line && sources.For(1)!.ContentId==0,"another sender on the log line: nothing given");
            // The reader's handler of hidden messages (Skip) also ends the wait.
            sources.Expect(2,"Aya Lune",73,Say);
            sources.Skip();
            Check(sources.Resolve(2002,3002,73,Say,new ChatLogLine(Say,"Aya Lune",73),Own)==ChatSourceMatch.NotWaiting && sources.For(2)!.ContentId==0,"hidden then skipped: nothing");
        }),
        ("Chat source match 1603: a tell you send: your own identifiers never go to the recipient",()=>
        {
            var sources=new ChatSources();
            sources.Expect(1,"Cy Vent",73,TellOut);
            Check(sources.Resolve(Own,Own+1,73,TellOut,new ChatLogLine(TellOut,"Cy Vent",73),Own)==ChatSourceMatch.Own && sources.For(1)!.ContentId==0,"your own ids: refused");
            sources.Expect(2,"Cy Vent",73,TellOut);
            Check(sources.Resolve(7001,8001,73,TellOut,new ChatLogLine(TellOut,"Cy Vent",73),Own)==ChatSourceMatch.Given && sources.For(2) is {ContentId:7001,AccountId:8001},"the recipient's ids, everything agreeing: given");
        }),
        ("Chat source match 1603: the kind the game gives, the log line's kind, name and world must all agree",()=>
        {
            var sources=new ChatSources();
            sources.Expect(1,"Aya Lune",73,Party);
            Check(sources.Resolve(1001,2001,73,Say,new ChatLogLine(Party,"Aya Lune",73),Own)==ChatSourceMatch.Kind && sources.For(1)!.ContentId==0,"another kind given: refused");
            sources.Expect(2,"Aya Lune",73,Party);
            Check(sources.Resolve(1001,2001,73,Party,new ChatLogLine(Say,"Aya Lune",73),Own)==ChatSourceMatch.Line,"another kind on the log line: refused");
            sources.Expect(3,"Aya Lune",73,Party);
            Check(sources.Resolve(1001,2001,73,Party,new ChatLogLine(Party,"Aya Lune",74),Own)==ChatSourceMatch.Line,"another home world on the log line: refused");
            sources.Expect(4,"Aya Lune",73,Party);
            Check(sources.Resolve(1001,2001,73,Party,new ChatLogLine(Party,"aya lune",73),Own)==ChatSourceMatch.Line,"names compared exactly");
            sources.Expect(5,"Aya Lune",73,Party);
            Check(sources.Resolve(1001,2001,73,Party,null,Own)==ChatSourceMatch.LineUnread,"no log line at the index: refused");
            sources.Expect(6,"Aya Lune",73,Party);
            Check(sources.Resolve(1001,2001,74,Party,new ChatLogLine(Party,"Aya Lune",73),Own)==ChatSourceMatch.World,"another world given: refused");
            Check(Enumerable.Range(1,6).All(id=>sources.For(id)!.ContentId==0),"none of them given");
            // The kind is the low seven bits: who does it and to whom (the high bits) do not matter.
            sources.Expect(7,"Aya Lune",73,(ushort)(TellIn|0x0400));
            Check(sources.Resolve(1001,2001,73,(ushort)(TellIn|0x2000),new ChatLogLine((ushort)(TellIn|0x0800),"Aya Lune",73),Own)==ChatSourceMatch.Given && sources.For(7)!.ContentId==1001,"same kind under other high bits: given");
        }),
        ("Chat source match 1603: wiring: the hook reads the game's log line at the index and your own id; hidden messages end the wait",()=>
        {
            string Read(string path)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+path)).Replace("\r\n","\n");
            var actions=Read("Chat/ChatPlayerActions.Dalamud.cs");
            Check(actions.Contains("sources.ResolveRead(contentId,accountId,worldId,chatType,()=>Line(module,messageIndex),module->LocalPlayerContentId)")
                && actions.Contains("module->GetLogMessageDetail(index,out var sender,out _,out var kind,out _,out _,out _)"),"kind, line at the game's index, own id");
            Check(actions.Contains("log.Information(\"Chat window, a sender's identifiers: {0} (kind {1}).\",match,chatType&ChatSources.KindMask);"),"each outcome said once, without name nor id");
            var reader=Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("chat.ChatMessageHandled+=OnHidden") && reader.Contains("private void OnHidden(IChatMessage message)\n    {\n        // Also during a competitive cut (CompetitiveGate.Cut), when nothing is copied: then nothing waits anyway.\n        sources?.Skip();"),"a hidden message ends the wait");
        }),
    ];
}
