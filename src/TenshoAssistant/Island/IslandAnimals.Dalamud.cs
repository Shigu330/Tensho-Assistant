using System.Net.Http;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using TenshoAssistant.Caching;
using TenshoAssistant.Community;

namespace TenshoAssistant.Island;

/// <summary>W-04 (0.685): the player's pasture, read on the framework thread. Field reads only: collecting and feeding are never called.</summary>
internal static class PastureReader
{
    public static unsafe PastureSnapshot Read(Func<uint,string?> name)
    {
        var manager=MJIManager.Instance();
        if(manager==null || !manager->IsPlayerInSanctuary)return new(PastureReadStatus.NotOnIsland,[]);
        var handler=manager->PastureHandler;
        if(handler==null)return new(PastureReadStatus.NotLoaded,[]);
        var animals=new List<PastureAnimal>();
        foreach(var animal in handler->MJIAnimals)
            if(animal.BNPCNameId!=0)animals.Add(new(animal.SlotId,animal.NicknameString,name(animal.BNPCNameId) ?? "",animal.FoodLevel,animal.Mood,animal.ManualLeavingsAvailable,animal.UnderCare));
        return new(PastureReadStatus.Loaded,animals);
    }
}

public sealed record IslandAnimalState(bool Loading,IslandAnimalDocument? Document,string? Error);

/// <summary>W-04 (0.685): the wiki's page, read on the player's click only (bounded HTTP, 25 s), kept in DataCache « wiki-animals » for the sources lifetime.</summary>
public sealed class IslandAnimalSource:IDisposable
{
    private const string Family="wiki-animals",Version="v1";
    private readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(25)};
    private readonly Func<TimeSpan> lifetime;private readonly DataCache? disk;
    private readonly CancellationTokenSource disposal=new();
    private IslandAnimalState state=new(false,null,null);
    public IslandAnimalSource(Func<TimeSpan> lifetime,DataCache? disk){this.lifetime=lifetime;this.disk=disk;}
    public IslandAnimalState State=>Volatile.Read(ref state);
    /// <summary>The wiki switch (Settings › Advanced): off, no download; a copy on disk is still shown.</summary>
    public Func<bool>? Enabled { get; set; }
    public void Request()
    {
        lock(disposal)
        {
            if(disposal.IsCancellationRequested || State.Loading)return;
            Volatile.Write(ref state,new(true,State.Document,null));
        }
        _=Task.Run(LoadAsync);
    }
    private async Task LoadAsync()
    {
        try
        {
            var token=disposal.Token;
            var saved=disk is null ? null:await disk.ReadAsync<IslandAnimalDocument>(Family,"en",Version,token).ConfigureAwait(false);
            var usable=saved is not null && saved.Value.Rows is { Count:>0 };
            if(usable && CacheAge.Fresh(saved!.RecordedAt,DateTimeOffset.UtcNow,lifetime())){Volatile.Write(ref state,new(false,saved.Value,null));return;}
            if(Enabled?.Invoke()==false){Volatile.Write(ref state,new(false,usable ? saved!.Value:State.Document,null));return;}
            var text=await BoundedHttp.ReadAsync(http,new Uri(IslandAnimalBook.Url),IslandAnimalBook.MaximumBytes,token).ConfigureAwait(false);
            var document=IslandAnimalBook.Parse(text,DateTimeOffset.UtcNow);
            Volatile.Write(ref state,new(false,document,null));
            if(disk is not null)_=disk.WriteAsync(Family,"en",Version,document,document.FetchedAt);
        }
        catch(OperationCanceledException) when(disposal.IsCancellationRequested){}
        catch(Exception error){Volatile.Write(ref state,new(false,State.Document,error.Message));}
    }
    public void Dispose(){lock(disposal){if(disposal.IsCancellationRequested)return;disposal.Cancel();}http.Dispose();}
}
