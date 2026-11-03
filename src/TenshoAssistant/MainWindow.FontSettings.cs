using Dalamud.Bindings.ImGui;
using TenshoAssistant.UI;

namespace TenshoAssistant;

// 0.394 (user request: « a setting for the fonts »): Settings › Interface › Fonts — family and size of the page titles, section headers
// and text of the Tensho window, each with its « ↺ »; FontManager rebuilds the fonts at the next frame.
internal sealed partial class MainWindow
{
    private FontSettings Fonts=>configuration.Fonts??=new();
    private static readonly FontSettings FontDefaults=new();

    private void DrawFontSettings()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("FontSettings.Header")+"###FontSettings"))return;
        ImGui.TextDisabled(localization.Get("FontSettings.Help"));
        FontRow("Title",()=>Fonts.Title,value=>Fonts.Title=value,()=>Fonts.TitleSize,value=>Fonts.TitleSize=value,FontDefaults.Title,FontDefaults.TitleSize,false);
        FontRow("Section",()=>Fonts.Section,value=>Fonts.Section=value,()=>Fonts.SectionSize,value=>Fonts.SectionSize=value,FontDefaults.Section,FontDefaults.SectionSize,false);
        FontRow("Body",()=>Fonts.Body,value=>Fonts.Body=value,()=>Fonts.BodySize,value=>Fonts.BodySize=value,FontDefaults.Body,FontDefaults.BodySize,true);
    }

    private void FontRow(string key,Func<string> family,Action<string> setFamily,Func<int> size,Action<int> setSize,string defaultFamily,int defaultSize,bool dalamudAllowed)
    {
        ImGui.PushID("Font"+key);
        try
        {
            var current=FontSettings.Family(family(),defaultFamily,dalamudAllowed);
            ImGui.SetNextItemWidth(160);
            if(ImGui.BeginCombo(localization.Get("FontSettings."+key)+"##Family",FamilyName(current)))
            {
                try
                {
                    var choices=dalamudAllowed ? FontSettings.GameFamilies.Prepend(FontSettings.DalamudFont):FontSettings.GameFamilies;
                    foreach(var choice in choices)
                        if(ImGui.Selectable(FamilyName(choice),choice==current))SaveSetting("FontSettings."+key,family,setFamily,choice);
                }
                finally{ImGui.EndCombo();}
            }
            Tip("FontSettings."+key+"Tip");
            if(DefaultButton("Family"+key,current!=defaultFamily))SaveSetting("FontSettings."+key,family,setFamily,defaultFamily);
            ImGui.SameLine();
            ImGui.BeginDisabled(current==FontSettings.DalamudFont);
            try
            {
                IntSetting("FontSettings.Size","FontSettings.SizeTip",()=>FontSettings.ClampSize(size()),setSize,FontSettings.MinimumSize,FontSettings.MaximumSize,defaultSize,90);
            }
            finally{ImGui.EndDisabled();}
        }
        finally{ImGui.PopID();}
    }

    private string FamilyName(string family)=>family==FontSettings.DalamudFont ? localization.Get("FontSettings.Dalamud"):family;
}
