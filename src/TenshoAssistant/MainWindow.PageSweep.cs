namespace TenshoAssistant;

// 0.724: the pages for the sweep of the self-check (Diagnostics/PageSweepWindow.Dalamud.cs). The market page is left out: with an item chosen, opening it asks Universalis.
internal sealed partial class MainWindow
{
    internal const int PageSweepSkipped=1;

    internal IReadOnlyList<(string Name,Action Draw)> PageSweepEntries()=>PageEntries.Where(entry=>entry.Page!=Page.Market && entry.Visible?.Invoke()!=false)
        .Select(entry=>(entry.Page.ToString(),(Action)(()=>
        {
            var kept=page;
            try{ValidateDrawContext();entry.Draw();}
            finally{page=kept;}
        }))).ToArray();
}
