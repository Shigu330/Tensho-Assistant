using TenshoAssistant.Items;

// 0.607: the list of annotated items.
internal static class AnnotatedItems607Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly Dictionary<uint,ItemNote> Notes=new(){[1]=new("rare fish",true,false),[2]=new("",false,true),[3]=new("  ",false,false)};
    private static string Name(uint id)=>id==1 ? "Zebra":id==2 ? "Apple":"Empty";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Annotated: by name, without empty notes; by label; by text in the name or note",() =>
        {
            Check(ItemNotes.Annotated(Notes,AnnotatedFilter.All,"",Name).Select(row=>row.ItemId).SequenceEqual<uint>([2,1]),"sorted, empty note out");
            Check(ItemNotes.Annotated(Notes,AnnotatedFilter.Important,"",Name) is [{ItemId:1}],"important");
            Check(ItemNotes.Annotated(Notes,AnnotatedFilter.ToSell,"",Name) is [{ItemId:2}],"to sell");
            Check(ItemNotes.Annotated(Notes,AnnotatedFilter.All,"FISH",Name) is [{ItemId:1}],"in the note");
            Check(ItemNotes.Annotated(Notes,AnnotatedFilter.All,"apple",Name) is [{ItemId:2}],"in the name");
            Check(ItemNotes.Annotated(Notes,AnnotatedFilter.ToSell,"fish",Name).Length==0,"both");
        }),
        ("Annotated: on the Items page",() =>
        {
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Items.cs")).Contains("DrawAnnotatedItems();"),"shown");
        }),
    ];
}
