using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Maps;

/// <summary>
/// g, M-03 (0.373): copies the map links (MapLinkPayload) of the chat messages the game shows (IChatGui.ChatMessageUnhandled: after
/// the other plugins, never changing the message) into the session history, while « chat map links » is on in the map settings.
/// Only links are read, never coordinates typed as text (false positives, format by language). Cleared at logout.
/// </summary>
public sealed class ChatMapLinkWatcher : IDisposable
{
    private readonly IChatGui chat;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly Func<bool> enabled;
    private bool disposed,warned;
    public ChatMapLinkHistory History{get;}=new();

    public ChatMapLinkWatcher(IChatGui chat,IClientState client,IPluginLog log,Func<bool> enabled)
    {
        this.chat=chat;this.client=client;this.log=log;this.enabled=enabled;
        chat.ChatMessageUnhandled+=OnMessage;
        try{client.Logout+=OnLogout;}catch{chat.ChatMessageUnhandled-=OnMessage;throw;}
    }

    private void OnMessage(IChatMessage message)
    {
        if(disposed || !enabled())return;
        try
        {
            foreach(var payload in message.Message.Payloads)
            {
                if(payload is not MapLinkPayload link)continue;
                History.Add(new(DateTimeOffset.UtcNow,Group(message.LogKind),message.Sender.TextValue.Trim(),link.TerritoryType.RowId,link.Map.RowId,
                    link.XCoord,link.YCoord,link.PlaceName));
            }
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A chat map link could not be read.");}
        }
    }

    internal static ChatLinkChannel Group(XivChatType type)=>type switch
    {
        XivChatType.Say=>ChatLinkChannel.Say,
        XivChatType.Shout=>ChatLinkChannel.Shout,
        XivChatType.Yell=>ChatLinkChannel.Yell,
        XivChatType.Party or XivChatType.CrossParty or XivChatType.PvPTeam=>ChatLinkChannel.Party,
        XivChatType.Alliance=>ChatLinkChannel.Alliance,
        XivChatType.FreeCompany=>ChatLinkChannel.FreeCompany,
        XivChatType.Ls1 or XivChatType.Ls2 or XivChatType.Ls3 or XivChatType.Ls4 or XivChatType.Ls5 or XivChatType.Ls6 or XivChatType.Ls7
            or XivChatType.Ls8=>ChatLinkChannel.Linkshell,
        XivChatType.CrossLinkShell1 or XivChatType.CrossLinkShell2 or XivChatType.CrossLinkShell3 or XivChatType.CrossLinkShell4
            or XivChatType.CrossLinkShell5 or XivChatType.CrossLinkShell6 or XivChatType.CrossLinkShell7 or XivChatType.CrossLinkShell8=>ChatLinkChannel.CrossLinkshell,
        XivChatType.TellIncoming or XivChatType.TellOutgoing=>ChatLinkChannel.Tell,
        XivChatType.NoviceNetwork=>ChatLinkChannel.Novice,
        _=>ChatLinkChannel.Other,
    };

    private void OnLogout(int type,int code)=>History.Clear();

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        chat.ChatMessageUnhandled-=OnMessage;
        client.Logout-=OnLogout;
        History.Clear();
    }
}
