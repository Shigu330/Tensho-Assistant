using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// X-119 step 2, part 2 and X-151 (0.1131): the one entry the desktop application calls for the plugin's saved data, written here so that a
// change of a store breaks the build, not the application at run time (the application calls this method by its name only, checked by
// DesktopData1131Tests, instead of rebuilding the stores by reflection). The folder is the application's own copy of the plugin's folder
// (DesktopMirror), never the plugin's; the stores are built as Plugin.cs builds them, and built again when the link says their category
// changed. Categories: LocalLinkProtocol.DesktopCategories.
internal sealed partial class MainWindow
{
    internal void UseDesktopData(string folder,string category,Action<Exception,string> warn)
    {
        switch(category)
        {
            case "bank":
                UseDesktopBank(folder,warn);
                break;
            case "craft-lists":
                CraftListStore=new Craft.CraftListStore(Path.Combine(folder,"CraftLists.json"),warn);
                break;
            case "my-tools":
                var previousTools=MyToolsStore;
                MyToolsStore=new MyTools.ToolTemplateStore(Path.Combine(folder,"MyTools.json"),warn);
                previousTools?.Dispose();
                break;
            case "voyages":
                UseDesktopVoyages(folder,warn);
                break;
            case "beast-teams":
                var teams=Path.Combine(folder,"BeastTeams.json");
                BeastTeamsFile=()=>teams;
                beastPlans=null;beastPlansLoaded=false;
                break;
            case "data":
                UseDesktopPlayerData(folder,warn);
                UseDesktopVoyages(folder,warn);
                UseDesktopBank(folder,warn);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category),category,"not a category the desktop application reads");
        }
    }

    // X-119 with X-173 (0.1147): the player's data file, the application's copy (TenshoDataDatabase.CopyForDesktop, SQLite's backup): Storage › Gil
    // shows the journal of the character with the newest entry (no character is logged in; choice D-142), its balances as last recorded. Read
    // off the frame; a newer reading replaces an older one, never the reverse. No copy: the page says offline, as before. The achievement
    // progress stays in the game for now (its page reads the game's watcher).
    // X-173 (0.1148): the voyage history from the copy of the data file once the plugin converted it (no VoyageHistory.json left to copy), from
    // the JSON copy before (the application never converts its copy itself: the next copy of the data file would undo it).
    private void UseDesktopVoyages(string folder,Action<Exception,string> warn)
    {
        var json=Path.Combine(folder,"VoyageHistory.json");var file=Path.Combine(folder,TenshoDataDatabase.FileName);
        VoyageHistorySource=new FreeCompany.VoyageHistoryStore(json,warn,!File.Exists(json) && File.Exists(file) ? new TenshoDataFile(file,warn):null);
    }

    // X-173 (0.1149): the bank and inventories from the copy of the data file once the plugin converted them (no bank file left in the copy:
    // DesktopMirror removes the copies of files gone from the plugin's folder), from the copy of the files before; the application never
    // converts its copy itself (the data file is given only when no bank file is left, so there is nothing to convert).
    private void UseDesktopBank(string folder,Action<Exception,string> warn)
    {
        var files=Path.Combine(folder,"InventoryBank");var legacy=Path.Combine(folder,"InventoryBank.json");var file=Path.Combine(folder,TenshoDataDatabase.FileName);
        var json=File.Exists(legacy) || Directory.Exists(files) && Directory.EnumerateFiles(files,"*.json*").Any();
        var previousBank=bank;
        bank=new InventoryBankService(new PerCharacterBankStore(files,legacy,data:!json && File.Exists(file) ? new TenshoDataFile(file,warn):null));
        previousBank?.Dispose();
    }

    private Inventory.GilJournalState? desktopGil;
    private int desktopGilGeneration;
    private void UseDesktopPlayerData(string folder,Action<Exception,string> warn)
    {
        var generation=Interlocked.Increment(ref desktopGilGeneration);
        var file=Path.Combine(folder,TenshoDataDatabase.FileName);
        GilJournalState=()=>Volatile.Read(ref desktopGil);
        if(!File.Exists(file)){Volatile.Write(ref desktopGil,null);return;}
        var journal=new Inventory.GilJournalStore(Path.Combine(folder,"GilJournal"),new TenshoDataFile(file,warn));
        _=Task.Run(()=>
        {
            Inventory.GilJournalState? state=null;
            try
            {
                var now=DateTimeOffset.UtcNow;
                if(journal.Latest() is { } character)
                {
                    var entries=Inventory.GilJournal.Prune(journal.Load(character,now),now);
                    var (gil,retainers,company)=Inventory.GilJournal.LastBalances(entries);
                    state=new Inventory.GilJournalState(character,entries.ToArray(),gil,retainers,company,false);
                }
            }
            catch(Exception error){warn(error,"The gil journal could not be read from the copy of the Tensho data file.");return;}
            if(Volatile.Read(ref desktopGilGeneration)==generation)Volatile.Write(ref desktopGil,state);
        });
    }

    // X-163 (0.1132): the alarms the desktop application rings now (AlarmBell: the plugin's rule, the application's own memory), as lines
    // in the plugin's language, and whether it plays a sound. Called by the application once a second, by its name (DesktopData1131Tests).
    // An alarm due while the game is in front is marked rung and not shown when DesktopOnlyGameBehind is on: it rings in the game.
    private Alarms.AlarmBell? desktopBell;
    internal (string[] Lines,bool Sound) DesktopDueAlarms(DateTimeOffset now,bool gameInFront)
    {
        var options=configuration.AlarmOptions ?? new();
        var catalog=weather.State.Catalog;
        var due=(desktopBell??=new()).Due(configuration.Alarms,now,catalog is null ? null:(alarm,time)=>Alarms.AlarmService.WeatherAt(catalog,alarm.TerritoryId,time));
        if(due.Count==0 || options.DesktopOnlyGameBehind && gameInFront)return ([],false);
        return ([.. due.Select(item=>localization.Format("Desktop.AlarmLine",item.Alarm.Label,item.Occurrence.ToLocalTime().ToString("t"),Math.Max(0,(int)Math.Ceiling((item.Occurrence-now).TotalMinutes))))],options.DesktopSound);
    }
}
