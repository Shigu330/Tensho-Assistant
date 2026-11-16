using TenshoAssistant.UI;

// X-460 (the user's capture of 15/11): the two « 100% » boxes of the map's tool column showed their text shifted right. Both are now as wide
// as the text with a padding on each side, and the field's left padding puts its text in the middle; at any font scale and in both languages
// (the text is the same digits in each).
internal static class MapZoomBoxes1500Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map zoom boxes 1500: « 100% » sits in the middle of the button and of the field, at 100 % and 150 % of the font",() =>
        {
            foreach(var scale in new[]{1f,1.5f})
            {
                float text=28*scale,padding=4*scale,frame=22*scale;
                var width=CenteredText.ButtonWidth(text,padding,26);
                Check(Math.Abs(width-(text+2*padding))<0.001f,$"x{scale}: a padding on each side ({width})");
                // ImGui draws a button's label in the room left between both paddings, centred: equal margins when it fits.
                var room=width-2*padding;
                Check(room>=text-0.001f,$"x{scale}: the label fits between the paddings, so it is centred, not pushed right");
                var left=CenteredText.Padding(width,text,1);
                Check(Math.Abs(left-(width-text-left))<0.001f,$"x{scale}: the field's text has the same margin left and right");
                var narrow=CenteredText.Padding(width,18*scale,1);
                Check(Math.Abs(narrow*2+18*scale-width)<0.001f,$"x{scale}: « 85% » centred too");
                Check(CenteredText.ButtonWidth(10,4,frame)==frame,"never narrower than the frame height");
            }
            Check(CenteredText.Padding(20,40,4)==4,"a text wider than the box starts at the usual padding");
        }),
        ("Map zoom boxes 1500: the map's column uses it for both boxes, the shared text button too",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("var fieldWidth=hundred;",StringComparison.Ordinal) && map.Contains("UI.CenteredText.Padding(fieldWidth,ImGui.CalcTextSize(text).X,1)",StringComparison.Ordinal)
                  && map.Contains("TextToolButton(\"100%\",\"MapView.Zoom100\",width:fieldWidth)",StringComparison.Ordinal) && map.Contains("finally{ImGui.PopStyleVar();",StringComparison.Ordinal),"map column");
            var button=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/Components/ToolButton.cs"));
            Check(button.Contains("UI.CenteredText.ButtonWidth(ImGui.CalcTextSize(text).X,ImGui.GetStyle().FramePadding.X,ImGui.GetFrameHeight())",StringComparison.Ordinal),"text button");
        }),
    ];
}
