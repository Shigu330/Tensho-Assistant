using Dalamud.Interface;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.UI;

/// <summary>
/// 0.394 (user request): owns the font handles of the Tensho window (UiComponents.TitleFont, SectionFont, BodyFont), built from the
/// settings and built again when they change (checked at the start of each Draw, before any font is pushed). Game fonts come from the
/// game files through Dalamud's font atlas; « Dalamud » for the text = no handle (Dalamud's own font).
/// </summary>
internal sealed class FontManager(IUiBuilder ui,IPluginLog log) : IDisposable
{
    private (string,int,string,int,string,int)? built;
    private bool disposed;

    public void Apply(FontSettings settings)
    {
        if(disposed)return;
        var key=settings.Key;
        if(built==key)return;
        Release();
        built=key;
        try
        {
            UiComponents.TitleFont=Game(key.Title,key.TitleSize);
            UiComponents.SectionFont=Game(key.Section,key.SectionSize);
            UiComponents.BodyFont=key.Body==FontSettings.DalamudFont ? null:Game(key.Body,key.BodySize);
        }
        catch(Exception error){Release();log.Warning(error,"Tensho fonts could not be built; the default font is used.");}
    }

    private IFontHandle Game(string family,int size)=>ui.FontAtlas.NewGameFontHandle(new GameFontStyle(Enum.Parse<GameFontFamily>(family),size));

    private static void Release()
    {
        UiComponents.TitleFont?.Dispose();UiComponents.TitleFont=null;
        UiComponents.SectionFont?.Dispose();UiComponents.SectionFont=null;
        UiComponents.BodyFont?.Dispose();UiComponents.BodyFont=null;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        Release();
    }
}
