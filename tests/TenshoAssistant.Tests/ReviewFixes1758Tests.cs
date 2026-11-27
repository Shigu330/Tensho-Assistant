using TenshoAssistant;
using TenshoAssistant.Market;

// X-767 (review of 0.1752 to 0.1756, minor M1 and M2). M1: SettingsFile.Close took the gate a write under way holds while it waits for
// another client's lock (up to 10 s): the game's thread waited there at unload before the saver's Flush, whose bound of 5 s and log line no
// longer played. M2: the « N sale(s) copied » note stayed next to another item's history or another filter, as if the lines shown had been
// copied (the bank's « N line(s) copied » the same).
internal static class ReviewFixes1758Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-settings-1758-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static SettingsFile<Configuration> Settings(string root) =>
        new(Path.Combine(root, "TenshoAssistant", SettingsFile<Configuration>.FileName), Path.Combine(root, "TenshoAssistant.json"), (_, _) => { });

    // True once a write waits for the lock (FairFileLock's waiting mark beside it), within 10 s.
    private static bool Waiting(SettingsFile<Configuration> file)
    {
        var until = Environment.TickCount64 + 10_000;
        while (Environment.TickCount64 < until)
        {
            if (FairFileLock.Waiting(file.LockPath, null).Count > 0) return true;
            Thread.Sleep(10);
        }
        return false;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review fixes 1758: behaviour: M1: Close returns at once while a write waits for another client's lock; that write ends as it began, the next one is the unload's", () => InTemp(root =>
        {
            var file = Settings(root);
            var value = file.Load(out _);
            Check(file.Save(value), "first save");
            value.DeveloperMode = !value.DeveloperMode;
            var other = FairFileLock.Hold(file.LockPath, 1_000, error => error);
            var underWay = Task.Run(() => file.Save(value));
            Check(Waiting(file), "the write waits for the other client's lock (its waiting mark)");
            Check(!underWay.IsCompleted, "the write under way");
            // X-886 (0.1908): read against the write, not the clock (Close measured 2144 ms once under the whole suite's load, a thread not
            // scheduled): Close returned while the write still waits for the lock the other client holds (before 0.1758 it took the gate that
            // write holds, so it could only return once the write had given up).
            file.Close();
            Check(!underWay.IsCompleted && Waiting(file), "Close did not wait for the write under way (still waiting for the other client's lock)");
            other.Dispose();
            Check(underWay.Wait(TimeSpan.FromSeconds(10)) && underWay.Result, "the write under way ends, written");
            Check(file.ClosingSave is null, "it began before the unload: not the unload's save");
            value.HidePanelsBehindGame = !value.HidePanelsBehindGame;
            Check(file.Save(value, onGameThread: true) && file.ClosingSave == true, "the next save is the unload's, written");
            var again = Settings(root).Load(out _);
            Check(again.DeveloperMode == value.DeveloperMode && again.HidePanelsBehindGame == value.HidePanelsBehindGame, "on disk");
        })),
        ("Review fixes 1758: behaviour: M1: at unload, the saver's Flush keeps its bound while the worker waits for another client's lock; the live settings are written after it", () => InTemp(root =>
        {
            var file = Settings(root);
            var value = file.Load(out _);
            Check(file.Save(value), "first save");
            var saver = new SettingsSaver<Configuration>(value, SettingsJson.Write, SettingsJson.Read<Configuration>, copy => file.Save(copy), action => action(), (_, _) => { });
            var other = FairFileLock.Hold(file.LockPath, 1_000, error => error);
            value.DeveloperMode = !value.DeveloperMode;
            saver.Save();
            Check(Waiting(file), "the worker waits for the other client's lock (its waiting mark)");
            value.HidePanelsBehindGame = !value.HidePanelsBehindGame;
            // X-886 (0.1908): read against the worker, not the clock: the other client lets go only once Flush has returned, so Close and a
            // Flush that kept its bound return while the worker still waits for that lock (its waiting mark); one that waited for the worker
            // would only return once the worker had given up.
            file.Close();
            Check(!saver.Flush(TimeSpan.FromMilliseconds(500)), "the worker still waits: Flush says so");
            Check(Waiting(file), "Close and Flush kept their bound: the worker still waits for the other client's lock");
            other.Dispose();
            Check(saver.Settle(TimeSpan.FromSeconds(15)), "settled once the other client let go");
            Check(file.ClosingSave == true, "the live settings written by the unload's save");
            var again = Settings(root).Load(out _);
            Check(again.DeveloperMode == value.DeveloperMode && again.HidePanelsBehindGame == value.HidePanelsBehindGame, "the last settings on disk");
        })),
        ("Review fixes 1758: behaviour: M2: the copied note shows only next to the view copied, and is gone for good once the view changes", () =>
        {
            var note = new CopiedNote<(string Item, int Filter, (int, bool) Sort)>();
            Check(note.For(("a", 0, (0, true))) is null, "nothing copied yet");
            note.Copied(("a", 0, (0, true)), "3 copied");
            Check(note.For(("a", 0, (0, true))) == "3 copied", "the view copied");
            Check(note.For(("b", 0, (0, true))) is null, "another item");
            Check(note.For(("a", 0, (0, true))) is null, "back to the item copied: gone for good (the sales may have changed)");
            note.Copied(("a", 0, (0, true)), "4 copied");
            Check(note.For(("a", 1, (0, true))) is null && note.For(("a", 0, (0, true))) is null, "another filter");
            note.Copied(("a", 0, (0, true)), "4 copied");
            Check(note.For(("a", 0, (1, false))) is null, "another order");
            var boards = new CopiedNote<(object? Board, int Filter)>();
            var board = new object();
            boards.Copied((board, 0), "2 copied");
            Check(boards.For((new object(), 0)) is null, "newer data for the same item: gone");
        }),
        ("Review fixes 1758: wiring: Close without the gate; the market and the bank show their note through CopiedNote, keyed by what they copied", () =>
        {
            var settings = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/SettingsFile.cs")).ReplaceLineEndings("\n");
            Check(settings.Contains("public void Close()=>closing=true;", StringComparison.Ordinal) && settings.Contains("private volatile bool closing;", StringComparison.Ordinal)
                && !settings.Contains("lock(gate)closing=true", StringComparison.Ordinal), "Close: no gate");
            var market = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketBoard.cs")).ReplaceLineEndings("\n");
            Check(market.Contains("marketSalesCopied.Copied(marketSalesKey,", StringComparison.Ordinal) && market.Contains("if(marketSalesCopied.For(marketSalesKey) is { } copied)", StringComparison.Ordinal)
                && !market.Contains("marketSalesCsvMessage", StringComparison.Ordinal), "the market's note");
            var bank = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.BankList.cs")).ReplaceLineEndings("\n");
            Check(bank.Contains("bankCopied.Copied(BankCopyKey(source),", StringComparison.Ordinal) && bank.Contains("if(bankCopied.For(BankCopyKey(source)) is { } message)", StringComparison.Ordinal)
                && bank.Contains("(source,bankCategoryFilter,bankQualityFilter,bankQuery);", StringComparison.Ordinal) && !bank.Contains("bankCsvMessage", StringComparison.Ordinal), "the bank's note");
        }),
    ];
}
