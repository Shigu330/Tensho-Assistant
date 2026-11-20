using TenshoAssistant.Competitive;

// 0.1584 (X-552): after 0.1524 the « Labyrinth reading » line was written only on a change, but a third of its lines (dalamud.log of 17 and
// 18/11: 321 lines in two evenings) came from the labyrinth's detail tips opening under the mouse; they no longer change the line.
internal static class LabyrinthHover1584Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Labyrinth hover 1584: a detail tip under the mouse leaves the line as it was, the board's list still changes it",() =>
        {
            string[] board=["XBMPetParty","XBMStageDetailList","_ActionBar"];
            var first=string.Join(", ",CompetitiveZones.LabyrinthWindows(board));
            Check(first=="XBMPetParty, XBMStageDetailList","the board's own windows, the list included");
            foreach(var tip in new[]{"XBMItemDetail","XBMPetActionDetail","XBMBattleMonsterDetail","XBMBattleDetailAction"})
                Check(string.Join(", ",CompetitiveZones.LabyrinthWindows([..board,tip]))==first,$"{tip} under the mouse changes nothing");
            Check(string.Join(", ",CompetitiveZones.LabyrinthWindows([..board,"XBMActivePet"]))!=first,"a pet chosen still changes it");
            Check(!CompetitiveZones.LabyrinthHoverTip("XBMStageDetailList") && !CompetitiveZones.LabyrinthHoverTip("ItemDetail"),"the list and a window of the game are not labyrinth tips");
        }),
        ("Labyrinth hover 1584: the lines of 18/11 replayed: fewer lines, the board and arena changes kept",() =>
        {
            string[][] seen=[["FadeMiddle","NowLoading"],["JobHudXBM0","XBMContentsMainHUD"],["JobHudXBM0","XBMContentsMainHUD","XBMItemDetail"],["JobHudXBM0","XBMContentsMainHUD"],
                ["XBMPetParty","XBMStageDetailList"],["XBMPetParty","XBMStageDetailList","XBMPetActionDetail"],["XBMPetParty","XBMStageDetailList"],["XBMActivePet","XBMPetParty","XBMStageDetailList"]];
            var written=0;string? last=null;
            foreach(var names in seen){var line=string.Join(", ",CompetitiveZones.LabyrinthWindows(names));if(line!=last){written++;last=line;}}
            Check(written==4,$"4 lines (loading, arena, board, pet), got {written}");
        }),
    ];
}
