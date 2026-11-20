using TenshoAssistant.Diagnostics;

// 0.1636 (X-564): the chat's first frame takes the equality members of the chat part's own types only (a record used as a key), no longer
// those of every type met outside it (about 135 methods in the game, none named by the log).
internal static class ChatFrameEquality1636Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    internal sealed record Key1636(int Id);
    internal sealed record Other1636(int Id);
    internal static class Chat1636 { public static int Draw()=>new Key1636(1).Id+new Other1636(2).Id+Out1636.Make(); }
    internal static class Out1636 { public static int Make()=>new Other1636(2).Id; }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat frame equality 1636: a record of the chat part keeps its equality members in the first frame; one met outside it does not",()=>
        {
            bool Chat(System.Reflection.MethodBase method)=>method.DeclaringType==typeof(Chat1636) || method.DeclaringType==typeof(Key1636);
            var frame=JitWarmup.ChatFirstFrame([typeof(Chat1636).GetMethod("Draw")!],Chat,typeof(Chat1636).Assembly);
            var names=frame.Select(method=>method.DeclaringType!.Name+"."+method.Name).ToArray();
            Check(names.Contains("Key1636.GetHashCode") && names.Contains("Key1636.Equals"),"the chat's record: "+string.Join(", ",names));
            Check(names.Contains("Other1636..ctor") && !names.Contains("Other1636.GetHashCode") && !names.Contains("Other1636.Equals"),"met outside: not its own: "+string.Join(", ",names));
        }),
    ];
}
