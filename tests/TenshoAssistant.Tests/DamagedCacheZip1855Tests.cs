using System.IO.Compression;
using TenshoAssistant.Caching;

// X-851 (review 0.1854, M1). Behaviour tests (R28.14) on real files: a « cache » zip damaged for good (not a zip, an entry with an unknown
// compression method) no longer keeps the pass from ending nor stops the zips after it; a zip held by another program still does.
internal static class DamagedCacheZip1855Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Folder()=>Path.Combine(Path.GetTempPath(),"tensho-1855-zip-"+Guid.NewGuid().ToString("N"));
    private static void Clean(string folder){try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}

    private const string Board="{\"Schema\":1,\"Family\":\"market-board\",\"Scope\":\"Odin:5057\",\"Version\":\"universalis-board-v1\",\"RecordedAt\":\"2026-11-01T12:00:00+00:00\",\"Value\":{\"listings\":[{\"retainerName\":\"Sellerofthings\"}]}}";

    // A zip holding one v1 board (other players' retainer names).
    private static string BoardZip(string folder,string backups,string stamp)
    {
        var cache=Path.Combine(folder,"cache-"+stamp);
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache,new string('a',64)+".json"),Board);
        Directory.CreateDirectory(backups);
        var zip=Path.Combine(backups,"backup-"+stamp+"-cache.zip");
        ZipFile.CreateFromDirectory(cache,zip);
        return zip;
    }

    // The compression method of every entry (local and central headers) set to 14 (LZMA): a zip no reader of .NET can open.
    private static void UnknownMethod(string zip)
    {
        var bytes=File.ReadAllBytes(zip);
        for(var at=0;at+12<bytes.Length;at++)
        {
            if(bytes[at]!=0x50 || bytes[at+1]!=0x4b)continue;
            if(bytes[at+2]==0x03 && bytes[at+3]==0x04){bytes[at+8]=14;bytes[at+9]=0;}
            else if(bytes[at+2]==0x01 && bytes[at+3]==0x02){bytes[at+10]=14;bytes[at+11]=0;}
        }
        File.WriteAllBytes(zip,bytes);
    }

    private static bool HoldsNames(string zip)
    {
        using var archive=ZipFile.OpenRead(zip);
        return archive.Entries.Any(entry=>{using var reader=new StreamReader(entry.Open());return reader.ReadToEnd().Contains("Sellerofthings",StringComparison.Ordinal);});
    }

    public static readonly (string Name,Action Run)[] Cases=
    [
        ("X-851: a damaged cache zip (not a zip, unknown compression method) is left as it is; the zips after it are cleaned and the pass ends with its mark",()=>
        {
            var folder=Folder();
            try
            {
                var backups=Path.Combine(folder,"Backups");
                Directory.CreateDirectory(backups);
                var garbage=Path.Combine(backups,"backup-20261030-120000-cache.zip");
                File.WriteAllBytes(garbage,[1,2,3,4,5,6,7,8,9]);
                var unknown=BoardZip(folder,backups,"20261031-120000");
                UnknownMethod(unknown);
                var unknownBytes=File.ReadAllBytes(unknown);
                var good=BoardZip(folder,backups,"20261101-120000");
                Check(RetiredCopies.Clean(backups,out var damaged)==1,"the readable zip after the damaged ones is written again");
                Check(damaged==2,"both damaged zips counted: "+damaged);
                Check(!HoldsNames(good),"the readable zip lost its v1 board");
                Check(File.ReadAllBytes(garbage).SequenceEqual(new byte[]{1,2,3,4,5,6,7,8,9}) && File.ReadAllBytes(unknown).SequenceEqual(unknownBytes),"the damaged zips are left byte for byte");
                Check(File.Exists(Path.Combine(backups,RetiredCopies.MarkName)),"the pass ends: the mark is written");
                Check(RetiredCopies.Clean(backups,out var again)==0 && again==0,"nothing read again at the next load");
            }
            finally{Clean(folder);}
        }),
        ("X-851: a zip held by another program still leaves the pass to the next load; a damaged one beside it does not change that",()=>
        {
            var folder=Folder();
            try
            {
                var backups=Path.Combine(folder,"Backups");
                Directory.CreateDirectory(backups);
                File.WriteAllBytes(Path.Combine(backups,"backup-20261030-120000-cache.zip"),[0,0,0]);
                var held=BoardZip(folder,backups,"20261101-120000");
                using(new FileStream(held,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                {
                    RetiredCopies.Clean(backups,out var damaged);
                    Check(damaged==1,"the damaged zip counted");
                    Check(!File.Exists(Path.Combine(backups,RetiredCopies.MarkName)),"held: no mark");
                }
                Check(RetiredCopies.Clean(backups)==1 && !HoldsNames(held),"cleaned at the next load");
                Check(File.Exists(Path.Combine(backups,RetiredCopies.MarkName)),"then the mark");
            }
            finally{Clean(folder);}
        }),
        ("X-851: wiring: the load says the damaged zips once",()=>
        {
            var load=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs"));
            Check(load.Contains("Caching.RetiredCopies.Clean(Discoveries.DiscoveryBackup.Folder(discoveryFolder),out var damaged,gameDataCancel.Token)",StringComparison.Ordinal),"the pass at load");
            Check(load.Contains("if(damaged>0)log.Information(",StringComparison.Ordinal),"said in the log, not a warning at each load");
        }),
    ];
}
