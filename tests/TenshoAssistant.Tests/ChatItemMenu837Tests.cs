// 0.837 (X-03, user test of « /tensho linktest »): the Tensho entry in the chat's menu on an item link.
internal static class ChatItemMenu837Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat item menu: the link item must be the hovered one; a name alone gets nothing",() =>
        {
            var bridge=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/ItemContextMenuBridge.Dalamud.cs"));
            Check(bridge.Contains("internal static bool ChatLinkMatches(uint linked,ulong hovered)=>linked!=0 && hovered!=0 && linked%1_000_000==hovered%1_000_000;"),"rule");
            Check(bridge.Contains("var chatItem=args.AddonName==\"ChatLog\" && ChatLinkItem(gui.HoveredItem);") && bridge.Contains("!itemWindow && !chatItem &&"),"chat only");
        }),
    ];
}
