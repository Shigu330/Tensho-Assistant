using TenshoAssistant.Craft;
using TenshoAssistant.MyTools;
using TenshoAssistant.UI;

// 0.1168 (X-180, night review N1, high): what the desktop application created or changed (my tools, alarms, craft presets, garden, the
// Beastmaster's teams) was written on its copy of the plugin's folder and settings, then overwritten from the game without a word, while the
// page said « added ». Now every save of the window throws there (DesktopReadOnly.Guard: each caller's failure path puts the previous value
// back and says « not saved »), the stores built on the copy are read-only, the controls that create or change are greyed and the status bar
// says why. Behaviour tests (R28, data loss): the stores on a copy never change their file, the guarded save never reaches the real one; the
// whole window built as the application builds it is checked by the headless run of Verify (Headless.ReadOnlyCheck).
internal static class DesktopReadOnly1168Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private const string Tool = """{"schema":1,"id":"my-tool","name":{"en":"Tool"},"blocks":[{"type":"checklist","reset":"daily","entries":[{"en":"One"}]},{"type":"counter","label":{"en":"Runs"},"target":5}]}""";

    private static void InFolder(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "Tensho-ReadOnly-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { Directory.Delete(root, true); }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Desktop read-only 1168: the guarded save throws in the desktop application and never reaches the plugin's save; the setting is put back", () =>
        {
            var desktop = true; var saved = 0;
            var save = DesktopReadOnly.Guard(() => desktop, () => saved++);
            var value = "new"; var error = SettingSave.Run(save, () => value = "before");
            Check(error is DesktopReadOnlyException && saved == 0 && value == "before", "refused, not saved, previous value back");
            desktop = false;
            Check(SettingSave.Run(save, () => value = "rolled back") is null && saved == 1 && value == "before", "in the game: saved as before");
            var thrown = false;
            try { DesktopReadOnly.Guard(() => true, () => saved++)(); } catch (DesktopReadOnlyException) { thrown = true; }
            Check(thrown && saved == 1, "a direct call throws too");
        }),
        ("Desktop read-only 1168: my tools on the application's copy: nothing installed, removed or ticked, the file's bytes unchanged", () => InFolder(root =>
        {
            var path = Path.Combine(root, "MyTools.json");
            var template = ToolTemplateFormat.Parse(Tool).Template!; var json = ToolTemplateFormat.Serialize(template);
            using (var game = new ToolTemplateStore(path, (_, _) => { })) { Check(game.Install(template, json), "the game's store installs"); game.FlushAsync().Wait(); }
            var before = File.ReadAllBytes(path);
            using (var copy = new ToolTemplateStore(path, (_, _) => { }, readOnly: true))
            {
                Check(copy.ReadOnly && copy.Tools.Count == 1, "read as it is");
                var other = ToolTemplateFormat.Parse(Tool.Replace("my-tool", "other")).Template!;
                Check(!copy.Install(other, ToolTemplateFormat.Serialize(other)) && !copy.Install(template, json), "nothing installed or replaced");
                copy.SetDone("my-tool", "0:0", true, DateTimeOffset.UtcNow); copy.SetCount("my-tool", "1:0", 3); copy.Remove("my-tool");
                copy.FlushAsync().Wait();
                Check(copy.Tools.Count == 1 && copy.Tools[0].Done.Count == 0 && copy.Tools[0].Counts.Count == 0, "nothing changed in memory");
            }
            Check(File.ReadAllBytes(path).SequenceEqual(before), "the file's bytes are unchanged");
        })),
        ("Desktop read-only 1168: craft lists on the application's copy: nothing set, the file not written (nor created)", () => InFolder(root =>
        {
            var path = Path.Combine(root, "CraftLists.json");
            var copy = new CraftListStore(path, (_, _) => { }, readOnly: true);
            Check(copy.ReadOnly && !copy.Set(0, new("Gear", [new(10, 5)])) && !File.Exists(path), "not created");
            Check(new CraftListStore(path, (_, _) => { }).Set(0, new("Gear", [new(10, 5)])), "the game's store writes");
            var before = File.ReadAllBytes(path);
            var again = new CraftListStore(path, (_, _) => { }, readOnly: true);
            Check(!again.Set(0, null) && !again.Set(1, new("Food", [])) && again.Lists.Count == 1, "nothing removed or added");
            Check(File.ReadAllBytes(path).SequenceEqual(before), "the file's bytes are unchanged");
        })),
        ("Desktop read-only 1168: wiring (the window's save guarded, the stores read-only on the copy, the teams never written, the controls greyed, said)", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("this.save = TenshoAssistant.UI.DesktopReadOnly.Guard(() => inDesktopApp, save);") && window.Contains("if(!lastPageDirty || inDesktopApp)return;"), "the window's save");
            var data = Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(data.Contains("\"CraftLists.json\"),warn,readOnly:true)") && data.Contains("\"MyTools.json\"),warn,readOnly:true)"), "read-only stores on the copy");
            Check(data.Contains("error is TenshoAssistant.UI.DesktopReadOnlyException ? \"Desktop.NotSaved\":key") && data.Contains("ImGui.BeginDisabled(inDesktopApp)"), "the text and the greying");
            Check(Read("src/TenshoAssistant/MainWindow.Beastmaster.cs").Contains("if (inDesktopApp) return;"), "teams never written there");
            Check(Read("src/TenshoAssistant/UI/Components/SettingRow.cs").Contains("if(error is TenshoAssistant.UI.DesktopReadOnlyException)saveError=SaveFailedText(error,\"Common.SaveError\");"), "settings: the footer says it");
            Check(Read("src/TenshoAssistant/MainWindow.Alarms.cs").Split("alarmStatus=SaveFailedText(error,\"Alarms.SaveFailed\")").Length == 4, "alarms: « not saved » instead of « added »");
            foreach (var file in new[] { "MainWindow.MyTools.cs", "MainWindow.Garden.cs", "MainWindow.Alarms.cs", "MainWindow.CraftPresets.cs" })
            {
                var text = Read("src/TenshoAssistant/" + file);
                Check(text.Contains("DesktopEditBegin();") && text.Split("DesktopEditBegin();").Length == text.Split("DesktopEditEnd();").Length, file + ": greyed, each scope closed");
            }
            Check(Read("src/TenshoAssistant/MainWindow.MyTools.cs").Contains("if(myToolDraft is { } draft && !store.ReadOnly)"), "no editor on a read-only store");
            Check(TenshoAssistant.Desktop.DesktopPages.Editing.All(page => TenshoAssistant.Desktop.DesktopPages.Kind(page) is not null and not TenshoAssistant.Desktop.DesktopPageKind.InGame), "the editing pages are app pages");
            Check(Read("src/TenshoAssistant.Desktop/StatusBar.cs").Contains("if(DesktopPages.Editing.Contains(page))lines.Add((host.Text(\"Desktop.ReadOnlyHere\"),false));"), "the status bar says it");
            Check(Read("src/TenshoAssistant.Desktop/Headless.cs").Contains("ReadOnlyCheck(host,report)"), "the headless run checks the whole window");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(text.Contains("\"NotSaved\": ") && text.Contains("\"ReadOnlyHere\": "), language);
            }
        }),
    ];
}
