using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1635 (X-564): the chat's first frame without a second level outside the chat part (1060 methods in 0.1634, 600 measured without), a
// « later » word left out at the first level too, and DrawGlyphAt, the helper found two levels out, named as a root.
internal static class ChatFrameTrim1635Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    internal static class Chat1635 { public static int Draw()=>Out1635.Link()+Out1635.ItemMenu(); }
    internal static class Out1635 { public static int Link()=>Out1635.Click(); public static int Click()=>1; public static int ItemMenu()=>Out1635.Click()+2; }
    internal sealed class MainWindow { public int DrawGlyphAt()=>3; public int DrawSomethingElse()=>4; }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat frame trim 1635: one level outside the chat part, a « later » word left out there too; nothing two levels out",()=>
        {
            var frame=JitWarmup.ChatFirstFrame([typeof(Chat1635).GetMethod("Draw")!],method=>method.DeclaringType==typeof(Chat1635),typeof(Chat1635).Assembly);
            var names=frame.Select(method=>method.DeclaringType!.Name+"."+method.Name).ToArray();
            Check(names.SequenceEqual(["Chat1635.Draw","Out1635.Link"]),string.Join(", ",names));
        }),
        ("Chat frame trim 1635: the main window's DrawGlyphAt is a root of the first frame; another helper is not; the name is still declared",()=>
        {
            Check(JitWarmup.ChatFrameRoot(typeof(MainWindow).GetMethod("DrawGlyphAt")!) && !JitWarmup.ChatFrameRoot(typeof(MainWindow).GetMethod("DrawSomethingElse")!),"roots");
            var sources=Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories).Select(File.ReadAllText);
            foreach(var root in JitWarmup.ChatHelperRoots)
                Check(sources.Any(text=>System.Text.RegularExpressions.Regex.IsMatch(text,@"(private|internal|public)[\w\s?<>]* "+root+@"\(")),"declared: "+root);
        }),
    ];
}
