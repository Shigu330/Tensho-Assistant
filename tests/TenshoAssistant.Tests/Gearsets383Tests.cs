using TenshoAssistant.Gear;
using TenshoAssistant.Inventory;

// 0.383 (g, M-12): game gear sets — owned pieces counted from wearable storages only, better owned pieces per slot for the set's job
// and level, one ring proposed once, HQ ids split, sets shared as TENSHO-GEAR1.
internal static class Gearsets383Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static GearItem Item(uint id,GearSlot slot,int itemLevel,int level=90,uint job=19)
        =>new(id,"Piece"+id,0,level,itemLevel,slot,false,1UL<<(int)job,new Dictionary<int,int>(),new Dictionary<int,int>(),2,true,false);
    private static InventorySnapshot Snapshot(ulong owner,int container,params uint[] items)
        =>new(DateTimeOffset.UtcNow,"test",[new InventoryContainerSnapshot(new(new(owner),new(container)),InventoryCoverage.Complete,
            items.Select(id=>new InventoryStack(new(new(id),false),1)))]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gear sets: better owned pieces per slot, job and level checked, rings proposed once, wearable storages only",() =>
        {
            var catalog = new GearCatalog([Item(1,GearSlot.Head,600),Item(2,GearSlot.Head,620),Item(3,GearSlot.Head,640,level:100),Item(4,GearSlot.Head,650,job:20),
                Item(10,GearSlot.RingRight,600),Item(11,GearSlot.RingRight,630),Item(12,GearSlot.RingRight,625),Item(20,GearSlot.Body,700)]);
            var set = new GameGearset(1,"Paladin",19,600,[new(GearSlot.Head,1,false,[]),new(GearSlot.RingRight,10,false,[]),new(GearSlot.RingLeft,10,false,[])]);
            var dresser = new BankEntry(new(7,BankStorageKind.GlamourDresser,0),"",Snapshot(7,0,20));
            var armoury = new BankEntry(new(7,BankStorageKind.Armoury,0),"",Snapshot(7,0,11,12));
            var owned = GameGearsets.Owned(Snapshot(7,0,2,3,4),[dresser,armoury],7);
            Check(!owned.ContainsKey(20) && owned.ContainsKey(11) && owned.ContainsKey(2),"dresser left out");
            var upgrades = GameGearsets.Upgrades(set,catalog,owned,90);
            Check(upgrades.Single(upgrade=>upgrade.Slot==GearSlot.Head).Better.Id==2,"level and job checked");
            Check(upgrades.Single(upgrade=>upgrade.Slot==GearSlot.RingRight).Better.Id==11 && upgrades.Single(upgrade=>upgrade.Slot==GearSlot.RingLeft).Better.Id==12,"each ring once");
            Check(GameGearsets.Upgrades(set,catalog,owned,null).Single(upgrade=>upgrade.Slot==GearSlot.Head).Better.Id==3,"level unknown: not checked");
            Check(GameGearsets.Owned(Snapshot(7,0,2),[],0).Count==0,"no character, nothing");
        }),
        ("Gear sets: HQ ids split, share code readable by the Gear page, field reads only",() =>
        {
            Check(GameGearsets.Split(1_040_000)==(40_000u,true) && GameGearsets.Split(40_000)==(40_000u,false),"HQ split");
            var set = new GameGearset(3,"",19,600,[new(GearSlot.Head,1,false,[new(5,3)])]);
            var code = GameGearsets.ShareCode(set);
            Check(code.StartsWith(GearShare.Prefix,StringComparison.Ordinal),"TENSHO-GEAR1");
            var reader = Read("Gear/GameGearsetReader.Dalamud.cs");
            Check(!reader.Contains("UpdateGearset",StringComparison.Ordinal) && !reader.Contains("DeleteGearset",StringComparison.Ordinal)
                && !reader.Contains("RenameGearset",StringComparison.Ordinal),"never changes a set");
        }),
    ];
}
