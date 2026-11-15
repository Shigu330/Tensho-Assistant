using Dalamud.Game.Chat;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-403 phase 1 (D-166; back from 0.471): copies the messages the game shows (IChatGui.ChatMessageUnhandled: after the other plugins, so what they hide
/// stays hidden) into the store, while the Tensho chat window is on: the sender's text, the message's text pieces, its kind and its time, as plain
/// managed data, then returns. Never changes a message and sends nothing. Not read during a competitive cut. Cleared at logout and at unload; nothing
/// goes to disk.
/// </summary>
internal sealed class ChatReader : IDisposable
{
    private readonly RegistrationScope registrations=new();
    private readonly IPluginLog log;private readonly Func<ChatSettings> settings;private readonly ChatStore store;
    private bool disposed,warned;

    public ChatReader(IChatGui chat,IClientState client,IPluginLog log,Func<ChatSettings> settings,ChatStore store)
    {
        this.log=log;this.settings=settings;this.store=store;
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
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        if(disposed)return;
        try
        {
            var current=settings();
            // Turned off (also by « restore every setting »): what was kept goes at the next message.
            if(!current.WindowOpen){if(store.Count>0)store.Clear();return;}
            var body=new List<ChatSegment>();
            foreach(var payload in message.Message.Payloads)
                if(payload is ITextProvider text && text.Text is { Length:>0 } value)body.Add(new(value));
            store.Add(ChatFormat.When(message.Timestamp,DateTimeOffset.UtcNow),(int)message.LogKind,message.Sender.TextValue,body,current.BoundedMaxLines);
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A chat message could not be copied for the Tensho chat window.");}
        }
    }

    private void OnLogout(int type,int code)=>store.Clear();

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        registrations.Dispose();
        store.Clear();
    }
}
