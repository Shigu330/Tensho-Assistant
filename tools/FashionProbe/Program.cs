using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Fashion;
using TenshoAssistant.Items;

if(args.Length is <1 or >2 || args.Length==2 && args[1]!="--fetch") throw new ArgumentException("Supply sqpack directory, optionally --fetch for the two fixed public dataset requests.");
using var data=new GameData(args[0],new LuminaOptions {DefaultExcelLanguage=Language.French});
var hints=new FashionHintCatalog(data.Excel.GetSheet<FashionCheckThemeCategory>().Select(row=>new FashionHint(row.RowId,row.Name.ExtractText())));
var sheet=data.Excel.GetSheet<Item>(); var copied=new List<ItemDefinition>(); var blocked=0;
foreach(var row in sheet)
{
    var name=row.Name.ExtractText(); if(row.RowId==0 || string.IsNullOrWhiteSpace(name)) continue;
    var slots=row.EquipSlotCategory.Value;
    var mask=row.EquipSlotCategory.RowId==0 ? EquipmentSlots.None:EquipmentSlotMap.FromNative(slots.Head,slots.Body,slots.Gloves,slots.Legs,slots.Feet,slots.Ears,slots.Neck,slots.Wrists,slots.FingerL,slots.FingerR,slots.MainHand,slots.OffHand,slots.SoulCrystal);
    if(slots.Head<0 && slots.Body>0)
    { if((mask&EquipmentSlots.Head)!=0 || (mask&EquipmentSlots.Body)==0) throw new InvalidOperationException("Blocked head slot was treated as wearable head."); blocked++; }
    if(slots.OffHand<0 && (mask&EquipmentSlots.OffHand)!=0)throw new InvalidOperationException("Blocked offhand treated as usable offhand.");
    copied.Add(new(row.RowId,name,row.CanBeHq,row.Icon,Equipment:mask));
}
var items=ItemCatalog.Build(copied,new("installed-game","French","probe",1));
if(hints.Hints.Count<100 || blocked==0 || copied.Count(item=>(item.Equipment&EquipmentSlots.Wearable)!=0)<1000
    || copied.Count(item=>(item.Equipment&EquipmentSlots.MainHand)!=0)<100 || copied.Count(item=>(item.Equipment&EquipmentSlots.OffHand)!=0)<100
    || copied.Count(item=>(item.Equipment&EquipmentSlots.SoulCrystal)!=0)==0) throw new InvalidOperationException("Native equipment metadata incomplete.");
Console.WriteLine($"PASS native equipment metadata: {hints.Hints.Count} named hints, {copied.Count(item=>(item.Equipment&EquipmentSlots.Wearable)!=0)} wearable, {copied.Count(item=>(item.Equipment&EquipmentSlots.MainHand)!=0)} mainhand, {copied.Count(item=>(item.Equipment&EquipmentSlots.OffHand)!=0)} offhand, {copied.Count(item=>(item.Equipment&EquipmentSlots.SoulCrystal)!=0)} soul crystals, {blocked} blocked-head body items.");
if(args.Length==2)
{
    using var source=new FashionDataService(); await source.RequestAsync();
    var dataset=source.State.Dataset ?? throw new InvalidOperationException(source.State.Error ?? "No fashion dataset.");
    var knownHints=hints.Hints.Select(hint=>hint.Id).ToHashSet();
    var references=dataset.HintIds.SelectMany(dataset.ForHint).ToArray();
    var hintId=dataset.HintIds.First(id=>knownHints.Contains(id) && dataset.Filter(id,EquipmentSlots.Body,items,"").Count>0);
    var candidates=dataset.Filter(hintId,EquipmentSlots.Body,items,"");
    if(candidates.Any(candidate=>candidate.Item is null || (candidate.Item.Equipment&EquipmentSlots.Body)==0)) throw new InvalidOperationException("Fashion body filter accepted an incompatible reference.");
    Console.WriteLine($"PASS explicit fashion source: {dataset.HintIds.Count} categories, {references.Length} hint/item links, {dataset.HintIds.Count(id=>!knownHints.Contains(id))} unknown hint IDs, {references.Count(match=>items.Find(match.ItemId) is null)} unresolved item references; providerDate={dataset.ProviderUpdatedAt:O}, dateStatus={dataset.DateStatus}.");
    Console.WriteLine($"PASS native fashion body filtering: hint {hintId} {hints.Hints.First(hint=>hint.Id==hintId).Name}, {candidates.Count} candidates; sample {candidates[0].Match.ItemId} {candidates[0].Item!.Name}, raw observations={candidates[0].Match.Reports}. No current-week or score inference.");
}
