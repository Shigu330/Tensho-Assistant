// 0.788 (X-29, user report): the wrong job said plainly in the crafting log panel.
internal static class WrongJob788Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Wrong job: both jobs named, warning colour, wrapped, with the button to wear the recipe's job",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftLog.cs"));
            Check(view.Contains("ImGui.TextWrapped(localization.Format(\"CraftLogUi.Remembered\",JobName(job),context.Recipe?.CraftType ?? \"\",rememberedAt.ToLocalTime().ToString(\"g\")));"),"named, wrapped");
            Check(view.Contains("ImGui.PushStyleColor(ImGuiCol.Text,UI.FfxivTheme.Warning);") && view.Contains("DrawEquipRecipeJob(worn,\"CraftLogEquipRemembered\")"),"warning, button");
            // 0.1019 (test audit #50): the message itself carries the three values, not just any line of the file.
            foreach(var lang in new[]{"en","fr"})
            {
                using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{lang}.json")));
                var text=document.RootElement.GetProperty("CraftLogUi").GetProperty("Remembered").GetString() ?? "";
                Check(text.Contains("{0}") && text.Contains("{1}") && text.Contains("{2}"),"three values: "+lang);
            }
        }),
    ];
}
