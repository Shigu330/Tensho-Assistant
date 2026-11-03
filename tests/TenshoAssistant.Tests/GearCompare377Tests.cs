using TenshoAssistant.Gear;

// 0.377 (g, M-07): hovered gear against the worn piece of its slot — base values (NQ or HQ), both rings for a ring, nothing for the worn piece itself.
internal static class GearCompare377Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static GearItem Item(uint id,GearSlot slot,int level,int strength,int crit=0,int hqStrength=0)
        =>new(id,"Piece"+id,0,90,level,slot,false,ulong.MaxValue,new Dictionary<int,int>{[1]=strength,[27]=crit},
            hqStrength>0 ? new Dictionary<int,int>{[1]=hqStrength}:new Dictionary<int,int>(),2,hqStrength>0,false);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gear compare: item level and stat differences with the worn piece, HQ values on both sides",() =>
        {
            var worn = Item(1,GearSlot.Head,600,100,50,hqStrength:10);var hovered = Item(2,GearSlot.Head,610,120,40);
            var find = new Dictionary<uint,GearItem>{[1]=worn,[2]=hovered};
            int[] order = [1,27];
            var list = GearCompare.Compare(hovered,false,[new(GearSlot.Head,1,true,[])],id=>find.GetValueOrDefault(id),order);
            Check(list.Count==1 && list[0].ItemLevelDelta==10 && list[0].Deltas.SequenceEqual([(1,10),(27,-10)]),"worn HQ counted");
            Check(GearCompare.Compare(worn,true,[new(GearSlot.Head,1,true,[])],id=>find.GetValueOrDefault(id),order).Count==0,"the worn piece itself");
            Check(GearCompare.Compare(hovered,false,[new(GearSlot.Body,1,false,[])],id=>find.GetValueOrDefault(id),order).Count==0,"nothing worn in the slot");
        }),
        ("Gear compare: a ring is compared with both worn rings",() =>
        {
            var left = Item(10,GearSlot.RingRight,600,50);var right = Item(11,GearSlot.RingRight,590,40);var hovered = Item(12,GearSlot.RingRight,610,60);
            var find = new Dictionary<uint,GearItem>{[10]=left,[11]=right,[12]=hovered};
            var list = GearCompare.Compare(hovered,false,[new(GearSlot.RingRight,10,false,[]),new(GearSlot.RingLeft,11,false,[])],id=>find.GetValueOrDefault(id),[1]);
            Check(list.Count==2 && list[0].ItemLevelDelta==10 && list[1].ItemLevelDelta==20,"both rings");
            Check(new TenshoAssistant.Configuration().HoverShowCompare && TenshoAssistant.ConfigurationReset.Reset.Contains("HoverShowCompare"),"on by default, reset list");
        }),
    ];
}
