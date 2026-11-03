using Lumina.Excel.Sheets;

namespace TenshoAssistant.Items;

/// <summary>Synchronous sheet-to-managed projection, shared with the installed-data probe. No row reference escapes.</summary>
public static class ItemUseSheetCopy
{
    public static ItemLeveUse? Craft(CraftLeve row)
    {
        if(row.RowId==0 || row.Leve.RowId==0)return null;
        var leve=row.Leve.Value;
        if(leve.DataId.RowType!=typeof(CraftLeve) || leve.DataId.RowId!=row.RowId)return null;
        if(row.Item.Count!=row.ItemCount.Count || row.Item.Count>16)throw new InvalidOperationException("Incompatible leve request structure.");
        var requests=new List<ItemUseRequest>();
        for(var index=0;index<row.Item.Count;index++)
            if(row.Item[index].RowId!=0)requests.Add(new(row.Item[index].RowId,row.ItemCount[index]>0 ? row.ItemCount[index]:null));
        if(requests.Count==0)return null;
        var name=leve.Name.ExtractText();
        return new(row.RowId,leve.RowId,string.IsNullOrWhiteSpace(name) ? $"Mandat {leve.RowId}":name,leve.ClassJobLevel,
            leve.ClassJobCategory.RowId==0 ? "":leve.ClassJobCategory.Value.Name.ExtractText(),
            leve.PlaceNameIssued.RowId==0 ? "":leve.PlaceNameIssued.Value.Name.ExtractText(),row.Repeats,Array.AsReadOnly(requests.ToArray()),(uint)Math.Max(0,leve.IconIssuer));
    }
    public static uint[] Supplies(GCSupplyDuty row)
    {
        if(row.SupplyData.Count>32)throw new InvalidOperationException("Incompatible delivery structure.");
        var result=new HashSet<uint>();var cells=0;
        foreach(var mission in row.SupplyData)
        {
            if(mission.Item.Count>64 || (cells+=mission.Item.Count)>512)throw new InvalidOperationException("Delivery catalogue too large.");
            foreach(var item in mission.Item)if(item.RowId!=0)result.Add(item.RowId);
        }
        return result.ToArray();
    }
}
