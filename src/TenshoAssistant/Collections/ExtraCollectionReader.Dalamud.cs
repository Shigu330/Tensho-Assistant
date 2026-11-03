using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Collections;

/// <summary>
/// g, C-04 (0.365): collection types beyond the eight of CollectionReader — hairstyles (CharaMakeCustomize sold as an item), fashion
/// accessories (Ornament), glasses (Glasses), chocobo bardings (BuddyEquip), portrait frames, backgrounds and decorations (BannerFrame,
/// BannerBg, BannerDecoration), master recipe books (SecretRecipeBook), McGuffins (McGuffin) and Occult Crescent lore (MKDLore), each
/// with its Dalamud IUnlockState answer. Read on the framework thread, 2 ms per frame at most, while the Collections page asked for it
/// in the last 5 seconds, again 30 seconds after the last read; logging out clears it. Not cached on disk (cheap to read again).
/// </summary>
public sealed class ExtraCollectionReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IUnlockState unlocks;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? state;
    private List<Func<IEnumerable<CollectionEntry>>>? steps;
    private List<CollectionEntry> building=[];
    private int step;
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    private bool disposed,warned;

    public IReadOnlyList<CollectionEntry>? State=>Volatile.Read(ref state);
    public bool Unavailable { get; private set; }

    public ExtraCollectionReader(IFramework framework,IClientState client,IPlayerState player,IUnlockState unlocks,IDataManager data,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.player=player;this.unlocks=unlocks;this.data=data;this.log=log;
        framework.Update+=OnUpdate;
    }

    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        // R-14 (0.453): before the character is loaded the game answers « not done » to everything: unknown, not zero.
        if(!client.IsLoggedIn || !player.IsLoaded){steps=null;if(State is not null)Volatile.Write(ref state,null);return;}
        try
        {
            if(steps is null)
            {
                if(now<nextRead || now>new DateTimeOffset(Volatile.Read(ref wantedUntilTicks),TimeSpan.Zero))return;
                steps=Steps();step=0;building=[];
            }
            var start=Stopwatch.GetTimestamp();
            while(step<steps.Count)
            {
                // Logged in, IUnlockState answers both ways: « not unlocked » is a read « not acquired », as CollectionStateOf for mounts.
                building.AddRange(steps[step++]().Select(entry=>entry.ConfirmedUnlocked ? entry:entry with {NegativeConfirmed=true}));
                if(Stopwatch.GetElapsedTime(start)>=TimeSpan.FromMilliseconds(2))return;
            }
            Volatile.Write(ref state,building.ToArray());
            steps=null;building=[];nextRead=now.AddSeconds(30);Unavailable=false;warned=false;
        }
        catch(Exception error)
        {
            steps=null;building=[];Unavailable=true;nextRead=now.AddSeconds(60);
            if(!warned){warned=true;log.Warning(error,"Extra collection types could not be read.");}
        }
    }

    // 0.387 (user report: no « how to obtain » in these tabs): the item that teaches a fashion accessory (ItemAction 20086) or a barding
    // (ItemAction 1013), Data[0] = the Ornament / BuddyEquip row (docs/evidence/0.387-collection-items.md). Built once, first item kept.
    private Dictionary<(uint Action,uint Row),uint>? teaching;
    private uint TeachingItem(uint action,uint row)
    {
        teaching??=data.GetExcelSheet<Item>().Where(item=>item.ItemAction.RowId!=0 && item.ItemAction.ValueNullable is { } use && use.Action.RowId is 20086 or 1013 && use.Data[0]!=0)
            .GroupBy(item=>(item.ItemAction.Value.Action.RowId,(uint)item.ItemAction.Value.Data[0])).ToDictionary(group=>group.Key,group=>group.Min(item=>item.RowId));
        return teaching.GetValueOrDefault((action,row));
    }

    // One step per sheet (each a few hundred rows at most); a failing sheet fails the read, logged once.
    private List<Func<IEnumerable<CollectionEntry>>> Steps()=>
    [
        ()=>data.GetExcelSheet<CharaMakeCustomize>().Where(row=>row.IsPurchasable && row.HintItem.RowId!=0 && row.HintItem.ValueNullable is not null)
            .GroupBy(row=>row.HintItem.RowId)
            .Select(group=>new CollectionEntry(CollectionKind.Hairstyle,group.Key,group.First().HintItem.Value.Name.ExtractText(),group.First().Icon,
                group.Any(row=>unlocks.IsCharaMakeCustomizeUnlocked(row)),ItemId:group.Key))
            .Where(entry=>entry.Name.Length>0),
        ()=>data.GetExcelSheet<Ornament>().Where(row=>row.RowId!=0 && !row.Singular.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.Ornament,row.RowId,row.Singular.ExtractText(),row.Icon,unlocks.IsOrnamentUnlocked(row),Number:row.Order,ItemId:TeachingItem(20086,row.RowId))),
        ()=>data.GetExcelSheet<Glasses>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.Glasses,row.RowId,row.Name.ExtractText(),(uint)Math.Max(0,row.Icon),unlocks.IsGlassesUnlocked(row))),
        ()=>data.GetExcelSheet<BuddyEquip>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.Barding,row.RowId,row.Name.ExtractText(),row.IconHead!=0 ? row.IconHead:row.IconBody!=0 ? row.IconBody:row.IconLegs,
                unlocks.IsBuddyEquipUnlocked(row),ItemId:TeachingItem(1013,row.RowId))),
        ()=>data.GetExcelSheet<BannerFrame>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.Portrait,row.RowId,row.Name.ExtractText(),(uint)Math.Max(0,row.Icon),unlocks.IsBannerFrameUnlocked(row),Description:"Frame")),
        ()=>data.GetExcelSheet<BannerBg>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.Portrait,100000+row.RowId,row.Name.ExtractText(),(uint)Math.Max(0,row.Icon),unlocks.IsBannerBgUnlocked(row),Description:"Background")),
        ()=>data.GetExcelSheet<BannerDecoration>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.Portrait,200000+row.RowId,row.Name.ExtractText(),(uint)Math.Max(0,row.Icon),unlocks.IsBannerDecorationUnlocked(row),Description:"Decoration")),
        ()=>data.GetExcelSheet<SecretRecipeBook>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.MasterBook,row.RowId,row.Name.ExtractText(),row.Item.ValueNullable?.Icon ?? 0,unlocks.IsSecretRecipeBookUnlocked(row),ItemId:row.Item.RowId)),
        ()=>data.GetExcelSheet<McGuffin>().Where(row=>row.RowId!=0 && row.UIData.ValueNullable is { } ui && !ui.Name.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.McGuffin,row.RowId,row.UIData.Value.Name.ExtractText(),row.UIData.Value.Icon,unlocks.IsMcGuffinUnlocked(row),Number:row.UIData.Value.Order)),
        ()=>data.GetExcelSheet<MKDLore>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty)
            .Select(row=>new CollectionEntry(CollectionKind.OccultLore,row.RowId,row.Name.ExtractText(),0,unlocks.IsMKDLoreUnlocked(row),Description:row.Description.ExtractText())),
    ];

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
