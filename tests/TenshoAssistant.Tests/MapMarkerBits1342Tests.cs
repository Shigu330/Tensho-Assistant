using TenshoAssistant.Maps;

// 0.1342 (X-351, review of the block 0.1318 to 0.1329, I3; docs/evidence/0.1342-map-marker-bits.md): the unlock bits of the fixed map icons are
// QuestManager.UnlockedMapMarkers, read most significant bit first. Checked on the array the game held on 11/14 at 16:15:27 (dalamud.log)
// against what the game's own map drew: Mor Dhona (map 25) and Ul'dah (map 14).
internal static class MapMarkerBits1342Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly byte[] Array=Convert.FromHexString(("FFFF9FDFFFE1F19439FFF38D54F4D7FFFF96FFF14C14833FFBFFBFFF19F5F86627C0").PadRight(128,'0'));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map marker bits 1342: the game's array read most significant bit first gives what the game draws (map 14 and Mor Dhona)",() =>
        {
            // Map 14 (16:11:08, position rule of 0.1340, 50 of 50 plain icons found): 68 and 118 drawn, 69 and 98 not.
            // Mor Dhona (11:39:48, size factor 100): 1 and 83 drawn; 43, 44, 157 and 175 not.
            var drawn=new Dictionary<ushort,bool>{[68]=true,[118]=true,[69]=false,[98]=false,[1]=true,[83]=true,[43]=false,[44]=false,[157]=false,[175]=false};
            foreach(var (bit,shown) in drawn)Check(MapMarkerUnlock.IsSet(Array,bit)==shown,$"bit {bit}: drawn {shown}");
            Check(MapMarkerUnlock.IsSet(Array,69)!=((Array[69/8]>>(69%8)&1)==1),"the least significant first reading (FFXIVClientStructs' BitArray) said 69 unlocked: the bug");
        }),
        ("Map marker bits 1342: the bit order and a bit past the array",() =>
        {
            Check(MapMarkerUnlock.IsSet([0x80],0) && !MapMarkerUnlock.IsSet([0x80],7) && MapMarkerUnlock.IsSet([0x01],7),"0x80 is bit 0, 0x01 bit 7");
            Check(MapMarkerUnlock.IsSet([0,0x40],9) && !MapMarkerUnlock.IsSet([0,0x40],14),"second byte");
            Check(!MapMarkerUnlock.IsSet([0xFF],8) && !MapMarkerUnlock.IsSet([],0),"past the array: not set (hidden)");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Replace("\r\n","\n");
            Check(reader.Contains("if(MapMarkerUnlock.IsSet(array,bit))set.Add(bit);") && !reader.Contains("UnlockedMapMarkersBitArray["),"the reader goes through IsSet, never the BitArray");
        }),
    ];
}
