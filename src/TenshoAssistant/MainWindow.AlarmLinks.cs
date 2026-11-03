using System.Numerics;
using TenshoAssistant.Alarms;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// g, R-12 (0.414): what the ringing alarm's chat line links to. The place: the alarm item's gathering point or fishing spot in the
// alarm's zone whose position is known (same positions as « Go there »), as the game's map coordinates; none when not known.
// « Open »: Tensho on the item's sheet, Gathering tab.
internal sealed partial class MainWindow
{
    internal (uint TerritoryId,uint MapId,float X,float Y)? AlarmPlace(TenshoAlarm alarm)
    {
        if(alarm.ItemId==0 || alarm.TerritoryId==0 || gatheringCatalog.State.Catalog is not { } catalog)return null;
        foreach(var source in catalog.ForItem(alarm.ItemId).Where(source=>source.TerritoryId==alarm.TerritoryId))
            if(GatheringPointPixel(source) is { } point)
            {
                var coordinate=MapProjection.CoordinateFromPixel(point.Pixel,point.Map);
                if(float.IsFinite(coordinate.X) && float.IsFinite(coordinate.Y))return (point.Map.TerritoryId,point.Map.MapId,coordinate.X,coordinate.Y);
            }
        return null;
    }

    internal void OpenAlarmItem(uint item)
    {
        if(item is 0 or > int.MaxValue)return;
        SelectItem(item);
        page=Page.Items;focusGatherTab=true;
    }
}
