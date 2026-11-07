using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.DeepDungeons;

/// <summary>W-15 (0.718): from the sheets only, in the game's language. Off the frame.</summary>
internal static class DeepDungeonLoader
{
    public static IReadOnlyList<DeepDungeonEntry> Read(IDataManager data)
    {
        var stones=data.GetExcelSheet<DeepDungeonMagicStone>();
        var clones=data.GetExcelSheet<DeepDungeonDemiclone>();
        var entries=new List<DeepDungeonEntry>();
        foreach(var row in data.GetExcelSheet<DeepDungeon>())
        {
            var name=row.Name.ExtractText();
            if(row.RowId==0 || name.Length==0)continue;
            var pomanders=new List<DeepDungeonThing>();
            foreach(var slot in row.PomanderSlot)
                if(slot.ValueNullable is { RowId:not 0 } item && !item.Name.IsEmpty)
                    pomanders.Add(new(item.RowId,item.Name.ExtractText(),DeepDungeonBoard.Text(item.Tooltip.ToMacroString(),row.RowId),item.Icon));
            var others=new List<DeepDungeonThing>();
            var (stoneRows,cloneRows)=DeepDungeonBoard.Others(row.RowId,row.MagiciteSlot.Select(slot=>slot.RowId));
            foreach(var id in stoneRows)
                if(stones.GetRowOrDefault(id) is { } stone && !stone.Name.IsEmpty)others.Add(new(stone.RowId,stone.Name.ExtractText(),DeepDungeonBoard.Text(stone.Tooltip.ToMacroString(),row.RowId),0));
            foreach(var id in cloneRows)
                if(clones.GetRowOrDefault(id) is { } clone && !clone.TitleCase.IsEmpty)others.Add(new(clone.RowId,clone.TitleCase.ExtractText(),DeepDungeonBoard.Text(clone.Description.ToMacroString(),row.RowId),0));
            entries.Add(new(row.RowId,name,pomanders,others));
        }
        return entries;
    }
}
