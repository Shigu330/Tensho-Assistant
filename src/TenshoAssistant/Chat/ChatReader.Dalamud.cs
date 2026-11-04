using Dalamud.Game.Chat;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Chat;

/// <summary>
/// CHAT-001: copies the messages the game shows (IChatGui.ChatMessageUnhandled: after the other plugins, so what they hide stays hidden) into the store, while the window is on. Never changes a
/// message and sends nothing. Cleared at logout; nothing goes to disk.
/// </summary>
internal sealed class ChatReader : IDisposable
{
    private readonly IChatGui chat;private readonly IClientState client;private readonly IPluginLog log;private readonly Func<ChatSettings> settings;private readonly ChatStore store;
    private bool disposed,warned;

    public ChatReader(IChatGui chat,IClientState client,IPluginLog log,Func<ChatSettings> settings,ChatStore store)
    {
        this.chat=chat;this.client=client;this.log=log;this.settings=settings;this.store=store;
        chat.ChatMessageUnhandled+=OnMessage;
        try{client.Logout+=OnLogout;}catch{chat.ChatMessageUnhandled-=OnMessage;throw;}
    }

    private void OnMessage(IChatMessage message)
    {
        if(disposed)return;
        try
        {
            var current=settings();
            if(!current.WindowOpen)return;
            var body=new List<ChatSegment>();var types=new List<string>();
            foreach(var payload in message.Message.Payloads)
            {
                types.Add(payload.Type.ToString());
                if(payload is ITextProvider text && text.Text is {Length:>0} value)body.Add(new(value));
            }
            store.Add(DateTimeOffset.UtcNow,(int)message.LogKind,message.Sender.TextValue,body,current.BoundedMaxLines,types);
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A chat message could not be copied.");}
        }
    }

    private void OnLogout(int type,int code)=>store.Clear();

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        chat.ChatMessageUnhandled-=OnMessage;
        client.Logout-=OnLogout;
        store.Clear();
    }
}
