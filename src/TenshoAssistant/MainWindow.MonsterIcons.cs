using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// 0.799 (X-38, user request): an icon above the head of a nearby monster that matters (Blue Mage spell by default; lists, hunting log, FATE when chosen), drawn behind
// every Tensho window; the reason on hover. Information only, read from what the game already shows (its monsters, its spellbook, the wiki's sources).
internal sealed partial class MainWindow
{
    private const uint BlueMageIcon=62100+BlueMageJob;
    internal void DrawMonsterOverheadIcons()
    {
        var settings=configuration.MapView;
        if(windowDisposed || !settings.MonsterIconsAbove || mapLive?.State is not { Monsters:{ Count:>0 } monsters } live)return;
        if(settings.MonsterIconsHideInCombat && live.InCombat)return;
        RefreshNearbyMonsters();
        var draw=ImGui.GetBackgroundDrawList();
        var size=Math.Clamp(settings.MonsterIconSize,16,96)*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale;
        var distance=Math.Clamp(settings.MonsterIconDistance,5,150);
        var mouse=ImGui.GetIO().MousePos;var mouseFree=!ImGui.GetIO().WantCaptureMouse;
        string? tip=null;
        foreach(var monster in monsters)
        {
            if(Vector3.Distance(monster.Position,live.Position)>distance)continue;
            var notes=MonsterNotes(monster,live.Fates);
            MonsterNote? first=null;
            foreach(var note in notes)if(MonsterIconAbove(note.Kind,settings)){first=note;break;}
            if(first is null || !gameGui.WorldToScreen(monster.Position+new Vector3(0,monster.Height+0.6f,0),out var screen))continue;
            var corner=screen-new Vector2(size/2,size);
            if(first.Kind==MonsterNoteKind.BlueSpell && textures.TryGetFromGameIcon(new GameIconLookup(BlueMageIcon),out var shared) && shared.TryGetWrap(out var icon,out _) && icon is not null)
                draw.AddImage(icon.Handle,corner,corner+new Vector2(size));
            else
            {
                var (symbol,colour)=MonsterGlyph(first.Kind);
                draw.AddCircleFilled(corner+new Vector2(size/2),size*0.55f,ImGui.GetColorU32(new Vector4(0,0,0,0.6f)),24);
                draw.AddText(UiBuilder.IconFont,size*0.8f,corner+new Vector2(size*0.1f),ImGui.GetColorU32(colour),symbol.ToIconString());
            }
            if(mouseFree && mouse.X>=corner.X && mouse.X<=corner.X+size && mouse.Y>=corner.Y && mouse.Y<=corner.Y+size)
                tip=localization.Format("MapView.MonsterLabel",monster.Name,monster.Level)+"\n"+string.Join("\n",notes.Where(note=>MonsterIconAbove(note.Kind,settings)).Select(note=>note.Text));
        }
        if(tip is not null)WrappedTooltip(tip);
    }
    private static bool MonsterIconAbove(MonsterNoteKind kind,MapViewSettings settings)=>kind switch
    {
        MonsterNoteKind.BlueSpell=>settings.AboveBlueSpell,
        MonsterNoteKind.List=>settings.AboveList,
        MonsterNoteKind.HuntLog=>settings.AboveHuntLog,
        MonsterNoteKind.Fate=>settings.AboveFate,
        _=>false,
    };
    internal static (FontAwesomeIcon Symbol,Vector4 Colour) MonsterGlyph(MonsterNoteKind kind)=>kind switch
    {
        MonsterNoteKind.HuntLog=>(FontAwesomeIcon.BookOpen,new Vector4(0.55f,0.80f,0.98f,1)),
        MonsterNoteKind.Fate=>(FontAwesomeIcon.Star,new Vector4(0.95f,0.85f,0.40f,1)),
        MonsterNoteKind.BlueSpell=>(FontAwesomeIcon.HatWizard,new Vector4(0.40f,0.60f,1f,1)),
        _=>(FontAwesomeIcon.Paw,new Vector4(0.98f,0.55f,0.35f,1)),
    };
}
