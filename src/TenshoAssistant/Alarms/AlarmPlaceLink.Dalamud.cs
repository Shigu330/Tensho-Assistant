using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Alarms;

/// <summary>
/// X-509 (review of 0.1490): the place link of an alarm's ring in the game's chat, whole: Dalamud's SeString.CreateMapLink (the link, the game's
/// arrow, the place's name and its coordinates as text, the link's end), as the game writes a place link. From 0.414 (R-12) the ring used
/// SeStringBuilder.AddMapLink, which in Dalamud 15.0.4.0 adds the link's start alone (IL read on 11/16: docs/evidence/0.1491-alarm-place-link.md):
/// the game's chat showed neither the name nor the coordinates, and the link stayed open over what followed. Built on the game's thread (the
/// place's name comes from the game's data, in the client's language).
/// </summary>
internal static class AlarmPlaceLink
{
    private static bool warned;

    /// <summary>The whole place link; null when Dalamud cannot make it (a place it does not know), said once in the log: the ring is then sent
    /// without a place link, never with a bare one.</summary>
    public static SeString? Build(uint territory,uint map,float x,float y,IPluginLog? log=null)
    {
        try{return SeString.CreateMapLink(territory,map,x,y,0.05f);}
        catch(Exception error)
        {
            if(!warned){warned=true;log?.Warning(error,"The place link of an alarm could not be made (territory {0}, map {1}); the ring is sent without it.",territory,map);}
            return null;
        }
    }
}
