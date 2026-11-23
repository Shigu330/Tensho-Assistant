using System.IO.Compression;
using TenshoAssistant;
using TenshoAssistant.Caching;

// 0.1854 (X-845, review 0.1844 M1). Behaviour tests (R28.14) on real zip files in folders of their own: the zip copies that « Empty the cache »
// took before 0.1840 kept the market boards of v1, with other players' retainer names; they are written again once without them, the rest
// of each copy kept, and a new copy never holds them.
internal static class RetiredCopies1854Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Folder(string name)=>Path.Combine(Path.GetTempPath(),"tensho-1854-"+name+"-"+Guid.NewGuid().ToString("N"));
    private static void Clean(string folder){try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}

    private static string Name(char fill)=>new string(fill,64)+".json";
    // A cached file as 0.1839 wrote it (the envelope's first fields are what the cache reads to know the copy).
    private static string Envelope(string family,string scope,string version,string value)
        =>"{\"Schema\":1,\"Family\":\""+family+"\",\"Scope\":\""+scope+"\",\"Version\":\""+version+"\",\"RecordedAt\":\"2026-11-01T12:00:00+00:00\",\"Value\":"+value+"}";
    private const string Named="{\"listings\":[{\"pricePerUnit\":120,\"worldName\":\"Odin\",\"retainerName\":\"Sellerofthings\"}]}";

    // A cache folder of before: one v1 board (names), one v2 board, another family's v1, a file in « previous ».
    private static string OldCache(string folder)
    {
        Directory.CreateDirectory(Path.Combine(folder,"previous"));
        File.WriteAllText(Path.Combine(folder,Name('a')),Envelope("market-board","Odin:5057","universalis-board-v1",Named));
        File.WriteAllText(Path.Combine(folder,Name('b')),Envelope("market-board","Odin:5057","universalis-board-v2","{\"listings\":[{\"pricePerUnit\":120}]}"));
        File.WriteAllText(Path.Combine(folder,Name('c')),Envelope("other-family","x","universalis-board-v1","[1,2,3]"));
        File.WriteAllText(Path.Combine(folder,"previous",Name('d')),Envelope("catalog-items","all","7.5","[]"));
        return folder;
    }
    // A zip as BackupZip wrote it before 0.1854 (no skip).
    private static string OldZip(string cache,string backups,string stamp)
    {
        Directory.CreateDirectory(backups);
        var zip=Path.Combine(backups,"backup-"+stamp+"-cache.zip");
        ZipFile.CreateFromDirectory(cache,zip);
        File.SetLastWriteTimeUtc(zip,new DateTime(2026,11,1,12,0,0,DateTimeKind.Utc));
        return zip;
    }
    private static Dictionary<string,string> Entries(string zip)
    {
        using var archive=ZipFile.OpenRead(zip);
        return archive.Entries.ToDictionary(entry=>entry.FullName.Replace('\\','/'),entry=>{using var reader=new StreamReader(entry.Open());return reader.ReadToEnd();});
    }

    public static readonly (string Name,Action Run)[] Cases=
    [
        ("X-845: an old cache zip loses its v1 boards (the retainer names) and keeps everything else, its time and its name",()=>
        {
            var folder=Folder("clean");
            try
            {
                var cache=OldCache(Path.Combine(folder,"DataCache-v1"));
                var backups=Path.Combine(folder,"Backups");
                var zip=OldZip(cache,backups,"20261101-120000");
                var clean=OldZip(cache,Path.Combine(folder,"elsewhere"),"20261101-120000");
                File.Delete(Path.Combine(cache,Name('a')));
                var withoutNames=OldZip(cache,backups,"20261102-120000");
                var untouched=File.ReadAllBytes(withoutNames);
                Check(Entries(zip).Values.Any(text=>text.Contains("Sellerofthings",StringComparison.Ordinal)),"the old zip holds a name before");
                Check(RetiredCopies.Clean(backups)==1,"one zip written again (the other held no v1 board)");
                var left=Entries(zip);
                Check(!left.Values.Any(text=>text.Contains("Sellerofthings",StringComparison.Ordinal)),"no retainer name left in the old zip");
                Check(left.Count==3 && left.ContainsKey(Name('b')) && left.ContainsKey(Name('c')) && left.ContainsKey("previous/"+Name('d')),
                    "the v2 board, the other family's v1 and the file in « previous » kept: "+string.Join(", ",left.Keys));
                Check(File.GetLastWriteTimeUtc(zip)==new DateTime(2026,11,1,12,0,0,DateTimeKind.Utc),"its time kept (the newest five are kept by time)");
                Check(File.ReadAllBytes(withoutNames).SequenceEqual(untouched),"a zip with no v1 board left byte for byte");
                Check(File.Exists(Path.Combine(backups,RetiredCopies.MarkName)),"the pass marked as done");
                Check(Directory.GetFiles(backups,"*.tmp").Length==0,"no passing file left");
                Check(Entries(clean).Values.Any(text=>text.Contains("Sellerofthings",StringComparison.Ordinal)),"a zip outside Backups is not touched");
                // Done once: a zip put back with names (by hand) is no longer looked at.
                File.Copy(clean,zip,true);
                Check(RetiredCopies.Clean(backups)==0,"after the mark, nothing is read again");
            }
            finally{Clean(folder);}
        }),
        ("X-845: a zip held by another program is left for the next load, which cleans it; no mark meanwhile",()=>
        {
            var folder=Folder("held");
            try
            {
                var cache=OldCache(Path.Combine(folder,"DataCache-v1"));
                var backups=Path.Combine(folder,"Backups");
                var zip=OldZip(cache,backups,"20261101-120000");
                using(new FileStream(zip,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                    Check(RetiredCopies.Clean(backups)==0,"held: not written");
                Check(!File.Exists(Path.Combine(backups,RetiredCopies.MarkName)),"no mark while a zip is left");
                Check(RetiredCopies.Clean(backups)==1 && !Entries(zip).Values.Any(text=>text.Contains("Sellerofthings",StringComparison.Ordinal)),"cleaned at the next load");
                Check(File.Exists(Path.Combine(backups,RetiredCopies.MarkName)),"then marked");
            }
            finally{Clean(folder);}
        }),
        ("X-845: « Empty the cache » takes its zip without the v1 boards not yet trimmed; a copy cut short is a passing file BackupZip sweeps",()=>
        {
            var folder=Folder("new");
            try
            {
                var cache=OldCache(Path.Combine(folder,"DataCache-v1"));
                var retired=DataCache.RetiredFiles(cache);
                Check(retired.Count==1 && retired.Contains(Name('a')),"only the market board of v1 is retired: "+string.Join(", ",retired));
                var zip=BackupZip.Create(cache,Path.Combine(folder,"Backups"),"cache",DateTimeOffset.UtcNow,retired);
                Check(zip is not null,"the copy taken");
                var entries=Entries(zip!);
                Check(!entries.Values.Any(text=>text.Contains("Sellerofthings",StringComparison.Ordinal)) && entries.Count==3,"no retainer name in the new copy, the rest kept: "+string.Join(", ",entries.Keys));
                Check(BackupZip.Leftover(Path.GetFileName(zip+"."+Guid.NewGuid().ToString("N")+".tmp")),"the passing file of a rewrite has a name BackupZip.Sweep removes");
            }
            finally{Clean(folder);}
        }),
        ("X-845: Emptying the cache passes the retired copies as BackupZip's skip; the load runs the pass on the Backups folder",()=>
        {
            var caching=File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant","MainWindow.Caching.cs")));
            Check(caching.Contains("BackupZip.Create(root,backups,\"cache\",DateTimeOffset.UtcNow,DataCache.RetiredFiles(root))",StringComparison.Ordinal),"the skip given");
            var load=File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant","Plugin.Discoveries.cs")));
            Check(load.Contains("Caching.RetiredCopies.Clean(Discoveries.DiscoveryBackup.Folder(discoveryFolder),out var damaged,gameDataCancel.Token)",StringComparison.Ordinal),"the pass at load");
        }),
    ];
}
