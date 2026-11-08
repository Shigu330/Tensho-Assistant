using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Inventory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-74 (0.860, user request, the night of 2026-11-08: « add as many coherent parsers as possible », after the community map docs/evidence/0.857-community-map.md):
/// where the items the player gets come from, read on the framework thread ten times a second, copied, written to the local discoveries base only.
/// - Desynthesis: AgentSalvage.DesynthItemId and DesynthResults (ItemId, Quantity) while its result is open.
/// - Ventures: AgentRetainerTask.RetainerData (RewardRetainerTaskId, RewardItemIds, RewardItemCount) while « RetainerTaskResult » shows.
/// - Duty chests: Loot.Items (ItemId, ItemCount, ChestObjectId) inside a duty (TerritoryType.ContentFinderCondition).
/// - From the bags' changes (ItemGains): fish caught while fishing (with the bait, PlayerState.FishingBait, and the weather, WeatherManager), a FATE's reward
///   (FateManager.CurrentFate ending), what the player's own target dropped (dead within 3 s, outside duties), what an item opened gave (one unit of a usable item
///   spent). Shops, trades, retainers, mail, market and crafting windows stop the attribution.
/// Never another player: only the player's own target (a battle NPC) and game rows. Cut in PvP (CompetitiveGate).
/// </summary>
public sealed unsafe class ItemDiscoveryWatcher : IDisposable
{
    private static readonly GameInventoryType[] Bags=[GameInventoryType.Inventory1,GameInventoryType.Inventory2,GameInventoryType.Inventory3,GameInventoryType.Inventory4,GameInventoryType.Crystals];
    // X-73.16 (0.872): the shop windows; while one is shown, the event NPC targeted is the shopkeeper, noted once per opening with its place.
    private static readonly string[] ShopWindows=["Shop","ShopExchangeItem","ShopExchangeCurrency","ShopExchangeCoin","InclusionShop","ShopCard","GrandCompanyExchange",
        "FreeShop","CollectablesShop","SkyIslandExchange"];
    // 0.911 (X-76): every shop window stops the attribution (a purchase just after a FATE or a kill is not its reward), then trades, retainers, mail, market, crafting.
    // Declared after ShopWindows: static fields are set in order.
    private static readonly string[] BusyWindows=[..ShopWindows,"Trade","RetainerList","InventoryRetainer",
        "InventoryRetainerLarge","RetainerSellList","RetainerSell","LetterList","LetterViewer","ItemSearch","ItemSearchResult","FreeCompanyChest","InventoryBuddy",
        "InventoryBuddy2","GrandCompanySupplyList","MiragePrismPrismBox","Cabinet","Repair","MateriaAttach","PurifyResult","SalvageResult",
        "SalvageItemSelector","Synthesis","SynthesisSimple","RecipeNote","RetainerTaskResult","ContentsTutorial","SkyIslandFinishReward"];
    private readonly IFramework framework;private readonly IClientState client;private readonly IGameGui gui;private readonly IObjectTable objects;private readonly ITargetManager targets;
    private readonly ICondition condition;private readonly IGameInventory inventory;private readonly DiscoveryStore store;private readonly IPluginLog log;
    private readonly Func<DiscoverySettings> settings;private readonly Func<bool> debug;private readonly Func<uint,uint> dutyOf;private readonly Func<uint,bool> isFish;private readonly Func<uint,bool> usable;
    private Dictionary<uint,long>? before,beforeHq;private long nextRead;private bool disposed,warned;
    private uint shopNpcSeen;
    private void ReadShopNpc(DateTimeOffset at,uint territory,uint map)
    {
        if(!ShopWindows.Any(name=>gui.GetAddonByName(name) is { IsNull:false,IsVisible:true })){shopNpcSeen=0;return;}
        if(targets.Target is not { ObjectKind:Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc,BaseId:not 0 } npc || npc.BaseId==shopNpcSeen || map==0)return;
        shopNpcSeen=npc.BaseId;
        store.SeeSource(DiscoveryKind.NpcPlace,npc.BaseId,0,1,at,territory,map,npc.Position);
        if(debug())log.Information("Discovery: shop NPC {0} at territory {1}.",npc.BaseId,territory);
    }
    private string salvageSeen="",ventureSeen="";private readonly OnceSet<(uint Chest,uint Index,uint Item)> lootSeen=new(1024);private uint lootTerritory;
    private uint fateId;private long fateEndedAt=-100_000;
    private (uint Name,uint Territory,uint Map,Vector3 Place,int Level,long At) death=(0,0,0,default,0,-100_000);
    // X-73.9 (0.863): blue magic spells learned (IUnlockState.Unlock, AozAction rows, handled on the next read) and beasts tamed (the Beastmaster's obtained set).
    public Func<IReadOnlySet<uint>?>? Beasts { get; set; }
    // X-73.12 (0.864): the festivals running (once a day each: their first and last days seen) and the vistas registered (with the weather and the Eorzea minute).
    public Func<IReadOnlySet<uint>?>? Vistas { get; set; }
    /// <summary>X-75.3 (0.877): the weather of the period before (territory, moment), from the game's forecast; 0 when not known.</summary>
    public Func<uint,DateTimeOffset,uint>? PreviousWeather { get; set; }
    private uint lastFish;private uint lastFishBait;private long lastFishAt=-100_000;
    // X-85 V4 (0.962): the spectral current of the ocean stop, read on the framework thread; its start notes the catch just before it.
    private bool spectralBefore;
    private void ReadSpectral(DateTimeOffset at,long now,uint territory)
    {
        var events=FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.Instance();
        var voyage=events==null ? null:events->GetInstanceContentOceanFishing();
        var spectral=voyage!=null && voyage->SpectralCurrentActive;
        if(spectral && !spectralBefore && lastFish!=0 && now-lastFishAt is >= 0 and <= 90_000)
        {
            store.SeeSource(DiscoveryKind.SpectralBefore,lastFish,lastFishBait,1,at,territory);
            if(debug())log.Information("Discovery: spectral current after catch {0} (bait {1}).",lastFish,lastFishBait);
        }
        spectralBefore=spectral;
    }
    /// <summary>
    /// X-73.18 (0.887, proved by the in-game capture, docs/evidence/0.887-fishing-bite.md): the line's state at 10 Hz. The cast starts at CastingOut (1); lures do
    /// not restart it; at Bite (5) the player's animation slot 7 holds the bite strength (FishingHookStrength 292 !, 293 !!, 294 !!!). A cast under Chum
    /// (status 763, « Amorçage ») is not measured.
    /// </summary>
    private int lineState=-1;private long castStarted=-100_000,bitAt=-100_000;private bool castChum;private uint biteStrength;
    /// <summary>
    /// X-76 P6 (0.933): Fisher's Intuition (status 568, as FishingBaitReader). When it appears, the fish caught last is its trigger, unless a catch lands within
    /// <see cref="IntuitionGrace"/> (the status can show before the bags count the fish): that one is. The start is written once the grace is over; the catches
    /// while it lasts are written with the trigger. Not proven in game yet (docs/evidence/0.933-fishing-intuition.md).
    /// </summary>
    private sealed record IntuitionRun(long Started,uint Trigger,int Seconds,bool Written);
    private IntuitionRun? intuition;
    private const long IntuitionGrace=3000;
    private const uint IntuitionStatus=568;
    private void ReadIntuition(long now,DateTimeOffset at,uint territory,uint map,Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter me)
    {
        var status=me.StatusList.FirstOrDefault(entry=>entry.StatusId==IntuitionStatus);
        if(status is null){intuition=null;return;}
        if(intuition is null){intuition=new(now,0,(int)Math.Round(status.RemainingTime),false);return;}
        if(!intuition.Written && now-intuition.Started>=IntuitionGrace)
        {
            var trigger=intuition.Trigger!=0 ? intuition.Trigger:lastFish;
            intuition=intuition with{Trigger=trigger,Written=true};
            if(trigger!=0 && intuition.Seconds is > 0 and < 3600)store.SeeSource(DiscoveryKind.IntuitionStart,trigger,0,intuition.Seconds,at,territory,map,me.Position);
        }
    }
    private void ReadLine(long now,Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter me)
    {
        var events=FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.Instance();
        var handler=events==null ? null:events->EventHandlerModule.FishingEventHandler;
        var state=handler==null ? -1:(int)handler->State;
        if(state==lineState)return;
        lineState=state;
        if(state==1){castStarted=now;bitAt=-100_000;castChum=me.StatusList.Any(status=>status.StatusId==763);}
        else if(state==5 && castStarted>0)
        {
            bitAt=now;
            var character=(FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)me.Address;
            biteStrength=ItemGains.BiteStrength(character->Timeline.TimelineSequencer.TimelineIds[7]);
            // X-98 (5) (0.965): at sea the strength is often unknown; in developer mode the timeline slots are logged then, to find where the bite is.
            if(biteStrength==0 && debug())
            {
                var slots=new List<string>();
                for(var slot=0;slot<character->Timeline.TimelineSequencer.TimelineIds.Length;slot++)
                    if(character->Timeline.TimelineSequencer.TimelineIds[slot] is var id and not 0)slots.Add($"{slot}:{id}");
                log.Information("Discovery: bite strength unknown; timeline slots {0}.",string.Join(" ",slots));
            }
        }
    }
    /// <summary>
    /// X-75.6 (0.880): the weather formula (WeatherCatalog, rule read in the community tools) checked against the game: 10 s after entering a zone, the forecast
    /// for now against WeatherManager.GetCurrentWeather. Zones with their own weather (some quests and duties) can differ: counted, logged in the debug log only.
    /// </summary>
    public Func<uint,DateTimeOffset,uint>? ForecastWeather { get; set; }
    /// <summary>X-96 (0.959): each in-game weather check (territory, forecast matched the game).</summary>
    public Action<uint,bool>? WeatherChecked { get; set; }
    public (int Matches,int Mismatches) WeatherChecks { get; private set; }
    private uint weatherZone;private long weatherCheckAt=-100_000;
    private void CheckWeather(DateTimeOffset at,long now,uint territory,uint duty)
    {
        if(territory!=weatherZone){weatherZone=territory;weatherCheckAt=duty==0 ? now+10_000:-100_000;return;}
        if(weatherCheckAt<0 || now<weatherCheckAt || ForecastWeather is not { } forecast)return;
        weatherCheckAt=-100_000;
        var expected=forecast(territory,at);
        var actual=FFXIVClientStructs.FFXIV.Client.Game.WeatherManager.Instance() is var manager && manager!=null ? manager->GetCurrentWeather():(byte)0;
        if(expected==0 || actual==0)return;
        WeatherChecks=expected==actual ? (WeatherChecks.Matches+1,WeatherChecks.Mismatches):(WeatherChecks.Matches,WeatherChecks.Mismatches+1);
        // X-96 (0.959): the result also tells whether a zone with a story weather now follows the standard forecast.
        WeatherChecked?.Invoke(territory,expected==actual);
        if(debug())log.Information("Weather check in territory {0}: forecast {1}, game {2}.",territory,expected,actual);
    }
    /// <summary>X-75.4 (0.878): the AozAction row of an action id (0: not a blue spell); battle NPCs casting one are noted once per cast.</summary>
    public Func<uint,uint>? BlueSpellOfAction { get; set; }
    private readonly Dictionary<ulong,uint> castsSeen=[];
    private void ReadCasters(DateTimeOffset at,uint territory,uint map)
    {
        if(BlueSpellOfAction is not { } spellOf || map==0)return;
        var casting=new HashSet<ulong>();
        foreach(var actor in objects)
        {
            if(actor is not IBattleNpc { BattleNpcKind:BattleNpcSubKind.Combatant,IsCasting:true,NameId:not 0 } npc || casting.Count>=64)continue;
            casting.Add(npc.GameObjectId);
            var action=npc.CastActionId;
            if(castsSeen.GetValueOrDefault(npc.GameObjectId)==action)continue;
            castsSeen[npc.GameObjectId]=action;
            if(spellOf(action) is not (> 0 and var spell))continue;
            store.SeeSource(DiscoveryKind.BlueSpellCaster,spell,npc.NameId,npc.Level,at,territory,map,npc.Position);
            if(debug())log.Information("Discovery: monster {0} casting blue spell {1}.",npc.NameId,spell);
        }
        foreach(var gone in castsSeen.Keys.Where(id=>!casting.Contains(id)).ToArray())castsSeen.Remove(gone);
    }
    private IReadOnlySet<uint>? vistasBefore;private int festivalDay=-1;private long festivalsAt=-100_000;
    private void ReadSeasonal(DateTimeOffset at,long now)
    {
        if(now-festivalsAt>=60_000)
        {
            festivalsAt=now;
            var day=ItemGains.DayNumber(at);
            if(day!=festivalDay && FFXIVClientStructs.FFXIV.Client.Game.GameMain.Instance() is var game && game!=null)
            {
                festivalDay=day;
                // 0.911 (X-76): festivalDay starts again at each plugin load: the store counts a day once (SeeFestival), reloads included.
                foreach(var festival in game->ActiveFestivals)
                    if(festival.Id!=0)store.SeeFestival(festival.Id,festival.Phase,day,at);
            }
        }
        if(Vistas?.Invoke() is not { } registered)return;
        if(vistasBefore is { } known)
            foreach(var vista in registered.Where(id=>!known.Contains(id)).Take(4))
            {
                var weather=FFXIVClientStructs.FFXIV.Client.Game.WeatherManager.Instance() is var manager && manager!=null ? manager->GetCurrentWeather():(byte)0;
                store.SeeSource(DiscoveryKind.VistaSeen,vista,weather,EorzeaMinute(at),at);
                if(debug())log.Information("Discovery: vista {0} registered in weather {1}.",vista,weather);
            }
        vistasBefore=registered;
    }
    private IReadOnlySet<uint>? beastsBefore;private IUnlockState? unlocks;private readonly Queue<uint> spellsLearned=new();
    public void WatchUnlocks(IUnlockState state){unlocks=state;registrations.Register(()=>state.Unlock+=OnUnlock,()=>state.Unlock-=OnUnlock);}
    private readonly RegistrationScope registrations=new();
    private void OnUnlock(Lumina.Excel.RowRef row)
    {
        if(disposed || TenshoAssistant.Competitive.CompetitiveGate.Cut || row.RowType!=typeof(Lumina.Excel.Sheets.AozAction) || spellsLearned.Count>=16)return;
        spellsLearned.Enqueue(row.RowId);
    }
    /// <summary>The monster a new spell or beast is put down to: the one that died within 15 s, else the player's own target (a battle NPC).</summary>
    private (uint Name,uint Territory,uint Map,Vector3 Place)? RecentMonster(long now,uint territory,uint map)
    {
        if(death.Name!=0 && now-death.At is >= 0 and <= 15_000)return (death.Name,death.Territory,death.Map,death.Place);
        return targets.Target is IBattleNpc { BattleNpcKind:BattleNpcSubKind.Combatant,NameId:not 0 } npc ? (npc.NameId,territory,map,npc.Position):null;
    }
    private void ReadLearned(DateTimeOffset at,long now,uint territory,uint map)
    {
        while(spellsLearned.TryDequeue(out var spell))
        {
            var monster=RecentMonster(now,territory,map);
            store.SeeSource(DiscoveryKind.BlueSpellLearned,spell,monster?.Name ?? 0,1,at,monster?.Territory ?? 0,monster?.Map ?? 0,monster?.Place);
            if(debug())log.Information("Discovery: blue spell {0} learned, monster {1}.",spell,monster?.Name ?? 0);
        }
        if(Beasts?.Invoke() is not { } obtained)return;
        if(beastsBefore is { } known)
            foreach(var beast in obtained.Where(id=>!known.Contains(id)).Take(8))
            {
                var monster=RecentMonster(now,territory,map);
                store.SeeSource(DiscoveryKind.BeastTamed,beast,monster?.Name ?? 0,1,at,monster?.Territory ?? 0,monster?.Map ?? 0,monster?.Place);
                if(debug())log.Information("Discovery: beast {0} tamed, monster {1}.",beast,monster?.Name ?? 0);
            }
        beastsBefore=obtained;
    }

    public ItemDiscoveryWatcher(IFramework framework,IClientState client,IGameGui gui,IObjectTable objects,ITargetManager targets,ICondition condition,IGameInventory inventory,
        DiscoveryStore store,IPluginLog log,Func<DiscoverySettings> settings,Func<bool> debug,Func<uint,uint> dutyOf,Func<uint,bool> isFish,Func<uint,bool> usable,
        Dalamud.Plugin.Services.IChatGui? chat=null)
    {
        this.framework=framework;this.client=client;this.gui=gui;this.objects=objects;this.targets=targets;this.condition=condition;this.inventory=inventory;
        this.store=store;this.log=log;this.settings=settings;this.debug=debug;this.dutyOf=dutyOf;this.isFish=isFish;this.usable=usable;
        this.chat=chat;
        // X-76 H10 (0.924): subscriptions owned by one RegistrationScope: a failure part-way undoes the ones already made; Dispose releases them in reverse order.
        try
        {
            if(chat is not null)registrations.Register(()=>chat.ChatMessageUnhandled+=OnSpearChat,()=>chat.ChatMessageUnhandled-=OnSpearChat);
            registrations.Register(()=>TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update),()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update));
        }
        catch{registrations.Dispose();throw;}
    }

    private void Update(IFramework unused)
    {
        if(disposed)return;
        var now=Environment.TickCount64;
        if(now<nextRead)return;
        nextRead=now+100;
        var wanted=settings();
        if(!wanted.Enabled || !wanted.ItemSources && !wanted.Fishing || !client.IsLoggedIn || objects.LocalPlayer is not { } me){before=null;return;}
        try
        {
            var at=DateTimeOffset.UtcNow;var territory=client.TerritoryType;var map=client.MapId;var duty=dutyOf(territory);
            CheckWeather(at,now,territory,duty);
            if(wanted.Fishing && me.ClassJob.RowId==18){ReadLine(now,me);ReadSpear(at,now,territory,map,me.Position);ReadIntuition(now,at,territory,map,me);ReadSpectral(at,now,territory);}
            if(wanted.ItemSources)
            {
                ReadSalvage(at);ReadPurify(at);ReadHandIn(at,now);ReadVenture(at);ReadLoot(at,territory,duty);
                ReadFate(now);ReadTarget(now,at,territory,map);ReadLearned(at,now,territory,map);ReadSeasonal(at,now);ReadShopNpc(at,territory,map);ReadCasters(at,territory,map);
            }
            var (counts,hqCounts)=Counts();
            if(before is null || beforeHq is null){before=counts;beforeHq=hqCounts;return;}
            var gains=ItemGains.Gains(before,counts);var losses=ItemGains.Losses(before,counts);before=counts;
            // X-76 E1 (0.916): a gain is an HQ sighting when HQ units of the item came in (the bags count HQ apart, slot.IsHq).
            var hqGains=ItemGains.Gains(beforeHq,hqCounts);beforeHq=hqCounts;
            if(gains.Count==0 || gains.Count>16)return;
            var fishing=wanted.Fishing && condition[ConditionFlag.Fishing];
            var busy=!fishing && (condition[ConditionFlag.Crafting] || condition[ConditionFlag.Gathering] || condition[ConditionFlag.PreparingToCraft]
                || BusyWindows.Any(name=>gui.GetAddonByName(name) is { IsNull:false,IsVisible:true }));
            var spent=ItemGains.SingleSpent(losses);
            // X-73.17 (0.873): field content and deep dungeons, told by the game's own directors (no territory list).
            var events=FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.Instance();
            var deepDungeon=duty!=0 && events!=null && events->GetInstanceContentDeepDungeon()!=null;
            var field=duty!=0 && events!=null && (FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.GetPublicContentDirectorByType(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentDirectorType.Eureka)!=null
                || FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.GetPublicContentDirectorByType(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentDirectorType.Bozja)!=null
                || FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.GetPublicContentDirectorByType(FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.PublicContentDirectorType.OccultCrescent)!=null);
            var cause=ItemGains.Attribute(fishing,busy || !wanted.ItemSources,now,fateEndedAt,fateId,death.At,duty!=0,spent,spent is { } used && usable(used),spent is { } same && gains.ContainsKey(same),
                field,deepDungeon);
            foreach(var (item,count) in gains)
            {
                var quantity=(int)Math.Min(count,100_000);var added=false;var kind=DiscoveryKind.ItemFromItem;var hq=hqGains.ContainsKey(item);
                switch(cause)
                {
                    case ItemGains.Cause.Fishing when isFish(item):
                        // X-75.3 (0.877): the cast's flags (FishingEventHandler): a mooch is put down to the fish caught before, not the bait in hand; a lure is kept.
                        var flags=events!=null && events->EventHandlerModule.FishingEventHandler is var handler && handler!=null ? (int)handler->CurrentCastBaitFlags:0;
                        var bait=(flags & 16)!=0 && lastFish!=0 ? lastFish:PlayerState.Instance() is var state && state!=null ? state->FishingBait:0u;
                        var weather=FFXIVClientStructs.FFXIV.Client.Game.WeatherManager.Instance() is var manager && manager!=null ? manager->GetCurrentWeather():(byte)0;
                        added=store.SeeSource(DiscoveryKind.FishCatch,item,bait,quantity,at,territory,map,me.Position,hq);
                        store.SeeSource(DiscoveryKind.FishCondition,item,weather,EorzeaMinute(at),at,territory);
                        if((flags & 3) is 1 or 2)store.SeeSource(DiscoveryKind.FishLure,item,(uint)(flags & 3),1,at,territory);
                        if(PreviousWeather?.Invoke(territory,at) is { } before and not 0)store.SeeSource(DiscoveryKind.FishTransition,item,before,EorzeaMinute(at),at,territory);
                        lastFish=item;lastFishBait=bait;lastFishAt=now;
                        if(bitAt>castStarted && castStarted>0 && !castChum && bitAt-castStarted is > 0 and < 600_000)
                        {
                            store.SeeSource(DiscoveryKind.FishBite,item,biteStrength,(int)((bitAt-castStarted)/100),at,territory,map,me.Position);
                            // X-76 P6 (0.933): the same bite time, keyed by the cast's bait.
                            if(bait!=0)store.SeeSource(DiscoveryKind.FishBiteBait,item,bait,(int)((bitAt-castStarted)/100),at,territory,map,me.Position);
                        }
                        // X-76 P6 (0.933): a catch that lands just after the intuition began is the one that started it; later ones are caught under it.
                        if(intuition is { } running)
                        {
                            if(running.Trigger==0 && now-running.Started<IntuitionGrace)intuition=running with{Trigger=item};
                            else store.SeeSource(DiscoveryKind.IntuitionCatch,item,running.Trigger,1,at,territory);
                        }
                        bitAt=-100_000;
                        kind=DiscoveryKind.FishCatch;break;
                    case ItemGains.Cause.FateReward:
                        added=store.SeeSource(DiscoveryKind.FateReward,item,fateId,quantity,at,hq:hq);kind=DiscoveryKind.FateReward;break;
                    case ItemGains.Cause.MonsterDrop:
                        added=store.SeeSource(DiscoveryKind.MonsterDrop,item,death.Name,quantity,at,death.Territory,death.Map,death.Place,hq);kind=DiscoveryKind.MonsterDrop;break;
                    case ItemGains.Cause.Opened:
                        added=store.SeeSource(DiscoveryKind.ItemFromItem,item,spent!.Value,quantity,at,hq:hq);break;
                    case ItemGains.Cause.DungeonCoffer:
                        // 0.911 (X-76): with the territory, like the duty's loot window (ReadLoot): one fact per item and duty.
                        added=store.SeeSource(DiscoveryKind.DutyChest,item,duty,quantity,at,territory,hq:hq);kind=DiscoveryKind.DutyChest;break;
                    default:continue;
                }
                if(added && debug())log.Information("Discovery: {0} item {1} ×{2}.",kind,item,quantity);
            }
        }
        catch(Exception error){before=null;if(!warned){warned=true;log.Warning(error,"The item discoveries could not be read.");}}
    }

    // Eorzea minute of the day (175 s an Eorzea hour), as Tensho's clocks count it.
    private static int EorzeaMinute(DateTimeOffset at)=>Gathering.GatheringTime.MinuteOfDay(at);

    /// <summary>Per item, the units in the bags, HQ and NQ together; and apart, the HQ units (X-76 E1, 0.916).</summary>
    private (Dictionary<uint,long> All,Dictionary<uint,long> Hq) Counts()
    {
        var counts=new Dictionary<uint,long>();var hq=new Dictionary<uint,long>();
        foreach(var bag in Bags)
            foreach(var slot in inventory.GetInventoryItems(bag))
                if(!slot.IsEmpty && slot.BaseItemId!=0 && slot.Quantity>0)
                {
                    counts[slot.BaseItemId]=counts.GetValueOrDefault(slot.BaseItemId)+slot.Quantity;
                    if(slot.IsHq)hq[slot.BaseItemId]=hq.GetValueOrDefault(slot.BaseItemId)+slot.Quantity;
                }
        return (counts,hq);
    }

    private void ReadSalvage(DateTimeOffset at)
    {
        var agent=AgentSalvage.Instance();
        if(agent==null || !agent->IsSalvageResultAddonOpen || agent->DesynthItemId==0){salvageSeen="";return;}
        var source=agent->DesynthItemId%1_000_000;var results=new List<(uint Item,int Count,bool Hq)>();
        // X-76 E1 (0.916): an HQ result carries the HQ offset in its raw id (not seen in game yet).
        foreach(var result in agent->DesynthResults)if(result.ItemId!=0 && result.Quantity>0)results.Add((result.ItemId%1_000_000,result.Quantity,ItemGains.IsHq(result.ItemId)));
        var signature=source+":"+string.Join(",",results.Select(result=>$"{result.Item}x{result.Count}"));
        if(results.Count==0 || signature==salvageSeen)return;
        salvageSeen=signature;
        foreach(var (item,count,hq) in results)store.SeeSource(DiscoveryKind.Desynthesis,item,source,count,at,hq:hq);
        if(debug())log.Information("Discovery: desynthesis of {0}: {1}.",source,signature);
    }

    /// <summary>
    /// X-73.18 (0.888, proved by the in-game capture, docs/evidence/0.888-aetherial-reduction.md): aetherial reduction (« Éthérolyse ») read in AgentPurify while
    /// its result window shows: the collectable reduced (ResultItemId, +500 000 for a collectable) and each result (ItemId, Count), once per result.
    /// </summary>
    private string purifySeen="";
    private void ReadPurify(DateTimeOffset at)
    {
        if(gui.GetAddonByName("PurifyResult") is not { IsNull:false,IsVisible:true }){purifySeen="";return;}
        var agent=AgentPurify.Instance();
        if(agent==null || agent->ResultItemId==0)return;
        var source=ItemGains.BaseItem(agent->ResultItemId);var results=new List<(uint Item,int Count)>();
        foreach(var result in agent->Results)if(result.ItemId!=0 && result.Count>0)results.Add((ItemGains.BaseItem(result.ItemId),result.Count));
        var signature=source+":"+string.Join(",",results.Select(result=>$"{result.Item}x{result.Count}"));
        if(results.Count==0 || signature==purifySeen)return;
        purifySeen=signature;
        foreach(var (item,count) in results)store.SeeSource(DiscoveryKind.Reduction,item,source,count,at);
        if(debug())log.Information("Discovery: aetherial reduction of {0}: {1}.",source,signature);
    }

    /// <summary>
    /// X-73.18 (0.889, proved for a leve by the in-game capture, docs/evidence/0.889-hand-in.md): the items an NPC asks for in the « Request » window
    /// (UIState.NpcTrade). The quest is the one whose step changes (or that ends) within 15 s after the window closes: the leve did so 4 s after. Leves are
    /// game data already: only quests are kept. Unverified for a quest until a quest hand-in is seen.
    /// </summary>
    private (List<(uint Item,int Quantity,bool Hq)> Items,Dictionary<ushort,byte> Steps,long ClosedAt)? handIn;
    private static Dictionary<ushort,byte> QuestSteps()
    {
        var steps=new Dictionary<ushort,byte>();
        var quests=FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance();
        if(quests==null)return steps;
        foreach(var quest in quests->NormalQuests)if(quest.QuestId!=0)steps[quest.QuestId]=quest.Sequence;
        return steps;
    }
    private void ReadHandIn(DateTimeOffset at,long now)
    {
        var shown=gui.GetAddonByName("Request") is { IsNull:false,IsVisible:true };
        if(shown)
        {
            var ui=UIState.Instance();
            if(ui==null)return;
            var requests=&ui->NpcTrade.Requests;
            var items=new List<(uint,int,bool)>();
            for(var index=0;index<Math.Min((int)requests->Count,requests->Items.Length);index++)
            {
                ref var item=ref requests->Items[index];
                if(item.ItemId!=0 && item.RequiredQuantity>0)items.Add((ItemGains.BaseItem(item.ItemId),item.RequiredQuantity,item.WantHQ));
            }
            if(items.Count>0)handIn=(items,handIn?.Steps ?? QuestSteps(),-1);
            return;
        }
        if(handIn is not { } pending)return;
        if(pending.ClosedAt<0){handIn=pending with {ClosedAt=now};return;}
        if(now-pending.ClosedAt>15_000){handIn=null;return;}
        var after=QuestSteps();
        var changed=pending.Steps.Where(step=>!after.TryGetValue(step.Key,out var sequence) || sequence!=step.Value).Select(step=>step.Key).ToArray();
        if(changed.Length==0)return;
        handIn=null;
        if(changed.Length!=1)return;
        var quest=0x10000u+changed[0];
        // 0.911 (X-76): an HQ demand under the item's HQ id (QuestItemAsks), the quantity in the levels.
        foreach(var (item,quantity,hq) in pending.Items)store.SeeSource(DiscoveryKind.QuestItemAsked,QuestItemAsks.Subject(item,hq),quest,quantity,at);
        if(debug())log.Information("Discovery: quest {0} asked for {1}.",quest,string.Join(", ",pending.Items.Select(entry=>$"{entry.Item}x{entry.Quantity}{(entry.Hq ? " HQ":"")}")));
    }

    /// <summary>
    /// X-73.18 (0.890, 0.891, 0.896; docs/evidence/0.891-spearfishing-slots.md, 0.896-spearfishing-lag.md): spearfishing. The window's AtkValues 21 (catches so far)
    /// and 12 (the item last caught): 21 grows by one at each catch, 12 follows a moment later (0.896: read once with the fish before). The fish speared is the slot
    /// of AddonSpearFishing.Fish that stops being available while keeping its size and speed (a fish that swims away is cleared to zero), in the 1.5 s before the
    /// catch; its speed is the one it had when it appeared (a shadow's speed can double while it swims). The catch is written once 12 changes, or 0.6 s later
    /// (the same fish again); two catches at once, or a second one before, give nothing.
    /// </summary>
    private int spearCatches=-1;private readonly bool[] spearShown=new bool[3];private readonly (int Size,int Speed)[] spearFirst=new (int,int)[3];
    private readonly List<(int Size,int Speed,long At)> spearGone=[];
    private (int Item,(int Size,int Speed)? Shadow,long Until,DateTimeOffset At,uint Territory,uint Map,Vector3 Position)? spearPending;
    // X-73.18 (0.898): a multiple catch waits for the chat's « you caught … » lines (one per fish, kind 67 GatheringSystemMessage, an item link, no sender).
    private (int Count,(int Size,int Speed)[] Speared,long Start,long Until,DateTimeOffset At,uint Territory,uint Map,Vector3 Position)? spearMulti;
    private readonly List<(uint Item,long At)> spearChat=[];
    private readonly Dalamud.Plugin.Services.IChatGui? chat;
    private void OnSpearChat(Dalamud.Game.Chat.IChatMessage message)
    {
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        if(disposed || spearCatches<0)return;
        try
        {
            if((int)message.LogKind!=67 || message.Sender.TextValue.Trim().Length!=0)return;
            var items=message.Message.Payloads.OfType<Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload>().Select(payload=>ItemGains.BaseItem(payload.ItemId)).Distinct().ToArray();
            if(items.Length!=1 || items[0]==0)return;
            spearChat.Add((items[0],Environment.TickCount64));
            if(spearChat.Count>16)spearChat.RemoveAt(0);
        }
        catch(Exception error){if(!warned){warned=true;log.Warning(error,"The spearfishing catches could not be read from the chat.");}}
    }
    private (int Size,int Speed)? SeenSpearShadow(uint item)=>store.MostSeenSpearShadow(item);
    private void ReadSpear(DateTimeOffset at,long now,uint territory,uint map,Vector3 position)
    {
        var handle=gui.GetAddonByName("SpearFishing");
        if(handle.IsNull || !handle.IsVisible){spearCatches=-1;Array.Clear(spearShown);spearGone.Clear();spearPending=null;spearMulti=null;spearChat.Clear();return;}
        var unit=(FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase*)handle.Address;var addon=(FFXIVClientStructs.FFXIV.Client.UI.AddonSpearFishing*)handle.Address;
        if(unit->AtkValues==null || unit->AtkValuesCount<=21)return;
        for(var index=0;index<Math.Min(addon->Fish.Length,spearShown.Length);index++)
        {
            var fish=addon->Fish[index];var size=(int)fish.Size;
            if(fish.Available && !spearShown[index])spearFirst[index]=(size,fish.Speed);
            if(spearShown[index] && !fish.Available && size is >= 1 and <= 3 && fish.Speed>0)
                spearGone.Add((size,spearFirst[index].Size==size && spearFirst[index].Speed>0 ? spearFirst[index].Speed:fish.Speed,now));
            spearShown[index]=fish.Available;
        }
        if(spearGone.Count>32)spearGone.RemoveRange(0,spearGone.Count-32);
        var count=TenshoAssistant.UI.AtkValueRead.Number(unit->AtkValues[21]);var item=TenshoAssistant.UI.AtkValueRead.Number(unit->AtkValues[12]);
        if(spearPending is { } pending && (item>0 && item!=pending.Item || now>=pending.Until))
        {
            spearPending=null;
            if(item>0 && pending.Shadow is { } shadow)
            {
                store.SeeSource(DiscoveryKind.SpearShadow,(uint)item,(uint)shadow.Size,shadow.Speed,pending.At,pending.Territory,pending.Map,pending.Position);
                if(debug())log.Information("Discovery: spearfishing {0}, shadow size {1}, speed {2}.",item,shadow.Size,shadow.Speed);
            }
            else if(debug())log.Information("Discovery: spearfishing {0}, shadow unknown.",item);
        }
        if(spearMulti is { } multi)
        {
            var lines=spearChat.Where(line=>line.At>=multi.Start-1_500).Select(line=>line.Item).ToArray();
            if(lines.Length>=multi.Count || now>=multi.Until)
            {
                spearMulti=null;
                var caught=lines.Length>=multi.Count ? lines[^multi.Count..]:[];
                var found=ItemGains.SpearAssign(caught,multi.Speared,SeenSpearShadow);
                foreach(var (fish,size,speed) in found)store.SeeSource(DiscoveryKind.SpearShadow,fish,(uint)size,speed,multi.At,multi.Territory,multi.Map,multi.Position);
                if(debug())log.Information("Discovery: spearfishing, {0} at once: {1} fish read in the chat, {2} slots, shadows {3}.",multi.Count,caught.Length,multi.Speared.Length,
                    found.Count==0 ? "unknown":string.Join(", ",found.Select(entry=>$"{entry.Item} {entry.Size}/{entry.Speed}")));
            }
        }
        if(count<0)return;
        if(count<spearCatches){spearGone.Clear();spearPending=null;spearMulti=null;}
        else if(spearCatches>=0 && count>spearCatches)
        {
            var speared=spearGone.Where(gone=>now-gone.At<=1_500).Select(gone=>(gone.Size,gone.Speed)).ToArray();
            spearGone.Clear();
            if(spearPending is not null || spearMulti is not null){spearPending=null;spearMulti=null;if(debug())log.Information("Discovery: spearfishing, catches too close together: not noted.");}
            else if(count==spearCatches+1)spearPending=(item,ItemGains.SpearShadow(speared),now+600,at,territory,map,position);
            else spearMulti=(count-spearCatches,speared,now,now+1_200,at,territory,map,position);
        }
        spearCatches=count;
    }

    private void ReadVenture(DateTimeOffset at)
    {
        if(gui.GetAddonByName("RetainerTaskResult") is not { IsNull:false,IsVisible:true }){ventureSeen="";return;}
        var agent=AgentRetainerTask.Instance();
        if(agent==null)return;
        var data=agent->RetainerData;var task=(uint)data.RewardRetainerTaskId;
        if(task==0)return;
        var rewards=new List<(uint Item,int Count,bool Hq)>();
        // X-76 E1 (0.916): an HQ reward carries the HQ offset in its raw id (not seen in game yet).
        for(var index=0;index<data.RewardItemIds.Length && index<data.RewardItemCount.Length;index++)
            if(data.RewardItemIds[index] is var id and not 0)rewards.Add((id%1_000_000,(int)Math.Min(data.RewardItemCount[index],100_000u),ItemGains.IsHq(id)));
        var signature=task+":"+string.Join(",",rewards.Select(reward=>$"{reward.Item}x{reward.Count}"));
        if(rewards.Count==0 || signature==ventureSeen)return;
        ventureSeen=signature;
        foreach(var (item,count,hq) in rewards)store.SeeSource(DiscoveryKind.Venture,item,task,count,at,hq:hq);
        if(debug())log.Information("Discovery: venture {0}.",signature);
    }

    private readonly OnceSet<uint> chestsSeen=new(256);
    private void ReadLoot(DateTimeOffset at,uint territory,uint duty)
    {
        if(territory!=lootTerritory){lootSeen.Clear();chestsSeen.Clear();lootTerritory=territory;}
        if(duty==0)return;
        var loot=Loot.Instance();
        if(loot==null)return;
        foreach(var item in loot->Items)
        {
            if(item.ItemId==0 || item.ChestObjectId==0 || !lootSeen.Add((item.ChestObjectId,item.ChestItemIndex,item.ItemId)))continue;
            // X-76 P8 (0.935): a chest seen for the first time in this zone counts once for its duty.
            if(chestsSeen.Add(item.ChestObjectId))store.SeeSource(DiscoveryKind.DutyChestOpened,duty,0,1,at,territory);
            // X-76 E1 (0.916): an HQ item carries the HQ offset in its raw id (not seen in game yet).
            store.SeeSource(DiscoveryKind.DutyChest,item.ItemId%1_000_000,duty,Math.Max(1,(int)item.ItemCount),at,territory,hq:ItemGains.IsHq(item.ItemId));
            if(debug())log.Information("Discovery: duty {0} chest item {1} ×{2}.",duty,item.ItemId,item.ItemCount);
        }
    }

    private void ReadFate(long now)
    {
        var manager=FateManager.Instance();
        if(manager==null || manager->CurrentFate==null)return;
        var fate=manager->CurrentFate;
        fateId=fate->FateId;
        if(fate->State is FateState.Ending or FateState.Ended)fateEndedAt=now;
    }

    private void ReadTarget(long now,DateTimeOffset at,uint territory,uint map)
    {
        if(targets.Target is not IBattleNpc { BattleNpcKind:BattleNpcSubKind.Combatant } npc || npc.CurrentHp!=0 || npc.NameId==0)return;
        if(death.Name==npc.NameId && now-death.At<5_000)return;
        death=(npc.NameId,territory,map,npc.Position,npc.Level,now);
        // X-76 P8 (0.935): the same deaths MonsterDrop credits, counted: the denominator of a drop rate.
        store.SeeSource(DiscoveryKind.MonsterKill,npc.NameId,0,npc.Level,at,territory);
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;registrations.Dispose();
    }
}
