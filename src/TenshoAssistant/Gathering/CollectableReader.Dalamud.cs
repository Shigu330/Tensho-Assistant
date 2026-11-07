using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gathering;

/// <summary>
/// R-06 (0.409): the game's Collectable window (AddonGatheringMasterpiece, named fields of FFXIVClientStructs: item name, chance, GP,
/// integrity), read on the framework thread five times a second while it is open and dropped when it closes; its AtkValues are
/// copied as text for developer mode (the current collectability has no named field yet: unknown until probed in game).
/// The thresholds catalogue is copied once from the game's sheets, off the framework thread.
/// </summary>
internal sealed class CollectableReader : IDisposable
{
    private const int MaxValues=64;
    private readonly object gate=new();
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IClientState client;private readonly IDataManager data;private readonly IPluginLog log;
    private bool disposed;private long nextRead;private int warned;private int loading;
    private CollectableWindowFrame? frame;private CollectableCatalog? catalog;
    public CollectableWindowFrame? Frame=>Volatile.Read(ref frame);
    // A-15 (0.432): the collectables in the bags (Inventory1-4), read every 5 s while a view asks (WantHoldings, 5 s window).
    private IReadOnlyList<HeldCollectable>? holdings;private long holdingsWanted;private long nextHoldings;
    public IReadOnlyList<HeldCollectable>? Holdings=>Volatile.Read(ref holdings);
    public void WantHoldings()=>Volatile.Write(ref holdingsWanted,Stopwatch.GetTimestamp()+Stopwatch.Frequency*5);
    public CollectableCatalog? Catalog
    {
        get
        {
            if(Volatile.Read(ref catalog) is { } ready)return ready;
            if(Interlocked.Exchange(ref loading,1)==0)_=Task.Run(Load);
            return null;
        }
    }

    /// <summary>R-14 (0.454): no retry this session (a lost sheet fails on every call): the pages say « unavailable », not « reading ».</summary>
    public bool CatalogFailed { get; private set; }
    /// <summary>R-14 (0.454): the last read of the bags failed; cleared by a good read.</summary>
    public bool HoldingsUnavailable { get; private set; }

    public CollectableReader(IFramework framework,IGameGui gui,IClientState client,IDataManager data,IPluginLog log)
    {this.framework=framework;this.gui=gui;this.client=client;this.data=data;this.log=log;TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);}

    private void Load()
    {
        try
        {
            var scripRows=new HashSet<uint>();
            foreach(var shop in data.GetExcelSheet<CollectablesShop>())
                if(shop.RewardType==1)foreach(var row in shop.ShopItems)if(row.RowId!=0)scripRows.Add(row.RowId);
            var rules=new List<CollectableRule>();
            foreach(var group in data.GetSubrowExcelSheet<CollectablesShopItem>())
                foreach(var row in group)
                {
                    if(row.Item.RowId==0 || row.CollectablesShopRefine.ValueNullable is not { } refine)continue;
                    var reward=row.CollectablesShopRewardScrip.ValueNullable;
                    rules.Add(new(row.Item.RowId,row.Item.ValueNullable?.Name.ExtractText() ?? "",row.LevelMin,row.LevelMax,
                        refine.LowCollectability,refine.MidCollectability,refine.HighCollectability,
                        reward?.LowReward ?? 0,reward?.MidReward ?? 0,reward?.HighReward ?? 0,scripRows.Contains(row.RowId),
                        row.CollectablesShopItemGroup.ValueNullable?.Name.ExtractText() ?? "",reward?.Currency ?? -1));
                }
            Volatile.Write(ref catalog,new CollectableCatalog(rules));
        }
        catch(Exception error){CatalogFailed=true;log.Warning(error,"Collectable thresholds unavailable.");}
    }

    private unsafe void Update(IFramework unused)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var now=Stopwatch.GetTimestamp();if(now<nextRead)return;nextRead=now+Stopwatch.Frequency/5;
            ReadHoldings(now);
            try
            {
                var handle=gui.GetAddonByName("GatheringMasterpiece");
                if(!client.IsLoggedIn || handle.IsNull || !handle.IsReady || !handle.IsVisible || gui.GameUiHidden){Volatile.Write(ref frame,null);return;}
                var addon=(AddonGatheringMasterpiece*)handle.Address;var unit=(AtkUnitBase*)handle.Address;
                if(unit->RootNode==null){Volatile.Write(ref frame,null);return;}
                var values=new List<string>();
                for(var index=0;index<Math.Min((int)unit->AtkValuesCount,MaxValues);index++)
                {
                    var value=unit->AtkValues[index];
                    values.Add(value.Type switch
                    {
                        AtkValueType.Int=>$"{index}:int {value.Int}",
                        AtkValueType.UInt=>$"{index}:uint {value.UInt}",
                        AtkValueType.Bool=>$"{index}:bool {value.Byte!=0}",
                        AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString=>$"{index}:text {Short(value.GetValueAsString())}",
                        _=>$"{index}:{value.Type}",
                    });
                }
                // 0.480: the window's percentages (Intuition's chance is not among its values), for the log in developer mode. After the values, so their positions do not move.
                if(unit->UldManager.NodeList!=null)
                    for(var index=0;index<Math.Min((int)unit->UldManager.NodeListCount,256);index++)
                    {
                        var node=unit->UldManager.NodeList[index];
                        if(node==null || node->Type!=NodeType.Text)continue;
                        var shown=((AtkTextNode*)node)->NodeText;
                        if(shown.Length is >0 and <=64 && shown.ToString().Contains('%'))values.Add($"node{node->NodeId}:{Short(shown.ToString())}");
                    }
                var name=Text(addon->ItemName);
                if(name is null){Volatile.Write(ref frame,null);return;}
                Volatile.Write(ref frame,new CollectableWindowFrame(name,Text(addon->ObtainChance),Collectables.Number(Text(addon->GPLeftover)),Collectables.Number(Text(addon->GPTotal)),
                    Collectables.Number(Text(addon->IntegrityLeftover)),Collectables.Number(Text(addon->IntegrityTotal)),values.ToArray(),
                    unit->X,unit->Y,unit->RootNode->Width*unit->Scale,DateTimeOffset.UtcNow));
            }
            catch(Exception error)
            {
                Volatile.Write(ref frame,null);nextRead=now+Stopwatch.Frequency*5;
                if(Interlocked.Exchange(ref warned,1)==0)log.Warning(error,"Collectable window unavailable; retry in five seconds.");
            }
        }
    }

    private unsafe void ReadHoldings(long now)
    {
        if(now<nextHoldings || now>Volatile.Read(ref holdingsWanted))return;
        nextHoldings=now+Stopwatch.Frequency*5;
        try
        {
            if(!client.IsLoggedIn){Volatile.Write(ref holdings,null);return;}
            var manager=FFXIVClientStructs.FFXIV.Client.Game.InventoryManager.Instance();
            if(manager==null)return;
            var held=new List<HeldCollectable>();
            foreach(var type in new[]{FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory1,FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory2,
                FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory3,FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory4})
            {
                var container=manager->GetInventoryContainer(type);
                // R-14 (0.454): a bag not loaded yet: no incomplete list, the last read (or « Reading… ») stays.
                if(container==null || !container->IsLoaded)return;
                for(var slot=0;slot<container->Size && slot<64;slot++)
                {
                    var item=container->GetInventorySlot(slot);
                    if(item==null || item->GetItemId()==0 || !item->IsCollectable())continue;
                    held.Add(new(item->GetBaseItemId(),item->GetSpiritbondOrCollectability()));
                }
            }
            Volatile.Write(ref holdings,held.ToArray());HoldingsUnavailable=false;
        }
        catch(Exception error){Volatile.Write(ref holdings,null);HoldingsUnavailable=true;if(Interlocked.Exchange(ref warned,1)==0)log.Warning(error,"Collectables in the bags could not be read.");}
    }

    private static string Short(string? text)=>text is null ? "" : text.Length>48 ? text[..48]+"…":text;
    private static unsafe string? Text(AtkTextNode* node)
    {
        if(node==null || !node->AtkResNode.IsVisible() || node->NodeText.Length>128)return null;
        return Dalamud.Game.Text.SeStringHandling.SeString.Parse(node->NodeText.AsSpan()).TextValue;
    }

    public void Dispose(){lock(gate){if(disposed)return;disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);Volatile.Write(ref frame,null);}}
}
