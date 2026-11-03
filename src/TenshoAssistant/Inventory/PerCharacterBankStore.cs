namespace TenshoAssistant.Inventory;

/// <summary>
/// g, I-03 (0.345, user's choice « un fichier par personnage »): the bank kept as one file per character, InventoryBank/&lt;id&gt;.json,
/// each read and written by its own <see cref="FileInventoryBankStore"/> (same bounds and checks, .bak copy, a corrupt file fails
/// closed). The id is the local character id the game gives the logged-in character (ContentId), never an Account ID.
/// Only the characters whose entries changed are written; a character no longer in the list (erased) has its files deleted.
/// The former shared file (InventoryBank.json) is split once: written into a temporary folder, moved into place only when complete,
/// then kept beside as « .migrated »; a failed or interrupted split leaves the former file untouched and is redone at the next load.
/// </summary>
public sealed class PerCharacterBankStore : IInventoryBankStore
{
    private readonly string folder;
    private readonly string legacy;
    private readonly object gate=new();
    private readonly Dictionary<ulong,FileInventoryBankStore> stores=[];
    private readonly Dictionary<ulong,BankEntry[]> saved=[];
    private bool loadFailed;

    public PerCharacterBankStore(string folder,string legacyFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);ArgumentException.ThrowIfNullOrWhiteSpace(legacyFile);
        this.folder=Path.GetFullPath(folder);legacy=Path.GetFullPath(legacyFile);
    }

    public static string FileName(ulong character)=>character.ToString(System.Globalization.CultureInfo.InvariantCulture)+".json";

    public BankEntry[] Load()
    {
        lock(gate)
        {
            try
            {
                Migrate();
                var all=new List<BankEntry>();
                saved.Clear();
                foreach(var id in Characters())
                {
                    var entries=Store(id).Load();
                    if(entries.Any(entry=>entry.Key.CharacterId!=id))throw new InvalidDataException("A character's bank file holds another character's entries.");
                    if(entries.Length>0)saved[id]=entries;
                    all.AddRange(entries);
                }
                var result=all.ToArray();
                InventoryBankService.ValidateEntries(result);
                return result;
            }
            catch{loadFailed=true;throw;}
        }
    }

    public void Save(IReadOnlyList<BankEntry> entries)
    {
        lock(gate)
        {
            if(loadFailed)throw new InvalidOperationException("A failed bank load prevents overwriting saved data.");
            InventoryBankService.ValidateEntries(entries);
            var groups=entries.GroupBy(entry=>entry.Key.CharacterId).ToDictionary(group=>group.Key,group=>group.ToArray());
            foreach(var (id,list) in groups)
            {
                if(saved.TryGetValue(id,out var before) && before.SequenceEqual(list))continue;
                Store(id).Save(list);
                saved[id]=list;
            }
            // Erased characters: their file and its copy go (an erased character must not come back at the next load).
            foreach(var id in Characters().Concat(saved.Keys).Distinct().Where(id=>!groups.ContainsKey(id)).ToArray())
            {
                var path=Path.Combine(folder,FileName(id));
                File.Delete(path);File.Delete(path+".bak");
                stores.Remove(id);saved.Remove(id);
            }
        }
    }

    private FileInventoryBankStore Store(ulong id)
    {
        if(!stores.TryGetValue(id,out var store))stores[id]=store=new FileInventoryBankStore(Path.Combine(folder,FileName(id)));
        return store;
    }

    // Characters with a file (or only its .bak) in the folder; other file names are ignored.
    private IEnumerable<ulong> Characters()
    {
        if(!Directory.Exists(folder))return [];
        return Directory.EnumerateFiles(folder,"*.json*")
            .Select(path=>Path.GetFileName(path))
            .Select(name=>name.EndsWith(".json.bak",StringComparison.Ordinal) ? name[..^4]:name)
            .Where(name=>name.EndsWith(".json",StringComparison.Ordinal))
            .Select(name=>ulong.TryParse(name[..^5],System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var id) ? id:0)
            .Where(id=>id!=0).Distinct().OrderBy(id=>id).ToArray();
    }

    private void Migrate()
    {
        if(Directory.Exists(folder) || !File.Exists(legacy) && !File.Exists(legacy+".bak"))return;
        var entries=new FileInventoryBankStore(legacy).Load();
        var temporary=folder+".migrating";
        if(Directory.Exists(temporary))Directory.Delete(temporary,true);
        Directory.CreateDirectory(temporary);
        foreach(var group in entries.GroupBy(entry=>entry.Key.CharacterId))
            new FileInventoryBankStore(Path.Combine(temporary,FileName(group.Key))).Save(group.ToArray());
        Directory.Move(temporary,folder);
        foreach(var path in new[]{legacy,legacy+".bak"})
            if(File.Exists(path))File.Move(path,FreeName(path+".migrated"));
    }

    private static string FreeName(string path)
    {
        var candidate=path;
        for(var index=1;File.Exists(candidate);index++)candidate=$"{path}.{index}";
        return candidate;
    }
}
