using TenshoAssistant.Fields;

// 0.1676 (X-684, seventh part: FATEs): one FATE tooltip (MainWindow.FateTooltip.cs) for the FATEs page, the field operations' FATEs and
// engagements and the Tensho map's list of FATEs under way: icon, name, level and kind, place, state now, description, players.
internal static class FateTips1676Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("FATE tips 1676: the facts, blanks left out",()=>
        {
            var facts=FateTips.Build(" Le roi des moogles ",60501,32,"ALEA","Noscea centrale · X 20 Y 18","Progression : 40 %"," Battez-le. ",12);
            Check(facts is { Name:"Le roi des moogles",Icon:60501,Level:32,Kind:"ALEA",Place:"Noscea centrale · X 20 Y 18",Now:"Progression : 40 %",Description:"Battez-le.",Participants:12 },"the facts");
            Check(FateTips.Build("x",0,0,"",null," ","",-3) is { Level:null,Kind:null,Place:null,Now:null,Description:null,Participants:0 },"nothing known: nothing said");
        }),
        ("FATE tips 1676: the FATEs page, the field operations and the map's list use the one FATE tooltip",()=>
        {
            Check(Read("src/TenshoAssistant/MainWindow.Fates.cs").Contains("if(RichTipHovered())DrawFateTooltip(FateTips.Build(row.Name,0,row.Level,"),"the FATEs page");
            var fieldContent=Read("src/TenshoAssistant/MainWindow.FieldContent.cs");
            Check(fieldContent.Contains("if(RichTipHovered())DrawFateTooltip(FateTips.Build(row.Name,0,row.Level,") && !fieldContent.Contains("WrappedTooltip(FieldEventTip(row))"),"the field operations");
            Check(Read("src/TenshoAssistant/MainWindow.MapLocalLists.cs").Contains("if(RichTipHovered())DrawFateTooltip(Fields.FateTips.Build(fate.Name,fate.Icon,fate.Level,"),"the map's FATEs under way");
        }),
    ];
}
