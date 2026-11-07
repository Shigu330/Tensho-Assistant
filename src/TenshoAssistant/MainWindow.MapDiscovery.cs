using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// P11 (0.803, user request): the undiscovered parts of the map as blank parchment, like the game's map; one image per map and set of regions, built off the frame.
internal sealed partial class MainWindow
{
    private const int DiscoveryImageSize=1024;
    private string discoveryKey="";private IDalamudTextureWrap? discoveryImage;private (string Key,IDalamudTextureWrap? Image)? discoveryBuilt;private bool discoveryBuilding;
    private readonly object discoveryGate=new();
    private IDalamudTextureWrap? MapDiscoveryImage(MapDefinition map,MapLiveState? live)
    {
        mapLive?.WantDiscovery(configuration.MapView.HideUndiscovered ? map.MapId:0);
        if(!configuration.MapView.HideUndiscovered || live is not { Undiscovered:{ Count:>0 } regions } || live.DiscoveryMapId!=map.MapId)return null;
        var key=map.MapId+":"+string.Join(',',regions);
        lock(discoveryGate)
        {
            // The image built last time is swapped in at the start of a frame; the old one was drawn in a finished frame.
            if(discoveryBuilt is { } built){discoveryBuilt=null;if(built.Key==key){discoveryImage?.Dispose();discoveryImage=built.Image;discoveryKey=key;}else built.Image?.Dispose();}
            if(discoveryKey==key)return discoveryImage;
            if(discoveryBuilding || mapLive is not { } reader)return discoveryImage is not null && discoveryKey.StartsWith(map.MapId+":",StringComparison.Ordinal) ? discoveryImage:null;
            discoveryBuilding=true;
            var (parchmentPath,discoveryPath)=MapDiscovery.Files(map.TexturePath);
            _=Task.Run(()=>
            {
                IDalamudTextureWrap? image=null;
                try
                {
                    if(reader.ReadTexture(discoveryPath) is { } cells && reader.ReadTexture(parchmentPath) is { } parchment && parchment.Width==parchment.Height)
                    {
                        var rgba=MapDiscovery.Overlay(parchment.Bgra,parchment.Width,MapDiscovery.Coverage(cells.Bgra,cells.Width,cells.Height,regions),DiscoveryImageSize);
                        image=textures.CreateFromRaw(RawImageSpecification.Rgba32(DiscoveryImageSize,DiscoveryImageSize),rgba,"Tensho map discovery");
                    }
                }
                catch(Exception error){log.Warning(error,"The undiscovered parts of map {0} could not be drawn.",map.MapId);}
                lock(discoveryGate){discoveryBuilding=false;if(windowDisposed)image?.Dispose();else{discoveryBuilt?.Image?.Dispose();discoveryBuilt=(key,image);}}
            });
            return null;
        }
    }
    private void DisposeMapDiscovery(){lock(discoveryGate){discoveryImage?.Dispose();discoveryImage=null;discoveryBuilt?.Image?.Dispose();discoveryBuilt=null;discoveryKey="";}}
}
