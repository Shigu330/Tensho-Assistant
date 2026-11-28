// 0.1670 (X-684, second part: quests): one quest tooltip (MainWindow.Quests.cs, DrawQuestTooltip) for the Quests page, the items' quest sources and the
// chat's quest links: icon, name, section and genre, level, zone, expansion, progress, repeatable; read from the quest's entry only.
internal static class QuestTips1670Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest tips 1670: one body with everything the quest's entry holds",()=>
        {
            var page=Read("MainWindow.Quests.cs");
            var body=page[page.IndexOf("private void DrawQuestTooltip(",StringComparison.Ordinal)..];
            foreach(var part in new[]{"entry.Section","entry.Category","QuestDetails.Level","QuestDetails.OtherLevel","QuestDetails.Zone","QuestDetails.Expansion","QuestProgressLabel(entry)","QuestDetails.Repeatable","TooltipScope.Begin()"})
                Check(body.Contains(part),part);
            Check(!body.Contains("quests.State") && !body.Contains("GetExcelSheet"),"nothing looked up under the mouse");
        }),
        ("Quest tips 1670: the Quests page, the quest sources and the chat's quest links use it",()=>
        {
            Check(Read("MainWindow.Quests.cs").Contains("if(RichTipHovered())DrawQuestTooltip(entry);"),"Quests page");
            Check(Read("MainWindow.QuestItemSources.cs").Contains("DrawQuestTooltip(questEntry);"),"quest sources");
            Check(Read("MainWindow.ChatWindow.cs").Contains("DrawQuestTooltip(quest,localization.Get(\"ChatWindow.QuestLinkTip\"));"),"chat link");
        }),
    ];
}
