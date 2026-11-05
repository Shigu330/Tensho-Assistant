namespace TenshoAssistant.Inventory;

/// <summary>Screen rectangle of the game's inventory window (whichever of its three layouts is shown).</summary>
public readonly record struct InventoryWindowRect(float X,float Y,float Width,float Height,float Scale=1);

/// <summary>0.535: whether a window drawn above the inventory covers the sort button. A window as large as the screen is a layer of the game, not a window in front.</summary>
public static class InventoryCover
{
    public static bool Covers(InventoryWindowRect cover,float x,float y,float width,float height,float screenWidth,float screenHeight)
    {
        if(cover.Width<=0 || cover.Height<=0 || cover.Width>=screenWidth*0.9f && cover.Height>=screenHeight*0.9f)return false;
        return x<cover.X+cover.Width && cover.X<x+width && y<cover.Y+cover.Height && cover.Y<y+height;
    }
}
