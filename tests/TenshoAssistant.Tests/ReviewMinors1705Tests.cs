using System.Numerics;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.UI;

// X-711 (0.1705, review of 0.1699 to 0.1703, minor findings): M1 a side bar click whose mouse moved a little is still a click; M2 a drag
// whose release was not seen ends without moving anything; M3 a submarine unlock chain cut in the middle is unknown, not shown as whole.
internal static class ReviewMinors1705Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static string Text(IReadOnlyList<uint> ids) => string.Join(",", ids);

    // Three rows of 26 px: A 0-26, B 26-52, C 52-78; `drag` names the row pressed and dragged this frame.
    private static void Rows(ListDrag list, int frame, string? drag = null)
    {
        list.Frame(frame);
        string[] ids = ["A", "B", "C"];
        for (var index = 0; index < ids.Length; index++) list.Row(ids[index], 10, index * 26, 200, index * 26 + 26, ids[index] == drag);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1705 (M1): a row dragged a little and released over itself is a click; another row's drag still blocks a click", () =>
        {
            var list = new ListDrag();
            Rows(list, 1);
            Check(list.KeepsClick("A") && list.KeepsClick("B"), "no drag: every click counts");
            Rows(list, 2, "B");
            Rows(list, 3);
            Check(list.KeepsClick("B"), "released over B after moving past the threshold: B's click counts");
            Check(!list.KeepsClick("A") && !list.KeepsClick("C"), "a drag of B: no other row opens");
            Check(list.Drop(40) is null && list.Dragged is null, "released over itself: nothing moves, the drag ends");
        }),
        ("Review minors 1705 (M2): a drag not seen on the frame before ends without moving anything; frames in a row keep it", () =>
        {
            var list = new ListDrag();
            Rows(list, 10, "B");
            Rows(list, 11);
            Rows(list, 12);
            Check(list.Dragged == "B", "frames in a row: the drag goes on");
            Rows(list, 40);
            Check(list.Dragged is null && list.Drop(75) is null, "the window was closed while the button was held: no drop later at the mouse's height");
            Rows(list, 41, "C");
            Check(list.Drop(5) is { } order && order.SequenceEqual(["C", "A", "B"]), "a new drag works again");
            Rows(list, int.MaxValue, "A");
            Rows(list, int.MinValue);
            Check(list.Dragged == "A", "the frame count wrapping round is still the next frame");
        }),
        ("Review minors 1705 (M3): a chain cut in the middle (a link the sheet does not know, a loop) is unknown unless a sector after the cut is unlocked", () =>
        {
            // 1 open; 2 by 1; 3 by 99 (not a sector of the sheet); 4 by 3; 5 by 4; 7 by 8 (8 a sector, not in the table); 6 by 7.
            var unlocks = new Dictionary<uint, SubmarineUnlock> { [1] = new(0, 1), [2] = new(1, 2), [3] = new(99, 3), [4] = new(3, 4), [5] = new(4, 5), [6] = new(7, 6), [7] = new(8, 6) };
            var sectors = new uint[] { 1, 2, 3, 4, 5, 6, 7, 8 }.ToDictionary(id => id, id => new SubmarineSector(id, 1, false, 1, Vector3.Zero, 10, 60, 100, "S" + id));
            Check(Text(SubmarineUnlocks.Path(sectors, unlocks, 5)) == "3,4,5" && !SubmarineUnlocks.Whole(unlocks, SubmarineUnlocks.Path(sectors, unlocks, 5)), "the chain of 5 is cut before 3");
            Check(SubmarineUnlocks.Route(sectors, unlocks, 5, null).State == SubmarineUnlockState.Unknown, "nothing read: the cut chain is unknown, not « explore 3, 4 »");
            Check(SubmarineUnlocks.Route(sectors, unlocks, 5, []).State == SubmarineUnlockState.Unknown, "nothing unlocked: unknown");
            var (state, explore) = SubmarineUnlocks.Route(sectors, unlocks, 5, [4]);
            Check(state == SubmarineUnlockState.Path && Text(explore) == "4", "4 already unlocked: the cut before it does not matter: explore 4");
            (state, explore) = SubmarineUnlocks.Route(sectors, unlocks, 5, [3]);
            Check(state == SubmarineUnlockState.Path && Text(explore) == "3,4", "3 already unlocked: from 3");
            Check(SubmarineUnlocks.Route(sectors, unlocks, 6, null).State == SubmarineUnlockState.Unknown, "a link missing from the table (7 by 8, 8 unknown to the table): unknown");
            (state, explore) = SubmarineUnlocks.Route(sectors, unlocks, 2, null);
            Check(state == SubmarineUnlockState.Path && Text(explore) == "1" && SubmarineUnlocks.Whole(unlocks, [1, 2]), "a whole chain is unchanged");
        }),
        ("Review minors 1705: wiring: the side bar numbers its frames and keeps the click of the row dragged", () =>
        {
            var window = Read("MainWindow.cs");
            Check(window.Contains("drag.Frame(ImGui.GetFrameCount());", StringComparison.Ordinal) && !window.Contains("drag.Clear();", StringComparison.Ordinal), "frames numbered (M2)");
            Check(window.Contains("if (!rows.KeepsClick(root.ToString())) clicked = false;", StringComparison.Ordinal) && !window.Contains("var dragging = rows.Dragged is not null;", StringComparison.Ordinal), "click kept (M1)");
            Check(Read("FreeCompany/SubmarineUnlocks.cs").Contains("if(last<0 && !Whole(unlocks,path))return (SubmarineUnlockState.Unknown,[]);", StringComparison.Ordinal), "cut chain unknown (M3)");
        }),
    ];
}
