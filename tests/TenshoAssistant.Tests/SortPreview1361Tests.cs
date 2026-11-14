using TenshoAssistant.Inventory;
using TenshoAssistant.UI;

// X-384 and X-385 (0.1361, user reports of 15/11 with screenshots): the sort preview draws the player's bags in the order the sort fills them
// (1, 3, 2, 4), each with its own number and slots; « Visual sort applied to the four bags… » was drawn twice in developer mode (the raw line
// of a known message is the same translated sentence).
internal static class SortPreview1361Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Sort preview 1361: the bags drawn 1, 3, 2, 4 like the fill; retainers and saddlebags in their own order", () =>
        {
            var bags = InventorySortScope.Personal(7);
            Check(InventorySortRules.ShownContainers(bags).SequenceEqual([0, 2, 1, 3]), "bags 1, 3, 2, 4");
            var fill = InventorySortRules.FillOrder(bags);
            var drawn = InventorySortRules.ShownContainers(bags).SelectMany(bag => Enumerable.Range(bag * bags.SlotsPerContainer, bags.SlotsPerContainer)).ToArray();
            Check(drawn.SequenceEqual(fill), "the slots read frame after frame in the order they are filled");
            var saddle = InventorySortScope.FromStorage(new BankKey(7, BankStorageKind.Saddlebag, 0));
            Check(InventorySortRules.ShownContainers(saddle).SequenceEqual([0, 1]), "the saddlebag unchanged");
            var retainer = InventorySortScope.FromStorage(new BankKey(7, BankStorageKind.Retainer, 42));
            Check(InventorySortRules.ShownContainers(retainer).SequenceEqual(Enumerable.Range(0, 7)), "a retainer's seven pages unchanged");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SortPreview.cs")).Replace("\r\n", "\n");
            Check(page.Contains("var shownBags=InventorySortRules.ShownContainers(scope);") && page.Contains("var bag=shownBags[shown];")
                && page.Contains("localization.Format(\"SortUi.Bag\",bag+1,bag*scope.SlotsPerContainer+1,(bag+1)*scope.SlotsPerContainer)")
                && page.Contains("var target = bag * scope.SlotsPerContainer + index;"), "each frame keeps its bag's number and slot numbers");
        }),
        ("Sort preview 1361: a known message once, its raw line in developer mode only when it says something else", () =>
        {
            const string sentence = "Tri visuel appliqué aux quatre sacs ; actualisation sous une seconde.";
            Check(UiStates.DeveloperDetail(sentence, sentence) is null, "the same sentence is not drawn twice");
            Check(UiStates.DeveloperDetail("Le tri a échoué.", "Sort not applied: slot 3 moved.") == "Sort not applied: slot 3 moved.", "an unknown message keeps its raw detail");
            var presentation = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ErrorPresentation.cs"));
            Check(presentation.Contains("if(developerMode && UiStates.DeveloperDetail(shown,RawMessage(message)) is { } detail)ImGui.TextWrapped(detail);")
                && !presentation.Contains("ImGui.TextWrapped(UiMessages.Get(localization,message,fallbackKey));\r\n        if(developerMode)ImGui.TextWrapped(RawMessage(message));"), "the service line guarded");
            var sort = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SortPreview.cs"));
            Check(sort.Split("DrawServiceMessage(status,\"ServiceErrors.Sort\")").Length == 2, "the sort status drawn by one line of the page");
        }),
    ];
}
