using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace TenshoAssistant;

// Step e (0.332, ULD before D17): the game's own window frame drawn behind Tensho's main window, opt-in (Settings › Interface).
// The textures and the nine-piece cut are the game's: ui/uld/WindowA_BgNormal_{Corner,H,V,HV}_hr1.tex, cut as the Inventory layout
// declares it (docs/evidence/0.332-uld-window-frame.md, tools/UldProbe): top band 64 px, sides 16 px, bottom band 32 px at 1×; the
// _hr1 textures are the same pieces at 2×, so the cut is kept as fractions of each texture. When a texture cannot be loaded (game
// update, missing file) nothing is drawn: the window keeps its normal look (R-14).
internal sealed partial class MainWindow
{
    private const string FrameCorner="ui/uld/WindowA_BgNormal_Corner_hr1.tex",FrameH="ui/uld/WindowA_BgNormal_H_hr1.tex",
        FrameV="ui/uld/WindowA_BgNormal_V_hr1.tex",FrameHV="ui/uld/WindowA_BgNormal_HV_hr1.tex";

    private void DrawGameFrame()
    {
        if(!configuration.GameFrames)return;
        if(!textures.GetFromGame(FrameCorner).TryGetWrap(out var corner,out _) || corner is null
           || !textures.GetFromGame(FrameH).TryGetWrap(out var horizontal,out _) || horizontal is null
           || !textures.GetFromGame(FrameV).TryGetWrap(out var vertical,out _) || vertical is null
           || !textures.GetFromGame(FrameHV).TryGetWrap(out var middle,out _) || middle is null)return;
        var scale=ImGuiHelpers.GlobalScale;
        float side=16*scale,top=64*scale,bottom=32*scale;
        var min=ImGui.GetWindowPos();var max=min+ImGui.GetWindowSize();
        if(max.X-min.X<2*side || max.Y-min.Y<top+bottom)return;
        var draw=ImGui.GetWindowDrawList();
        // Texture rectangles of the 1× cut, as fractions (Corner and H are 32×96, V and HV 32×32 at 1×).
        var x0=min.X;var x1=min.X+side;var x2=max.X-side;var x3=max.X;
        var y0=min.Y;var y1=min.Y+top;var y2=max.Y-bottom;var y3=max.Y;
        void Piece(Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap texture,float ax,float ay,float bx,float by,float u0,float v0,float u1,float v1)
            =>draw.AddImage(texture.Handle,new Vector2(ax,ay),new Vector2(bx,by),new Vector2(u0,v0),new Vector2(u1,v1));
        // The window's draw list is clipped to its content: the frame covers the whole window, title bar included.
        draw.PushClipRect(min,max,false);
        try
        {
            Piece(corner,x0,y0,x1,y1,0,0,0.5f,64/96f);
            Piece(horizontal,x1,y0,x2,y1,0,0,1,64/96f);
            Piece(corner,x2,y0,x3,y1,0.5f,0,1,64/96f);
            Piece(vertical,x0,y1,x1,y2,0,0,0.5f,1);
            Piece(middle,x1,y1,x2,y2,0,0,1,1);
            Piece(vertical,x2,y1,x3,y2,0.5f,0,1,1);
            Piece(corner,x0,y2,x1,y3,0,64/96f,0.5f,1);
            Piece(horizontal,x1,y2,x2,y3,0,64/96f,1,1);
            Piece(corner,x2,y2,x3,y3,0.5f,64/96f,1,1);
        }
        finally{draw.PopClipRect();}
    }
}
