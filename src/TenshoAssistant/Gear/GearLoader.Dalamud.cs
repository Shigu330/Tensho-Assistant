using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gear;

/// <summary>
/// R-11 (0.310): builds the equipment catalog from the Item sheet once, on a worker (game data only, docs/evidence/0.310-gear-sheets.md),
/// and copies the worn equipment (item, HQ, melded materia) once a second on the framework thread. Removed on dispose.
/// </summary>
public sealed unsafe class GearLoader : IDisposable
{
    private readonly IFramework framework;
    private readonly IDataManager data;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly CancellationTokenSource cancellation=new();
    private GearCatalog? catalog;
    private IReadOnlyList<EquippedPiece> equipped=[];
    private DateTimeOffset nextRead;
    private bool disposed;
    private volatile bool unavailable;
    private string? lastError;

    public GearCatalog? Catalog=>Volatile.Read(ref catalog);
    private volatile bool catalogFailed;
    /// <summary>0.492 (R-14): the catalog could not be built (a sheet changed after a game update): the pages say so, not « loading ».</summary>
    public bool CatalogFailed=>catalogFailed;
    public IReadOnlyList<EquippedPiece> Equipped=>Volatile.Read(ref equipped);
    /// <summary>0.331 (R-14): the last read of the worn gear failed (perhaps after a game update); Equipped is then empty, not the old set.</summary>
    public bool Unavailable=>unavailable;

    public GearLoader(IFramework framework,IDataManager data,IClientState client,IPluginLog log)
    {
        this.framework=framework;this.data=data;this.client=client;this.log=log;
        var token=cancellation.Token;
        _=Task.Run(()=>
        {
            try{Volatile.Write(ref catalog,Build(token));}
            catch(OperationCanceledException){}
            catch(Exception error){catalogFailed=true;log.Warning(error,"Tensho gear catalog could not be built.");}
        },token);
        framework.Update+=OnUpdate;
    }

    private GearCatalog Build(CancellationToken token)
    {
        // ClassJobCategory columns are named by the English job abbreviation; bit = ClassJob row.
        var columns=typeof(ClassJobCategory).GetProperties().Where(property=>property.PropertyType==typeof(bool)).ToDictionary(property=>property.Name);
        var jobs=data.GetExcelSheet<ClassJob>(ClientLanguage.English).Where(job=>job.RowId<64 && !job.Abbreviation.IsEmpty)
            .Select(job=>(Bit:job.RowId,Column:columns.GetValueOrDefault(job.Abbreviation.ExtractText()))).Where(job=>job.Column is not null).ToArray();
        var masks=new Dictionary<uint,ulong>();
        var categories=data.GetExcelSheet<ClassJobCategory>();
        // 0.312: melding caps = ItemLevel column of the stat × BaseParam percentage of the slot / 1000 (docs/evidence/0.312-materia.md).
        var levels=data.GetExcelSheet<ItemLevel>();var baseParams=data.GetExcelSheet<BaseParam>();
        var levelColumns=new Dictionary<int,System.Reflection.PropertyInfo?>();
        foreach(var (param,name) in MeldableColumns)levelColumns[param]=typeof(ItemLevel).GetProperty(name);
        var capCache=new Dictionary<(uint Level,string Slot),IReadOnlyDictionary<int,int>>();
        IReadOnlyDictionary<int,int> CapsFor(uint level,string slotColumn)
        {
            if(capCache.TryGetValue((level,slotColumn),out var known))return known;
            var caps=new Dictionary<int,int>();
            var slotProperty=typeof(BaseParam).GetProperty(slotColumn);
            if(slotProperty is not null && levels.TryGetRow(level,out var levelRow))
                foreach(var (param,column) in levelColumns)
                {
                    if(column is null || !baseParams.TryGetRow((uint)param,out var baseParam))continue;
                    var value=Convert.ToInt32(column.GetValue(levelRow));var percent=Convert.ToInt32(slotProperty.GetValue(baseParam));
                    if(value>0 && percent>0)caps[param]=(int)Math.Round(value*percent/1000.0,MidpointRounding.AwayFromZero);
                }
            return capCache[(level,slotColumn)]=caps;
        }
        var items=new List<GearItem>();
        var bardingItems=new Dictionary<uint,uint>();
        foreach(var row in data.GetExcelSheet<Item>())
        {
            token.ThrowIfCancellationRequested();
            // 0.340: an item whose action 1013 teaches a chocobo barding (Data[0] = BuddyEquip row); the first one found is kept.
            if(row.ItemAction.RowId!=0 && row.ItemAction.IsValid && row.ItemAction.Value.Action.RowId==ChocoboBarding.TeachAction)
                bardingItems.TryAdd(row.ItemAction.Value.Data[0],row.RowId);
            if(row.EquipSlotCategory.RowId==0 || row.Name.IsEmpty || !row.EquipSlotCategory.IsValid)continue;
            var slots=row.EquipSlotCategory.Value;
            GearSlot? slot=slots.MainHand>0 ? GearSlot.MainHand:slots.OffHand>0 ? GearSlot.OffHand:slots.Head>0 ? GearSlot.Head:slots.Body>0 ? GearSlot.Body
                :slots.Gloves>0 ? GearSlot.Hands:slots.Legs>0 ? GearSlot.Legs:slots.Feet>0 ? GearSlot.Feet:slots.Ears>0 ? GearSlot.Ears:slots.Neck>0 ? GearSlot.Neck
                :slots.Wrists>0 ? GearSlot.Wrists:slots.FingerL>0 || slots.FingerR>0 ? GearSlot.RingRight:null;
            if(slot is null)continue;
            var categoryId=row.ClassJobCategory.RowId;
            if(!masks.TryGetValue(categoryId,out var mask))
            {
                mask=0;
                if(categories.TryGetRow(categoryId,out var category))
                    foreach(var (bit,column) in jobs)if(column!.GetValue(category) is true)mask|=1UL<<(int)bit;
                masks[categoryId]=mask;
            }
            if(mask==0)continue;
            var parameters=new Dictionary<int,int>();
            foreach(var (param,value) in row.BaseParam.Zip(row.BaseParamValue))if(param.RowId!=0 && value!=0)parameters[(int)param.RowId]=parameters.GetValueOrDefault((int)param.RowId)+value;
            if(row.DefensePhys>0)parameters[21]=parameters.GetValueOrDefault(21)+row.DefensePhys;
            if(row.DefenseMag>0)parameters[24]=parameters.GetValueOrDefault(24)+row.DefenseMag;
            if(row.DamagePhys>0)parameters[12]=row.DamagePhys;
            if(row.DamageMag>0)parameters[13]=row.DamageMag;
            var hq=new Dictionary<int,int>();
            if(row.CanBeHq)
                foreach(var (param,value) in row.BaseParamSpecial.Zip(row.BaseParamValueSpecial))if(param.RowId!=0 && value!=0)hq[(int)param.RowId]=hq.GetValueOrDefault((int)param.RowId)+value;
            var twoHanded=slots.MainHand>0 && slots.OffHand<0;
            var slotColumn=slot switch
            {
                GearSlot.MainHand=>twoHanded ? "TwoHandWeaponPercent":"OneHandWeaponPercent",GearSlot.OffHand=>"OffHandPercent",GearSlot.Head=>"HeadPercent",
                GearSlot.Body=>"ChestPercent",GearSlot.Hands=>"HandsPercent",GearSlot.Legs=>"LegsPercent",GearSlot.Feet=>"FeetPercent",GearSlot.Ears=>"EarringPercent",
                GearSlot.Neck=>"NecklacePercent",GearSlot.Wrists=>"BraceletPercent",_=>"RingPercent",
            };
            items.Add(new GearItem(row.RowId,row.Name.ExtractText(),row.Icon,row.LevelEquip,(int)row.LevelItem.RowId,slot.Value,twoHanded,mask,
                parameters,hq,row.MateriaSlotCount,row.CanBeHq,row.IsUnique,row.IsAdvancedMeldingPermitted,
                row.MateriaSlotCount>0 || row.IsAdvancedMeldingPermitted ? CapsFor(row.LevelItem.RowId,slotColumn):null));
        }
        // Materia that give a stat, with each grade's item and value (Materia sheet).
        var names=data.GetExcelSheet<Item>();
        var materia=data.GetExcelSheet<Materia>().Where(kind=>kind.BaseParam.RowId!=0)
            .Select(kind=>new MateriaKind(kind.RowId,(int)kind.BaseParam.RowId,kind.Item.Select((item,grade)=>(Grade:grade,ItemId:item.RowId,Name:item.RowId!=0 && names.TryGetRow(item.RowId,out var named) ? named.Name.ExtractText():"",Value:grade<kind.Value.Count ? (int)kind.Value[grade]:0))
                .Where(entry=>entry.ItemId!=0 && entry.Value>0).ToArray()))
            .Where(kind=>kind.Grades.Count>0).ToArray();
        // 0.338 (DEC-023, point 8): overmeld success rates, row = materia grade, one column per advanced socket.
        var combatRates=data.GetExcelSheet<MateriaJoinRate>().ToDictionary(row=>(int)row.RowId,row=>new OvermeldRate(row.NQOvermeldPercentSlot.ToArray(),row.HQOvermeldPercentSlot.ToArray()));
        var craftRates=data.GetExcelSheet<MateriaJoinRateGatherCraft>().ToDictionary(row=>(int)row.RowId,row=>new OvermeldRate(row.NQOvermeldPercentSlot.ToArray(),row.HQOvermeldPercentSlot.ToArray()));
        var bardings=data.GetExcelSheet<BuddyEquip>().Where(row=>!row.Name.IsEmpty)
            .Select(row=>new BardingSet(row.RowId,row.Name.ExtractText(),row.IconHead,row.IconBody,row.IconLegs,row.GrandCompany.RowId,row.Order,bardingItems.GetValueOrDefault(row.RowId)))
            .ToArray();
        return new GearCatalog(items,materia,combatRates,craftRates,bardings);
    }

    // Stats a materia can raise (BaseParam row → ItemLevel column).
    private static readonly (int Param,string Column)[] MeldableColumns=[(6,"Piety"),(10,"GP"),(11,"CP"),(19,"Tenacity"),(22,"DirectHitRate"),(27,"CriticalHit"),
        (44,"Determination"),(45,"SkillSpeed"),(46,"SpellSpeed"),(70,"Craftsmanship"),(71,"Control"),(72,"Gathering"),(73,"Perception")];

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(1);
        try
        {
            if(!client.IsLoggedIn){Volatile.Write(ref equipped,[]);return;}
            var container=InventoryManager.Instance()==null ? null:InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
            if(container==null || !container->IsLoaded)return;
            var materiaSheet=data.GetExcelSheet<Materia>();
            var pieces=new List<EquippedPiece>();
            // EquippedItems order: main hand, off hand, head, body, hands, waist (unused), legs, feet, ears, neck, wrists, right ring, left ring.
            GearSlot?[] order=[GearSlot.MainHand,GearSlot.OffHand,GearSlot.Head,GearSlot.Body,GearSlot.Hands,null,GearSlot.Legs,GearSlot.Feet,
                GearSlot.Ears,GearSlot.Neck,GearSlot.Wrists,GearSlot.RingRight,GearSlot.RingLeft];
            for(var index=0;index<order.Length && index<container->Size;index++)
            {
                if(order[index] is not { } slot)continue;
                var item=container->GetInventorySlot(index);
                if(item==null || item->GetBaseItemId()==0)continue;
                var materia=new List<(int,int)>();var melds=new List<MeldedMateria>();
                for(byte socket=0;socket<5;socket++)
                {
                    var id=item->GetMateriaId(socket);
                    if(id==0 || !materiaSheet.TryGetRow(id,out var row) || row.BaseParam.RowId==0)continue;
                    var grade=item->GetMateriaGrade(socket);
                    if(grade<row.Value.Count){materia.Add(((int)row.BaseParam.RowId,row.Value[grade]));melds.Add(new(id,grade));}
                }
                // 0.376 (g, M-06): wear and spiritbond, each guarded on its own; the percentage function only once the game resolved it.
                int? condition=null,bond=null;
                try{if((nint)InventoryItem.MemberFunctionPointers.GetConditionPercentage!=0)condition=item->GetConditionPercentage();}
                catch(Exception){condition=null;}
                try{if(!item->IsCollectable())bond=item->GetSpiritbondOrCollectability();}
                catch(Exception){bond=null;}
                pieces.Add(new(slot,item->GetBaseItemId(),item->IsHighQuality(),materia,melds,condition,bond));
            }
            Volatile.Write(ref equipped,pieces);
            unavailable=false;lastError=null;
        }
        catch(Exception error)
        {
            // 0.331: the old set is no longer published as worn after a failure; the warning is written once per distinct error.
            Volatile.Write(ref equipped,[]);unavailable=true;
            if(error.Message!=lastError){lastError=error.Message;log.Warning(error,"Tensho equipped gear could not be read.");}
            nextRead=now.AddSeconds(30);
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
        cancellation.Cancel();
        cancellation.Dispose();
    }
}
