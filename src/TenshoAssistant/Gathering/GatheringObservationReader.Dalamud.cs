using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Gathering;

internal sealed class GatheringObservationReader : IDisposable
{
    private readonly object gate=new();private readonly GatheringObservationService service;
    private readonly IFramework framework;private readonly IPlayerState player;private readonly IClientState client;private readonly IGameGui gui;private readonly IObjectTable objects;private readonly IPluginLog log;
    private bool disposed;private long nextRead;private ulong owner;
    public GatheringObservationReader(GatheringObservationService service,IFramework framework,IPlayerState player,IClientState client,IGameGui gui,IObjectTable objects,IPluginLog log)
    {this.service=service;this.framework=framework;this.player=player;this.client=client;this.gui=gui;this.objects=objects;this.log=log;framework.Update+=Update;}
    private ulong CurrentOwner=>client.IsLoggedIn && player.IsLoaded ? player.ContentId:0;
    private static unsafe bool Visible(AtkResNode* node)
    {
        if(node==null)return false;var count=0;
        while(node!=null){if(++count>32 || !node->IsVisible())return false;node=node->ParentNode;}return true;
    }
    private static unsafe AtkResNode* Node(AtkUldManager* manager,uint id)
    {
        if(manager==null || manager->NodeList==null || manager->NodeListCount is <1 or >GatheringUiLayout.MaxNodes || manager->NodeListCount>manager->NodeListSize)return null;
        AtkResNode* found=null;
        for(var index=0;index<manager->NodeListCount;index++)
        {var node=manager->NodeList[index];if(node!=null && node->NodeId==id){if(found!=null)return null;found=node;}}
        return found;
    }
    private static unsafe string? Text(AtkResNode* node)
    {
        if(!Visible(node) || node->Type!=NodeType.Text)return null;
        var text=node->GetAsAtkTextNode();if(text==null || text->NodeText.Length>64)return null;
        return Dalamud.Game.Text.SeStringHandling.SeString.Parse(text->NodeText.AsSpan()).TextValue;
    }
    private static unsafe GatheringRawSlot[] Copy(AddonGathering* addon)
    {
        if(addon->ItemIds.Length!=8 || addon->ItemFlags.Length!=8 || addon->GatheredItemComponentCheckbox.Length!=8)
            throw new InvalidOperationException("Nombre de lignes de récolte incompatible.");
        var copied=new GatheringRawSlot[8];
        for(var index=0;index<8;index++)
        {
            AtkComponentCheckBox* component=addon->GatheredItemComponentCheckbox[index];
            var visible=component!=null && component->OwnerNode!=null && Visible(&component->OwnerNode->AtkResNode);
            string? chance=null;string? boon=null;string? quantity=null;
            if(visible && addon->ItemIds[index]!=0)
            {
                chance=Text(Node(&component->UldManager,GatheringUiLayout.Success));boon=Text(Node(&component->UldManager,GatheringUiLayout.Boon));
                var iconNode=Node(&component->UldManager,GatheringUiLayout.Icon);
                if(Visible(iconNode) && (int)iconNode->Type>=1000)
                {var icon=iconNode->GetAsAtkComponentIcon();if(icon!=null)quantity=Text(Node(&icon->UldManager,GatheringUiLayout.Quantity));}
            }
            var rawId=visible ? addon->ItemIds[index]:0;
            var (baseId,kind)=Dalamud.Utility.ItemUtil.GetBaseId(rawId);
            var context=baseId!=0 && kind is Dalamud.Utility.ItemKind.Normal or Dalamud.Utility.ItemKind.Hq or Dalamud.Utility.ItemKind.Collectible;
            copied[index]=new(index,rawId,addon->ItemFlags[index],visible,chance,boon,quantity,context ? baseId:0,context);
        }
        return copied;
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
                var handle=gui.GetAddonByName("Gathering");
                if(handle.IsNull){service.Missing(ticket,current,GatheringObservationStatus.AddonAbsent);return;}
                var masterpiece=gui.GetAddonByName("GatheringMasterpiece");var local=objects.LocalPlayer;
                if(gui.GameUiHidden || !handle.IsReady || !handle.IsVisible || !masterpiece.IsNull && masterpiece.IsVisible || local is null || local.ClassJob.RowId is not (16 or 17))
                {service.Missing(ticket,current,GatheringObservationStatus.NotReady);return;}
                var addon=(AddonGathering*)handle.Address;var status=addon->GatherStatus;
                if(status is not (1 or 2)){service.Missing(ticket,current,GatheringObservationStatus.NotReady);return;}
                var job=local.ClassJob.RowId;var level=local.Level;var gp=local.CurrentGp;var maxGp=local.MaxGp;
                var attempts=Text((AtkResNode*)addon->IntegrityLeftover);var total=Text((AtkResNode*)addon->IntegrityTotal);var slots=Copy(addon);
                if(CurrentOwner!=current){service.BeginRead(CurrentOwner);return;}
                var after=gui.GetAddonByName("Gathering");
                if(after.IsNull || after.Address!=handle.Address || !after.IsReady || !after.IsVisible || gui.GameUiHidden
                    || objects.LocalPlayer?.Address!=local.Address || local.ClassJob.RowId!=job || local.Level!=level || local.CurrentGp!=gp || local.MaxGp!=maxGp
                    || addon->GatherStatus!=status || Text((AtkResNode*)addon->IntegrityLeftover)!=attempts || Text((AtkResNode*)addon->IntegrityTotal)!=total || !slots.SequenceEqual(Copy(addon)))
                {service.Missing(ticket,current,GatheringObservationStatus.NotReady);return;}
                var unit=(AtkUnitBase*)handle.Address;
                service.SetWindow(unit->RootNode==null ? null:(unit->X,unit->Y,unit->RootNode->Width*unit->Scale,unit->RootNode->Height*unit->Scale));
                service.Publish(ticket,current,job,level,gp,maxGp,status,attempts,total,slots);
            }
            catch(Exception error){service.Fail(ticket,current,error);nextRead=now+Stopwatch.Frequency*5;log.Warning(error,"Gathering observation unavailable; retry in five seconds.");}
        }
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;framework.Update-=Update;service.Dispose();}}
}
