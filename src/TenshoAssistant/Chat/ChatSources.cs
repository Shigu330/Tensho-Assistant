namespace TenshoAssistant.Chat;

/// <summary>X-654 (0.1602): who sent a message of the Tensho chat window: name and world from the message, the game's identifiers from its log (0: not known).</summary>
public sealed record ChatSource(string Name,ushort World,ulong ContentId=0,ulong AccountId=0);

/// <summary>X-660 (0.1603): a line of the game's own log, read at the index the game gives with a message's identifiers: its kind and the name
/// and home world of its sender's player link (empty name: none).</summary>
public readonly record struct ChatLogLine(ushort Kind,string Name,ushort World);

/// <summary>X-660 (0.1603): what became of the identifiers the game gave (said in the log, never with a name).</summary>
public enum ChatSourceMatch : byte { Given, NotWaiting, NoIdentifiers, Own, Kind, LineUnread, Line, World }

/// <summary>
/// X-654 (0.1602, the user's request of 20/11, choice « by the game's menu » of 20/11): the sender of each message, in memory only (never in the
/// chat history on disk: a player's game identifiers are not kept), for the menu of a player in the chat window. The game gives a message's
/// identifiers right after the message itself (RaptureLogModule.AddMsgSourceEntry, called for each message just after the chat event, as the
/// Chat 2 plugin reads it, docs/evidence/0.1602-chat-player-menu.md): <see cref="Expect"/> notes the message just copied, <see cref="Resolve"/>
/// gives it its identifiers once. X-660 (0.1603, review of 0.1602, I1): order and world were not enough (a message hidden by another plugin, a
/// tell you send whose link is the recipient's): the identifiers are given only when everything agrees: the kind the game gives (chatType) is
/// the expected line's, the game's own log line at the index it gives has that kind and that sender (name and home world), the world is the
/// sender's, and they are not your own character's; else nothing (the entries stay greyed): an identifier is never given to another player.
/// Bounded; cleared with the messages.
/// </summary>
public sealed class ChatSources
{
    public const int MaximumKept=2000;
    /// <summary>The kind of a message is the low seven bits of the game's value (the rest: who does it and to whom).</summary>
    public const ushort KindMask=0x7F;
    private readonly object gate=new();
    private readonly Dictionary<long,ChatSource> sources=[];
    private readonly Queue<long> order=new();
    private long expected=-1;
    private ushort expectedKind;

    /// <summary>The message just copied, of that kind, sent by a player (name and world from its sender); a message without one: <paramref name="name"/> empty.</summary>
    public void Expect(long id,string name,ushort world,ushort kind)
    {
        lock(gate)
        {
            expected=-1;
            if(name.Length==0 || world==0)return;
            if(!sources.ContainsKey(id)){order.Enqueue(id);while(order.Count>MaximumKept)sources.Remove(order.Dequeue());}
            sources[id]=new(name,world);
            expected=id;expectedKind=(ushort)(kind&KindMask);
        }
    }

    /// <summary>
    /// The game's identifiers of a message, with the kind and world it gives, the game's log line at its index (null: not readable) and your
    /// own character's id; given once to the message just copied, only when they all agree (X-660).
    /// </summary>
    public ChatSourceMatch Resolve(ulong contentId,ulong accountId,ushort world,ushort chatType,ChatLogLine? line,ulong own)
        =>ResolveRead(contentId,accountId,world,chatType,()=>line,own);

    /// <summary>
    /// X-662 M2 (0.1606, review of 0.1605): the same, the game's log line read only when a message waits for its identifiers and the
    /// identifiers, owner and kind already agree (<paramref name="line"/> called at most once, never for the other messages of the game).
    /// </summary>
    public ChatSourceMatch ResolveRead(ulong contentId,ulong accountId,ushort world,ushort chatType,Func<ChatLogLine?> line,ulong own)
    {
        lock(gate)
        {
            var id=expected;expected=-1;
            if(id<0 || !sources.TryGetValue(id,out var source))return ChatSourceMatch.NotWaiting;
            if(contentId==0)return ChatSourceMatch.NoIdentifiers;
            if(contentId==own)return ChatSourceMatch.Own;
            if((chatType&KindMask)!=expectedKind)return ChatSourceMatch.Kind;
            if(line() is not { } read)return ChatSourceMatch.LineUnread;
            if((read.Kind&KindMask)!=expectedKind || !string.Equals(read.Name,source.Name,StringComparison.Ordinal) || read.World!=source.World)return ChatSourceMatch.Line;
            if(source.World!=world)return ChatSourceMatch.World;
            sources[id]=source with{ContentId=contentId,AccountId=accountId};
            return ChatSourceMatch.Given;
        }
    }

    /// <summary>A message that is not followed by its identifiers (a system line, a message hidden by another plugin) ends the wait: the next identifiers are not its.</summary>
    public void Skip(){lock(gate)expected=-1;}

    public ChatSource? For(long id){lock(gate)return sources.GetValueOrDefault(id);}
    public int Count{get{lock(gate)return sources.Count;}}
    public void Clear(){lock(gate){sources.Clear();order.Clear();expected=-1;}}
}

/// <summary>X-654 (0.1602): the entries of a player's menu in the chat window, after Tensho's own; their texts are the client's (Addon rows).</summary>
public enum ChatPlayerAction : byte { Tell, Reply, Invite, Plate, Mute, Target, GameMenu }

/// <summary>X-654 (0.1602): one entry, its text's Addon row (0: Tensho's own text), and whether it can be used now.</summary>
public readonly record struct ChatPlayerEntry(ChatPlayerAction Action,uint AddonRow,bool Enabled);

/// <summary>
/// X-654 (0.1602): which entries a player's menu offers, from what is known of the sender. The client's texts (Addon, game 2026.11.03): 50
/// « Murmurer » / « Send Tell », 368 « Répondre avec le même mode de discussion », 54 « Inviter dans l'équipe », 15083 « Voir la carte de
/// personnage », 14833 « Ajouter à la liste muette », 8320 « Cibler ». Friend request and blacklist only through the game's own menu of a
/// character in range (GameMenu, the user's choice of 20/11); no « Report ». Pure: the actions are done by ChatPlayerActions.
/// </summary>
public static class ChatPlayerMenu
{
    public const uint TellRow=50,ReplyRow=368,InviteRow=54,PlateRow=15083,MuteRow=14833,TargetRow=8320;

    /// <summary>The channels whose mode « Reply in selected mode » sets (the game's ChatChannel values of ClientStructs: Say 1, Party 2, Alliance 3), tells apart.</summary>
    public static int? ReplyChannel(ChatChannel channel)=>channel switch{ChatChannel.Say=>1,ChatChannel.Party=>2,ChatChannel.Alliance=>3,_=>null};

    public static IReadOnlyList<ChatPlayerEntry> Entries(ChatSource? source,ChatChannel channel,bool inRange,bool canInvite)
    {
        if(source is null)return [];
        var known=source.ContentId!=0;
        var tell=channel is ChatChannel.TellIn or ChatChannel.TellOut;
        var list=new List<ChatPlayerEntry>{new(ChatPlayerAction.Tell,TellRow,known)};
        if(tell || ReplyChannel(channel) is not null)list.Add(new(ChatPlayerAction.Reply,ReplyRow,!tell || known));
        if(canInvite)list.Add(new(ChatPlayerAction.Invite,InviteRow,true));
        list.Add(new(ChatPlayerAction.Plate,PlateRow,known));
        list.Add(new(ChatPlayerAction.Mute,MuteRow,known && source.AccountId!=0));
        list.Add(new(ChatPlayerAction.Target,TargetRow,inRange));
        if(inRange)list.Add(new(ChatPlayerAction.GameMenu,0,true));
        return list;
    }
}
