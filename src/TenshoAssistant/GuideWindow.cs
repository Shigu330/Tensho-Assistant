using System.Numerics;
using Dalamud.Interface.Windowing;

namespace TenshoAssistant;

/// <summary>The first-run guide as a Dalamud window (ONBOARD-001). Content and state stay in MainWindow.Guide; hidden in combat and in duties, it comes back after.</summary>
internal sealed class GuideWindow : Window
{
    private readonly MainWindow owner;

    public GuideWindow(MainWindow owner) : base("###TenshoGuide")
    {
        this.owner = owner;
        RespectCloseHotkey = false;
        Size = new Vector2(520, 460);
        SizeCondition = Dalamud.Bindings.ImGui.ImGuiCond.FirstUseEver;
    }

    public override bool DrawConditions() => owner.GuideVisible();
    public override void PreDraw() { WindowName = owner.GuideTitle() + "###TenshoGuide"; owner.GuideThemePush(); }
    public override void PostDraw() => owner.GuideThemePop();
    public override void Draw() => owner.DrawGuideContents();
    public override void OnClose() => owner.CloseGuide();
}
