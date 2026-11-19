using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1250, fifth slice, GPT review of 11/11, accepted by the user): the discoveries family out of Plugin.cs, after the desktop (0.1225),
// persistence (0.1239), server bar (0.1240) and panel (0.1249) families. The local base of discoveries (X-43): its store, its folder, the
// exchange through GitHub (X-77.1; one install path and one release path, in the plugin's registrations) and its loading off the frame,
// moved as they were: the prices seen at the market board first (MarketSeen.json lives in the same folder), then the discoveries
// (SQLite since X-172, the files of before converted once; a database that cannot be opened is never replaced; a base that cannot be read
// is set aside, never overwritten by an empty one). Nothing is written before it is read. The item watcher that fills the base stays with
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
        registrations.Register(()=>mainWindow.DiscoveryExchange=discoveryExchange=new Discoveries.GitHubExchange(discoveryFolder),()=>discoveryExchange?.Dispose());
        return (discoveries,discoveryFolder);
    }

    /// <summary>The prices seen, then the discoveries, read on a worker (as before the move); the chat lines go through the framework thread.</summary>
    private void LoadDiscoveries(string discoveryFolder,Discoveries.DiscoveryStore discoveries,Market.SeenPriceStore seenPrices,IFramework framework,IChatGui chat)
    {
        _=Task.Run(async()=>
        {
            try{if(Market.SeenPriceStore.Read(Path.Combine(discoveryFolder,"MarketSeen.json")) is { } prices)seenPrices.Load(prices);}
            catch(Exception error)
            {
                // X-587 (external audit A15 of 18/11): set aside under a dated name, never over one set aside before.
                var pricesPath=Path.Combine(discoveryFolder,"MarketSeen.json");var aside=Discoveries.DiscoveryFiles.SetAsideName(pricesPath,DateTimeOffset.UtcNow);
                log.Warning(error,"The market prices seen could not be read; the file is set aside as {0}.",Path.GetFileName(aside));
                try{File.Move(pricesPath,aside,false);}catch(Exception){}
            }
            var saved=Path.Combine(discoveryFolder,"Discoveries.json");
            // X-172 (0.1136, trial docs/evidence/x172-sqlite-trial.md): the discoveries in Discoveries.db (SQLite), the files of before converted once
            // and kept aside. A database that exists but cannot be opened (SQLite not loaded, damaged, newer layout) is never replaced: nothing is
            // saved this session; without a database (conversion failed), the JSON files stay in use as before.
            var databasePath=Path.Combine(discoveryFolder,Discoveries.DiscoveryDatabase.FileName);
            Discoveries.DiscoveryDatabase? database=null;
            try
            {
                var aside=Discoveries.DiscoveryDatabase.ConvertFiles(discoveryFolder,DateTimeOffset.UtcNow,(path,error)=>log.Warning(error,"Discoveries {0} could not be read while converting; set aside.",Path.GetFileName(path)),
                    merged=>log.Information("Discoveries: Discoveries.json written again after the conversion (an older version); {0} facts merged into the database.",merged),
                    (path,error)=>log.Information("Discoveries: {0} is held by another program for now and stays where it is; the next start sets it aside ({1}).",Path.GetFileName(path),error.Message));
                database=new Discoveries.DiscoveryDatabase(databasePath);
                var (mine,sharedCount)=database.Count();
                log.Information("Discoveries: SQLite {0} database, {1} facts of yours, {2} shared{3}.",database.LibraryVersion(),mine,sharedCount,
                    aside is null ? "":$"; the files of before kept in {Path.GetFileName(aside)}");
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
                log.Error(error,"The discoveries could not be moved to SQLite: the JSON files stay in use this session.");
            }
            mainWindow.DiscoveryDatabase=database;
            if(database is { } opened)
            {
                // X-588 (external audit A01 of 18/11): your facts and the shared kinds' list read inside a retry (outside any try before: an error
                // left the page « loading » for the session); each failure said on the page; still failing after the last try, the page opens
                // without your facts and nothing is sent to GitHub this session (MineIncomplete); new sightings are still saved, row by row,
                // added to what the base holds (X-171).
                var (gotMine,mine)=await Discoveries.DiscoveryStart.ReadWithRetries(()=>(Facts:opened.ReadMine((key,error)=>log.Warning(error,"A discovery row could not be read ({0}); left in the database.",key)),Kinds:opened.SharedKinds()),
                    Discoveries.DiscoveryStart.Tries,Discoveries.DiscoveryStart.Wait,
                    (error,attempt)=>{log.Warning(error,"Your discoveries could not be read (try {0} of {1}).",attempt,Discoveries.DiscoveryStart.Tries);mainWindow.DiscoveriesReadFailed=attempt;},
                    gameDataCancel.Token).ConfigureAwait(false);
                if(gotMine)
                {
                    discoveries.Load(mine.Facts);
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
