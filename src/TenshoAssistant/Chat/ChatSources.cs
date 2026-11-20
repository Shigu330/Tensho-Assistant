namespace TenshoAssistant.Chat;

/// <summary>X-654 (0.1602): who sent a message of the Tensho chat window: name and world from the message, the game's identifiers from its log (0: not known).</summary>
public sealed record ChatSource(string Name,ushort World,ulong ContentId=0,ulong AccountId=0);

/// <summary>X-660 (0.1603): a line of the game's own log, read at the index the game gives with a message's identifiers: its kind and the name
/// and home world of its sender's player link (empty name: none).</summary>
public readonly record struct ChatLogLine(ushort Kind,string Name,ushort World);

/// <summary>X-679 (0.1660): where a message's menu found its sender (said once each in the log, never with a name): its own source with the
/// identifiers, the identifiers of the same player's other message, its own source without them, only the name its text gives, nothing.</summary>
public enum ChatSourceFound : byte { Message, Player, MessageOnly, Named, None }

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
    // X-679 (0.1660): the identifiers are the character's, not the message's: the last ones given to a player (name and home world) serve
    // their other messages too (one from the history on disk, a repeat folded into its first copy, a message whose number changed).
    private readonly Dictionary<(string Name,ushort World),ChatSource> players=[];
    private readonly Queue<(string Name,ushort World)> playerOrder=new();
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
            var given=source with{ContentId=contentId,AccountId=accountId};
            sources[id]=given;
            var key=(given.Name,given.World);
            if(!players.ContainsKey(key)){playerOrder.Enqueue(key);while(playerOrder.Count>MaximumKept)players.Remove(playerOrder.Dequeue());}
            players[key]=given;
            return ChatSourceMatch.Given;
        }
    }

    /// <summary>
    /// X-679 (0.1658, the user's report of 20/11 around 21:50: no game entry at all in a player's menu): the chat history put back after a
    /// reload numbers every line again, this session's too (ChatStore.Restore, 10 to 30 s after the load: dalamud.log of 20/11, 21:32:03 then
    /// 21:32:31, 23:24:20 then 23:24:31); the senders kept by the old numbers were lost, and the menu of every message received in between
    /// showed none of the game's entries. They follow their lines now, the message waiting for its identifiers too.
    /// </summary>
    public void Renumber(IReadOnlyDictionary<long,long> ids)
    {
        lock(gate)
        {
            if(ids.Count==0)return;
            var moved=new Dictionary<long,ChatSource>();
            foreach(var (before,after) in ids)
                if(sources.Remove(before,out var source))moved[after]=source;
            foreach(var (id,source) in moved)sources[id]=source;
            var kept=order.Select(id=>ids.GetValueOrDefault(id,id)).Where(sources.ContainsKey).Distinct().ToList();
            order.Clear();
            foreach(var id in kept)order.Enqueue(id);
            if(expected>=0)expected=ids.GetValueOrDefault(expected,expected);
        }
    }

    /// <summary>A message that is not followed by its identifiers (a system line, a message hidden by another plugin) ends the wait: the next identifiers are not its.</summary>
    public void Skip(){lock(gate)expected=-1;}

    public ChatSource? For(long id){lock(gate)return sources.GetValueOrDefault(id);}

    /// <summary>
    /// X-679 (0.1660, the user's report of 20/11: a player's menu with none of the game's entries): the sender of a message for its menu,
    /// never nothing when the message names a player. Its own source with the game's identifiers; else the identifiers last given to the same
    /// player (same name and home world: the character's, never another player's); else its own source without them; else the player the
    /// message names (<paramref name="name"/> and <paramref name="world"/>, read from its sender's text), without identifiers; null only when
    /// neither is known.
    /// </summary>
    public (ChatSource? Source,ChatSourceFound How) Find(long id,string name,ushort world)
    {
        lock(gate)
        {
            var own=sources.GetValueOrDefault(id);
            if(own is { ContentId:not 0 })return (own,ChatSourceFound.Message);
            var key=own is not null ? (own.Name,own.World):(name,world);
            if(key.Item1.Length>0 && key.Item2!=0 && players.TryGetValue(key,out var known))return (known,ChatSourceFound.Player);
            if(own is not null)return (own,ChatSourceFound.MessageOnly);
            return name.Length>0 && world!=0 ? (new ChatSource(name,world),ChatSourceFound.Named):(null,ChatSourceFound.None);
        }
    }

    public int Count{get{lock(gate)return sources.Count;}}
    public void Clear(){lock(gate){sources.Clear();order.Clear();players.Clear();playerOrder.Clear();expected=-1;}}
}

/// <summary>X-654 (0.1602): the entries of a player's menu in the chat window, after Tensho's own; their texts are the client's (Addon rows).</summary>
/// X-680 (0.1662, the user's request of 20/11: « we still miss things », the game's menu beside Tensho's): the game's other entries.
public enum ChatPlayerAction : byte { Tell, Reply, Invite, Plate, Mute, Target, GameMenu, Friend, Blacklist, Report, PartyFinder }

/// <summary>X-679 (0.1660): why an entry is greyed, said in its tooltip (None: it can be used).</summary>
/// X-680 (0.1662): GameOnly, the game does it only from its own menu (no function of the game's library for it); NoReplyMode, the channel's
/// mode is not known to the game's library (only say, party and alliance are).
public enum ChatPlayerWhy : byte { None, NoSender, NoIdentifiers, NoAccount, NotInRange, GameOnly, NoReplyMode }

/// <summary>X-654 (0.1602): one entry, its text's Addon row (0: Tensho's own text), and whether it can be used now. X-679 (0.1660): why not.</summary>
/// X-680 (0.1662): Block, an entry of the game's « Blocking functions » submenu (Addon 16061), drawn inside it.
public readonly record struct ChatPlayerEntry(ChatPlayerAction Action,uint AddonRow,bool Enabled,ChatPlayerWhy Why=ChatPlayerWhy.None,bool Block=false);

/// <summary>
/// X-654 (0.1602): which entries a player's menu offers, from what is known of the sender. The client's texts (Addon, game 2026.11.03): 50
/// « Murmurer » / « Send Tell », 368 « Répondre avec le même mode de discussion », 54 « Inviter dans l'équipe », 15083 « Voir la carte de
/// personnage », 14833 « Ajouter à la liste muette », 8320 « Cibler ». Friend request and blacklist only through the game's own menu of a
/// character in range (GameMenu, the user's choice of 20/11); no « Report ». Pure: the actions are done by ChatPlayerActions.
/// </summary>
public static class ChatPlayerMenu
{
    public const uint TellRow=50,ReplyRow=368,InviteRow=54,PlateRow=15083,MuteRow=14833,TargetRow=8320;
    /// <summary>X-680 (0.1662): the client's texts of the game's other entries (Addon, French client 2026.11.03, XbmProbe of 21/11): 61 « Envoyer
    /// une requête d'ami », 16061 « Fonctions de blocage », 59 « Ajouter à la liste noire », 10870 « Signaler ce joueur », 2200 « Recherche
    /// d'équipe ».</summary>
    public const uint FriendRow=61,BlockRow=16061,BlacklistRow=59,ReportRow=10870,PartyFinderRow=2200;

    /// <summary>The channels whose mode « Reply in selected mode » sets (the game's ChatChannel values of ClientStructs: Say 1, Party 2, Alliance 3), tells apart.</summary>
    public static int? ReplyChannel(ChatChannel channel)=>channel switch{ChatChannel.Say=>1,ChatChannel.Party=>2,ChatChannel.Alliance=>3,_=>null};

    /// <summary>
    /// X-679 (0.1660): a sender not known (null) shows the entries greyed with the reason, never an empty menu. X-681 (0.1660, the user's
    /// request of 20/11: « Tell, can't it be done with the name? »): a tell, and the reply to a tell, need only the player's name and home world,
    /// which every source has (the typing box set to a tell to that player, as « /tell Name@World » does; nothing sent).
    /// </summary>
    /// <remarks>X-680 (0.1662, the user's request of 20/11, two pictures of the same « Yell »): the game's entries in the game's order:
    /// tell, reply in the same mode (every spoken channel, as the game offers it), invite, friend request, blocking functions (mute list,
    /// blacklist), report, party finder, target, adventurer plate, then Tensho's « Game menu… » for a character in range. What the game's
    /// library cannot do (friend request, blacklist, report: no function, docs/evidence/0.1662-chat-player-menu-game-entries.md) is greyed
    /// with why; « Game menu… » or the game's chat do it.</remarks>
    public static IReadOnlyList<ChatPlayerEntry> Entries(ChatSource? source,ChatChannel channel,bool inRange,bool canInvite)
    {
        var tell=channel is ChatChannel.TellIn or ChatChannel.TellOut;
        var known=source is { ContentId:not 0 };
        var account=known && source!.AccountId!=0;
        ChatPlayerWhy Need(bool can,ChatPlayerWhy why)=>source is null ? ChatPlayerWhy.NoSender:can ? ChatPlayerWhy.None:why;
        ChatPlayerEntry Entry(ChatPlayerAction action,uint row,bool can,ChatPlayerWhy why,bool block=false)
            =>new(action,row,source is not null && can,Need(can,why),block);
        var list=new List<ChatPlayerEntry>{Entry(ChatPlayerAction.Tell,TellRow,true,ChatPlayerWhy.None)};
        if(ChatChannels.Spoken(channel))
        {
            var mode=tell || ReplyChannel(channel) is not null;
            list.Add(Entry(ChatPlayerAction.Reply,ReplyRow,mode,ChatPlayerWhy.NoReplyMode));
        }
        if(source is not null && canInvite)list.Add(Entry(ChatPlayerAction.Invite,InviteRow,true,ChatPlayerWhy.None));
        list.Add(Entry(ChatPlayerAction.Friend,FriendRow,false,ChatPlayerWhy.GameOnly));
        list.Add(Entry(ChatPlayerAction.Mute,MuteRow,account,known ? ChatPlayerWhy.NoAccount:ChatPlayerWhy.NoIdentifiers,true));
        list.Add(Entry(ChatPlayerAction.Blacklist,BlacklistRow,false,ChatPlayerWhy.GameOnly,true));
        list.Add(Entry(ChatPlayerAction.Report,ReportRow,false,ChatPlayerWhy.GameOnly));
        list.Add(Entry(ChatPlayerAction.PartyFinder,PartyFinderRow,known,ChatPlayerWhy.NoIdentifiers));
        list.Add(Entry(ChatPlayerAction.Target,TargetRow,inRange,ChatPlayerWhy.NotInRange));
        list.Add(Entry(ChatPlayerAction.Plate,PlateRow,known,ChatPlayerWhy.NoIdentifiers));
        if(source is not null && inRange)list.Add(new(ChatPlayerAction.GameMenu,0,true));
        return list;
    }

    /// <summary>X-679 (0.1660): the player a message names, from its sender's text (glyphs taken off, « Name@World »): the name, and the world's
    /// name (empty: none written, the game writes none for a player of the world you are on).</summary>
    public static (string Name,string World) Named(string? sender)
    {
        var clean=ChatConversations.Key(sender);
        return (ChatTextFilter.PlayerName(clean),ChatCopy.WorldOf(clean));
    }
}
