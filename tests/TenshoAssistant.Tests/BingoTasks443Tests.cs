// N-02 (0.443): Wondrous Tails tasks still to do and to claim, named in the game's language.
internal static class BingoTasks443Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Wondrous Tails tasks: read with the function checked, open and claimable shown, names from the game's sheets",() =>
        {
            var timers=Read("Reminders/GameTimers.Dalamud.cs");
            Check(timers.Contains("(nint)PlayerState.MemberFunctionPointers.GetWeeklyBingoTaskStatus!=0") && timers.Contains("state->WeeklyBingoOrderData"),"read, address checked");
            Check(Read("MainWindow.Reminders.cs").Contains("tasks.Where(task=>task.Status is 0 or 1).DistinctBy(task=>task.Order)"),"open and claimable only");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("duty.ContentLinkType==1 && duty.Content.RowId!=0") && plugin.Contains("row.Text.ValueNullable?.Description.ExtractText()"),"duty name for type 0, text otherwise");
        }),
    ];
}
