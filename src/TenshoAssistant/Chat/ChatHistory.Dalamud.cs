using Dalamud.Plugin.Services;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-475: the game's side of the chat history (ChatHistory, ChatHistoryWriter): once a second, on the game's thread, through the competitive
/// gate (nothing in PvP and ranked play, as the rest of Tensho), it opens the history of the character logged in while the history is on and
/// the window shows (read back off the game's thread, then put before this session's lines: ChatStore.Restore), writes the new messages the
/// reader hands it, closes it at logout or when the window is turned off (what is queued is still written), and drops what is queued when
/// the history is turned off. Only the local character's number names its file. Nothing is ever sent anywhere.
/// </summary>
internal sealed class ChatHistoryKeeper : IDisposable
{
    private readonly IFramework framework;private readonly IClientState client;private readonly IPlayerState player;
    private readonly IPluginLog log;private readonly Func<ChatSettings> settings;private readonly ChatStore store;
    private readonly BackgroundWriter background;
    private readonly ChatHistoryWriter writer;
    private long nextCheck=-100_000;
    private ulong open;
    private ChatHistory.Loaded? loaded;
    private bool disposed;

    public ChatHistoryKeeper(IFramework framework,IClientState client,IPlayerState player,IPluginLog log,Func<ChatSettings> settings,ChatStore store,string pluginFolder)
    {
        this.framework=framework;this.client=client;this.player=player;this.log=log;this.settings=settings;this.store=store;
        background=new BackgroundWriter((error,message)=>log.Warning(error,message));
        writer=new ChatHistoryWriter(Path.Combine(pluginFolder,ChatHistory.FolderName),background.Enqueue,(error,message)=>log.Warning(error,message));
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    /// <summary>The folder of the history files (said to the player when asked to turn it on).</summary>
    public string Folder=>writer.Folder;

    /// <summary>A new message the reader copied (game's thread): kept when the history of its character is open.</summary>
    public void Keep(ChatRecord record)
    {
        var current=settings();
        if(disposed || !current.KeepHistory)return;
        // A message of the first second after login (or a reload) is kept too: the history is opened here when it is not yet.
        Sync(current,DateTimeOffset.UtcNow);
        if(open!=0)writer.Keep(record);
    }

    /// <summary>Opens the history of the character logged in (closes the one before) when the window shows; game's thread.</summary>
    private void Sync(ChatSettings current,DateTimeOffset at)
    {
        var who=current.WindowOpen && client.IsLoggedIn && player.IsLoaded ? player.ContentId:0UL;
        if(who==open)return;
        if(open!=0)writer.Close(at,current.BoundedHistoryMessages,current.BoundedHistoryDays);
        Volatile.Write(ref loaded,null);
        open=who;
        if(who!=0)writer.Open(who,at,current.BoundedHistoryMessages,current.BoundedHistoryDays,read=>Volatile.Write(ref loaded,read));
    }

    /// <summary>« Erase the history » (game's thread): every character's file, and what is queued; <paramref name="done"/> says whether every file went.</summary>
    public void Erase(Action<bool>? done=null)=>writer.Erase(done);

    private void OnUpdate(IFramework _)
    {
        if(disposed)return;
        var now=Environment.TickCount64;
        if(now-nextCheck<1000)return;
        nextCheck=now;
        try
        {
            var current=settings();
            var at=DateTimeOffset.UtcNow;
            var messages=current.BoundedHistoryMessages;var days=current.BoundedHistoryDays;
            if(!current.KeepHistory)
            {
                // Turned off: nothing more is written, what was queued is dropped (the files stay until « Erase the history »).
                if(open!=0 || writer.Pending>0){writer.Drop();open=0;Volatile.Write(ref loaded,null);}
                return;
            }
            Sync(current,at);
            if(open!=0 && Interlocked.Exchange(ref loaded,null) is { } back)
            {
                var put=store.Restore(back.Records,current.BoundedMaxLines);
                log.Information("Chat history: {0} messages read back, {1} put before this session's lines, {2} lines that could not be read skipped.",back.Records.Count,put,back.Bad);
            }
            if(writer.Pending>0)writer.Flush(at,messages,days);
        }
        catch(Exception error)
        {
            log.Warning(error,"The chat history could not be kept this second.");
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        var current=settings();
        // At unload the queue is still written (when the history is on), and waited for a moment.
        if(current.KeepHistory)writer.Close(DateTimeOffset.UtcNow,current.BoundedHistoryMessages,current.BoundedHistoryDays);
        else writer.Drop();
        if(!background.Flush(TimeSpan.FromSeconds(2)))log.Warning("The chat history was still being written at unload.");
    }
}
