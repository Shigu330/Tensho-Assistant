using TenshoAssistant.Chat;

// 0.1664 (X-683, the user's request of 20/11 around 21:50, picture « Agrégat d'eau »): an item linked in the Tensho chat window shows the body
// of Tensho's common item tooltip (DrawItemInfoBody reused, not copied), each part by the item panel's settings, the clicks' line at the bottom.
internal static class ChatItemTip1664Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat item tip 1664: the name on top, what a click does at the bottom",()=>
        {
            Check(ChatFormat.TipLines("Agrégat d'eau\nClic gauche : l'ouvre dans Tensho. Clic droit : menu d'objet de Tensho.")==("Agrégat d'eau","Clic gauche : l'ouvre dans Tensho. Clic droit : menu d'objet de Tensho."),"two parts");
            Check(ChatFormat.TipLines("Agrégat d'eau (HQ)\r\nLeft click")==("Agrégat d'eau (HQ)","Left click") && ChatFormat.TipLines("Only")==("Only",""),"a carriage return, or one line");
        }),
        ("Chat item tip 1664: the chat link's tooltip draws the common body as the common tooltip does, and the description; nothing else of its own",()=>
        {
            var chat=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var at=chat.IndexOf("var (named,clicks)=ChatFormat.TipLines(",StringComparison.Ordinal);
            var tip=chat[at..chat.IndexOf("if(ChatLinkClicked(id))",at,StringComparison.Ordinal)];
            Check(tip.Contains("DrawItemInfoBody(item,item.Id);") && !tip.Contains("DrawItemGearStats(") && tip.Contains("ChatPlayers?.Description(item.Id)"),"the common body, the description; the gear part only in the common tooltip (X-294)");
            Check(tip.IndexOf("DrawItemInfoBody(",StringComparison.Ordinal)<tip.IndexOf("ImGui.TextDisabled(clicks);",StringComparison.Ordinal),"the clicks' line last");
            Check(!tip.Contains("DrawHoveredStock(") && !tip.Contains("DrawHoveredUsages(") && !tip.Contains("Universalis"),"not copied, no price asked from here");
            var hover=Read("src/TenshoAssistant/MainWindow.MarketHover.cs");
            Check(hover.Contains("            DrawItemInfoBody(item,id);\n        }") && hover.Contains("var stack=hoverPanel ? itemHover.State.Stack:null;"),"the same call as Tensho's common item tooltip; the game's stack only under the game's tooltip");
        }),
    ];
}
