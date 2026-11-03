using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// g, R-05 (0.402): above the Gathering list, the character's fishing, spearfishing and gathering notebooks (done / total), « hide what is
// already in the notebook », and the CSV export (clipboard and a file in the plugin folder).
internal sealed partial class MainWindow
{
    private NotebookState? DrawNotebookBar(ItemCatalog items)
    {
        NotebookSource?.Want();
        var notebook=NotebookSource?.State;
        if(notebook is null){ImGui.TextDisabled(localization.Get("Notebooks.Reading"));return null;}
        var fishing=notebook.Count(NotebookKind.Fishing);var spear=notebook.Count(NotebookKind.Spearfishing);var gathering=notebook.Count(NotebookKind.Gathering);
        ImGui.TextDisabled(localization.Format("Notebooks.Counts",fishing.Done,fishing.Total,spear.Done,spear.Total,gathering.Done,gathering.Total));
        ImGui.Checkbox(localization.Get("Notebooks.HideDone")+"##HideDone",ref sourceHideDone);
        Tip("Notebooks.HideDoneTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Notebooks.Copy")))
        {
            ImGui.SetClipboardText(Notebooks.Csv(notebook.Entries,id=>items.Find(id)?.Name ?? id.ToString()));
            notebookExportMessage=localization.Format("Notebooks.Copied",notebook.Entries.Count);
        }
        Tip("Notebooks.CopyTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Notebooks.Save")) && NotebookExportFolder?.Invoke() is { } folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                var path=Path.Combine(folder,$"Notebooks-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.csv");
                File.WriteAllText(path,Notebooks.Csv(notebook.Entries,id=>items.Find(id)?.Name ?? id.ToString()),new System.Text.UTF8Encoding(true));
                notebookExportMessage=localization.Format("Notebooks.Saved",path);
            }
            catch(Exception error){notebookExportMessage=localization.Get("Notebooks.SaveFailed");log.Warning(error,"Notebook export could not be saved.");}
        }
        Tip("Notebooks.SaveTip");
        if(notebookExportMessage is { } message)ImGui.TextDisabled(message);
        return notebook;
    }
}
