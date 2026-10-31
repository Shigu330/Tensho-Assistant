namespace TenshoAssistant.Items;

[Flags]
public enum EquipmentSlots
{
    None=0, Head=1, Body=2, Hands=4, Legs=8, Feet=16, Ears=32, Neck=64, Wrists=128, Ring=256,
    Wearable=Head|Body|Hands|Legs|Feet|Ears|Neck|Wrists|Ring,
    MainHand=512,OffHand=1024,SoulCrystal=2048,
    All=Wearable|MainHand|OffHand|SoulCrystal
}
public static class EquipmentSlotMap
{
    // Negative native values reserve/block a slot; they do not equip the item in that slot.
    public static EquipmentSlots FromNative(sbyte head,sbyte body,sbyte hands,sbyte legs,sbyte feet,sbyte ears,sbyte neck,sbyte wrists,sbyte leftRing,sbyte rightRing,sbyte mainHand=0,sbyte offHand=0,sbyte soulCrystal=0)
        => (head>0 ? EquipmentSlots.Head:0)|(body>0 ? EquipmentSlots.Body:0)|(hands>0 ? EquipmentSlots.Hands:0)
           |(legs>0 ? EquipmentSlots.Legs:0)|(feet>0 ? EquipmentSlots.Feet:0)|(ears>0 ? EquipmentSlots.Ears:0)
           |(neck>0 ? EquipmentSlots.Neck:0)|(wrists>0 ? EquipmentSlots.Wrists:0)|(leftRing>0 || rightRing>0 ? EquipmentSlots.Ring:0)
           |(mainHand>0 ? EquipmentSlots.MainHand:0)|(offHand>0 ? EquipmentSlots.OffHand:0)|(soulCrystal>0 ? EquipmentSlots.SoulCrystal:0);
}
