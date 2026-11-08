using TenshoAssistant.Inventory;

namespace TenshoAssistant.Gear;

/// <summary>One piece of a game gear set: slot, item (HQ apart) and its materia.</summary>
public sealed record GearsetPiece(GearSlot Slot,uint ItemId,bool Hq,IReadOnlyList<MeldedMateria> Materia);

/// <summary>A gear set of the game (RaptureGearsetModule) as copied on the framework thread: number shown in game, name, job, item level.</summary>
public sealed record GameGearset(int Number,string Name,uint Job,int ItemLevel,IReadOnlyList<GearsetPiece> Pieces);

/// <summary>The game's gear sets and the job levels (ClassJob row → level; null when not read), copied together.</summary>
public sealed record GearsetState(IReadOnlyList<GameGearset> Sets,IReadOnlyDictionary<uint,int> JobLevels,IReadOnlyList<JobProgressRow>? Jobs=null)
{
    /// <summary>
    /// X-76 (0.955): the same content as <paramref name="before"/>: sets (number, name, job, item level, pieces with item, HQ and materia),
    /// job levels and progress. The reader then keeps the old object, so the caches keyed on its reference are not rebuilt every read.
    /// </summary>
    public bool SameAs(GearsetState? before)
    {
        if(ReferenceEquals(before,this))return true;
        if(before is null || before.Sets.Count!=Sets.Count || before.JobLevels.Count!=JobLevels.Count || (before.Jobs is null)!=(Jobs is null))return false;
        for(var index=0;index<Sets.Count;index++)
        {
            var (old,now)=(before.Sets[index],Sets[index]);
            if(old.Number!=now.Number || !string.Equals(old.Name,now.Name,StringComparison.Ordinal) || old.Job!=now.Job || old.ItemLevel!=now.ItemLevel
               || old.Pieces.Count!=now.Pieces.Count)return false;
            for(var piece=0;piece<now.Pieces.Count;piece++)
            {
                var (was,current)=(old.Pieces[piece],now.Pieces[piece]);
                if(was.Slot!=current.Slot || was.ItemId!=current.ItemId || was.Hq!=current.Hq || !was.Materia.SequenceEqual(current.Materia))return false;
            }
        }
        foreach(var (job,level) in JobLevels)
            if(!before.JobLevels.TryGetValue(job,out var was) || was!=level)return false;
        return Jobs is null || before.Jobs!.SequenceEqual(Jobs);
    }
}

/// <summary>A better piece the player already owns for one slot of a set: the set's piece (null = empty slot) and the owned one.</summary>
public sealed record GearsetUpgrade(GearSlot Slot,GearItem? Current,GearItem Better,bool Hq);

/// <summary>
/// g, M-12 (0.383): the game's gear sets on the Gear page (user's choice: shared by the TENSHO-GEAR1 code only, no third-party site;
/// sets are never changed by Tensho). For every set, the owned pieces with a higher item level that its job can wear at its level.
/// </summary>
public static class GameGearsets
{
    /// <summary>Gear set item order (GearsetItemIndex): main hand … left ring; belt (5) and soul crystal (13) have no slot.</summary>
    public static readonly GearSlot?[] SlotOrder=[GearSlot.MainHand,GearSlot.OffHand,GearSlot.Head,GearSlot.Body,GearSlot.Hands,null,GearSlot.Legs,GearSlot.Feet,
        GearSlot.Ears,GearSlot.Neck,GearSlot.Wrists,GearSlot.RingRight,GearSlot.RingLeft];

    /// <summary>The game writes an HQ item as id + 1 000 000 in several places; both forms are read (to confirm in game for gear sets).</summary>
    public static (uint ItemId,bool Hq) Split(uint raw)=>raw>=1_000_000 && raw<2_000_000 ? (raw-1_000_000,true):(raw,false);

    /// <summary>
    /// Items the character holds where gear can be worn from: live bags and storages, and its recorded armoury, bags, saddlebags and
    /// retainers. Glamour dresser, armoire and company chest are left out (gear there cannot be worn as is). Value = an HQ copy is held.
    /// </summary>
    public static IReadOnlyDictionary<uint,bool> Owned(InventorySnapshot? live,IEnumerable<BankEntry> entries,ulong character)
    {
        var owned=new Dictionary<uint,bool>();
        void Add(InventorySnapshot snapshot)
        {
            foreach(var container in snapshot.Containers.Values)
                foreach(var stack in container.Stacks)
                    if(stack.Quantity>0 && stack.Item.Id.Value!=0)
                        owned[stack.Item.Id.Value]=owned.GetValueOrDefault(stack.Item.Id.Value) || stack.Item.IsHighQuality;
        }
        if(character==0)return owned;
        if(live is not null)Add(live);
        foreach(var entry in entries)
            if(entry.Key.CharacterId==character && entry.Key.Kind is BankStorageKind.Armoury or BankStorageKind.Bags or BankStorageKind.Saddlebag
                   or BankStorageKind.PremiumSaddlebag or BankStorageKind.Retainer)
                Add(entry.Snapshot);
        return owned;
    }

    /// <summary>
    /// Owned pieces better than the set's (item level), per slot, the best first: wearable by the set's job at <paramref name="jobLevel"/>
    /// (null = level unknown, not checked), not already in the set. A ring fits both ring slots; one owned ring is proposed once.
    /// </summary>
    public static IReadOnlyList<GearsetUpgrade> Upgrades(GameGearset set,GearCatalog catalog,IReadOnlyDictionary<uint,bool> owned,int? jobLevel)
    {
        var inSet=set.Pieces.Select(piece=>piece.ItemId).ToHashSet();
        var used=new HashSet<uint>();
        var result=new List<GearsetUpgrade>();
        foreach(var slot in SlotOrder.OfType<GearSlot>())
        {
            var current=set.Pieces.FirstOrDefault(piece=>piece.Slot==slot) is { } piece ? catalog.Find(piece.ItemId):null;
            var best=owned.Keys.Select(id=>catalog.Find(id)).OfType<GearItem>()
                .Where(item=>GearRules.Fits(item,slot) && item.WearableBy(set.Job) && (jobLevel is not { } level || item.Level<=level)
                    && !inSet.Contains(item.Id) && !used.Contains(item.Id) && item.ItemLevel>(current?.ItemLevel ?? 0))
                .OrderByDescending(item=>item.ItemLevel).ThenBy(item=>item.Id).FirstOrDefault();
            if(best is null)continue;
            used.Add(best.Id);
            result.Add(new(slot,current,best,owned[best.Id] && best.CanBeHq));
        }
        return result;
    }

    /// <summary>R-10 (0.411): the job's set with the lowest number (the one the game's own job switch would use first), or none.</summary>
    public static GameGearset? ForJob(IReadOnlyList<GameGearset> sets,uint job)=>sets.Where(set=>set.Job==job).OrderBy(set=>set.Number).FirstOrDefault();

    /// <summary>
    /// R-10 (0.411): the job of a gathering source — GatheringType 0 Mining and 1 Quarrying: miner (16); 2 Logging and 3 Harvesting:
    /// botanist (17); a fishing spot: fisher (18); any other type (5 has no readable name): none. docs/evidence/0.411-gathering-jobs.md.
    /// </summary>
    public static uint? GatheringJob(uint gatheringType,bool fishing)=>fishing ? 18u : gatheringType switch { 0 or 1=>16u, 2 or 3=>17u, _=>null };

    /// <summary>
    /// 0.385 (g, X-08): a job at random for the dailies, as one of its sets (the lowest number of each job, so a job with many sets is not
    /// favoured); with <paramref name="belowTop"/>, only jobs under the highest level among the player's jobs (levels unknown = kept).
    /// </summary>
    public static GameGearset? RandomJob(IReadOnlyList<GameGearset> sets,IReadOnlyDictionary<uint,int> levels,bool belowTop,Func<int,int> next)
    {
        var top=levels.Count>0 ? levels.Values.Max():0;
        var pool=sets.GroupBy(set=>set.Job).Select(group=>group.OrderBy(set=>set.Number).First())
            .Where(set=>!belowTop || !levels.TryGetValue(set.Job,out var level) || level<top).OrderBy(set=>set.Number).ToArray();
        return pool.Length==0 ? null:pool[Math.Clamp(next(pool.Length),0,pool.Length-1)];
    }

    /// <summary>The set as a TENSHO-GEAR1 code (same format as the Gear page's builds), with its materia.</summary>
    public static string ShareCode(GameGearset set)
        =>GearShare.Export(set.Job,set.Pieces.ToDictionary(piece=>piece.Slot,piece=>piece.ItemId),
            set.Pieces.Where(piece=>piece.Materia.Count>0).ToDictionary(piece=>piece.Slot,piece=>piece.Materia));
}
