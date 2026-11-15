using FFXIVClientStructs.FFXIV.Client.Game;
using TenshoAssistant.Inventory;

namespace TenshoAssistant.Character;

/// <summary>
/// X-196 (0.1409): every retainer of the logged-in character (RetainerManager, known once the retainer list was opened at a summoning bell),
/// with its stock from the game's item finder copy (X-307, RetainerFinderReader), joined by the retainer id here and not kept. Framework
/// thread only; values are copied. Null when the list was not opened, or when the game's layout looks moved (R08: unknown, never guessed).
/// </summary>
internal static unsafe class RetainerReader
{
    public static RetainerState[]? Read(Func<byte,string>? jobName,Func<byte,string>? townName)
    {
        var manager=RetainerManager.Instance();
        if(manager==null || !manager->IsReady)return null;
        var count=(int)manager->GetRetainerCount();
        if(count<0 || count>CharacterStatuses.MaximumVentures)return null;
        IReadOnlyList<FinderRetainer>? finder;
        try{finder=RetainerFinderReader.Read();}
        catch(Exception){finder=null;}
        var found=new List<RetainerState>();
        for(var index=0;index<count;index++)
        {
            var retainer=manager->GetRetainerBySortedIndex((uint)index);
            if(retainer==null || retainer->RetainerId==0)continue;
            var id=retainer->RetainerId;
            var copy=finder?.FirstOrDefault(row=>row.RetainerId==id);
            found.Add(RetainerStates.From(retainer->NameString,Name(jobName,retainer->ClassJob),retainer->Level,retainer->Gil,retainer->ItemCount,
                retainer->MarketItemCount,retainer->MarketExpire,retainer->VentureId,retainer->VentureComplete,Name(townName,(byte)retainer->Town),
                copy is null ? null:RetainerFinderCompare.Stock(copy)));
        }
        return [.. found];
    }

    private static string Name(Func<byte,string>? name,byte id)
    {
        if(name is null || id==0)return "";
        try{return name(id);}
        catch(Exception){return "";}
    }
}
