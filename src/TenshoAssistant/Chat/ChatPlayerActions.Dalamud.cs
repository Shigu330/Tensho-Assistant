using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Game.Config;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
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
internal sealed unsafe class ChatPlayerActions : IDisposable,IChatLineData
{
    private readonly IFramework framework;private readonly IClientState client;private readonly IObjectTable objects;private readonly ITargetManager targets;
    private readonly IPartyList party;private readonly IDataManager data;private readonly IPluginLog log;private readonly ChatSources sources;
    private readonly IGameConfig? config;
    // X-733 (0.1734): the game's words of a meal's bonuses (Addon 1016 and 1017), evaluated as the game does (null: none, as outside the game).
    private readonly ISeStringEvaluator? strings;
    private bool disposed;
    private Hook<RaptureLogModule.Delegates.AddMsgSourceEntry>? hook;
    private bool failed;

    public ChatPlayerActions(IGameInteropProvider interop,IFramework framework,IClientState client,IObjectTable objects,ITargetManager targets,IPartyList party,IDataManager data,IPluginLog log,ChatSources sources,IGameConfig? config=null,ISeStringEvaluator? strings=null)
    {
        this.framework=framework;this.client=client;this.objects=objects;this.targets=targets;this.party=party;this.data=data;this.log=log;this.sources=sources;this.config=config;this.strings=strings;
        client.Logout+=OnLogout;
        // X-690 (0.1666): each job's role, read once from the ClassJob sheet; the jobs the game shows, once a second.
        try{sources.Jobs.SetRoles(data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().Select(row=>(row.RowId,row.Role)));}
        catch(Exception error){log.Warning(error,"Chat window: the jobs' roles could not be read (maybe after a game update): no role colour.");}
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnJobsTick);
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

    // X-473 (0.1715): the statuses' tooltips read once each, on hover (the Status sheet, the client's language).
    private readonly Dictionary<uint,ChatStatusTip?> statuses=[];
    /// <summary>X-473 (0.1715): a status the game linked in a message: its icon, name and description as the game's tooltip gives them (texts
    /// without the game's formatting); null for row 0 or a row without a name (never a tooltip made up).</summary>
    public ChatStatusTip? Status(uint id)
    {
        if(id==0)return null;
        if(statuses.TryGetValue(id,out var known))return known;
        var tip=data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(id) is { } status && status.Name.ExtractText().Trim() is { Length:>0 } name
            ? new ChatStatusTip(status.Icon,name,status.Description.ExtractText().Trim()):null;
        if(statuses.Count<2000)statuses[id]=tip;
        return tip;
    }

    // X-473 (0.1720): the actions by name (Action and ActionTransient sheets, the client's language), made once on a thread of their own: no
    // tooltip until they are ready, none at all if they could not be read. X-734 (0.1734): with the combat lines' templates (LogMessage) that
    // say where a line names one (ChatActionLines), made at the first message the chat window copies (Prepare), before any combat line.
    private Task<ChatActionData>? actionTips;
    private sealed record ChatActionData((Dictionary<string,ChatStatusTip> Players,Dictionary<string,ChatStatusTip> All) Tips,ChatActionLines? Lines);
    /// <summary>X-473 (0.1720): an action a combat line names, as the game's tooltip shows it; null when the name is not one action for sure
    /// (ChatActionTips) or the actions are not read yet.</summary>
    public ChatStatusTip? Action(string name,bool byPlayer)
    {
        actionTips??=Task.Run(ActionTips);
        return actionTips.IsCompletedSuccessfully ? ChatActionTips.Find(actionTips.Result.Tips,name,byPlayer):null;
    }

    /// <summary>X-734 (0.1734): starts reading the actions' names and the lines' templates, once.</summary>
    public void Prepare()=>actionTips??=Task.Run(ActionTips);

    /// <summary>X-734 (0.1734): where the last text of a combat line of <paramref name="kind"/> names an action; null when none, or not read yet.</summary>
    public (int Start,int Length)? ActionIn(int kind,string text)
        =>actionTips is { IsCompletedSuccessfully:true } ready && ready.Result.Lines is { } lines ? lines.Find(kind,text):null;

    private ChatActionData ActionTips()
    {
        try
        {
            var transient=data.GetExcelSheet<Lumina.Excel.Sheets.ActionTransient>();
            // X-734 (0.1734): the general actions (Sprint, Teleport, Return…) are the player's, with the GeneralAction sheet's description
            // when ActionTransient has none (Teleport: none there, 98 characters here, read on 21/11).
            var general=data.GetExcelSheet<Lumina.Excel.Sheets.GeneralAction>().Where(row=>row.Action.RowId!=0).GroupBy(row=>row.Action.RowId)
                .ToDictionary(group=>group.Key,group=>group.First().Description.ExtractText().Trim());
            var rows=data.GetExcelSheet<Lumina.Excel.Sheets.Action>().Where(row=>!row.Name.IsEmpty)
                .Select(row=>new ChatActionRow(row.RowId,row.Name.ExtractText(),row.Icon,row.IsPlayerAction,row.IsPvP,
                    transient.GetRowOrDefault(row.RowId)?.Description.ExtractText().Trim() is { Length:>0 } described ? described:general.GetValueOrDefault(row.RowId,""),
                    row.ClassJobCategory.RowId,general.ContainsKey(row.RowId))).ToList();
            var tips=ChatActionTips.Build(rows);
            var lines=Lines(rows);
            log.Information("Chat window, the actions' names read for their tooltips: {0} rows, {1} names of the player's actions, {2} names in all; {3} combat kinds whose lines can end with an action's name.",
                rows.Count,tips.Players.Count,tips.All.Count,lines?.Kinds ?? 0);
            return new(tips,lines);
        }
        catch(Exception error)
        {
            log.Warning(error,"Chat window, the actions could not be read (maybe after a game update): no action tooltip.");
            return new((new Dictionary<string,ChatStatusTip>(),new Dictionary<string,ChatStatusTip>()),null);
        }
    }

    /// <summary>X-734 (0.1734): the combat lines' templates (LogMessage, the client's language) and the actions' names; null when unreadable
    /// (no action is then found in a line).</summary>
    private ChatActionLines? Lines(IEnumerable<ChatActionRow> rows)
    {
        try
        {
            var templates=data.GetExcelSheet<Lumina.Excel.Sheets.LogMessage>().Where(row=>ChatChannels.From((int)row.LogKind.RowId)==ChatChannel.Combat)
                .Select(row=>((int)row.LogKind.RowId,row.Text.ToMacroString())).ToList();
            return new ChatActionLines(templates,rows.Select(row=>row.Name),data.Language!=Dalamud.Game.ClientLanguage.Japanese);
        }
        catch(Exception error)
        {
            log.Warning(error,"Chat window, the combat lines' templates could not be read (maybe after a game update): no action named in a line.");
            return null;
        }
    }

    // X-733 (0.1734): a meal's or a medicine's status looked for on the one who gained it, at its line and in the frames after (the game may
    // print the line first); each outcome said once in the log. A failure turns it off until the plugin reloads. X-756 (0.1739): compared
    // with the Param held at the line (ChatStatusWatch): a meal eaten over another one still held is never taken for the older one.
    private readonly HashSet<string> statusSaid=[];
    private bool statusFailed;
    /// <summary>X-733 (0.1734): reads into <paramref name="param"/> the Param of <paramref name="status"/> on the player named (name and home
    /// world), or on you (<paramref name="name"/> null): now, or in the next frames (on the game's thread); another player's half a second at
    /// most after the line (X-762, D-208).</summary>
    public void StatusParam(ChatStatusParam param,uint status,string? name,ushort world)=>ReadStatus(param,status,name,world,0,0,Environment.TickCount64,false);

    private void ReadStatus(ChatStatusParam param,uint status,string? name,ushort world,int frame,ushort atLine,long lineAt,bool fresh)
    {
        if(disposed || statusFailed || TenshoAssistant.Competitive.CompetitiveGate.Cut || !client.IsLoggedIn)return;
        try
        {
            var elapsed=Environment.TickCount64-lineAt;
            // X-762 (review of 0.1746, I2; D-208, the owner's choice of 21/11 at about 8:15 pm): another player's status is read half a second
            // at most after its line (what D-207 was asked on); past it, never read again: no bonus rather than an older meal.
            if(name is not null && !ChatStatusWatch.OtherMayRead(elapsed)){var end=ChatStatusWatch.OtherEnd(atLine,fresh);if(end.Value!=0)param.Value=end.Value;StatusSaid(end.How);return;}
            var character=name is null ? objects.LocalPlayer:objects.OfType<IPlayerCharacter>().FirstOrDefault(found=>found.Name.TextValue==name && found.HomeWorld.RowId==world);
            if(character is null){StatusSaid("the one who gained it is not in range");return;}
            ushort seen=0;float remaining=0;var extended=false;
            foreach(var held in character.StatusList)
            {
                if(seen==0 && held.StatusId==status && held.Param!=0){seen=held.Param;remaining=held.RemainingTime;}
                if(ChatStatusWatch.ExtendsMeals(held.StatusId))extended=true;
            }
            if(frame==0)atLine=seen;
            // X-766 (review of 0.1751, X-764 M2; D-211, the owner's choice of 21/11 at about 10:30 pm, R04): another player's Well Fed status
            // held at its line is the line's when its remaining time is the meal's full duration (put on just before the line).
            if(frame==0 && name is not null && status==Craft.CraftConsumables.WellFed && seen!=0)fresh=MealPutOnAtLine(seen,remaining,extended);
            var read=name is null ? ChatStatusWatch.Next(atLine,seen,frame,elapsed):ChatStatusWatch.Other(atLine,seen,elapsed,fresh);
            if(read.Over){if(read.Value!=0)param.Value=read.Value;StatusSaid(read.How);if(read.Value!=0 && read.Value!=atLine)StatusLate(frame,elapsed);return;}
            framework.RunOnTick(()=>ReadStatus(param,status,name,world,frame+1,atLine,lineAt,fresh),delayTicks:1);
        }
        catch(Exception error)
        {
            statusFailed=true;
            log.Warning(error,"Chat window, a meal's status could not be read (maybe after a game update): its tooltip shows the status's description only, until the plugin reloads.");
        }
    }

    // X-766 (D-211): each meal's full duration by its Param (ItemAction, read once); null when unreadable (then no meal is taken as put on
    // at its line: as before). The gap between the full duration and the remaining time at the line, the closest seen, is said in the log:
    // what ChatStatusWatch.FreshSeconds must cover, measured in game.
    private IReadOnlyDictionary<ushort,int>? mealDurations;
    private bool mealDurationsRead;
    private float mealGapSaid=float.MaxValue;
    private bool MealPutOnAtLine(ushort param,float remaining,bool extended)
    {
        if(!mealDurationsRead)
        {
            mealDurationsRead=true;
            try
            {
                mealDurations=ChatStatusWatch.MealDurations(data.GetExcelSheet<Lumina.Excel.Sheets.ItemAction>().Where(row=>row.Data.Count>=3 && row.DataHQ.Count>=3)
                    .SelectMany(row=>new[]{((uint)row.Data[0],(uint)row.Data[1],(uint)row.Data[2],false),((uint)row.DataHQ[0],(uint)row.DataHQ[1],(uint)row.DataHQ[2],true)}));
                log.Information("Chat window, the meals' full durations read for another player's meal: {0} Params, {1}.",mealDurations.Count,
                    string.Join(", ",mealDurations.Values.GroupBy(seconds=>seconds).OrderBy(group=>group.Key).Select(group=>$"{group.Count()} of {group.Key} s")));
            }
            catch(Exception error){log.Warning(error,"Chat window, the meals' durations could not be read (maybe after a game update): another player's meal put on before its line shows no bonus.");}
        }
        var full=mealDurations?.GetValueOrDefault(param) ?? 0;
        if(full>0 && !extended && Math.Abs(full-remaining)<mealGapSaid)
        {
            mealGapSaid=Math.Abs(full-remaining);
            log.Information("Chat window, another player's meal held at its line {0:F1} s from its full duration ({1} s): the closest seen since the plugin loaded (taken as put on just before its line within {2} s).",full-remaining,full,ChatStatusWatch.FreshSeconds);
        }
        return ChatStatusWatch.PutOnAtLine(remaining,full,extended);
    }

    private void StatusSaid(string how)
    {
        if(statusSaid.Add(how))log.Information("Chat window, a meal's or a medicine's status read for its line's tooltip: {0}.",how);
    }

    // X-760 (0.1744, review of 0.1742, M2): the longest delay seen between a line and its status put on, said in the log each time it grows:
    // what the watch must cover (ChatStatusWatch.WatchMs), measured in game.
    private long statusLateMs=-1;
    private void StatusLate(int frame,long elapsed)
    {
        if(elapsed<=statusLateMs)return;
        statusLateMs=elapsed;
        log.Information("Chat window, a meal's or a medicine's status put on {0} frame(s) ({1} ms) after its line: the longest seen since the plugin loaded (the watch lasts {2} ms at least for your own status, {3} ms at most for another player's).",frame,elapsed,ChatStatusWatch.WatchMs,ChatStatusWatch.OtherMs);
    }

    // X-733 (0.1734): the lines of a meal's or a medicine's tooltip, made once per status and Param (ItemFood and BaseParam sheets, Addon 1016
    // and 1017, the client's language, evaluated by Dalamud as the game does); null when they cannot be made (the description then).
    private readonly Dictionary<(uint,ushort),IReadOnlyList<string>?> foods=[];
    /// <summary>X-733 (0.1734): what the game's tooltip of a meal's or a medicine's status shows for this Param; null when not known.</summary>
    public IReadOnlyList<string>? FoodLines(uint status,ushort param)
    {
        if(param==0 || strings is null || !ChatFoodTip.Holds(status))return null;
        if(foods.TryGetValue((status,param),out var known))return known;
        IReadOnlyList<string>? lines=null;
        try
        {
            var (row,hq)=ChatFoodTip.Row(param);
            if(data.GetExcelSheet<Lumina.Excel.Sheets.ItemFood>().GetRowOrDefault(row) is { } food)
                lines=ChatFoodTip.Lines(status,hq,food.EXPBonusPercent,
                    food.Params.Where(bonus=>bonus.BaseParam.RowId!=0).Select(bonus=>new ChatFoodBonus(bonus.BaseParam.ValueNullable?.Name.ExtractText() ?? "",bonus.IsRelative,bonus.Value,bonus.Max,bonus.ValueHQ,bonus.MaxHQ)).ToList(),
                    exp=>strings.EvaluateFromAddon(1017,[new((uint)exp)]).ExtractText(),
                    (relative,value,max)=>strings.EvaluateFromAddon(1016,[new(relative ? 1u:0u),new((uint)value),new((uint)max)]).ExtractText()) is { Count:>0 } made ? made:null;
        }
        catch(Exception error){log.Warning(error,"Chat window, a meal's bonuses could not be read (maybe after a game update): its tooltip shows the status's description.");}
        if(foods.Count<200)foods[(status,param)]=lines;
        return lines;
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
                if(ChatPlayerMenu.ReplyChannel(channel) is { } mode)RaptureShellModule.Instance()->ChangeChatChannel(mode,ChatPlayerMenu.ReplyLinkshell(channel),null,true);
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
            // X-699 (0.1688): friend request, blacklist and report near you open the game's own menu of that character, where they are
            // (docs/evidence/0.1688-chat-player-menu-far.md); far away they are greyed, so never run.
            case ChatPlayerAction.GameMenu:
            case ChatPlayerAction.Friend:
            case ChatPlayerAction.Blacklist:
            case ChatPlayerAction.Report:
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

    // X-690 (0.1666): the jobs the game shows now (party, alliance, nearby), once a second, memory only; nothing during a cut (R03.2).
    private long jobsAt=-100_000;
    private bool jobsSaid,allianceSaid;
    private void OnJobsTick(IFramework _)
    {
        var now=Environment.TickCount64;
        if(now-jobsAt<1000)return;
        jobsAt=now;
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut || !client.IsLoggedIn || objects.LocalPlayer is null){sources.Jobs.Clear();return;}
        try
        {
            var players=new List<(string,ushort,uint)>();
            foreach(var member in party)players.Add((member.Name.TextValue,(ushort)member.World.RowId,member.ClassJob.RowId));
            players.AddRange(AllianceJobs());
            foreach(var character in objects.OfType<IPlayerCharacter>())players.Add((character.Name.TextValue,(ushort)character.HomeWorld.RowId,character.ClassJob.RowId));
            sources.Jobs.Replace(players);
            sources.Jobs.SetColours(Colour(UiConfigOption.LogColorRoleTank),Colour(UiConfigOption.LogColorRoleHealer),Colour(UiConfigOption.LogColorRoleDPS));
            if(!jobsSaid){jobsSaid=true;log.Information("Chat window, the jobs the game shows: {0} players, first read (memory only).",sources.Jobs.Count);}
        }
        catch(Exception error)
        {
            sources.Jobs.Clear();
            if(!jobsSaid){jobsSaid=true;log.Warning(error,"Chat window, the players' jobs could not be read (said once): no job icon nor role colour.");}
        }
    }

    private uint? Colour(UiConfigOption option)
    {
        try{return config is not null && config.TryGet(option,out uint value) ? value:null;}
        catch(InvalidOperationException){return null;}
    }

    /// <summary>X-690 (0.1666): the alliance's other members (GroupManager, the game's own list: name, home world, job); none outside an alliance.</summary>
    private List<(string,ushort,uint)> AllianceJobs()
    {
        var found=new List<(string,ushort,uint)>();
        try
        {
            var manager=GroupManager.Instance();
            if(manager==null || !manager->MainGroup.IsAlliance)return found;
            foreach(ref readonly var member in manager->MainGroup.AllianceMembers)
                if(member.ClassJob!=0 && member.HomeWorld!=0 && member.NameString is { Length:>0 } name)found.Add((name,member.HomeWorld,member.ClassJob));
        }
        catch(Exception error)
        {
            if(!allianceSaid){allianceSaid=true;log.Warning(error,"Chat window, the alliance's jobs could not be read (maybe after a game update; said once).");}
        }
        return found;
    }

    public void Dispose(){disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnJobsTick);client.Logout-=OnLogout;hook?.Dispose();hook=null;sources.Clear();}
}
