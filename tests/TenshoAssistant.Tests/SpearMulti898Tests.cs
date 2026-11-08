// 0.898 (X-73.18): the shadows of a multiple spearfishing catch, from the fish the chat names and the slots the window shows speared.
using TenshoAssistant.Discoveries;
internal static class SpearMulti898Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Spearfishing multiple catch: alike slots give all, a known fish takes its shadow, the last one the rest, ambiguous left out",() =>
        {
            (int,int)? None(uint item)=>null;
            Check(ItemGains.SpearAssign([10,11],[(1,200),(1,200)],None).SequenceEqual(new[]{(10u,1,200),(11u,1,200)}),"alike");
            var known=ItemGains.SpearAssign([10,11],[(3,300),(1,200)],item=>item==11 ? (1,200):null);
            Check(known.Count==2 && known.Contains((11u,1,200)) && known.Contains((10u,3,300)),"elimination");
            Check(ItemGains.SpearAssign([10,11],[(3,300),(1,200)],None).Count==0,"ambiguous");
            Check(ItemGains.SpearAssign([10,11,12],[(3,300),(1,200)],None).Count==0 && ItemGains.SpearAssign([],[],None).Count==0,"counts differ");
            var three=ItemGains.SpearAssign([10,11,12],[(3,300),(1,200),(1,200)],item=>item==10 ? (3,300):null);
            Check(three.Count==3 && three.Contains((11u,1,200)) && three.Contains((12u,1,200)),"rest alike");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if((int)message.LogKind!=67 || message.Sender.TextValue.Trim().Length!=0)return;") && watcher.Contains("()=>chat.ChatMessageUnhandled-=OnSpearChat)"),"chat, owned");
        }),
    ];
}
