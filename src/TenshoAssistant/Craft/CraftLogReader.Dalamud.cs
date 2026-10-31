using System.Numerics;
using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Craft;

internal sealed class CraftLogReader : IDisposable
{
    private readonly CraftLogObservationService service;
    private readonly RecipeCatalogService recipes;
    private readonly Configuration configuration;
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly object gate=new();
    private long nextRead,nextWarning;
    private bool disposed;
    public CraftLogReader(CraftLogObservationService service,RecipeCatalogService recipes,Configuration configuration,
        IFramework framework,IPlayerState player,IClientState client,IGameGui gui,IPluginLog log)
    {
        this.service=service;this.recipes=recipes;this.configuration=configuration;this.framework=framework;
        this.player=player;this.client=client;this.gui=gui;this.log=log;framework.Update+=Update;
        try{client.Logout+=Logout;}catch{framework.Update-=Update;throw;}
    }
    private void Logout(int type,int code){lock(gate){service.Reset();nextRead=0;}}
    private ulong Owner=>client.IsLoggedIn && player.IsLoaded ? player.ContentId:0;
    private static unsafe int? ReadQuality(AtkTextNode* node)
    {
        if(node==null || node->NodeText.Length>128)return null;
        var parent=&node->AtkResNode;var depth=0;
        while(parent!=null)
        {
            if(++depth>32 || !parent->IsVisible())return null;
            parent=parent->ParentNode;
        }
        return CraftLiveFrame.ParseNumber(Dalamud.Game.Text.SeStringHandling.SeString.Parse(node->NodeText.AsSpan()).TextValue);
    }
    private unsafe void Update(IFramework _)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var owner=Owner;
            if(owner==0 || !configuration.CraftLogHelperEnabled){service.Reset();nextRead=0;return;}
            var now=Stopwatch.GetTimestamp();
            try
            {
                if(service.TakeOpen() is { } request && request.Owner==owner)
                {
                    var recipe=recipes.State.Catalog?.ForResult(request.ItemId).FirstOrDefault(row=>row.RecipeId==request.RecipeId);
                    var agent=AgentRecipeNote.Instance();
                    if(recipe is null || agent==null){service.Clear("Carnet indisponible ou catalogue de recettes encore en lecture.");return;}
                    agent->OpenRecipeByRecipeId(request.RecipeId);nextRead=0;
                }
                var handle=gui.GetAddonByName("RecipeNote");
                if(gui.GameUiHidden || handle.IsNull || !handle.IsReady || !handle.IsVisible){service.Clear();nextRead=0;return;}
                if(now<nextRead)return;
                nextRead=now+Stopwatch.Frequency/5;
                var note=RecipeNote.Instance();
                var list=note==null ? null:note->RecipeList;
                if(note==null || !note->IsRecipeListReady || list==null || list->Recipes==null
                    || list->RecipeCount is <1 or >20000 || list->SelectedIndex>=list->RecipeCount){service.Clear();return;}
                var index=list->SelectedIndex;var entry=list->Recipes+index;
                var id=entry->RecipeId;var item=entry->ItemId;var craft=entry->CraftType;var job=player.ClassJob.RowId;
                var addon=(AtkUnitBase*)handle.Address;
                var recipeAddon=(AddonRecipeNote*)handle.Address;
                var starting=ReadQuality(recipeAddon->SelectedRecipeStartingQuality);
                var maximum=ReadQuality(recipeAddon->SelectedRecipeMaximumQuality);
                var position=new Vector2(addon->X,addon->Y);var size=new Vector2(addon->GetScaledWidth(true),addon->GetScaledHeight(true));
                if(Owner!=owner || player.ClassJob.RowId!=job || note->RecipeList!=list || !note->IsRecipeListReady || list->SelectedIndex!=index
                    || entry->RecipeId!=id || entry->ItemId!=item || entry->CraftType!=craft){service.Clear();return;}
                if(id==0 || item==0 || craft>7){service.Clear();return;}
                service.Publish(new(owner,id,item,craft,position,size,DateTimeOffset.UtcNow,job,StartingQuality:starting,MaximumQuality:maximum));
            }
            catch(Exception error)
            {
                service.Clear("Lecture du carnet de synthèse indisponible.");
                nextRead=now+Stopwatch.Frequency;
                if(now>=nextWarning){nextWarning=now+5*Stopwatch.Frequency;log.Warning(error,"Tensho crafting log unavailable.");}
            }
        }
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;framework.Update-=Update;client.Logout-=Logout;service.Reset();}}
}
