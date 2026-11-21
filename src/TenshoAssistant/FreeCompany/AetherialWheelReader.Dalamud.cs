using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.FreeCompany;

/// <summary>
/// X-225 (0.1719, the user's request of 12/11): the company's wheel stand, read in its window (AetherialWheel) once a second while it is open:
/// each slot's wheel and charge (layout read on 21/11 and checked at each reading: docs/evidence/0.1234-aetherial-wheels.md), kept per house in
/// the configuration with when it is primed (AetherialWheels.Update), for the Reminders page and the automatic alarms. Read-only; nothing
/// during a cut (R03.2). A window not as read on 21/11 is said once and nothing is read (R08); a failure stops the reader, said once.
/// </summary>
internal sealed unsafe class AetherialWheelReader : IDisposable
{
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IDataManager data;private readonly IPluginLog log;
    private readonly Configuration configuration;private readonly Action save;private readonly Func<bool> developer;
    private Dictionary<uint,AetherialWheelRow>? rows;
    private readonly Dictionary<uint,string> names=[];
    private (IReadOnlyList<AetherialWheelRead> Slots,DateTimeOffset At)? before;
    private long readAt=-100_000;
    private bool failed,layoutSaid,houseSaid,noSlotSaid,disposed;
    private string lastSaid="";

    public AetherialWheelReader(IFramework framework,IGameGui gui,IDataManager data,IPluginLog log,Configuration configuration,Action save,Func<bool> developer)
    {
        this.framework=framework;this.gui=gui;this.data=data;this.log=log;this.configuration=configuration;this.save=save;this.developer=developer;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed || failed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var tick=Environment.TickCount64;
        if(tick-readAt<1000)return;
        readAt=tick;
        try
        {
            var handle=gui.GetAddonByName("AetherialWheel");
            if(handle.IsNull || !handle.IsVisible){before=null;return;}
            var unit=(AtkUnitBase*)handle.Address;
            if(unit->AtkValues==null)return;
            var values=new int?[Math.Min((int)unit->AtkValuesCount,AetherialWheels.First+AetherialWheels.Stride*AetherialWheels.MaximumSlots)];
            for(var index=0;index<values.Length;index++)
            {
                var value=unit->AtkValues[index];
                values[index]=value.Type==AtkValueType.Int ? (int?)value.Int:value.Type==AtkValueType.UInt && value.UInt<=int.MaxValue ? (int?)(int)value.UInt:null;
            }
            rows??=Rows();
            // X-726 (0.1721): no slot shown yet: nothing read, the saved wheels and their alarms stay (developer's log only).
            if(AetherialWheels.NoSlotYet(values))
            {
                if(developer() && !noSlotSaid){noSlotSaid=true;log.Information("Aetherial wheels: the stand's window shows no slot yet: not read.");}
                return;
            }
            if(AetherialWheels.Parse(values,Row) is not { } read)
            {
                if(!layoutSaid){layoutSaid=true;log.Warning("Aetherial wheels: the stand's window is not as read on 21/11 (maybe after a game update): the wheels are not read.");}
                return;
            }
            var house=HouseId();
            if(house==0)
            {
                if(!houseSaid){houseSaid=true;log.Information("Aetherial wheels: the stand's window is open but the house is not known: not read.");}
                return;
            }
            var now=DateTimeOffset.UtcNow;
            var updated=AetherialWheels.Update(configuration.AetherialWheels,house,read,before,now,Row,Name);
            before=(read,now);
            if(!AetherialWheels.SameAs(configuration.AetherialWheels,updated)){configuration.AetherialWheels=updated;save();}
            if(developer() && AetherialWheels.Describe(read) is var said && said!=lastSaid){lastSaid=said;log.Information("Aetherial wheels read: {0}.",said);}
        }
        catch(Exception error){failed=true;log.Warning(error,"Aetherial wheels could not be read (maybe after a game update): the Reminders page keeps the last reading.");}
    }

    /// <summary>The game's wheels, by their item primed or not (the AetherialWheel sheet; rows without an item or hours left out).</summary>
    private Dictionary<uint,AetherialWheelRow> Rows()
    {
        var found=new Dictionary<uint,AetherialWheelRow>();
        foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.AetherialWheel>())
        {
            if(row.ItemUnprimed.RowId==0 || row.HoursRequired==0)continue;
            var wheel=new AetherialWheelRow(row.ItemUnprimed.RowId,row.ItemPrimed.RowId,row.Grade,row.HoursRequired);
            found.TryAdd(wheel.Unprimed,wheel);
            if(wheel.Primed!=0)found.TryAdd(wheel.Primed,wheel);
        }
        return found;
    }

    private AetherialWheelRow? Row(uint item)=>rows?.GetValueOrDefault(item);

    /// <summary>An item's name in the client's language (the Item sheet), read once.</summary>
    private string Name(uint item)
    {
        if(names.TryGetValue(item,out var known))return known;
        var name=data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(item)?.Name.ExtractText() ?? "";
        names[item]=name;
        return name;
    }

    private static ulong HouseId()
    {
        if((nint)HousingManager.MemberFunctionPointers.GetCurrentHouseId==0)return 0;
        var housing=HousingManager.Instance();
        return housing==null ? 0:housing->GetCurrentHouseId().Id;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
