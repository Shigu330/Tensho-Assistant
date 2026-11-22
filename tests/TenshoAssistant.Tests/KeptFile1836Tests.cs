using System.Text;
using TenshoAssistant;
using TenshoAssistant.Inventory;
using TenshoAssistant.FreeCompany;

// 0.1836 (X-833, security review I5): AtomicFile wrote the temporary file without flushing it to the disk, then moved it over the old one, with
// no copy: after a power cut or a blue screen just after a save, NTFS could keep the rename without the data (a file of zeros), and the player's
// crafting lists, my tools, beast teams, duty progress, gil and voyage history went « unavailable, never written over », lost without a copy.
// Now: Flush(true) before the move; the player's data kept (« .bak », File.Replace) and read back by AtomicFile.ReadKept when the file does not
// read, the damaged file moved aside under a dated name.
internal static class KeptFile1836Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static void InFolder(Action<string> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-tests","kept-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{run(folder);}
        finally{try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    // A reader that accepts « v<number> » only, as a JSON reader refuses a file of zeros.
    private static int Version(string path)
    {
        var text=File.ReadAllText(path);
        return text.StartsWith('v') && int.TryParse(text.AsSpan(1),out var number) ? number : throw new InvalidDataException("not a version: "+text.Length+" chars");
    }

    public static IEnumerable<(string Name,Action Test)> Cases =>
    [
        ("Kept file 1836: a kept write keeps the version it replaces; a plain write keeps none (X-833)",() => InFolder(folder =>
        {
            var path=Path.Combine(folder,"Lists.json");
            AtomicFile.Write(path,"v1",keep:true);
            Check(Version(path)==1 && !File.Exists(AtomicFile.BackupOf(path)),"the first write: nothing to keep");
            AtomicFile.Write(path,"v2",keep:true);AtomicFile.Write(path,"v3",keep:true);
            Check(Version(path)==3 && Version(AtomicFile.BackupOf(path))==2,"the file and the version before the last write");
            var plain=Path.Combine(folder,"Cache.json");
            AtomicFile.Write(plain,"v1");AtomicFile.Write(plain,"v2");
            Check(Version(plain)==2 && !File.Exists(AtomicFile.BackupOf(plain)),"a cache keeps no copy (a token or a cache never lingers in a .bak)");
            Check(Directory.GetFiles(folder,"*.tmp").Length==0,"no temporary file left");
        })),
        ("Kept file 1836: a file of zeros after a power cut: its previous version read, the damaged file kept aside (X-833)",() => InFolder(folder =>
        {
            var path=Path.Combine(folder,"Lists.json");
            AtomicFile.Write(path,"v1",keep:true);AtomicFile.Write(path,"v2",keep:true);
            File.WriteAllBytes(path,new byte[2]);   // the right size, zeros: the rename kept, the data not
            string? told=null;Exception? why=null;
            var now=new DateTimeOffset(2026,11,22,23,0,0,TimeSpan.Zero);
            Check(AtomicFile.ReadKept(path,Version,(aside,error)=>{told=aside;why=error;},now)==1,"the previous version is read");
            Check(told==path+".broken-20261122-230000" && File.ReadAllBytes(told).SequenceEqual(new byte[2]) && !File.Exists(path) && why is InvalidDataException,
                $"the damaged file moved aside, its bytes kept, the reason told ({told})");
            Check(Version(AtomicFile.BackupOf(path))==1,"the .bak left as it is");
            AtomicFile.Write(path,"v3",keep:true);
            Check(Version(path)==3 && Version(AtomicFile.BackupOf(path))==1 && AtomicFile.ReadKept(path,Version)==3,"the next save writes the file; the good copy is still the .bak");
        })),
        ("Kept file 1836: missing with its .bak, both damaged, or held by another program (X-833)",() => InFolder(folder =>
        {
            var path=Path.Combine(folder,"Lists.json");
            AtomicFile.Write(path,"v1",keep:true);AtomicFile.Write(path,"v2",keep:true);
            File.Delete(path);
            string? told=null;
            Check(AtomicFile.ReadKept(path,Version,(aside,_)=>told=aside)==1 && told==path,"a replace cut short (no file, a .bak): the .bak read");
            File.WriteAllText(path,"broken");File.WriteAllText(AtomicFile.BackupOf(path),"broken too");
            try{AtomicFile.ReadKept(path,Version);Check(false,"both damaged: an error");}
            catch(InvalidDataException error){Check(error.Message=="not a version: 6 chars","the file's own error, not the .bak's: "+error.Message);}
            Check(File.ReadAllText(path)=="broken" && Directory.GetFiles(folder,"*.broken-*").Length==0,"both damaged: nothing moved, nothing written (the caller keeps it as it is)");
            AtomicFile.Write(AtomicFile.BackupOf(path),"v1");File.WriteAllText(path,"v5");
            using(new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {
                try{AtomicFile.ReadKept(path,Version);Check(false,"held: an error");}
                catch(IOException){}
            }
            Check(File.ReadAllText(path)=="v5" && Directory.GetFiles(folder,"*.broken-*").Length==0,"a file held by another program is not damage: never set aside nor replaced by its .bak");
            Check(AtomicFile.ReadKept(Path.Combine(folder,"none.json"),file=>File.Exists(file) ? 1 : 0)==0,"neither the file nor a .bak: the reader's own « missing »");
        })),
        ("Kept file 1836: the gil and voyage files read their previous version; every kept store goes through it (X-833)",() => InFolder(folder =>
        {
            var gil=Path.Combine(folder,"gil.json");
            var day=new DateTimeOffset(2026,11,20,0,0,0,TimeSpan.Zero);
            GilJournal.Write(gil,[new GilEntry(day,100,100,GilSource.Character,GilCategory.Other)]);GilJournal.Write(gil,[new GilEntry(day,100,100,GilSource.Character,GilCategory.Other),new GilEntry(day.AddDays(1),200,300,GilSource.Character,GilCategory.Other)]);
            File.WriteAllBytes(gil,new byte[40]);
            Check(GilJournal.Read(gil).Count==1,"gil journal: the version before the last save");
            var voyages=Path.Combine(folder,"voyages.json");
            VoyageHistory.Write(voyages,[]);VoyageHistory.Write(voyages,[]);File.WriteAllBytes(voyages,new byte[40]);
            Check(VoyageHistory.Read(voyages).Count==0 && Directory.GetFiles(folder,"voyages.json.broken-*").Length==1,"voyage history: read again from its .bak");
            var source=Path.GetFullPath("src/TenshoAssistant");
            string Text(string file)=>File.ReadAllText(Path.Combine(source,file));
            Check(Text("AtomicFile.cs").Contains("file.Flush(flushToDisk:true);",StringComparison.Ordinal),"flushed to the disk before the move");
            foreach(var (file,write,read) in new[]{
                ("Craft/CraftLists.cs","Json),keep:true);","AtomicFile.ReadKept(path,ReadFile,recovered)"),
                ("MyTools/ToolTemplateStore.cs","Options),keep:true);","AtomicFile.ReadKept(path,ReadFile,"),
                ("Beastmaster/BeastTeams.cs","AtomicFile.Write(file, text, keep: true)",null),
                ("Beastmaster/BeastPlansFile.cs",null,"AtomicFile.ReadKept(file,"),
                ("Duties/DutyProgressReader.Dalamud.cs","AtomicFile.Write(path,text,keep:true)","AtomicFile.ReadKept(file,"),
                ("Inventory/GilJournal.cs","Json),keep:true);","AtomicFile.ReadKept(path,ReadFile)"),
                ("FreeCompany/VoyageHistory.cs","Json),keep:true);","AtomicFile.ReadKept(path,ReadFile)")})
            {
                if(write is not null)Check(Text(file).Contains(write,StringComparison.Ordinal),file+": kept write");
                if(read is not null)Check(Text(file).Contains(read,StringComparison.Ordinal),file+": kept read");
            }
        })),
    ];
}
