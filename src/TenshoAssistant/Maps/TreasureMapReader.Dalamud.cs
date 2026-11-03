using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Maps;

/// <summary>The deciphered treasure map held: its spot (map, territory, world position) and the map's name.</summary>
public sealed record TreasureMapSpot(uint Rank,ushort SubKey,uint MapId,uint TerritoryId,Vector3 Position,string Name);

/// <summary>
/// g, M-14 (0.384): every 5 seconds on the framework thread while logged in, reads the deciphered treasure map
/// (EventItemManager.GetTreasureHuntRank and GetTreasureSpotSubKey, game functions called only once the game resolved them) and
/// resolves it through the game data (TreasureSpot row = rank, subrow = sub key → Level: map, territory, position; to confirm in game,
/// docs/evidence/0.384-treasure-map.md). Null when no map is deciphered or it cannot be resolved. Cleared at logout.
/// </summary>
public sealed unsafe class TreasureMapReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private TreasureMapSpot? spot;
    private (uint,ushort) lastKey;
    private DateTimeOffset nextRead;
    private bool disposed,warned;
    public TreasureMapSpot? Spot=>Volatile.Read(ref spot);

    public TreasureMapReader(IFramework framework,IClientState client,IDataManager data,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.data=data;this.log=log;
        framework.Update+=OnUpdate;
        try{client.Logout+=OnLogout;}catch{framework.Update-=OnUpdate;throw;}
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead || !client.IsLoggedIn)return;
        nextRead=now.AddSeconds(5);
        try
        {
            var manager=EventItemManager.Instance();
            if(manager==null || (nint)EventItemManager.MemberFunctionPointers.GetTreasureHuntRank==0 || (nint)EventItemManager.MemberFunctionPointers.GetTreasureSpotSubKey==0)
            {
                Volatile.Write(ref spot,null);nextRead=now.AddSeconds(60);
                // R-14 (0.454): said once, like a failed read; never a silent « no map ».
                if(!warned){warned=true;log.Warning("The treasure map could not be read: game function not resolved (perhaps after a game update).");}
                return;
            }
            var rank=manager->GetTreasureHuntRank();var sub=manager->GetTreasureSpotSubKey();
            if(rank==0){lastKey=default;Volatile.Write(ref spot,null);return;}
            if(lastKey==(rank,sub) && Volatile.Read(ref spot) is not null)return;
            lastKey=(rank,sub);
            TreasureMapSpot? found=null;
            if(data.GetSubrowExcelSheet<TreasureSpot>().TryGetSubrow(rank,sub,out var row) && row.Location.RowId!=0 && row.Location.ValueNullable is { } level
               && level.Map.RowId!=0)
            {
                var name=data.GetExcelSheet<TreasureHuntRank>().GetRowOrDefault(rank) is { } kind && kind.ItemName.RowId!=0 ? kind.ItemName.Value.Name.ExtractText():"";
                found=new(rank,sub,level.Map.RowId,level.Territory.RowId,new Vector3(level.X,level.Y,level.Z),name);
            }
            Volatile.Write(ref spot,found);
            warned=false;
        }
        catch(Exception error)
        {
            Volatile.Write(ref spot,null);nextRead=now.AddSeconds(60);
            if(!warned){warned=true;log.Warning(error,"The treasure map could not be read.");}
        }
    }

    private void OnLogout(int type,int code){lastKey=default;Volatile.Write(ref spot,null);}

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
        client.Logout-=OnLogout;
    }
}
