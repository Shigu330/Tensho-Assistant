using Dalamud.Game.Chat;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-403 phase 1 (D-166; back from 0.471): copies the messages the game shows (IChatGui.ChatMessageUnhandled: after the other plugins, so what they hide
/// stays hidden) into the store, while the Tensho chat window is on: the sender's text, the message's text pieces, its kind and its time, as plain
/// managed data, then returns. Never changes a message and sends nothing. Not read during a competitive cut. Cleared at logout and at unload; nothing
/// goes to disk, except to the chat history when the player turned it on (X-475, ChatHistoryKeeper: on this PC only).
/// </summary>
internal sealed class ChatReader : IDisposable
{
    private readonly RegistrationScope registrations=new();
    private readonly IPluginLog log;private readonly Func<ChatSettings> settings;private readonly ChatStore store;
    private readonly Func<bool>? developer;
    // X-554 (review of 0.1520): your character's name, to mark what you say yourself (the game gives no subject for it).
    private readonly Func<string?>? ownName;
    // X-475: the chat history on disk (null: none, as outside the game).
    private readonly ChatHistoryKeeper? history;
    private bool disposed,warned;
    // X-461 (0.1463): developer mode: the kinds of pieces of a message with a link, a few per session (kinds and lengths only, never the words).
    // X-479 (0.1468): a few per kind of message (loot lines took all twenty on 11/15 before any message of a player); and a link shown without
    // text of its own (the window writes its target's name) is said apart, with what the game sent for it.
    private readonly ChatPieceLog piecesSaid=new();
    private int bareSaid;
    // X-525 M3 (0.1503): developer mode: the icon codes met in senders' names, once per code, at most twenty per session (codes only).
    private readonly ChatPieceLog senderIconsSaid=new(1,20);
    private const int BareSaidMax=10;
    // X-654 (0.1602): the sender of each message, for a player's menu (in memory only; null: none, as outside the game). Cleared with the
    // messages: window off, logout (ChatPlayerActions), unload.
    internal ChatSources? Sources{get;set;}
    private ChatSources? sources=>Sources;

    public ChatReader(IChatGui chat,IClientState client,IPluginLog log,Func<ChatSettings> settings,ChatStore store,Func<bool>? developer=null,ChatHistoryKeeper? history=null,Func<string?>? ownName=null)
    {
        this.log=log;this.settings=settings;this.store=store;this.developer=developer;this.history=history;this.ownName=ownName;
        // One owner for both subscriptions: a failure part-way undoes the ones made; Dispose releases them in reverse order.
        try
        {
            registrations.Register(()=>chat.ChatMessageUnhandled+=OnMessage,()=>chat.ChatMessageUnhandled-=OnMessage);
            registrations.Register(()=>client.Logout+=OnLogout,()=>client.Logout-=OnLogout);
        }
        catch{registrations.Dispose();throw;}
    }

    private void OnMessage(IChatMessage message)
    {
        // X-654: the identifiers that follow a message not copied are not given to anyone.
        sources?.Skip();
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        if(disposed)return;
        try
        {
            var current=settings();
            // Turned off (also by « restore every setting »): what was kept goes at the next message.
            if(!current.WindowOpen)sources?.Clear();
            if(!current.WindowOpen){if(store.Count>0)store.Clear();return;}
            // X-403 phase 2 (0.1441): the links too (a place or an item), with the text that shows them; a link whose target cannot be
            // read stays plain text. X-479 (0.1468): each payload told apart here (ChatPayload, pure), the rest in ChatPayloads.
            var payloads=message.Message.Payloads.Select(Payload).ToList();
            // X-496 M1 (0.1478): a link of Dalamud or of a plugin keeps its own string (from the link to its end), the one Dalamud passes
            // to its handler (docs/evidence/0.1478-dalamud-link-handler.md).
            for(var index=0;index<payloads.Count;index++)
                if(payloads[index] is { Kind:ChatPayloadKind.Dalamud,Link:{ } link })
                {
                    var last=ChatPayloads.LinkLast(payloads,index);
                    payloads[index]=payloads[index] with{Link=link with{Data=new SeString(message.Message.Payloads.Skip(index).Take(last-index+1).ToList()).Encode()}};
                }
            var body=ChatPayloads.Segments(payloads);
            if(developer?.Invoke()==true && ChatPayloads.HasLink(payloads))
            {
                var kind=(int)message.LogKind;
                if(piecesSaid.Take(kind))
                    log.Information("Chat window, pieces of a message with a link (kind {0}): {1}.",kind,ChatPayloads.Describe(payloads));
                if(bareSaid<BareSaidMax && ChatPayloads.Bare(body)>0)
                {
                    bareSaid++;
                    log.Information("Chat window, a link without text of its own (kind {0}), its target's name written instead: {1}.",kind,ChatPayloads.Describe(payloads));
                }
            }
            // X-472 (0.1471): the sender with its world icon written « @ » (TextValue left it out: « NameWorld »).
            var sender=ChatPayloads.Text(message.Sender.Payloads.Select(Payload));
            if(developer?.Invoke()==true && message.Sender.Payloads.Select(Payload).ToList() is var senderPayloads)
                foreach(var icon in senderPayloads.Where(payload=>payload.Kind==ChatPayloadKind.Icon).Select(payload=>payload.Icon).Distinct())
                    if(senderIconsSaid.Take((int)icon))
                        log.Information("Chat window, icons in a sender's name (kind {0}): {1}.",(int)message.LogKind,string.Join(", ",ChatPayloads.SenderIcons(senderPayloads)));
            // X-484 (0.1482): who does it and to whom (Dalamud 15: IChatMessage.SourceKind and TargetKind), for the game's log filters.
            // X-554 (0.1521): what you say yourself marked as yours (ChatAlert.Source): it never alerts nor counts as unread.
            var source=ChatAlert.Source(ChatChannels.From((int)message.LogKind),(byte)message.SourceKind,sender,ownName?.Invoke());
            var record=store.Add(ChatFormat.When(message.Timestamp,DateTimeOffset.UtcNow),(int)message.LogKind,sender,body,current.BoundedMaxLines,source,(byte)message.TargetKind);
            // X-475: kept on disk too when the player turned the history on.
            history?.Keep(record);
            // X-654 (0.1602): who sent it (name and world of the sender's player link), waiting for the game's identifiers.
            if(sources is not null && message.Sender.Payloads.OfType<PlayerPayload>().FirstOrDefault() is { } player)
                sources.Expect(record.Id,player.PlayerName,(ushort)player.World.RowId);
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A chat message could not be copied for the Tensho chat window.");}
        }
    }

    /// <summary>X-479 (0.1468): one of the game's payloads as the reader tells it apart (ChatPayload: pure, replayed by the tests on recorded messages).</summary>
    internal static ChatPayload Payload(Payload payload)=>payload switch
    {
        MapLinkPayload map=>new(ChatPayloadKind.Map,"MapLink",Link:MapLink(map)),
        ItemPayload item=>new(ChatPayloadKind.Item,"Item",Link:new ChatLink(ChatSegmentKind.ItemLink,ItemId:item.ItemId,Hq:item.IsHQ)),
        // 0.1442: a quest link.
        QuestPayload quest=>new(ChatPayloadKind.Quest,"Quest",Link:new ChatLink(ChatSegmentKind.QuestLink,QuestId:quest.Quest.RowId)),
        // X-474 (0.1477): a link of Dalamud or of a plugin (« Learn more here » of « Developer Mode is enabled »): its plugin and command.
        DalamudLinkPayload dalamud=>new(ChatPayloadKind.Dalamud,"DalamudLink",Link:new ChatLink(ChatSegmentKind.DalamudLink,Plugin:dalamud.Plugin ?? "",CommandId:dalamud.CommandId)),
        RawPayload raw when raw.Equals(RawPayload.LinkTerminator)=>new(ChatPayloadKind.LinkEnd,"LinkEnd"),
        ITextProvider text=>new(ChatPayloadKind.Text,payload.GetType().Name,Text:text.Text),
        IconPayload icon=>new(ChatPayloadKind.Icon,payload.GetType().Name,Icon:(uint)icon.Icon),
        RawPayload raw=>new(ChatPayloadKind.Raw,"Raw",Size:raw.Data.Length),
        _=>new(ChatPayloadKind.Other,payload.GetType().Name),
    };

    private static ChatLink? MapLink(MapLinkPayload map)
    {
        try{return new ChatLink(ChatSegmentKind.MapLink,map.TerritoryType.RowId,map.Map.RowId,map.XCoord,map.YCoord,map.PlaceName ?? "");}
        catch(Exception){return null;}
    }

    private void OnLogout(int type,int code)=>store.Clear();

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        registrations.Dispose();
        store.Clear();
        // X-475: the history's queue (lines already made) written when it is on, then released.
        history?.Dispose();
    }
}
