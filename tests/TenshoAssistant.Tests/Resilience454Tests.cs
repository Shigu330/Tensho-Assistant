// R-14 (0.454): last confirmed findings of the audit: « unavailable » instead of « reading » for ever, one lost sheet stays local, functions checked.
internal static class Resilience454Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Title by job: only a title this character owns is sent",() =>
        {
            var changer=Read("Collections/TitleChanger.Dalamud.cs");
            Check(changer.Contains("if(send!=0 && (!ui->TitleList.UnlockedTitlesBitArray.TryGet(send,out var owned) || !owned))"),"checked");
            Check(changer.IndexOf("UnlockedTitlesBitArray",StringComparison.Ordinal)<changer.IndexOf("SendTitleIdUpdate(send)",StringComparison.Ordinal),"before sending");
        }),
        ("Collections: a lost linked sheet leaves one card or location, not every category",() =>
        {
            var reader=Read("Collections/CollectionReader.Dalamud.cs");
            Check(reader.Contains("try { linked = details.TripleTriadCardRarity.IsValid && details.TripleTriadCardType.IsValid; }"),"cards");
            Check(reader.Contains("catch (Exception) { location = \"\"; blueIssue ="),"locations");
        }),
        ("Collectables and notebooks: a failure says « unavailable », a bag not loaded publishes nothing incomplete",() =>
        {
            var reader=Read("Gathering/CollectableReader.Dalamud.cs");
            Check(reader.Contains("catch(Exception error){CatalogFailed=true;") && reader.Contains("HoldingsUnavailable=true;") && reader.Contains("if(container==null || !container->IsLoaded)return;"),"collectables");
            Check(Read("MainWindow.CollectableTable.cs").Contains("reader.CatalogFailed ? \"CollectablePanel.Unavailable\"") && Read("MainWindow.CollectablePanel.cs").Contains("Collectables.CatalogFailed ? \"CollectablePanel.Unavailable\""),"pages");
            Check(new TenshoAssistant.Gathering.NotebookState([],DateTimeOffset.UnixEpoch).GatheringUnavailable==false,"available by default");
            var notebook=Read("Gathering/NotebookReader.Dalamud.cs");
            Check(notebook.Contains("new NotebookState(entries,now,!gatheringReadable)") && notebook.Contains("Volatile.Write(ref unavailable,true);"),"notebook");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Read($"Localization/Languages/{language}.json");
                foreach(var key in new[]{"\"HeldUnavailable\"","\"GatheringUnavailable\""})Check(text.Contains(key),language+" "+key);
            }
        }),
        ("Community names: a lost sheet is remembered, each table built alone",() =>
        {
            var names=Read("Items/GameNames.Lumina.cs");
            Check(names.Contains("catch(Exception error){Warn(error);garland[(type,id)]=null;return null;}") && names.Contains("var monsters=Safe(()=>") && names.Contains("var duties=Safe(()=>"),"names");
        }),
        ("Functions checked: retainer sorter (map node move: Map328Tests), treasure map said once; Triad data plausible; no French in the code",() =>
        {
            Check(Read("Inventory/InventoryNativeReader.Dalamud.cs").Contains("(nint)ItemOrderModule.MemberFunctionPointers.GetActiveRetainerSorter != 0") && Read("Inventory/InventoryVisualSortBridge.Dalamud.cs").Contains("(nint)ItemOrderModule.MemberFunctionPointers.GetActiveRetainerSorter!=0"),"sorter");
            Check(Read("Maps/TreasureMapReader.Dalamud.cs").Contains("The treasure map could not be read: game function not resolved"),"treasure");
            Check(Read("MiniGames/TriadNpcLoader.Dalamud.cs").Contains("if (cardOfItem.Count == 0 || npcOfMatch.Count == 0) throw new InvalidOperationException("),"triad");
            Check(!Read("Gathering/GatheringObservationReader.Dalamud.cs").Contains("GetAsAtkTextNode") && Read("Craft/CraftLiveObservation.cs").Contains("\"Native text too long.\""),"cast, English");
        }),
    ];
}
