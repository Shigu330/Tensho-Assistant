using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Character;

// 0.1228 (X-218, user's request of 12/11, K-32): each character's job levels and gear sets kept in TenshoData.db and shown in Storage › Characters.
// Behaviour tests (R28: data loss, concurrency, privacy): a reading saved and read back; an older reading (another client on the same character)
// never written over a newer one, in either order; a failed save leaves the reading of before whole; a reading out of bounds refused; « Erase this
// character » removes both tables' rows of that character only. Text tests for the wiring (reader, plugin, page, desktop copy).
internal static class CharacterJobs1228Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset T0=new(2026,11,13,8,0,0,TimeSpan.Zero);

    private static CharacterJobs Reading(ulong character,DateTimeOffset at,int level,params SavedGearset[] sets)
        => new(character,at,new Dictionary<uint,int>{[19]=level,[24]=level-10},sets);
    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-jobs-1228-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(Path.Combine(root,TenshoDataDatabase.FileName));}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static SavedGearset G(int number,string name,uint job,int level)=>new(number,name,job,level);
    private static CharacterJobsData Store(string file)=>new(new TenshoDataFile(file,(error,message)=>throw new Exception(message,error)));
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Character jobs 1228: behaviour: a reading saved is read back whole (levels, sets, time), per character",() => InTemp(file =>
        {
            var store=Store(file);
            Check(store.Save(Reading(5,T0,100,G(1,"Paladin main",19,730),G(2,"",24,700))),"first reading saved");
            Check(store.Save(Reading(6,T0,90)),"another character saved");
            var all=Store(file).Load();
            Check(all.Count==2,"two characters: "+all.Count);
            var five=all[5];
            Check(five.Levels[19]==100 && five.Levels[24]==90 && five.Levels.Count==2,"levels read back");
            Check(five.Gearsets.Count==2 && five.Gearsets[0]==new SavedGearset(1,"Paladin main",19,730) && five.Gearsets[1].Name=="","sets read back in order");
            Check(five.SeenAt==T0,"time read back");
            Check(all[6].Gearsets.Count==0,"a character with no set");
        })),
        ("Character jobs 1228: behaviour: two clients on one character: the newer reading is kept whatever the order of the saves (concurrency)",() => InTemp(file =>
        {
            var first=Store(file);var second=Store(file);
            Check(first.Save(Reading(5,T0.AddMinutes(5),100,G(1,"New",19,740))),"newer saved");
            Check(!second.Save(Reading(5,T0,80,G(1,"Old",19,600))),"older reading refused");
            Check(!second.Save(Reading(5,T0.AddMinutes(5),80)),"same time refused (never replaced by an equal reading)");
            var kept=first.Load()[5];
            Check(kept.Levels[19]==100 && kept.Gearsets.Single().Name=="New","newer kept: "+kept.Levels[19]);
            // Both clients saving at once from two threads: whichever ends last, the newest reading is the one kept.
            for(var round=0;round<5;round++)
            {
                var at=T0.AddHours(round+1);
                var a=Task.Run(()=>first.Save(Reading(5,at,50+round)));
                var b=Task.Run(()=>second.Save(Reading(5,at.AddSeconds(1),60+round)));
                Task.WaitAll(a,b);
                Check(first.Load()[5].Levels[19]==60+round,"round "+round+": the later reading kept");
            }
        })),
        ("Character jobs 1228: behaviour: a save that fails in its transaction leaves the reading of before whole (data loss)",() => InTemp(file =>
        {
            var store=Store(file);
            store.Save(Reading(5,T0,100,G(1,"Kept",19,730),G(2,"Kept too",24,700)));
            store.DuringSave=()=>throw new IOException("disk full");
            try{store.Save(Reading(5,T0.AddMinutes(1),20));Check(false,"the failure is said");}
            catch(IOException){}
            store.DuringSave=null;
            var kept=Store(file).Load()[5];
            Check(kept.Levels[19]==100 && kept.Gearsets.Count==2 && kept.SeenAt==T0,"reading of before whole");
        })),
        ("Character jobs 1228: behaviour: a reading out of bounds is refused, never written",() => InTemp(file =>
        {
            var store=Store(file);
            Check(!store.Save(new CharacterJobs(0,T0,new Dictionary<uint,int>{[19]=10},[])),"no character");
            Check(!store.Save(new CharacterJobs(5,T0,new Dictionary<uint,int>(),[])),"no level read");
            Check(!store.Save(new CharacterJobs(5,T0,new Dictionary<uint,int>{[19]=255},[])),"level beyond the game's");
            Check(!store.Save(new CharacterJobs(5,T0,new Dictionary<uint,int>{[19]=10},[G(1,"a",19,700),G(1,"b",19,700)])),"two sets with one number");
            Check(!store.Save(new CharacterJobs(5,T0,new Dictionary<uint,int>{[19]=10},[G(1,new string('x',CharacterJobsData.MaxName+1),19,700)])),"name too long");
            Check(store.Load().Count==0,"nothing written");
        })),
        ("Character jobs 1228: behaviour: « Erase this character » removes its job levels and gear sets, the other character kept (privacy)",() => InTemp(file =>
        {
            var store=Store(file);
            store.Save(Reading(5,T0,100,G(1,"Qzxjv Jobsmarker",19,730)));store.Save(Reading(6,T0,90,G(1,"Other",19,700)));
            Check(TenshoDataDatabase.CharacterTables.Contains("character_jobs") && TenshoDataDatabase.CharacterTables.Contains("character_gearsets"),"both tables erased with a character");
            var database=new TenshoDataDatabase(file);
            using(var open=database.Open())
            using(var transaction=open.BeginTransaction())
            {
                TenshoDataDatabase.EraseCharacter(open,transaction,5);
                transaction.Commit();
            }
            var all=Store(file).Load();
            Check(!all.ContainsKey(5) && all[6].Gearsets.Single().Name=="Other","character 5 gone, 6 kept");
            using var check=database.Open();
            Check(Convert.ToInt64(TenshoDataDatabase.Scalar(check,"SELECT COUNT(*) FROM character_gearsets WHERE name LIKE '%Jobsmarker%';"))==0,"no gear set name left");
        })),
        ("Character jobs 1228: SameContent ignores the time and sees a changed level or set",() =>
        {
            var a=Reading(5,T0,100,G(1,"A",19,730));
            Check(a.SameContent(Reading(5,T0.AddHours(1),100,G(1,"A",19,730))),"same content, later");
            Check(!a.SameContent(Reading(5,T0,99,G(1,"A",19,730))),"level changed");
            Check(!a.SameContent(Reading(5,T0,100,G(1,"A",19,731))),"set changed");
            Check(!a.SameContent(Reading(6,T0,100,G(1,"A",19,730))),"other character");
            Check(!a.SameContent(null),"nothing before");
        }),
        ("Character jobs 1228: wiring: copy at login by the gear set reader, saved off the frame, page column and details, desktop copy",() =>
        {
            var reader=Source("src/TenshoAssistant/Gear/GameGearsetReader.Dalamud.cs");
            Check(reader.Contains("public Action<GearsetState>? Copied") && reader.Contains("var copyDue=Copied is not null && now>=nextCopy;")
                  && reader.Contains("nextCopy=now.AddSeconds(levels.Count>0 ? 300:10);"),"reader copies about 10 s after a login, then every 5 minutes");
            var plugin=Source("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("gameGearsets.Copied=read=>") && plugin.Contains("characterJobsWriter.Enqueue(") && plugin.Contains("if(jobs.SameContent(lastCharacterJobs))return;"),
                "plugin saves changed readings through a background writer");
            var page=Source("src/TenshoAssistant/MainWindow.Characters.cs");
            Check(page.Contains("\"Retainers\",\"Jobs\"") && page.Contains("DrawCharacterJobsCell(summary.Id);") && page.Contains("DrawCharacterJobsDetails(characterSummaries);"),"page column and details");
            Check(Source("src/TenshoAssistant/MainWindow.CharacterJobs.cs").Contains("characterJobsLoading=Task.Run(data.Load);"),"read off the frame");
            Check(Source("src/TenshoAssistant/MainWindow.DesktopData.cs").Contains("CharacterJobs=File.Exists(jobsFile)"),"desktop reads the copy of the data file");
        }),
    ];
}
