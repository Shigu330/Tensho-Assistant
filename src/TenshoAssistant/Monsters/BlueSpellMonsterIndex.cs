using TenshoAssistant.Community;

namespace TenshoAssistant.Monsters;

/// <summary>
/// 0.798 (X-38, user request): which monsters teach which blue magic spell, from the wiki's spell pages (the game links no spell to a monster). Copies on disk are
/// read without network; the missing pages are asked only on a click, one every three seconds (the wiki's shared cadence). The wiki's English monster names are
/// matched to the game's BNpcName rows by their English name, case ignored: information only, « according to the wiki ».
/// </summary>
public sealed class BlueSpellMonsterIndex : IDisposable
{
    private static readonly IReadOnlyDictionary<uint,IReadOnlyList<uint>> None=new Dictionary<uint,IReadOnlyList<uint>>();
    private readonly WikiMonsterSources wiki;private readonly Func<string,IReadOnlyList<uint>> nameIds;private readonly Func<bool> enabled;
    private readonly object gate=new();private readonly Dictionary<uint,IReadOnlyList<WikiMonsterRow>> bySpell=[];
    private readonly CancellationTokenSource disposal=new();
    private IReadOnlyDictionary<uint,IReadOnlyList<uint>> spellsByMonster=None;
    private int running,loaded,asked,done,failed;private string? error;

    public BlueSpellMonsterIndex(WikiMonsterSources wiki,Func<string,IReadOnlyList<uint>> nameIds,Func<bool> enabled){this.wiki=wiki;this.nameIds=nameIds;this.enabled=enabled;}

    public int Known{get{lock(gate)return bySpell.Count;}}
    public bool Running=>Volatile.Read(ref running)==1;
    public (int Done,int Asked,int Failed) Progress=>(Volatile.Read(ref done),Volatile.Read(ref asked),Volatile.Read(ref failed));
    public string? Error=>Volatile.Read(ref error);
    /// <summary>The spells (AozAction rows) a monster (BNpcName row) teaches, according to the wiki pages read.</summary>
    public IReadOnlyList<uint> SpellsOf(uint nameId)=>Volatile.Read(ref spellsByMonster).TryGetValue(nameId,out var spells) ? spells:[];

    /// <summary>The copies already on disk, once; never the network.</summary>
    public void LoadCached(IReadOnlyList<uint> spells)
    {
        if(spells.Count==0 || Interlocked.Exchange(ref loaded,1)==1)return;
        var token=disposal.Token;
        _=Task.Run(async()=>
        {
            foreach(var spell in spells)
            {
                try{if(await wiki.ReadCachedAsync(spell,token).ConfigureAwait(false) is { } document)Add(spell,document.Monsters);}
                catch(OperationCanceledException){return;}
                catch(Exception){}
            }
            Rebuild();
        },token);
    }

    /// <summary>On a click: the pages not read yet, one every three seconds; a page that fails is counted and skipped.</summary>
    public void FetchMissing(IReadOnlyList<uint> spells)
    {
        if(Interlocked.Exchange(ref running,1)==1)return;
        var token=disposal.Token;
        _=Task.Run(async()=>
        {
            try
            {
                uint[] missing;
                lock(gate)missing=spells.Where(spell=>!bySpell.ContainsKey(spell)).Distinct().ToArray();
                Volatile.Write(ref asked,missing.Length);Volatile.Write(ref done,0);Volatile.Write(ref failed,0);Volatile.Write(ref error,null);
                foreach(var spell in missing)
                {
                    if(!enabled()){Volatile.Write(ref error,"disabled");return;}
                    for(var attempt=0;;attempt++)
                    {
                        try{var document=await wiki.FetchAsync(spell,token).ConfigureAwait(false);Add(spell,document.Monsters);Interlocked.Increment(ref done);break;}
                        catch(ObjectDisposedException){return;}
                        catch(SourceDisabledException){Volatile.Write(ref error,"disabled");return;}
                        catch(InvalidOperationException) when(attempt<5){await Task.Delay(1000,token).ConfigureAwait(false);}
                        // X-170 (0.1144, GPT audit): a page that runs out of time waits the same retry delay (before, the whole reading stopped
                        // silently); only the plugin's own stop ends it.
                        catch(OperationCanceledException) when(!token.IsCancellationRequested && attempt<5){await Task.Delay(1000,token).ConfigureAwait(false);}
                        catch(OperationCanceledException) when(token.IsCancellationRequested){throw;}
                        catch(Exception){Interlocked.Increment(ref failed);break;}
                    }
                    Rebuild();
                    await Task.Delay(WikiMonsterSources.RequestInterval+TimeSpan.FromMilliseconds(100),token).ConfigureAwait(false);
                }
            }
            catch(OperationCanceledException){}
            finally{Rebuild();Volatile.Write(ref running,0);}
        },token);
    }

    private void Add(uint spell,IReadOnlyList<WikiMonsterRow> rows){lock(gate)bySpell[spell]=rows;}
    private void Rebuild()
    {
        Dictionary<uint,IReadOnlyList<WikiMonsterRow>> copy;
        lock(gate)copy=new(bySpell);
        Volatile.Write(ref spellsByMonster,Map(copy,nameIds));
    }

    /// <summary>Spells per monster row: every row with the wiki's English name teaches the spell.</summary>
    public static IReadOnlyDictionary<uint,IReadOnlyList<uint>> Map(IReadOnlyDictionary<uint,IReadOnlyList<WikiMonsterRow>> bySpell,Func<string,IReadOnlyList<uint>> nameIds)
    {
        var result=new Dictionary<uint,List<uint>>();
        foreach(var (spell,rows) in bySpell)
            foreach(var row in rows)
                foreach(var id in nameIds(row.Name.Trim()))
                {
                    if(!result.TryGetValue(id,out var spells))result[id]=spells=[];
                    if(!spells.Contains(spell))spells.Add(spell);
                }
        return result.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<uint>)pair.Value);
    }

    public void Dispose(){disposal.Cancel();}
}
