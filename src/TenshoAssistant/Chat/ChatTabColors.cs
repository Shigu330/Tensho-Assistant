using System.Numerics;
using TenshoAssistant.UI;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-478 (the user's request of 15/11): the colour a tab's name is drawn in. The player's colour as chosen when it reads on both the inactive
/// and the active tab (contrast of 3:1 at least, as the accent of « Follow Dalamud's style »); otherwise it moves towards the theme's text
/// colour, a tenth at a time, until it does: a name always legible, in the light and the dark themes alike. Pure (tests).
/// X-495 (0.1478): checked against every background a tab is drawn on: inactive, active, hovered, and the same two without the window's focus
/// (TabUnfocused, TabUnfocusedActive: the chat window's usual state, NoFocusOnAppearing).
/// </summary>
public static class ChatTabColors
{
    public const double MinimumContrast=3;

    public static Vector4 Readable(Vector4 chosen,Vector4 text,params Vector4[] backgrounds)
    {
        for(var step=0;step<=10;step++)
        {
            var colour=Vector4.Lerp(chosen,text,step/10f);
            colour.W=1;
            if(backgrounds.All(background=>ThemePalettes.Contrast(colour,background)>=MinimumContrast))return colour;
        }
        return text;
    }
}
