using System.Text;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.650 (phase 5, C33 and T5): the in-game readings of the global plan, written to the log. Temporary: each part goes once its point is coded.
/// Developer mode only, read on the framework thread, nothing changed in the game, and a limit on the lines of each part.
/// </summary>
public sealed unsafe class ReadingCapture : IDisposable
{
    private const long SlowPartEvery=TimeSpan.TicksPerMillisecond*500;
    private readonly IFramework framework;
    private readonly IObjectTable objects;
    private readonly ICondition condition;
    private readonly IGameGui gui;
    private readonly IChatGui chat;
    private readonly Func<bool> enabled;
    private readonly IPluginLog log;
    private readonly Dictionary<string,int> lines=[];
    private readonly Dictionary<int,int> messageKinds=[];
    private string mapMarkers="";
    private string statuses="",synthesis="",workshop="",questMarkers="",monsterEvents="",eventMarkers="",leves="",retainer="",airship="",islandState="",islandAgenda="";
    private bool fishing,disposed,warned;
    // X-73.13 (0.869): the fishing line's state (cast, bite, how long, the player's animation at the bite), aetherial reduction results, items asked at a hand-in.
    private int fishState=-1;private long castAt=-100_000;private string purify="",npcTrade="";
    // X-75.7 (0.881, docs/project/audits/0.874-probe-coverage.md): the windows whose numbers would replace community data (spearfishing size and speed, airship
    // destinations and results, submarine results, overmeld rate, spiritbond extraction, letters' attachments), the Fashion Report preview and best score
    // (dyes, evaluations), and the island's wild animals. Numbers only: no text is written, so no name (a letter's sender) can reach the log.
    private static readonly string[] ProbedWindows=["SpearFishing","AirShipExploration","AirShipExplorationResult","SubmarineExplorationResult","MateriaAttachDialog",
        "Materialize","LetterViewer"];
    private readonly Dictionary<string,string> windowValues=[];private string fashionSeen="",islandObjects="";
    private long slowAt;

    public ReadingCapture(IFramework framework,IObjectTable objects,ICondition condition,IGameGui gui,IChatGui chat,Func<bool> enabled,IPluginLog log)
    {
        this.framework=framework;this.objects=objects;this.condition=condition;this.gui=gui;this.chat=chat;this.enabled=enabled;this.log=log;
        // X-76 H10 (0.924): subscriptions owned by one RegistrationScope: a failure part-way undoes the ones already made; Dispose releases them in reverse order.
        try
        {
            registrations.Register(()=>TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate),()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate));
            registrations.Register(()=>chat.ChatMessageUnhandled+=OnMessage,()=>chat.ChatMessageUnhandled-=OnMessage);
        }
        catch{registrations.Dispose();throw;}
    }
    private readonly RegistrationScope registrations=new();

    /// <summary>Writes a line, up to the limit of its part (0.899: or of its own key, so that one window does not use up the others' lines).</summary>
    private void Line(string part,int limit,string text,string? key=null)
    {
        var counted=key ?? part;
        var count=lines.GetValueOrDefault(counted);
        if(count>=limit)return;
        lines[counted]=count+1;
        log.Information("Reading {0}: {1}",part,text);
    }

    private void OnUpdate(IFramework unused)
    {
        if(disposed || !enabled())return;
        try
        {
            var local=objects.LocalPlayer;
            if(local is null)return;
            var job=local.ClassJob.RowId;
            if(job is 16 or 17 or 18)ReadStatuses(local);
            var flag=condition[ConditionFlag.Fishing];
            if(flag!=fishing){fishing=flag;Line("fishing",200,$"fishing flag {flag}");}
            if(job==18)ReadFishingLine(local);
            var now=DateTime.UtcNow.Ticks;
            if(now-slowAt<SlowPartEvery)return;
            slowAt=now;
            ReadSynthesis();
            ReadWorkshop();
            ReadQuestMarkers();
            ReadMonsterEvents();
            ReadEventMarkers();
            ReadMapMarkers();
            ReadLeves();
            ReadRetainer();
            ReadAirships();
            ReadIsland();
            ReadPurify();
            ReadNpcTrade();
            ReadWindows();
            ReadFashion();
            ReadIslandObjects();
            ReadFields();
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A reading could not be made.");}
        }
    }

    // X-73.13 (0.869): each change of the fishing line's state (FishingEventHandler, held by EventFramework.EventHandlerModule: no handler id to find), the cast's
    // bait flags (lures, mooch, swimbait), the time since the cast, and at the bite the player's animation slots (where the bite strength ! / !! / !!! is believed to show).
    private void ReadFishingLine(Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter local)
    {
        var events=FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.Instance();
        if(events==null)return;
        var handler=events->EventHandlerModule.FishingEventHandler;
        if(handler==null)return;
        var state=(int)handler->State;
        if(state==fishState)return;
        fishState=state;
        var now=Environment.TickCount64;
        if(state is 1 or 12 && now-castAt>3_000)castAt=now;
        var text=new StringBuilder($"line state {state} ({handler->State}), bait flags {(int)handler->CurrentCastBaitFlags} ({handler->CurrentCastBaitFlags}), {(now-castAt)/1000.0:F1} s since cast");
        text.Append($", mooch {handler->CanMoochPreviousCatch}/{handler->CanMooch2PreviousCatch}");
        if(state==5)
        {
            var character=(FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)local.Address;
            var sequencer=&character->Timeline.TimelineSequencer;
            text.Append(", timelines");
            for(var slot=0;slot<sequencer->TimelineIds.Length;slot++)if(sequencer->TimelineIds[slot]!=0)text.Append($" {slot}:{sequencer->TimelineIds[slot]}");
        }
        Line("fishline",400,text.ToString());
    }

    private void ReadWindows()
    {
        foreach(var name in ProbedWindows)
        {
            var handle=gui.GetAddonByName(name);
            if(handle.IsNull || !handle.IsVisible){windowValues.Remove(name);continue;}
            var unit=(AtkUnitBase*)handle.Address;
            var values=new StringBuilder();
            AppendValues(values,unit,96);
            // 0.890 (X-73.18): the spearfishing shadows in sight (AddonSpearFishing.Fish), to check the catch's shadow against the window.
            if(name=="SpearFishing")
            {
                var spear=(AddonSpearFishing*)handle.Address;
                values.Append(" | slots");
                for(var slot=0;slot<spear->Fish.Length;slot++)
                {
                    var fish=spear->Fish[slot];
                    values.Append($" {slot}:{(fish.Available ? 1:0)}/{(int)fish.Size}/{fish.Speed}/{(fish.GuaranteedLarge ? 1:0)}/{(fish.InverseDirection ? 1:0)}");
                }
            }
            var all=values.ToString();
            if(windowValues.GetValueOrDefault(name)==all)continue;
            windowValues[name]=all;
            // 0.899 (the user's melding test wrote nothing: the spearfishing sessions had used the 160 lines of « addons »): a limit per window.
            Line("addons",160,$"{name}:{all}","addons/"+name);
        }
    }

    private void ReadFashion()
    {
        var manager=FFXIVClientStructs.FFXIV.Client.Game.FashionCheckManager.Instance();
        if(manager==null)return;
        var best=&manager->HighScore;
        var text=new StringBuilder($"preview theme {manager->Preview.WeeklyTheme}, high {manager->Preview.HighScore}, remaining {manager->Preview.Remaining}; theme {manager->Theme.WeeklyTheme}; best score {best->Score}, slots");
        for(var slot=0;slot<best->ItemIds.Length;slot++)
            if(best->ItemIds[slot]!=0)text.Append($" {slot}:{best->ItemIds[slot]}/{(slot<best->ItemEvaluations.Length ? best->ItemEvaluations[slot]:0)}/dye {(slot<best->Stain0Ids.Length ? best->Stain0Ids[slot]:0)},{(slot<best->Stain1Ids.Length ? best->Stain1Ids[slot]:0)}");
        var all=text.ToString();
        if(all==fashionSeen)return;
        fashionSeen=all;
        Line("fashion",40,all);
    }

    // X-90 (0.973, user request « Eureka: elemental level and magia allocation »; DEC-041: preparation, the player's own progress only, nothing of
    // the combat): the field states the game keeps (ClientStructs EurekaState, BozjaState, OccultCrescentState, members checked with tools/ApiProbe),
    // written on change in developer mode, to prove the fields before a page uses them.
    private string fieldState="";
    private void ReadFields()
    {
        var text="";
        var eurekaDirector=FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.GetPublicContentDirectorByType(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentDirectorType.Eureka);
        var bozjaDirector=FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.GetPublicContentDirectorByType(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentDirectorType.Bozja);
        var occultDirector=FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.GetPublicContentDirectorByType(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentDirectorType.OccultCrescent);
        if(eurekaDirector!=null)
        {
            var eureka=(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentEureka*)eurekaDirector;
            var state=eureka->State;
            text=$"eureka max {eureka->MaxElementalLevel}, exp {state.CurrentExperience}/{state.NeededExperience}, magia fire {state.Fire} ice {state.Ice} wind {state.Wind} earth {state.Earth} lightning {state.Lightning} water {state.Water}, magicite {state.Magicite}, aether {state.MagiaAether} charge {state.MagiaAetherCharge}";
        }
        else if(bozjaDirector!=null)
        {
            var bozja=(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentBozja*)bozjaDirector;
            var holster=new StringBuilder();
            foreach(var action in bozja->State.HolsterActions)if(action!=0)holster.Append(' ').Append(action);
            text=$"bozja exp {bozja->State.CurrentExperience}/{bozja->State.NeededExperience}, holster{holster}";
        }
        else if(occultDirector!=null)
        {
            var occult=(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentOccultCrescent*)occultDirector;
            var state=occult->State;
            var levels=new StringBuilder();
            foreach(var level in state.SupportJobLevels)levels.Append(' ').Append(level);
            text=$"occult knowledge {state.CurrentKnowledge}/{state.NeededKnowledge} sync {state.KnowledgeLevelSync}, support job {state.CurrentSupportJob} levels{levels}, silver {state.Silver} gold {state.Gold}";
        }
        if(text==fieldState)return;
        fieldState=text;
        if(text.Length>0)Line("fields",120,text);
    }
    private void ReadIslandObjects()
    {
        if((nint)MJIManager.StaticAddressPointers.ppInstance==0)return;
        var manager=MJIManager.Instance();
        if(manager==null || !manager->IsPlayerInSanctuary){islandObjects="";return;}
        var seen=objects.Where(actor=>actor.ObjectKind is Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && actor.BaseId!=0).Take(40)
            .Select(actor=>actor is Dalamud.Game.ClientState.Objects.Types.IBattleNpc npc ? $"{npc.BaseId}/{npc.NameId}/{npc.BattleNpcKind}":$"{actor.BaseId}")
            .Distinct().Order(StringComparer.Ordinal).ToArray();
        var all=string.Join(" ",seen);
        if(all==islandObjects)return;
        islandObjects=all;
        Line("islandobjects",40,all);
    }

    // X-73.13 (0.869): what aetherial reduction gives (AgentPurify: the item reduced, the results and their counts, the bonus flag).
    private void ReadPurify()
    {
        var agent=AgentPurify.Instance();
        if(agent==null)return;
        var text=new StringBuilder($"item {agent->ResultItemId}, bonus {agent->ResultBonus}, auto {agent->AutoItemId} left {agent->AutoItemsLeft}/{agent->AutoItemCount}, results");
        foreach(var result in agent->Results)if(result.ItemId!=0)text.Append($" {result.ItemId}x{result.Count}");
        var all=text.ToString();
        if(all==purify || agent->ResultItemId==0 && agent->AutoItemId==0)return;
        purify=all;
        Line("purify",60,all);
    }

    // X-73.13 (0.869): the items an NPC asks for at a hand-in (UIState.NpcTrade, the « Request » window), with the target's base id to find the quest.
    private void ReadNpcTrade()
    {
        var ui=FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
        if(ui==null)return;
        var requests=&ui->NpcTrade.Requests;
        var request=gui.GetAddonByName("Request");
        var text=new StringBuilder($"window {(request.IsNull ? "closed":request.IsVisible ? "shown":"hidden")}, target {objects.LocalPlayer?.TargetObject?.BaseId ?? 0}, items");
        for(var index=0;index<Math.Min((int)requests->Count,requests->Items.Length);index++)
        {
            var item=requests->Items[index];
            text.Append($" {item.ItemId}x{item.RequiredQuantity}{(item.WantHQ ? " HQ":"")}{(item.WantCollectible ? $" collectible {item.MinCollectibility}":"")}");
        }
        var all=text.ToString();
        if(all==npcTrade || requests->Count==0 && request.IsNull)return;
        npcTrade=all;
        Line("npctrade",60,all);
    }

    // C28, G2: the statuses of a gatherer, 568 Fisher's Intuition and 2891 Spearfisher's Intuition among them.
    private void ReadStatuses(Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter local)
    {
        var ids=string.Join(",",local.StatusList.Where(status=>status.StatusId!=0).Select(status=>$"{status.StatusId}:{status.Param}").OrderBy(id=>id,StringComparer.Ordinal));
        if(ids==statuses)return;
        statuses=ids;
        Line("statuses",200,string.Join(", ",local.StatusList.Where(status=>status.StatusId!=0).Select(status=>$"{status.StatusId} (param {status.Param}, {status.RemainingTime:F1} s)")));
    }

    // 0.671 (N-10): what the workshops' model still does not know: when the demand loads, the two grooves, the rest days and the crafts the game calls efficient.
    private void ReadIsland()
    {
        if((nint)MJIManager.StaticAddressPointers.ppInstance==0)return;
        var manager=MJIManager.Instance();
        if(manager==null || !manager->IsPlayerInSanctuary)return;
        var workshops=new StringBuilder();
        for(var index=0;index<4;index++)
        {
            var workshop=manager->IslandState.Workshops[index];
            workshops.Append($" {workshop.PlaceId}/{workshop.BuildingLevel}/{workshop.UnderConstruction}/{workshop.HoursToCompletion}");
        }
        var shifts=manager->SupplyAndDemandShifts;
        var filled=0;
        foreach(var shift in shifts)if(shift!=0)filled++;
        var landmarks=new StringBuilder();
        foreach(var landmark in manager->LandmarkPlacements)landmarks.Append($" {landmark.LandmarkId}/{landmark.UnderConstruction}");
        var state=$"rank {manager->IslandState.CurrentRank}, dirty {manager->DemandDirty}, popularity {manager->CurrentPopularity}/{manager->NextPopularity}, cycle {manager->CurrentCycleDay}, groove {manager->CurrentGroove}, rest {string.Join(".",manager->CraftworksRestDays.ToArray())}, shifts {filled}/{shifts.Length} first {shifts[1]:X2} {shifts[2]:X2}, workshops{workshops}, landmarks{landmarks}";
        if(state!=islandState){islandState=state;Line("island",200,state);}
        var agent=AgentMJICraftSchedule.Instance();
        if(agent==null || agent->Data==null)return;
        var data=agent->Data;
        var entries=new StringBuilder();
        var schedules=data->WorkshopSchedules;
        for(var index=0;index<schedules.Length;index++)
        {
            entries.Append($" w{index}:{schedules[index].NumEfficientCrafts}e");
            var entryData=schedules[index].EntryData;
            for(var entry=0;entry<Math.Min((int)schedules[index].NumScheduleEntries,entryData.Length);entry++)
                entries.Append($" {entryData[entry].CraftObjectId}@{entryData[entry].StartingSlot}+{entryData[entry].Duration}{(entryData[entry].Efficient ? "e":"")}");
        }
        var agenda=$"state {data->UpdateState}, groove {data->Groove}, rest {data->RestCycles:X}, shown {data->CycleDisplayed}, cycle {data->CycleInProgress}, hour {data->HourSinceCycleStart}, level {data->IslandLevel},{entries}";
        if(agenda!=islandAgenda){islandAgenda=agenda;Line("islandAgenda",200,agenda);}
    }

    // 0.654: what the game says of the accepted leves, to tell a started, a finished and an abandoned one.
    private void ReadLeves()
    {
        var quests=QuestManager.Instance();
        if(quests==null)return;
        var text=new StringBuilder();
        foreach(ref var leve in quests->LeveQuests)if(leve.LeveId!=0)text.Append($" {leve.LeveId} (sequence {leve.Sequence}, flags {leve.Flags}, hidden {leve.IsHidden}, priority {leve.IsPriority})");
        var all=$"{quests->NumAcceptedLeveQuests} accepted:{text}";
        if(all==leves)return;
        leves=all;
        Line("leves",60,all);
    }

    // C13: the HQ chance beside the quality, and every number of the window.
    // 0.786 (X-28, user request: count what the workshop already received): the game keeps it nowhere Tensho knows; the delivery window and the
    // material list are dumped whole, to learn their layout before anything is counted.
    private void ReadWorkshop()
    {
        var text=new StringBuilder();
        foreach(var name in new[]{"SubmarinePartsMenu","CompanyCraftMaterial","CompanyCraftRecipeNoteBook"})
        {
            var handle=gui.GetAddonByName(name);
            if(handle.IsNull || !handle.IsVisible)continue;
            var addon=(AtkUnitBase*)handle.Address;
            text.Append($" [{name}: {addon->AtkValuesCount} values");
            AppendValues(text,addon,400,texts:true);
            text.Append(']');
        }
        var agent=AgentCompanyCraftMaterial.Instance();
        if(agent!=null && agent->IsAgentActive())
            text.Append($" [material agent: result {agent->ResultItem}, supply {string.Join(",",agent->SupplyItems.ToArray())}, selected {agent->SelectedSupplyItemIndex}]");
        var now=text.ToString();
        if(now==workshop)return;
        workshop=now;
        if(now.Length>0)Line("workshop",60,now);
    }

    // 0.792 (X-32, user request: what opening and closing the game map unblocked): a leve's area was missing from AgentMap.EventMarkers until the game map
    // was opened in range (docs/evidence/0.654-leve-area-memory.md). The game's own Map (UIState) also holds quest and leve markers: written here, with
    // whether the game map is open, to see if they are there while it is closed.
    // 0.797 (X-37): which event a nearby enemy belongs to (FATE, leve…), to tell a leve's targets; written when it changes, enemies with an event or a FATE only.
    // 0.825 (X-57, user report: a culinarian quest drawn out of the Upper Decks, its target right): every marker of the game map's list with its place and kind,
    // written when it changes (two markers for quest 2046 on map 11: icons 71141 and 71143).
    private void ReadEventMarkers()
    {
        var agent=AgentMap.Instance();
        if(agent==null)return;
        var count=agent->EventMarkers.LongCount;
        if(count is <0 or >256)return;
        var text=new StringBuilder($"open map {agent->CurrentMapId}, selected {agent->SelectedMapId}:");
        foreach(var marker in agent->EventMarkers)
            text.Append($" [{marker.IconId} map {marker.MapId} objective {marker.ObjectiveId} data {marker.DataId} type {marker.MarkerType} level {marker.LevelId} at {marker.Position.X:F1},{marker.Position.Z:F1} height {marker.Position.Y:F1} flags {marker.Flags:X2} state {marker.EventState} territory {marker.TerritoryTypeId}]");
        var now=text.ToString();
        if(now==eventMarkers)return;
        eventMarkers=now;
        Line("eventMarkers",60,now);
    }

    // 0.842 (X-67, user request: the game map's up and down arrows on an icon above or below): the game map window's own markers (AgentMap.MapMarkers, built while it is
    // open) with their flags and second icon, the player's height beside, written when it changes, to find which field carries the arrow. Markers with flags or a
    // second icon only, at most 60.
    private void ReadMapMarkers()
    {
        var agent=AgentMap.Instance();
        if(agent==null || agent->MapMarkerCount==0)return;
        var count=Math.Min((int)agent->MapMarkerCount,agent->MapMarkers.Length);
        // 0.844: the player's height no longer decides a new line (it wrote one every half second).
        var text=new StringBuilder($"map {agent->CurrentMapId}, {count} markers:");
        var shown=0;
        for(var index=0;index<count && shown<60;index++)
        {
            var marker=agent->MapMarkers[index].MapMarker;
            // Fixed icons (flags 0002, area links 0052) left out: quest and event icons (70000 and up), other flags, a second icon.
            if(marker.IconId<70000 && marker.IconFlags is 0 or 0x0002 or 0x0052 && marker.SecondaryIconId==0)continue;
            shown++;
            text.Append($" [icon {marker.IconId} second {marker.SecondaryIconId} flags {marker.IconFlags:X4} at {marker.X},{marker.Y} type {agent->MapMarkers[index].DataType} key {agent->MapMarkers[index].DataKey}]");
        }
        var now=text.ToString();
        if(now==mapMarkers)return;
        mapMarkers=now;
        Line("mapMarkers",60,now+$" (player height {(objects.LocalPlayer?.Position.Y ?? float.NaN):F1})");
    }

    private void ReadMonsterEvents()
    {
        var text=new StringBuilder();var count=0;
        foreach(var item in objects)
        {
            if(count>=30)break;
            if(item is not Dalamud.Game.ClientState.Objects.Types.IBattleNpc { CurrentHp:>0 } npc || npc.BattleNpcKind!=Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind.Combatant)continue;
            var native=(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)npc.Address;
            if(native==null || native->FateId==0 && native->EventId.Id==0)continue;
            count++;
            text.Append($" [{npc.Name.TextValue} name {npc.NameId} base {npc.BaseId}: fate {native->FateId}, event {native->EventId.Id:X8} content {native->EventId.ContentId} entry {native->EventId.EntryId}]");
        }
        var now=text.ToString();
        if(now==monsterEvents)return;
        monsterEvents=now;
        if(now.Length>0)Line("monsterEvents",60,now);
    }

    private void ReadQuestMarkers()
    {
        var map=FFXIVClientStructs.FFXIV.Client.Game.UI.Map.Instance();
        if(map==null)return;
        var text=new StringBuilder();
        void Marker(string kind,FFXIVClientStructs.FFXIV.Client.Game.UI.MarkerInfo* info)
        {
            var count=info->MarkerData.LongCount;
            if(info->ObjectiveId==0 && count==0)return;
            text.Append($" [{kind} {info->ObjectiveId} render {info->ShouldRender}, {count} data");
            if(count is <0 or >32){text.Append(" out of range]");return;}
            foreach(var data in info->MarkerData)
                text.Append($"; map {data.MapId} icon {data.IconId} level {data.LevelId} at {data.Position.X:F1},{data.Position.Z:F1} radius {data.Radius:F1} type {data.MarkerType}");
            text.Append(']');
        }
        var leves=map->LevequestMarkers;
        for(var index=0;index<leves.Length;index++)fixed(FFXIVClientStructs.FFXIV.Client.Game.UI.MarkerInfo* info=&leves[index])Marker("leve",info);
        Marker("active leve",&map->ActiveLevequestMarker);
        var quests=map->QuestMarkers;
        for(var index=0;index<quests.Length;index++)fixed(FFXIVClientStructs.FFXIV.Client.Game.UI.MarkerInfo* info=&quests[index])Marker("quest",info);
        if(text.Length==0)return;
        var agent=AgentMap.Instance();
        var events=agent==null ? -1:agent->EventMarkers.LongCount;
        var now=$"game map {(agent!=null && agent->IsAgentActive() ? "open":"closed")}, event markers {events};{text}";
        if(now==questMarkers)return;
        questMarkers=now;
        Line("questMarkers",80,now);
    }

    private void ReadSynthesis()
    {
        var handle=gui.GetAddonByName("Synthesis");
        if(handle.IsNull || !handle.IsVisible){synthesis="";return;}
        var addon=(AddonSynthesis*)handle.Address;
        var text=$"quality {Text(addon->CurrentQuality)} / {Text(addon->MaxQuality)}, HQ {Text(addon->HQPercentage)}";
        if(text==synthesis)return;
        synthesis=text;
        var values=new StringBuilder();
        AppendValues(values,&addon->AtkUnitBase,64);
        Line("synthesis",150,$"{text}; values:{values}");
    }

    // X-76 (step 2): a window's first values, written « index=value » (numbers and flags; texts only when asked), the same for every part above.
    private static void AppendValues(StringBuilder to,AtkUnitBase* unit,int max,bool texts=false)
    {
        for(var index=0;index<Math.Min((int)unit->AtkValuesCount,max) && unit->AtkValues!=null;index++)
        {
            var value=unit->AtkValues[index];
            if(value.Type==AtkValueType.UInt)to.Append($" {index}={value.UInt}");
            else if(value.Type==AtkValueType.Int)to.Append($" {index}={value.Int}");
            else if(value.Type==AtkValueType.Bool)to.Append($" {index}={value.Bool}");
            else if(texts && TenshoAssistant.UI.AtkValueRead.IsText(value.Type))to.Append($" {index}=\"{value.GetValueAsString()}\"");
        }
    }

    // I10: are the crystals and the gil of a retainer loaded while its inventory is open?
    private void ReadRetainer()
    {
        var open=gui.GetAddonByName("InventoryRetainer");
        var large=gui.GetAddonByName("InventoryRetainerLarge");
        if((open.IsNull || !open.IsVisible) && (large.IsNull || !large.IsVisible)){retainer="";return;}
        var text=$"gil {Container(InventoryType.RetainerGil)}; crystals {Container(InventoryType.RetainerCrystals)}";
        if(text==retainer)return;
        retainer=text;
        Line("retainer",30,text);
    }
    private static string Container(InventoryType type)
    {
        var manager=InventoryManager.Instance();
        var container=manager==null ? null:manager->GetInventoryContainer(type);
        if(container==null)return "missing";
        var used=0;var total=0L;
        if(container->IsLoaded && container->Size is >0 and <=64)
            for(var index=0;index<container->Size;index++)
            {
                var slot=container->GetInventorySlot(index);
                if(slot==null || slot->GetBaseItemId()==0)continue;
                used++;total+=slot->Quantity;
            }
        return $"loaded {container->IsLoaded}, size {container->Size}, used {used}, total {total}";
    }

    // T26: what the game keeps of an airship's voyage, in its logs and in the unnamed fields of its finds.
    private void ReadAirships()
    {
        var housing=HousingManager.Instance();
        if(housing==null || housing->WorkshopTerritory==null){airship="";return;}
        var data=&housing->WorkshopTerritory->Airship;
        var count=Math.Min((int)data->AirshipCount,data->Data.Length);
        var text=new StringBuilder($"{count} airships");
        for(var index=0;index<count;index++)
        {
            var found=data->Data[index].GatheredData;
            text.Append($"; airship {index + 1} return {data->Data[index].ReturnTime}, finds");
            for(var entry=0;entry<found.Length;entry++)
            {
                var item=found[entry];
                var raw=(uint*)&item;
                text.Append($" [{item.ItemIdPrimary}:{raw[6]},{raw[8]},{raw[10]} {item.ItemIdAdditional}:{raw[7]},{raw[9]},{raw[11]}]");
            }
        }
        var logs=new StringBuilder();var filled=0;
        for(var index=0;index<data->AirshipLogs.Length;index++)
        {
            fixed(FFXIVClientStructs.FFXIV.Client.System.String.Utf8String* entry=&data->AirshipLogs[index])
            {
                var line=TenshoAssistant.Maps.MapLiveReader.TooltipText(entry);
                if(line.Length==0)continue;
                if(++filled<=25)logs.Append($" #{index}={line}");
            }
        }
        text.Append($"; {filled} log lines:{logs}");
        var all=text.ToString();
        if(all==airship)return;
        airship=all;
        Line("airship",12,all);
    }

    // C30: which lines of the game say a catch.
    private void OnMessage(IChatMessage message)
    {
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        if(disposed || !enabled())return;
        try
        {
            if(message.Sender.TextValue.Trim().Length!=0)return;
            // 0.888 (privacy, found in the user's log): a message that names a player (« X uses … ») is never written, not even masked: the name followed
            // the marker as plain text. Only the game's own lines about you remain.
            if(message.Message.Payloads.Any(payload=>payload is Dalamud.Game.Text.SeStringHandling.Payloads.PlayerPayload))return;
            if(objects.LocalPlayer?.ClassJob.RowId==18)Line("fishing",200,$"chat [{(int)message.LogKind}] {message.Message.TextValue}");
            // 0.809 (X-43, step 3): which game messages tell a defeat, a drop, a blue spell learned, a beast caught. The first five of each message kind,
            // sender empty (the game speaking), player names masked, item rows written out.
            var kind=(int)message.LogKind;
            var seen=messageKinds.GetValueOrDefault(kind);
            if(seen>=5 || messageKinds.Count>=80 && seen==0)return;
            messageKinds[kind]=seen+1;
            var text=new StringBuilder();
            foreach(var payload in message.Message.Payloads)
                switch(payload)
                {
                    case Dalamud.Game.Text.SeStringHandling.Payloads.PlayerPayload:text.Append("<player>");break;
                    case Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload item:text.Append($"[item {item.ItemId}]");break;
                    case Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload words:text.Append(words.Text);break;
                }
            Line("messages",400,$"kind {kind}: {text}");
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A reading could not be made.");}
        }
    }

    private static string Text(AtkTextNode* node)
    {
        if(node==null || node->NodeText.Length>256)return "";
        return SeString.Parse(node->NodeText.AsSpan()).TextValue;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        registrations.Dispose();
    }
}
