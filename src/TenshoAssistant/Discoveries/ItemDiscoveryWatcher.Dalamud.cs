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
    private static readonly string[] BusyWindows=["Shop","ShopExchangeItem","ShopExchangeCurrency","InclusionShop","ShopCard","Trade","RetainerList","InventoryRetainer",
        "InventoryRetainerLarge","RetainerSellList","RetainerSell","LetterList","LetterViewer","ItemSearch","ItemSearchResult","FreeCompanyChest","InventoryBuddy",
        "InventoryBuddy2","GrandCompanySupplyList","GrandCompanyExchange","MiragePrismPrismBox","Cabinet","Repair","MateriaAttach","PurifyResult","SalvageResult",
        "SalvageItemSelector","Synthesis","SynthesisSimple","RecipeNote","RetainerTaskResult","ContentsTutorial","SkyIslandFinishReward"];
    private readonly IFramework framework;private readonly IClientState client;private readonly IGameGui gui;private readonly IObjectTable objects;private readonly ITargetManager targets;
    private readonly ICondition condition;private readonly IGameInventory inventory;private readonly DiscoveryStore store;private readonly IPluginLog log;
    private readonly Func<DiscoverySettings> settings;private readonly Func<bool> debug;private readonly Func<uint,uint> dutyOf;private readonly Func<uint,bool> isFish;private readonly Func<uint,bool> usable;
    private Dictionary<uint,long>? before;private long nextRead;private bool disposed,warned;
    // X-73.16 (0.872): the shop windows; while one is shown, the event NPC targeted is the shopkeeper, noted once per opening with its place.
    private static readonly string[] ShopWindows=["Shop","ShopExchangeItem","ShopExchangeCurrency","ShopExchangeCoin","InclusionShop","ShopCard","GrandCompanyExchange",
        "FreeShop","CollectablesShop","SkyIslandExchange"];
    private uint shopNpcSeen;
    private void ReadShopNpc(DateTimeOffset at,uint territory,uint map)
    {
        if(!ShopWindows.Any(name=>gui.GetAddonByName(name) is { IsNull:false,IsVisible:true })){shopNpcSeen=0;return;}
        if(targets.Target is not { ObjectKind:Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc,BaseId:not 0 } npc || npc.BaseId==shopNpcSeen || map==0)return;
        shopNpcSeen=npc.BaseId;
        store.SeeSource(DiscoveryKind.NpcPlace,npc.BaseId,0,1,at,territory,map,npc.Position);
        if(debug())log.Information("Discovery: shop NPC {0} at territory {1}.",npc.BaseId,territory);
    }
    private string salvageSeen="",ventureSeen="";private readonly HashSet<(uint Chest,uint Index,uint Item)> lootSeen=[];private uint lootTerritory;
    private uint fateId;private long fateEndedAt=-100_000;
    private (uint Name,uint Territory,uint Map,Vector3 Place,int Level,long At) death=(0,0,0,default,0,-100_000);
    // X-73.9 (0.863): blue magic spells learned (IUnlockState.Unlock, AozAction rows, handled on the next read) and beasts tamed (the Beastmaster's obtained set).
    public Func<IReadOnlySet<uint>?>? Beasts { get; set; }
    // X-73.12 (0.864): the festivals running (once a day each: their first and last days seen) and the vistas registered (with the weather and the Eorzea minute).
    public Func<IReadOnlySet<uint>?>? Vistas { get; set; }
    /// <summary>X-75.3 (0.877): the weather of the period before (territory, moment), from the game's forecast; 0 when not known.</summary>
    public Func<uint,DateTimeOffset,uint>? PreviousWeather { get; set; }
    private uint lastFish;
    /// <summary>
    /// X-73.18 (0.887, proved by the in-game capture, docs/evidence/0.887-fishing-bite.md): the line's state at 10 Hz. The cast starts at CastingOut (1); lures do
    /// not restart it; at Bite (5) the player's animation slot 7 holds the bite strength (FishingHookStrength 292 !, 293 !!, 294 !!!). A cast under Chum
    /// (status 763, « Amorçage ») is not measured.
    /// </summary>
    private int lineState=-1;private long castStarted=-100_000,bitAt=-100_000;private bool castChum;private uint biteStrength;
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
        }
    }
    /// <summary>
    /// X-75.6 (0.880): the weather formula (WeatherCatalog, rule read in the community tools) checked against the game: 10 s after entering a zone, the forecast
    /// for now against WeatherManager.GetCurrentWeather. Zones with their own weather (some quests and duties) can differ: counted, logged in the debug log only.
    /// </summary>
    public Func<uint,DateTimeOffset,uint>? ForecastWeather { get; set; }
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
                foreach(var festival in game->ActiveFestivals)
                    if(festival.Id!=0)store.SeeSource(DiscoveryKind.FestivalSeen,festival.Id,festival.Phase,day,at);
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
    public void WatchUnlocks(IUnlockState state){unlocks=state;state.Unlock+=OnUnlock;}
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
        DiscoveryStore store,IPluginLog log,Func<DiscoverySettings> settings,Func<bool> debug,Func<uint,uint> dutyOf,Func<uint,bool> isFish,Func<uint,bool> usable)
    {
        this.framework=framework;this.client=client;this.gui=gui;this.objects=objects;this.targets=targets;this.condition=condition;this.inventory=inventory;
        this.store=store;this.log=log;this.settings=settings;this.debug=debug;this.dutyOf=dutyOf;this.isFish=isFish;this.usable=usable;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);
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
            if(wanted.Fishing && me.ClassJob.RowId==18)ReadLine(now,me);
            if(wanted.ItemSources)
            {
                ReadSalvage(at);ReadPurify(at);ReadVenture(at);ReadLoot(at,territory,duty);
                ReadFate(now);ReadTarget(now,territory,map);ReadLearned(at,now,territory,map);ReadSeasonal(at,now);ReadShopNpc(at,territory,map);ReadCasters(at,territory,map);
            }
            var counts=Counts();
            if(before is null){before=counts;return;}
            var gains=ItemGains.Gains(before,counts);var losses=ItemGains.Losses(before,counts);before=counts;
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
                var quantity=(int)Math.Min(count,100_000);var added=false;var kind=DiscoveryKind.ItemFromItem;
                switch(cause)
                {
                    case ItemGains.Cause.Fishing when isFish(item):
                        // X-75.3 (0.877): the cast's flags (FishingEventHandler): a mooch is put down to the fish caught before, not the bait in hand; a lure is kept.
                        var flags=events!=null && events->EventHandlerModule.FishingEventHandler is var handler && handler!=null ? (int)handler->CurrentCastBaitFlags:0;
                        var bait=(flags & 16)!=0 && lastFish!=0 ? lastFish:PlayerState.Instance() is var state && state!=null ? state->FishingBait:0u;
                        var weather=FFXIVClientStructs.FFXIV.Client.Game.WeatherManager.Instance() is var manager && manager!=null ? manager->GetCurrentWeather():(byte)0;
                        added=store.SeeSource(DiscoveryKind.FishCatch,item,bait,quantity,at,territory,map,me.Position);
                        store.SeeSource(DiscoveryKind.FishCondition,item,weather,EorzeaMinute(at),at,territory);
                        if((flags & 3) is 1 or 2)store.SeeSource(DiscoveryKind.FishLure,item,(uint)(flags & 3),1,at,territory);
                        if(PreviousWeather?.Invoke(territory,at) is { } before and not 0)store.SeeSource(DiscoveryKind.FishTransition,item,before,EorzeaMinute(at),at,territory);
                        lastFish=item;
                        if(bitAt>castStarted && castStarted>0 && !castChum && bitAt-castStarted is > 0 and < 600_000)
                            store.SeeSource(DiscoveryKind.FishBite,item,biteStrength,(int)((bitAt-castStarted)/100),at,territory,map,me.Position);
                        bitAt=-100_000;
                        kind=DiscoveryKind.FishCatch;break;
                    case ItemGains.Cause.FateReward:
                        added=store.SeeSource(DiscoveryKind.FateReward,item,fateId,quantity,at);kind=DiscoveryKind.FateReward;break;
                    case ItemGains.Cause.MonsterDrop:
                        added=store.SeeSource(DiscoveryKind.MonsterDrop,item,death.Name,quantity,at,death.Territory,death.Map,death.Place);kind=DiscoveryKind.MonsterDrop;break;
                    case ItemGains.Cause.Opened:
                        added=store.SeeSource(DiscoveryKind.ItemFromItem,item,spent!.Value,quantity,at);break;
                    case ItemGains.Cause.DungeonCoffer:
                        added=store.SeeSource(DiscoveryKind.DutyChest,item,duty,quantity,at);kind=DiscoveryKind.DutyChest;break;
                    default:continue;
                }
                if(added && debug())log.Information("Discovery: {0} item {1} ×{2}.",kind,item,quantity);
            }
        }
        catch(Exception error){before=null;if(!warned){warned=true;log.Warning(error,"The item discoveries could not be read.");}}
    }

    // Eorzea minute of the day (175 s an Eorzea hour), as Tensho's clocks count it.
    private static int EorzeaMinute(DateTimeOffset at)=>(int)(at.ToUnixTimeMilliseconds()*3600/175/1000/60%1440);

    private Dictionary<uint,long> Counts()
    {
        var counts=new Dictionary<uint,long>();
        foreach(var bag in Bags)
            foreach(var slot in inventory.GetInventoryItems(bag))
                if(!slot.IsEmpty && slot.BaseItemId!=0 && slot.Quantity>0)counts[slot.BaseItemId]=counts.GetValueOrDefault(slot.BaseItemId)+slot.Quantity;
        return counts;
    }

    private void ReadSalvage(DateTimeOffset at)
    {
        var agent=AgentSalvage.Instance();
        if(agent==null || !agent->IsSalvageResultAddonOpen || agent->DesynthItemId==0){salvageSeen="";return;}
        var source=agent->DesynthItemId%1_000_000;var results=new List<(uint Item,int Count)>();
        foreach(var result in agent->DesynthResults)if(result.ItemId!=0 && result.Quantity>0)results.Add((result.ItemId%1_000_000,result.Quantity));
        var signature=source+":"+string.Join(",",results.Select(result=>$"{result.Item}x{result.Count}"));
        if(results.Count==0 || signature==salvageSeen)return;
        salvageSeen=signature;
        foreach(var (item,count) in results)store.SeeSource(DiscoveryKind.Desynthesis,item,source,count,at);
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

    private void ReadVenture(DateTimeOffset at)
    {
        if(gui.GetAddonByName("RetainerTaskResult") is not { IsNull:false,IsVisible:true }){ventureSeen="";return;}
        var agent=AgentRetainerTask.Instance();
        if(agent==null)return;
        var data=agent->RetainerData;var task=(uint)data.RewardRetainerTaskId;
        if(task==0)return;
        var rewards=new List<(uint Item,int Count)>();
        for(var index=0;index<data.RewardItemIds.Length && index<data.RewardItemCount.Length;index++)
            if(data.RewardItemIds[index] is var id and not 0)rewards.Add((id%1_000_000,(int)Math.Min(data.RewardItemCount[index],100_000u)));
        var signature=task+":"+string.Join(",",rewards.Select(reward=>$"{reward.Item}x{reward.Count}"));
        if(rewards.Count==0 || signature==ventureSeen)return;
        ventureSeen=signature;
        foreach(var (item,count) in rewards)store.SeeSource(DiscoveryKind.Venture,item,task,count,at);
        if(debug())log.Information("Discovery: venture {0}.",signature);
    }

    private void ReadLoot(DateTimeOffset at,uint territory,uint duty)
    {
        if(territory!=lootTerritory){lootSeen.Clear();lootTerritory=territory;}
        if(duty==0)return;
        var loot=Loot.Instance();
        if(loot==null)return;
        foreach(var item in loot->Items)
        {
            if(item.ItemId==0 || item.ChestObjectId==0 || lootSeen.Count>=256 || !lootSeen.Add((item.ChestObjectId,item.ChestItemIndex,item.ItemId)))continue;
            store.SeeSource(DiscoveryKind.DutyChest,item.ItemId%1_000_000,duty,Math.Max(1,(int)item.ItemCount),at,territory);
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

    private void ReadTarget(long now,uint territory,uint map)
    {
        if(targets.Target is not IBattleNpc { BattleNpcKind:BattleNpcSubKind.Combatant } npc || npc.CurrentHp!=0 || npc.NameId==0)return;
        if(death.Name==npc.NameId && now-death.At<5_000)return;
        death=(npc.NameId,territory,map,npc.Position,npc.Level,now);
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);
        if(unlocks is not null)unlocks.Unlock-=OnUnlock;
    }
}
