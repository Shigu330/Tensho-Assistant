using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Craft;

/// <summary>A-11 (0.421): the recipes the character has crafted at least once (Recipe row ids), as read from the game.</summary>
public sealed record RecipeLogState(IReadOnlySet<uint> Crafted,int Total,DateTimeOffset ReadAt)
{
    public bool? WasCrafted(uint recipeId)=>Total==0 ? null:Crafted.Contains(recipeId);
}

/// <summary>
/// A-11 (0.421): while a crafting view asks (<see cref="Want"/>, 5 s window), asks the game every 10 seconds on the framework thread
/// whether each recipe of the catalogue has been crafted (QuestManager.IsRecipeComplete, by Recipe row; a game function called only once
/// resolved; to confirm in game). Cleared at logout; a failure is logged once and publishes nothing.
/// </summary>
public sealed unsafe class RecipeLogReader : IDisposable
{
    private readonly IFramework framework;private readonly IClientState client;private readonly IPluginLog log;
    private readonly Func<RecipeCatalog?> catalog;
    private RecipeLogState? state;
    private long wantedUntil;private DateTimeOffset nextRead;private bool disposed,warned;
    public RecipeLogState? State=>Volatile.Read(ref state);

    public RecipeLogReader(IFramework framework,IClientState client,IPluginLog log,Func<RecipeCatalog?> catalog)
    {
        this.framework=framework;this.client=client;this.log=log;this.catalog=catalog;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
        try{client.Logout+=OnLogout;}catch{TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);throw;}
    }

    public void Want()=>Volatile.Write(ref wantedUntil,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead || now.UtcTicks>Volatile.Read(ref wantedUntil) || !client.IsLoggedIn)return;
        nextRead=now.AddSeconds(10);
        try
        {
            // R-14 (0.507): a lost function is logged once, and an old reading is not kept as current.
            if((nint)QuestManager.MemberFunctionPointers.IsRecipeComplete==0)
            {
                Volatile.Write(ref state,null);
                if(!warned){warned=true;log.Warning("The crafting log cannot be read: function not resolved.");}
                return;
            }
            if(catalog() is not { } recipes)return;
            var crafted=new HashSet<uint>();var total=0;
            foreach(var recipe in recipes.All)
            {
                total++;
                if(QuestManager.IsRecipeComplete(recipe.RecipeId))crafted.Add(recipe.RecipeId);
            }
            Volatile.Write(ref state,new RecipeLogState(crafted,total,now));
            warned=false;
        }
        catch(Exception error)
        {
            nextRead=now.AddSeconds(30);
            Volatile.Write(ref state,null);
            if(!warned){warned=true;log.Warning(error,"The crafting log could not be read.");}
        }
    }

    private void OnLogout(int type,int code)=>Volatile.Write(ref state,null);

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        client.Logout-=OnLogout;
    }
}
