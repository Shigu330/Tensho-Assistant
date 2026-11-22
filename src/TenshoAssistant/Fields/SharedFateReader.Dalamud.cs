using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Fields;

/// <summary>
/// X-213: the shared FATE zones from the game's sheets (<see cref="SharedFateZone"/>): FateProgressUI, and each zone's gemstone trader
/// (FateShop rows, keyed like the NPC, placed in the zone by a Level row of type 8), its special shops rank by rank, their articles and
/// prices, the achievement the last rank's articles need. Read off the frame, once (the Level sheet is long). Probed on 22/11 (evidence 0.1781).
/// </summary>
internal static class SharedFateLoader
{
    public static IReadOnlyList<SharedFateZone> Read(IDataManager data)
    {
        var traders=data.GetExcelSheet<FateShop>().Where(row=>row.SpecialShop.Any(shop=>shop.RowId!=0)).ToDictionary(row=>row.RowId);
        var traderOf=new Dictionary<uint,uint>();
        foreach(var level in data.GetExcelSheet<Level>())
            if(level.Type==8 && traders.ContainsKey(level.Object.RowId))traderOf.TryAdd(level.Territory.RowId,level.Object.RowId);
        var shops=data.GetExcelSheet<SpecialShop>();
        var items=data.GetExcelSheet<Item>();
        var achievements=data.GetExcelSheet<Achievement>();
        var zones=new List<SharedFateZone>();
        foreach(var row in data.GetExcelSheet<FateProgressUI>())
        {
            var territory=row.Location.RowId;
            if(territory==0)continue;
            int[] next=[row.ReqFatesToRank2,row.ReqFatesToRank3,row.ReqFatesToRank4];
            var maxRank=SharedFates.MaxRank(next);
            var name=row.Location.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() is { Length:>0 } place ? place:territory.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var rewards=new List<SharedFateReward>();
            string? achievement=null;
            var known=traderOf.TryGetValue(territory,out var trader);
            if(known)
            {
                var byRank=new List<IReadOnlyList<SharedFateShopEntry>>();
                foreach(var shop in traders[trader].SpecialShop)
                {
                    var entries=new List<SharedFateShopEntry>();
                    if(shop.RowId!=0 && shops.TryGetRow(shop.RowId,out var special))
                        foreach(var entry in special.Item)
                        {
                            var cost=entry.ItemCosts.FirstOrDefault(price=>price.CurrencyCost>0);
                            foreach(var receive in entry.ReceiveItems)
                                if(receive.Item.RowId!=0)entries.Add(new(receive.Item.RowId,cost.ItemCost.RowId,(int)cost.CurrencyCost,entry.AchievementUnlock.RowId));
                        }
                    byRank.Add(entries);
                }
                var gate=byRank.SelectMany(entries=>entries).Select(entry=>entry.Gate).FirstOrDefault(id=>id!=0);
                if(gate!=0 && achievements.TryGetRow(gate,out var unlock) && unlock.Name.ExtractText() is { Length:>0 } title)achievement=title;
                foreach(var (entry,rank) in SharedFates.Ranks(maxRank,byRank))
                    if(items.TryGetRow(entry.Item,out var item))
                        rewards.Add(new(entry.Item,item.Name.ExtractText(),item.Icon,entry.CostItem,items.TryGetRow(entry.CostItem,out var currency) ? currency.Name.ExtractText():"",entry.Cost,rank));
            }
            zones.Add(new(territory,name,row.DisplayOrder,maxRank,[..next.Take(maxRank-1)],rewards,achievement,known));
        }
        return [..zones.OrderBy(zone=>zone.Order)];
    }
}

/// <summary>
/// X-213: once a second on the framework thread, while the game's Shared FATE window (<see cref="SharedFates.Window"/>) is open: each zone
/// of AgentFateProgress (three tabs of six zones: territory, rank, highest rank, FATEs toward the next rank and FATEs it asks), read only (no
/// game function called, nothing asked of the game), merged into the character's copy kept in the settings (<c>store</c>, saved by
/// <c>save</c> when it changes). The window closed: nothing read. Cut in competitive content like the rest. An agent lost after a game
/// update is said once in the log (R08); the copy stays as it was.
/// </summary>
public sealed unsafe class SharedFateReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<Dictionary<ulong,Dictionary<uint,SharedFateRead>>> store;
    private readonly System.Action save;
    private ulong character;
    private bool stamped,disposed,warned;
    private long nextLook=-100_000;
    private IReadOnlyDictionary<uint,SharedFateRead>? shown;

    /// <summary>What was read for the character logged in (null: nobody logged in, or the window never read for this character).</summary>
    public IReadOnlyDictionary<uint,SharedFateRead>? Shown=>Volatile.Read(ref shown);

    public SharedFateReader(IFramework framework,IClientState client,IPlayerState player,IGameGui gui,IPluginLog log,
        Func<Dictionary<ulong,Dictionary<uint,SharedFateRead>>> store,System.Action save)
    {
        this.framework=framework;this.client=client;this.player=player;this.gui=gui;this.log=log;this.store=store;this.save=save;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed)return;
        var now=Environment.TickCount64;
        if(now-nextLook<1000)return;
        nextLook=now;
        if(!client.IsLoggedIn || !player.IsLoaded || player.ContentId==0){character=0;stamped=false;Volatile.Write(ref shown,null);return;}
        if(player.ContentId!=character)
        {
            character=player.ContentId;stamped=false;
            Volatile.Write(ref shown,store().GetValueOrDefault(character));
        }
        var handle=gui.GetAddonByName(SharedFates.Window);
        if(handle.IsNull || !handle.IsReady || !handle.IsVisible){stamped=false;return;}
        try
        {
            var agent=AgentFateProgress.Instance();
            if(agent==null)throw new InvalidOperationException("AgentFateProgress did not resolve.");
            var seen=new List<SharedFateSeen>();
            string? texts=null;
            foreach(var tab in agent->Tabs)
                foreach(var zone in tab.Zones)
                {
                    if(zone.TerritoryTypeId==0)continue;
                    seen.Add(new(zone.TerritoryTypeId,zone.CurrentRank,zone.MaxRank,zone.FateProgress,zone.NeededFates));
                    texts??=$"« {zone.RankText} » « {zone.ProgressText} »";
                }
            var all=store();
            var valid=seen.Count(SharedFates.Valid);
            if(SharedFates.Merged(all.GetValueOrDefault(character),seen,DateTimeOffset.UtcNow,!stamped && valid>0) is { } merged)
            {
                all[character]=merged;
                Volatile.Write(ref shown,merged);
                save();
            }
            if(!stamped && valid>0)
            {
                stamped=true;
                // Once per opening, for the in-game check (R06): what the window held, and the game's own texts of its first zone.
                log.Information("Shared FATE window read (X-213): {0} zones, {1} kept; first zone's texts {2}.",seen.Count,valid,texts ?? "none");
            }
            warned=false;
        }
        catch(Exception error)
        {
            nextLook=now+60_000;
            if(!warned){warned=true;log.Warning(error,"The Shared FATE window could not be read (perhaps after a game update); the copy stays as it was.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
