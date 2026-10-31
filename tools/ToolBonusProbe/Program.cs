using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Items;

if(args.Length!=1)throw new ArgumentException("Supply installed sqpack directory.");
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.English});
var items=data.Excel.GetSheet<Item>();var tools=0;var empty=0;var enhanced=0;var other=0;var foreign=0;
foreach(var row in items)
{
    var description=row.Description.ExtractText();
    var marker=description.Contains(CraftToolBonuses.EnhancedDescription,StringComparison.Ordinal);
    var percent=CraftToolBonuses.FromDeclaredMetadata(description,row.ItemSpecialBonus.RowId,row.ItemSpecialBonusParam);
    var tool=row.EquipSlotCategory.RowId!=0 && row.EquipSlotCategory.Value.MainHand>0 && row.ClassJobUse.RowId is >=8 and <=15;
    if(marker && !tool){foreign++;Console.WriteLine($"OTHER BONUS ITEM {row.RowId} {row.Name.ExtractText()}");}
    if(!tool)continue;tools++;
    if(percent==175)
    {
        enhanced++;if(enhanced<=8)Console.WriteLine($"ENHANCED {row.RowId} job={row.ClassJobUse.RowId} special={row.ItemSpecialBonus.RowId}/{row.ItemSpecialBonusParam} {row.Name.ExtractText()}");
    }
    else if(percent==150)empty++;
    else{other++;if(other<=5)Console.WriteLine($"UNKNOWN {row.RowId} {row.Name.ExtractText()} {description}");}
}
Console.WriteLine($"TOOLS {tools}, declared-empty {empty}, enhanced175 {enhanced}, unknown {other}, bonus-outside-mainhand {foreign}");
if(enhanced==0 || foreign!=0)throw new Exception("Tool bonus assumptions not established for installed game.");
