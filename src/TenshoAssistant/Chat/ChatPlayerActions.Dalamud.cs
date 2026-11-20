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
        // X-679 (0.1658): the first call after the load is said once, with its delay (a measure in the log); a failure is said once, no longer
        // swallowed without a word.
        if(!firstSaid){firstSaid=true;log.Information("Chat window, the game's sender identifiers: first call {0:0.0} s after the load.",loaded.Elapsed.TotalSeconds);}
        try{Said(sources.ResolveRead(contentId,accountId,worldId,chatType,()=>Line(module,messageIndex),module->LocalPlayerContentId),chatType);}
        catch(Exception error)
        {
            if(!errorSaid){errorSaid=true;log.Warning(error,"Chat window, a sender's identifiers could not be read (said once).");}
        }
    }

    private readonly System.Diagnostics.Stopwatch loaded=System.Diagnostics.Stopwatch.StartNew();
    private bool firstSaid,errorSaid;

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
    // whether the identifiers reach the messages or why not. X-679 (0.1658): « NotWaiting » too (no message waited for them).
    private readonly bool[] matchSaid=new bool[Enum.GetValues<ChatSourceMatch>().Length];
    private void Said(ChatSourceMatch match,ushort chatType)
    {
        if(matchSaid[(int)match])return;
        matchSaid[(int)match]=true;
        log.Information("Chat window, a sender's identifiers: {0} (kind {1}).",match,chatType&ChatSources.KindMask);
    }

    // X-679 (0.1660): where a menu found its sender, said once each per session (how and kind only, never a name): « Named » for a new
    // message means the chat event gave no player link to note (the message was never waiting for its identifiers).
    private readonly bool[] foundSaid=new bool[Enum.GetValues<ChatSourceFound>().Length];
    public void SaidFound(ChatSourceFound how,int kind)
    {
        if(foundSaid[(int)how])return;
        foundSaid[(int)how]=true;
        log.Information("Chat window, a player's menu found its sender: {0} (kind {1}).",how,kind);
    }

    // X-679 (0.1660): a world's number from its name (the World sheet, read once); 0 when unknown.
    private Dictionary<string,ushort>? worldIds;
    public ushort WorldId(string name)
    {
        if(name.Length==0)return 0;
        worldIds??=data.GetExcelSheet<Lumina.Excel.Sheets.World>().Where(row=>!row.Name.IsEmpty)
            .GroupBy(row=>row.Name.ExtractText(),StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>(ushort)group.First().RowId,StringComparer.OrdinalIgnoreCase);
        return worldIds.GetValueOrDefault(name.Trim());
    }
    /// <summary>X-679 (0.1660): the world you are on (the game writes no world for a player of this world).</summary>
    public ushort CurrentWorldId()=>CurrentWorld();

    /// <summary>The client's text of an Addon row (empty: not found).</summary>
    public string Text(uint row)=>data.GetExcelSheet<Lumina.Excel.Sheets.Addon>().GetRowOrDefault(row)?.Text.ExtractText() ?? "";
    /// <summary>X-455 E3 (0.1617): a world's name from the client's World sheet; empty for none or an unknown one.</summary>
    public string World(ushort id)=>id==0 ? "":data.GetExcelSheet<Lumina.Excel.Sheets.World>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";

    /// <summary>X-473 (0.1627, R04): the job and level the game shows of that player: a member of your party (its list), else the character
    /// nearby; null otherwise. Read when the mouse is on the name (the game's thread draws the window), never kept.</summary>
    public (string Job,int Level)? Seen(ChatSource source)
    {
        foreach(var member in party)
            if(member.Name.TextValue==source.Name && member.World.RowId==source.World && member.Level>0)
                return (member.ClassJob.ValueNullable?.Name.ExtractText() ?? "",member.Level);
        return InRange(source) is { } character && character.Level>0 ? (character.ClassJob.ValueNullable?.Name.ExtractText() ?? "",character.Level):null;
    }

    // X-473 (0.1627): the items' descriptions read once each, on hover (the Item sheet, the client's language).
    private readonly Dictionary<uint,string> descriptions=[];
    /// <summary>X-473 (0.1627): an item's description as the game's tooltip gives it (its text, without the game's formatting); empty when none.</summary>
    public string Description(uint item)
    {
        if(descriptions.TryGetValue(item,out var known))return known;
        var text=data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(item)?.Description.ExtractText().Trim() ?? "";
        if(descriptions.Count<2000)descriptions[item]=text;
        return text;
    }

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
        // X-681 (0.1660): never during a cut (PvP, ranked contents: R03.2), checked at the click and at the tick.
        if(failed || TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        framework.RunOnTick(()=>
        {
            if(!client.IsLoggedIn || objects.LocalPlayer is null || TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
            try{Do(action,source,channel);}
            catch(Exception error){failed=true;log.Warning(error,"A game entry of a player's menu (chat window) failed (maybe after a game update): those entries are off until the plugin reloads.");}
        });
    }

    private void Do(ChatPlayerAction action,ChatSource source,ChatChannel channel)
    {
        switch(action)
        {
            // X-681 (0.1660, the user's request of 20/11): by name and home world too, without the game's identifiers (0, as for a tell
            // typed « /tell Name@World »: docs/evidence/0.1660-tell-by-name.md); only the typing box is set, nothing is sent (R03.3).
            case ChatPlayerAction.Tell:
            case ChatPlayerAction.Reply when channel is ChatChannel.TellIn or ChatChannel.TellOut:
                if(source.Name.Length==0 || source.World==0)return;
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
            // X-680 (0.1662): the player's party finder listing, as the game's menu opens it (AgentLookingForGroup.OpenListingByContentId of
            // FFXIVClientStructs); a player recruiting nothing: the game says so.
            case ChatPlayerAction.PartyFinder:
                if(source.ContentId!=0)AgentLookingForGroup.Instance()->OpenListingByContentId(source.ContentId);
                break;
        }
    }

    /// <summary>The senders are another character's business at logout: dropped, as the messages are.</summary>
    private void OnLogout(int type,int code)=>sources.Clear();

    public void Dispose(){client.Logout-=OnLogout;hook?.Dispose();hook=null;sources.Clear();}
}
