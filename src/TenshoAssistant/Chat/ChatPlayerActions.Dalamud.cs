using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-654 (0.1602, the user's request of 20/11 and choice « by the game's menu »): the game's player entries of the Tensho chat window's menu,
/// done the way the Chat 2 plugin does them (read to understand, EUPL-1.2, nothing copied: docs/evidence/0.1602-chat-player-menu.md), with
/// FFXIVClientStructs' functions only (kept up to date with Dalamud, R08): the message's identifiers read after the game's chat event
/// (RaptureLogModule.AddMsgSourceEntry, a hook that calls the game first and only copies them, into ChatSources, in memory), then, on the
/// player's click only (R03.3, nothing automatic), run on the framework thread with the character loaded: tell (RaptureShellModule.
/// SetContextTellTarget: the typing box set to a tell to that player, nothing sent), reply in the message's mode, party invite
/// (InfoProxyPartyInvite), adventurer plate (AgentCharaCard), mute list (AgentMutelist), target (the object table), and the game's own
/// menu of a character in range (AgentHUD.OpenContextMenuFromTarget: friend request, blacklist… as the game offers them). A failure turns the
/// entries off until the plugin reloads, said once.
/// </summary>
internal sealed unsafe class ChatPlayerActions : IDisposable
{
    private readonly IFramework framework;private readonly IClientState client;private readonly IObjectTable objects;private readonly ITargetManager targets;
    private readonly IPartyList party;private readonly IDataManager data;private readonly IPluginLog log;private readonly ChatSources sources;
    private Hook<RaptureLogModule.Delegates.AddMsgSourceEntry>? hook;
    private bool failed;

    public ChatPlayerActions(IGameInteropProvider interop,IFramework framework,IClientState client,IObjectTable objects,ITargetManager targets,IPartyList party,IDataManager data,IPluginLog log,ChatSources sources)
    {
        this.framework=framework;this.client=client;this.objects=objects;this.targets=targets;this.party=party;this.data=data;this.log=log;this.sources=sources;
        client.Logout+=OnLogout;
        try
        {
            if((nint)RaptureLogModule.MemberFunctionPointers.AddMsgSourceEntry==0)throw new InvalidOperationException("RaptureLogModule.AddMsgSourceEntry not resolved");
            hook=interop.HookFromAddress<RaptureLogModule.Delegates.AddMsgSourceEntry>((nint)RaptureLogModule.MemberFunctionPointers.AddMsgSourceEntry,OnSource);
            hook.Enable();
        }
        catch(Exception error){hook=null;log.Warning(error,"The chat's sender identifiers could not be read (maybe after a game update): the game's entries of a player's menu that need them stay off.");}
    }

    public bool Available=>!failed;

    private void OnSource(RaptureLogModule* module,ulong contentId,ulong accountId,int messageIndex,ushort worldId,ushort chatType)
    {
        hook!.Original(module,contentId,accountId,messageIndex,worldId,chatType);
        // X-662 M2 (0.1606, review of 0.1605): while Tensho is cut (PvP, ranked contents, R03.2) nothing is read; else the game's log line
        // only for the message that waits for its identifiers (ResolveRead calls Line at most once, after the cheap checks).
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut){sources.Skip();return;}
        try{Said(sources.ResolveRead(contentId,accountId,worldId,chatType,()=>Line(module,messageIndex),module->LocalPlayerContentId),chatType);}
        catch(Exception){}
    }

    /// <summary>
    /// X-660 (0.1603): the game's own log line at the index the game gives with the identifiers (RaptureLogModule.GetLogMessageDetail of
    /// FFXIVClientStructs): its kind and its sender's player link; null when the game has no line there.
    /// </summary>
    private static ChatLogLine? Line(RaptureLogModule* module,int index)
    {
        if(index<0 || !module->GetLogMessageDetail(index,out var sender,out _,out var kind,out _,out _,out _))return null;
        var player=Dalamud.Game.Text.SeStringHandling.SeString.Parse(sender).Payloads.OfType<Dalamud.Game.Text.SeStringHandling.Payloads.PlayerPayload>().FirstOrDefault();
        return new ChatLogLine(kind,player?.PlayerName ?? "",(ushort)(player?.World.RowId ?? 0));
    }

    // X-660 (0.1603): each outcome said once per session (outcome and kind only, never a name nor an identifier), so a game test shows
    // whether the identifiers reach the messages or why not.
    private readonly bool[] matchSaid=new bool[Enum.GetValues<ChatSourceMatch>().Length];
    private void Said(ChatSourceMatch match,ushort chatType)
    {
        if(match==ChatSourceMatch.NotWaiting || matchSaid[(int)match])return;
        matchSaid[(int)match]=true;
        log.Information("Chat window, a sender's identifiers: {0} (kind {1}).",match,chatType&ChatSources.KindMask);
    }

    /// <summary>The client's text of an Addon row (empty: not found).</summary>
    public string Text(uint row)=>data.GetExcelSheet<Lumina.Excel.Sheets.Addon>().GetRowOrDefault(row)?.Text.ExtractText() ?? "";

    /// <summary>The character of that player in range (name and home world), or null.</summary>
    public IPlayerCharacter? InRange(ChatSource source)
        =>objects.OfType<IPlayerCharacter>().FirstOrDefault(character=>character.Name.TextValue==source.Name && character.HomeWorld.RowId==source.World);

    /// <summary>A party invite can be asked: no party, or you lead it, and the player is not in it.</summary>
    public bool CanInvite(ChatSource source)
    {
        if(party.Length==0)return source.ContentId!=0 || CurrentWorld()==source.World;
        var leader=party[(int)party.PartyLeaderIndex];
        if(leader is null || (ulong)leader.ContentId!=PlayerContentId())return false;
        if(party.Any(member=>member.Name.TextValue==source.Name && member.World.RowId==source.World))return false;
        return source.ContentId!=0 || CurrentWorld()==source.World;
    }
    private ushort CurrentWorld()=>(ushort)(objects.LocalPlayer?.CurrentWorld.RowId ?? 0);
    private static ulong PlayerContentId()=>FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance()->ContentId;

    /// <summary>The entry the player clicked, done at the next framework tick with the character loaded; nothing otherwise.</summary>
    public void Run(ChatPlayerAction action,ChatSource source,ChatChannel channel)
    {
        if(failed)return;
        framework.RunOnTick(()=>
        {
            if(!client.IsLoggedIn || objects.LocalPlayer is null)return;
            try{Do(action,source,channel);}
            catch(Exception error){failed=true;log.Warning(error,"A game entry of a player's menu (chat window) failed (maybe after a game update): those entries are off until the plugin reloads.");}
        });
    }

    private void Do(ChatPlayerAction action,ChatSource source,ChatChannel channel)
    {
        switch(action)
        {
            case ChatPlayerAction.Tell:
            case ChatPlayerAction.Reply when channel is ChatChannel.TellIn or ChatChannel.TellOut:
                if(source.ContentId==0)return;
                var world=data.GetExcelSheet<Lumina.Excel.Sheets.World>().GetRowOrDefault(source.World)?.Name.ExtractText() ?? "";
                var name=Utf8String.FromString(source.Name);var worldName=Utf8String.FromString(world);
                try{RaptureShellModule.Instance()->SetContextTellTarget(name,worldName,source.World,source.AccountId,source.ContentId,0,true);}
                finally{name->Dtor(true);worldName->Dtor(true);}
                break;
            case ChatPlayerAction.Reply:
                if(ChatPlayerMenu.ReplyChannel(channel) is { } mode)RaptureShellModule.Instance()->ChangeChatChannel(mode,0,null,true);
                break;
            case ChatPlayerAction.Invite:
                var invite=InfoProxyPartyInvite.Instance();
                if(source.World==CurrentWorld())invite->InviteToParty(source.ContentId,source.Name,source.World);
                else if(source.ContentId!=0)invite->InviteToPartyContentId(source.ContentId,source.World);
                break;
            case ChatPlayerAction.Plate:
                if(source.ContentId!=0)AgentCharaCard.Instance()->OpenCharaCard(source.ContentId);
                break;
            case ChatPlayerAction.Mute:
                if(source.ContentId!=0 && source.AccountId!=0)AgentMutelist.Instance()->Add(source.AccountId,source.ContentId,source.Name,(short)source.World);
                break;
            case ChatPlayerAction.Target:
                if(InRange(source) is { } character)targets.Target=character;
                break;
            case ChatPlayerAction.GameMenu:
                if(InRange(source) is { } shown)AgentHUD.Instance()->OpenContextMenuFromTarget((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)shown.Address);
                break;
        }
    }

    /// <summary>The senders are another character's business at logout: dropped, as the messages are.</summary>
    private void OnLogout(int type,int code)=>sources.Clear();

    public void Dispose(){client.Logout-=OnLogout;hook?.Dispose();hook=null;sources.Clear();}
}
