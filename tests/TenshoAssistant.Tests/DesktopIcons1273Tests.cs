// 0.1273 (desktop application, finding of relay 43): Font Awesome glyphs such as the caret arrows of the folded categories, the check
// marks, the droplet and the question mark drew as full rectangles in the application. Cause found by trial: the icon font merged with
// ImGui's default horizontal oversampling (three times); without oversampling they draw right (captures before and after). The setting
// is kept by this test; the picture itself is checked by a capture (the headless run has no pixels).
internal static class DesktopIcons1273Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop icons 1273: the icon font is merged without oversampling, snapped to the pixel; the atlas can be written to a picture",() =>
        {
            var host=Read("tools/PluginUiTest/src/PluginUiTest.Host/ImGuiHeadless.cs");
            Check(host.Contains("config.MergeMode=true;config.GlyphMinAdvanceX=size;config.OversampleH=1;config.PixelSnapH=true;"),"icons: merged, no horizontal oversampling, pixel snapping");
            var merge=host[host.IndexOf("var config=ImGui.ImFontConfig();",StringComparison.Ordinal)..host.IndexOf("io.Fonts.Build();",StringComparison.Ordinal)];
            Check(!merge.Contains("OversampleH=3") && !merge.Contains("OversampleH=2"),"never oversampled again");
            Check(host.Contains("HostFonts.Main=DalamudFonts ? io.Fonts.AddFontFromFileTTF(text.AsSpan(),size,default,textRanges)"),"the text font keeps ImGui's default (oversampled: smoother letters)");
            var program=Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.Contains("if(Option(args,\"--atlas=\") is { } atlasPath)") && program.Contains("Png.Write(atlasPath,w,h,atlasCopy);"),"--atlas=<png>: the font atlas as built, for a glyph that draws wrong");
        }),
    ];
}
