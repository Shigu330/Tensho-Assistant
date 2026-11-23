using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1250, fifth slice, GPT review of 11/11, accepted by the user): the discoveries family out of Plugin.cs, after the desktop (0.1225),
// persistence (0.1239), server bar (0.1240) and panel (0.1249) families. The local base of discoveries (X-43): its store, its folder, the
// exchange through GitHub (X-77.1; one install path and one release path, in the plugin's registrations) and its loading off the frame,
// moved as they were: the prices seen at the market board first (MarketSeen.json lives in the same folder), then the discoveries
// (SQLite since X-172; X-574, 0.1793: the files of before no longer converted, left aside; a database that cannot be opened is never replaced;
// a base that cannot be read is set aside, never overwritten by an empty one). Nothing is written before it is read. The item watcher that fills the base stays with
// the game's readers in Plugin.cs. DiscoveriesFamily1250Tests.
public sealed partial class Plugin
{
    /// <summary>The store and folder of the discoveries, given to the main window, and the GitHub exchange (opt-in, connection by code).</summary>
    private (Discoveries.DiscoveryStore Store,string Folder) RegisterDiscoveries(IDalamudPluginInterface pluginInterface)
    {
        var discoveryFolder=Path.Combine(pluginInterface.GetPluginConfigDirectory(),"Discoveries");
        var discoveries=new Discoveries.DiscoveryStore();
        mainWindow.DiscoveryStore=discoveries;mainWindow.DiscoveryFolder=discoveryFolder;
        // X-77.1 (0.883): the exchange of discoveries through GitHub (connection by code, opt-in).
        Discoveries.GitHubExchange? discoveryExchange=null;
        registrations.Register(()=>mainWindow.DiscoveryExchange=discoveryExchange=new Discoveries.GitHubExchange(discoveryFolder){Log=message=>log.Information(message)},()=>discoveryExchange?.Dispose());
        return (discoveries,discoveryFolder);
    }

    /// <summary>X-619 M2 (review of 0.1557): MarketSeen.json held at the start, read again every 30 s (10 min at most) and joined to the prices
    /// seen meanwhile; still held, the next save joins it.</summary>
    private async Task ReadSeenPricesLater(string seenPath,Market.SeenPriceStore seenPrices)
    {
        try
        {
            if(await Market.SeenPriceStore.ReadWhenFree(seenPath,TimeSpan.FromSeconds(30),20,gameDataCancel.Token).ConfigureAwait(false) is { } later)
            {seenPrices.Load(later);log.Information("The market prices seen were read once the file was free: {0} entries joined.",later.Count);}
            else log.Warning("The market prices seen could still not be read; the next save joins the file.");
        }
        catch(OperationCanceledException){}
        catch(Exception error){log.Warning(error,"The market prices seen could not be read again.");}
    }

    /// <summary>The prices seen, then the discoveries, read on a worker (as before the move); the chat lines go through the framework thread.</summary>
    private void LoadDiscoveries(string discoveryFolder,Discoveries.DiscoveryStore discoveries,Market.SeenPriceStore seenPrices,IFramework framework,IChatGui chat)
    {
        _=Task.Run(async()=>
        {
            // X-587 (external audit A15 of 18/11): damaged, set aside under a dated name, never over one set aside before. X-618 (M2): held by
            // another program, read again, then left where it is (the next save joins it), never set aside as damaged.
            try
            {
                var seenPath=Path.Combine(discoveryFolder,"MarketSeen.json");
                var held=false;
                if(Market.SeenPriceStore.ReadAtStart(seenPath,DateTimeOffset.UtcNow,
                    (aside,error)=>log.Warning(error,"The market prices seen could not be read; the file is set aside as {0}.",Path.GetFileName(aside)),
                    error=>{held=true;log.Warning(error,"The market prices seen could not be read for now (held by another program?); the file stays where it is, read again every 30 s and joined at the next save.");},
                    // X-639 (bench X-631, review of 0.1560, M3): damaged and not moved: not read again every 30 s for nothing; the next save sets it aside.
                    stuck:error=>log.Warning(error,"The market prices seen are damaged and could not be set aside for now; the file stays where it is and the next save sets it aside.")) is { } prices)
                    seenPrices.Load(prices);
                // X-619 M2 (review of 0.1557): held at the start, the file is read again later and joined to the prices seen meanwhile (before,
                // its prices stayed out of memory for the whole session).
                if(held)_=ReadSeenPricesLater(seenPath,seenPrices);
            }
            catch(Exception error){log.Warning(error,"The market prices seen could not be read.");}
            // X-793 (0.1800, review of 0.1799, M2): what a copy cut short by a killed or crashed game left in Backups (a whole snapshot of the
            // base, a zip half written) removed once an hour old.
            try
            {
                var removed=BackupZip.Sweep(Discoveries.DiscoveryBackup.Folder(discoveryFolder),DateTime.UtcNow);
                if(removed>0)log.Information("Backups: {0} file(s) left by a copy cut short (the game closed or crashed during it) removed.",removed);
            }
            catch(Exception error){log.Warning(error,"The Backups folder could not be looked through for copies cut short.");}
            // X-845 (0.1854, review 0.1844 M1): the zip copies of the cache taken before 0.1840 written again once without the market boards
            // of v1 (other players' retainer names); a mark in Backups says the pass is done.
            try
            {
                var cleaned=Caching.RetiredCopies.Clean(Discoveries.DiscoveryBackup.Folder(discoveryFolder),gameDataCancel.Token);
                if(cleaned>0)log.Information("Backups: {0} old cache copy(ies) written again without the market boards that kept other players' retainer names.",cleaned);
            }
            catch(OperationCanceledException){}
            catch(Exception error){log.Warning(error,"The old cache copies in Backups could not be looked through.");}
            var saved=Path.Combine(discoveryFolder,"Discoveries.json");
            // X-172 (0.1136, trial docs/evidence/x172-sqlite-trial.md): the discoveries in Discoveries.db (SQLite). A database that exists but cannot
            // be opened (SQLite not loaded, damaged, newer layout) is never replaced: nothing is saved this session; without a database (it could not
            // be created), the JSON files stay in use as before. X-574 (0.1793, the user's choice of 17/11): the files of before (Discoveries.json,
            // Shared/) are no longer converted: still there beside the database, they are left aside, never read, moved nor removed (said once).
            var databasePath=Path.Combine(discoveryFolder,Discoveries.DiscoveryDatabase.FileName);
            Discoveries.DiscoveryDatabase? database=null;
            try
            {
                database=new Discoveries.DiscoveryDatabase(databasePath){Log=message=>log.Warning(message)};
                var (mine,sharedCount)=database.Count();
                log.Information("Discoveries: SQLite {0} database, {1} facts of yours, {2} shared.",database.LibraryVersion(),mine,sharedCount);
                if(Discoveries.DiscoveryFiles.OldFiles(saved) is { Length:>0 } old)
                    log.Information(old.Length==1 ? "Discoveries: a file of an old format ({0}) was left aside, not read (X-574)."
                        : "Discoveries: files of an old format ({0}) were left aside, not read (X-574).",string.Join(", ",old));
            }
            catch(Exception error)
            {
                database=null;
                if(File.Exists(databasePath))
                {
                    mainWindow.DiscoveriesWriteBlocked=true;
                    // X-587 (external audit A16 of 18/11): your facts are not in memory: nothing is sent to your gist this session.
                    discoveries.MineIncomplete=true;
                    log.Error(error,"The discoveries database could not be opened: nothing is saved this session.");
                    var said=localization.Format("DiscoveriesUi.BaseBlocked",Discoveries.DiscoveryDatabase.FileName);
                    _=framework.RunOnFrameworkThread(()=>chat.PrintError("[Tensho] "+said));
                    mainWindow.DiscoveriesLoaded=true;
                    return;
                }
                log.Error(error,"The discoveries database could not be created: the JSON files stay in use this session.");
            }
            mainWindow.DiscoveryDatabase=database;
            if(database is not null)
            {
                // X-750 (0.1794): kept open while the plugin runs (this load runs off the frame); closed at unload (FlushDiscoveries).
                // X-785 M2 (0.1798, review of 0.1795): kept only once given to the window, whose unload closes what it holds; an unload that came
                // before (its close found no base) is seen here and the base closed again, so no connection outlives the plugin. A failure to
                // keep it open is said and the base used as before (each save opens its own connection): it was taken for a base that cannot
                // be opened (nothing saved this session, nothing sent to the gist).
                try{database.KeepOpen();}
                catch(Exception error){log.Warning(error,"The discoveries database could not be kept open; each save opens it again (X-750).");}
                if(gameDataCancel.IsCancellationRequested)database.Close();
            }
            if(database is { } opened)
            {
                // X-588 (external audit A01 of 18/11): your facts and the shared kinds' list read inside a retry (outside any try before: an error
                // left the page « loading » for the session); each failure said on the page; still failing after the last try, the page opens
                // without your facts and nothing is sent to GitHub this session (MineIncomplete); new sightings are still saved, row by row,
                // added to what the base holds (X-171).
                // X-618 (M5): a row of yours that cannot be read leaves your facts incomplete: nothing sent to your gist (DiscoveryStart.Incomplete).
                // X-619 M1 (review of 0.1557): such a row is then set aside (DiscoveryDatabase.SetAside): the next start reads your facts whole
                // again (before, the row stayed and blocked the sharing for good).
                // X-652 (user decision D-188 b of 20/11): and this session's sending resumes once the rows are set aside and listed for the read-back
                // of their copy on your gist (DiscoveryStart.Resume); before, it stayed blocked until the next start.
                var unreadable=new HashSet<string>();
                var (gotMine,mine)=await Discoveries.DiscoveryStart.ReadWithRetries(()=>(Facts:opened.ReadMine(Discoveries.DiscoveryStart.Incomplete(discoveries,(key,error)=>{lock(unreadable)unreadable.Add(key);log.Warning(error,"A discovery row could not be read ({0}); it is set aside after the read.",key);})),Kinds:opened.SharedKinds()),
                    Discoveries.DiscoveryStart.Tries,Discoveries.DiscoveryStart.Wait,
                    (error,attempt)=>{log.Warning(error,"Your discoveries could not be read (try {0} of {1}).",attempt,Discoveries.DiscoveryStart.Tries);mainWindow.DiscoveriesReadFailed=attempt;},
                    gameDataCancel.Token).ConfigureAwait(false);
                if(gotMine)
                {
                    discoveries.Load(mine.Facts);
                    string[] aside;lock(unreadable)aside=[..unreadable];
                    var moved=0;
                    if(aside.Length>0)
                    {
                        try{moved=opened.SetAside(aside,DateTimeOffset.UtcNow);log.Warning("Discoveries: {0} row(s) that could not be read set aside in the table facts_aside of {1} (kept, never erased); the next start reads your facts whole.",moved,Discoveries.DiscoveryDatabase.FileName);}
                        catch(Exception error){log.Warning(error,"Discoveries: the rows that could not be read could not be set aside; they stay and are tried again at the next start.");}
                    }
                    // X-621 (review of 0.1560, I1): your rows set aside (now or at an earlier start) and not settled: the next send reads their copy
                    // back from your gist first (GitHubExchange.KeepAside), never replaces the gist without them. Not listed: nothing sent this session.
                    try
                    {
                        if(opened.AsidePending() is { Count:>0 } pending)
                        {
                            discoveries.Aside=new(pending,kept=>opened.RestoreAside(pending,kept,DateTimeOffset.UtcNow));
                            log.Information("Discoveries: {0} row(s) of yours set aside and not settled; the next send reads their copy back from your gist first.",pending.Count);
                        }
                    }
                    catch(Exception error){discoveries.MineIncomplete=true;log.Warning(error,"Discoveries: the rows set aside could not be listed; nothing is sent to your gist this session.");}
                    if(Discoveries.DiscoveryStart.Resume(discoveries,aside,moved))
                        log.Information("Discoveries: the sending to your gist resumes this session; the copy of the {0} row(s) set aside is read back from your gist before any send (X-652).",aside.Length);
                    else if(aside.Length>0)
                        log.Warning("Discoveries: {0} row(s) could not be read and {1} set aside; nothing is sent to your gist this session.",aside.Length,moved);
                    discoveries.UseSharedSource(kind=>opened.ReadShared(kind,(key,error)=>log.Warning(error,"A discovery row could not be read ({0}); left in the database.",key)),mine.Kinds);
                    mainWindow.DiscoveriesReadFailed=0;
                }
                else discoveries.MineIncomplete=true;
                mainWindow.DiscoveriesLoaded=true;
                return;
            }
            // X-93 part 2 (0.1039): a base that cannot be read (over 64 MB, damaged) is set aside under a dated name and said in the chat and on the page,
            // never overwritten by an empty base; if it cannot even be moved, nothing is saved this session.
            var read=Discoveries.DiscoveryFiles.ReadBase(saved,DateTimeOffset.UtcNow,(aside,error)=>log.Warning(error,"The discoveries could not be read; the file is set aside as {0}.",Path.GetFileName(aside)));
            // X-587 (external audit A16 of 18/11): a base set aside or that could not be read: only this session's facts are held, never sent.
            if(read.Unsafe || read.SetAside is not null)discoveries.MineIncomplete=true;
            if(read.File is { } file)discoveries.Load(file);
            if(read.Unsafe){mainWindow.DiscoveriesWriteBlocked=true;log.Error("The discoveries file could not be read nor set aside: nothing is saved this session.");}
            if(read.SetAside is { } setAside)
            {
                mainWindow.DiscoveriesSetAside=setAside;
                var said=localization.Format(read.Unsafe ? "DiscoveriesUi.BaseBlocked":"DiscoveriesUi.BaseSetAside",Path.GetFileName(setAside));
                _=framework.RunOnFrameworkThread(()=>chat.PrintError("[Tensho] "+said));
            }
            // X-93 (0.941): the shared discoveries, kept apart by kind, read only when a page asks for their kind (an unreadable file is set aside).
            discoveries.UseSharedSource(kind=>Discoveries.DiscoveryFiles.ReadSharedKind(saved,kind,(path,error)=>log.Warning(error,"Shared discoveries {0} could not be read; the file is kept as .broken.",Path.GetFileName(path))),
                Discoveries.DiscoveryFiles.SharedKindsOnDisk(saved));
            mainWindow.DiscoveriesLoaded=true;
        });
    }
}
