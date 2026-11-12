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
            case "characters":
                UseDesktopCharacters(folder,warn);
                break;
            case "duties":
                UseDesktopDuties(folder,warn);
                break;
            case "discoveries":
                UseDesktopDiscoveries(folder,warn);
                break;
            case "data":
                UseDesktopPlayerData(folder,warn);
                UseDesktopVoyages(folder,warn);
                UseDesktopBank(folder,warn);
                UseDesktopAchievements(folder,warn);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category),category,"not a category the desktop application reads");
        }
    }

    // X-119 with X-173 (0.1147): the player's data file, the application's copy (TenshoDataDatabase.CopyForDesktop, SQLite's backup): Storage › Gil
    // shows the journal of the character with the newest entry (no character is logged in; choice D-142), its balances as last recorded. Read
    // off the frame; a newer reading replaces an older one, never the reverse. No copy: the page says offline, as before. The achievement
    // progress: since 0.1150 from the same copy (UseDesktopAchievements).
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

    // X-119 with X-173 (0.1150): the achievement progress from the copy of the data file (Collection › Achievements and the tracked list): the
    // character whose progress was seen last (no character is logged in; as the gil journal, D-142). Read off the frame; a newer reading replaces
    // an older one, never the reverse. The copy holds only what the plugin converted: the application never converts (the file of before is not
    // copied). No copy: « not seen yet », as before.
    private int desktopAchievementsGeneration;
    private void UseDesktopAchievements(string folder,Action<Exception,string> warn)
    {
        var generation=Interlocked.Increment(ref desktopAchievementsGeneration);
        var file=Path.Combine(folder,TenshoDataDatabase.FileName);
        if(!File.Exists(file)){Volatile.Write(ref desktopAchievements,null);return;}
        var progress=new Collections.AchievementProgressData(Path.Combine(folder,"AchievementProgress.json"),new TenshoDataFile(file,warn));
        _=Task.Run(()=>
        {
            IReadOnlyDictionary<uint,Collections.AchievementProgress>? seen=null;
            try
            {
                var all=progress.Load(DateTimeOffset.UtcNow);
                if(Collections.AchievementProgressData.Latest(all) is { } character)seen=all[character];
            }
            catch(Exception error){warn(error,"The achievement progress could not be read from the copy of the Tensho data file.");return;}
            if(Volatile.Read(ref desktopAchievementsGeneration)==generation)Volatile.Write(ref desktopAchievements,seen);
        });
    }

    // X-119 (0.1151): the player's characters (Reminders › Characters) from the copy of Characters/ (one file per character, as the plugin wrote
    // them), read off the frame; a file that does not read is left out and said (the plugin keeps it); a newer reading replaces an older one,
    // never the reverse. Erasing a character stays in the game (the application's copy is not the plugin's file).
    private IReadOnlyList<Character.CharacterStatus>? desktopCharacters;
    private int desktopCharactersGeneration;
    private void UseDesktopCharacters(string folder,Action<Exception,string> warn)
    {
        var generation=Interlocked.Increment(ref desktopCharactersGeneration);
        var characters=Path.Combine(folder,"Characters");
        _=Task.Run(()=>
        {
            IReadOnlyList<Character.CharacterStatus> read;
            try{read=[.. Character.CharacterStatuses.ReadAll(characters,(_,error)=>warn(error,"A character file of the copy could not be read; it is left out."))];}
            catch(Exception error){warn(error,"The characters could not be read from the copy.");return;}
            if(Volatile.Read(ref desktopCharactersGeneration)==generation)Volatile.Write(ref desktopCharacters,read);
        });
    }

    // X-119 (0.1151): the duty progress (Duties, Deep dungeons) from the copy of DutyProgress.json: the state saved for the last character, as the
    // game shows it logged out (DutyProgress.LastShown), read off the frame; a newer reading replaces an older one. A file that does not read:
    // unknown, as before.
    private Duties.DutyProgressState? desktopDuties;
    private int desktopDutiesGeneration;
    private void UseDesktopDuties(string folder,Action<Exception,string> warn)
    {
        var generation=Interlocked.Increment(ref desktopDutiesGeneration);
        var file=Path.Combine(folder,"DutyProgress.json");
        DutyProgressSource=()=>Volatile.Read(ref desktopDuties);
        _=Task.Run(()=>
        {
            Duties.DutyProgressState? state=null;
            try{if(File.Exists(file))state=Duties.DutyProgress.LastShown(File.ReadAllText(file));}
            catch(Exception error){warn(error,"The duty progress could not be read from the copy.");}
            if(Volatile.Read(ref desktopDutiesGeneration)==generation)Volatile.Write(ref desktopDuties,state);
        });
    }

    // X-119 (0.1152): the discoveries (the Discoveries page, an item's sources) from the application's copy of Discoveries.db (SQLite's backup of the
    // plugin's file, never the folder's other files), read off the frame into a new store, then shown; a newer reading replaces an older one. Never
    // written: the copy is not the plugin's base (DiscoveriesWriteBlocked; no GitHub exchange in the application). No copy: « loading », as before.
    private int desktopDiscoveriesGeneration;
    private void UseDesktopDiscoveries(string folder,Action<Exception,string> warn)
    {
        var generation=Interlocked.Increment(ref desktopDiscoveriesGeneration);
        var discoveries=Path.Combine(folder,"Discoveries");var file=Path.Combine(discoveries,Discoveries.DiscoveryDatabase.FileName);
        DiscoveriesWriteBlocked=true;
        if(!File.Exists(file))return;
        _=Task.Run(()=>
        {
            var store=new Discoveries.DiscoveryStore();
            try
            {
                var database=new Discoveries.DiscoveryDatabase(file);
                store.Load(database.ReadMine((_,error)=>warn(error,"A discovery row of the copy could not be read; it is left out.")));
                store.UseSharedSource(kind=>database.ReadShared(kind,(_,error)=>warn(error,"A discovery row of the copy could not be read; it is left out.")),database.SharedKinds());
            }
            catch(Exception error){warn(error,"The discoveries could not be read from the copy.");return;}
            if(Volatile.Read(ref desktopDiscoveriesGeneration)!=generation)return;
            DiscoveryFolder=discoveries;DiscoveryStore=store;DiscoveriesLoaded=true;
        });
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
    // X-163 (0.1154): whether the application also shows a Windows notification for them; called by its name (DesktopNotice1154Tests).
    internal bool DesktopAlarmNotification()=>(configuration.AlarmOptions ?? new()).DesktopNotification;
    private Alarms.AlarmBell? desktopBell;
    private Reminders.GardenBell? desktopGarden;
    internal (string[] Lines,bool Sound) DesktopDueAlarms(DateTimeOffset now,bool gameInFront)
    {
        var options=configuration.AlarmOptions ?? new();
        var catalog=weather.State.Catalog;
        var due=(desktopBell??=new()).Due(configuration.Alarms,now,catalog is null ? null:(alarm,time)=>Alarms.AlarmService.WeatherAt(catalog,alarm.TerritoryId,time));
        // X-163 (0.1153): the garden beds ripe or to tend too, when the garden reminders are on (GardenBell: the journal's rule, the application's own
        // memory; a bed the plugin already said is not said again), in the same words as the game's chat line.
        var garden=configuration.GardenAlerts ? (desktopGarden??=new()).Due(configuration.GardenBeds,now):[];
        if(due.Count+garden.Count==0 || options.DesktopOnlyGameBehind && gameInFront)return ([],false);
        return ([.. due.Select(item=>localization.Format("Desktop.AlarmLine",item.Alarm.Label,item.Occurrence.ToLocalTime().ToString("t"),Math.Max(0,(int)Math.Ceiling((item.Occurrence-now).TotalMinutes)))),
            .. garden.Select(item=>localization.Format(item.State==Reminders.GardenState.Ripe ? "Garden.RipeNotice":"Garden.TendNotice",item.Bed.Seed,item.Bed.Place))],options.DesktopSound);
    }
}
