using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.Game;
using TenshoAssistant.Items;

namespace TenshoAssistant.Craft;

internal sealed class CraftLiveReader : IDisposable
{
    private readonly object gate=new();
    private readonly CraftLiveService service; private readonly RecipeCatalogService recipes;private readonly CraftActionCatalogService actions;private readonly ItemCatalogService items;
    private readonly IFramework framework; private readonly IPlayerState player; private readonly IClientState client;
    private readonly IGameGui gui; private readonly IObjectTable objects; private readonly IPluginLog log;
    private long nextRead; private ulong owner; private bool disposed;
    public CraftLiveReader(CraftLiveService service,RecipeCatalogService recipes,CraftActionCatalogService actions,ItemCatalogService items,IFramework framework,IPlayerState player,IClientState client,IGameGui gui,IObjectTable objects,IPluginLog log)
    {this.service=service;this.recipes=recipes;this.actions=actions;this.items=items;this.framework=framework;this.player=player;this.client=client;this.gui=gui;this.objects=objects;this.log=log;framework.Update+=Update;}
    private static unsafe uint? MainHand()
    {
        var manager=InventoryManager.Instance();var container=manager==null ? null:manager->GetInventoryContainer(InventoryType.EquippedItems);
        if(container==null || !container->IsLoaded || container->Size is <1 or >32)return null;
        var slot=container->GetInventorySlot(0);if(slot==null || slot->Slot!=0)return null;
        var id=slot->GetBaseItemId();return id==0 ? null:id;
    }
    private static unsafe CraftNativeNumbers? Numbers(AddonSynthesis* addon)
    {
        if(addon->AtkValuesCount<26 || addon->AtkValues==null)return null;
        foreach(var index in new[]{5,6,7,8,9,12,15,16,17})if(addon->AtkValues[index].Type!=AtkValueType.UInt)return null;
        if(addon->AtkValues[25].Type!=AtkValueType.Bool)return null;
        var values=addon->AtkValues;
        return new(values[5].UInt,values[6].UInt,values[9].UInt,values[17].UInt,values[7].UInt,values[8].UInt,values[15].UInt,values[16].UInt,values[12].UInt,values[25].Bool);
    }
    private ulong CurrentOwner=>client.IsLoggedIn && player.IsLoaded ? player.ContentId:0;
    private static unsafe string Text(AtkTextNode* node)
    {
        if(node==null)return "";
        var parent=&node->AtkResNode;var parents=0;
        while(parent!=null)
        {
            if(++parents>32)throw new InvalidOperationException("Hiérarchie du texte de synthèse invalide.");
            if(!parent->IsVisible())return "";
            parent=parent->ParentNode;
        }
        if(node->NodeText.Length>2048)throw new InvalidOperationException("Texte de synthèse trop long.");
        return Dalamud.Game.Text.SeStringHandling.SeString.Parse(node->NodeText.AsSpan()).TextValue;
    }
    private unsafe void Update(IFramework unused)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var current=CurrentOwner;if(current!=owner){owner=current;nextRead=0;}
            if(service.BeginRead(current) is not { } ticket)return;
            var now=Stopwatch.GetTimestamp();if(now<nextRead)return;nextRead=now+Stopwatch.Frequency/5;
            try
            {
                var handle=gui.GetAddonByName("Synthesis");
                if(handle.IsNull){service.Missing(ticket,current,CraftLiveStatusKind.AddonAbsent);return;}
                if(gui.GameUiHidden || !handle.IsReady || !handle.IsVisible){service.Missing(ticket,current,CraftLiveStatusKind.NotReady);return;}
                var eventFramework=EventFramework.Instance();var handler=eventFramework==null ? null:eventFramework->GetCraftEventHandler();
                var local=objects.LocalPlayer;
                if(handler==null || local is null || local.ClassJob.RowId is <8 or >15 || local.Level==0 || local.MaxCp==0)
                {service.Missing(ticket,current,CraftLiveStatusKind.NotReady);return;}
                var addon=(AddonSynthesis*)handle.Address;
                var cpBefore=local.CurrentCp;var maxCpBefore=local.MaxCp;
                var toolBefore=MainHand();
                CraftLiveTool? tool=null;
                if(toolBefore is { } toolId)
                {
                    var metadataTool=items.State.Catalog?.Find(toolId)?.CraftTool;
                    tool=new(toolId,metadataTool?.JobId ?? 0,metadataTool?.GoodQualityPercent);
                }
                var numbers=Numbers(addon);var nativeActions=new List<CraftNativeAction>();
                var recipe=handler->RecipeId;var step=handler->StepNumber;var condition=handler->Condition;
                var nativeFlags=(byte)handler->CraftFlags;
                if(numbers is {Condition:<=3,Expert:false} && (uint)condition==numbers.Condition+1 && (nativeFlags&9)==0 && actions.State.Catalog is { } actionCatalog)
                {
                    var manager=ActionManager.Instance();
                    if(manager!=null)
                    foreach(var spec in CraftActionCatalog.Specs)
                    {
                        if(actionCatalog.Find(local.ClassJob.RowId,spec.Kind) is not { } metadata)continue;
                        var type=metadata.NativeId>=100000 ? ActionType.CraftAction:ActionType.Action;
                        nativeActions.Add(new(spec.Kind,metadata.NativeId,manager->GetActionStatus(type,metadata.NativeId,0xE0000000,false,true)));
                    }
                }
                var effects=new[]{addon->CraftEffect1,addon->CraftEffect2,addon->CraftEffect3,addon->CraftEffect4,addon->CraftEffect5,addon->CraftEffect6,addon->CraftEffect7,addon->CraftEffect8,addon->CraftEffect9};
                var copiedEffects=effects.Select(effect=>new CraftLiveEffect(Text(effect.Name),Text(effect.StepsRemaining))).Where(effect=>effect.Name.Length>0).ToArray();
                var statuses=new List<CraftLiveStatus>();
                var statusCount=0;
                var statusLength=local.StatusList.Length;
                if(statusLength is <1 or >60)throw new InvalidOperationException("Longueur de liste des statuts incompatible.");
                foreach(var status in local.StatusList)
                {
                    if(statusCount++==60)throw new InvalidOperationException("Trop de statuts observés.");
                    if(status.StatusId!=0)statuses.Add(new(status.StatusId,status.Param,status.RemainingTime));
                }
                var raw=new CraftLiveRaw(recipe,handler->ItemResult,handler->CraftType,step,(byte)condition,(byte)handler->CraftFlags,(uint)handler->StateFlags,
                    local.ClassJob.RowId,new(local.Level,player.GetAttribute(Dalamud.Game.Player.PlayerAttribute.Craftsmanship),player.GetAttribute(Dalamud.Game.Player.PlayerAttribute.Control),(int)local.MaxCp),
                    cpBefore,maxCpBefore,Text(addon->ItemName),Text(addon->Condition),Text(addon->CraftEffectOverflow),
                    new(Text(addon->CurrentProgress),Text(addon->MaxProgress),Text(addon->CurrentQuality),Text(addon->MaxQuality),Text(addon->CurrentDurability),Text(addon->StartingDurability),Text(addon->StepNumber)),copiedEffects,statuses,statusCount==statusLength,numbers,nativeActions,nativeActions.Count==CraftActionCatalog.Specs.Count,tool);
                if(CurrentOwner!=current){service.BeginRead(CurrentOwner);return;}
                if(objects.LocalPlayer?.Address!=local.Address || player.ClassJob.RowId!=raw.JobId || objects.LocalPlayer?.ClassJob.RowId!=raw.JobId)
                {service.Missing(ticket,current,CraftLiveStatusKind.NotReady);return;}
                var after=gui.GetAddonByName("Synthesis");
                if(after.IsNull || after.Address!=handle.Address || !after.IsReady || !after.IsVisible || gui.GameUiHidden
                    || eventFramework->GetCraftEventHandler()!=handler || handler->RecipeId!=recipe || handler->StepNumber!=step || handler->Condition!=condition
                    || (byte)handler->CraftFlags!=nativeFlags || Numbers(addon)!=numbers || local.CurrentCp!=raw.Cp || local.MaxCp!=raw.MaxCp || MainHand()!=toolBefore)
                {service.Missing(ticket,current,CraftLiveStatusKind.NotReady);return;}
                service.Publish(ticket,current,raw,recipes.State.Catalog);
            }
            catch(Exception error){service.Fail(ticket,current,error);nextRead=now+Stopwatch.Frequency*5;log.Warning(error,"Craft observation unavailable; retry in five seconds.");}
        }
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;framework.Update-=Update;service.Dispose();}}
}
