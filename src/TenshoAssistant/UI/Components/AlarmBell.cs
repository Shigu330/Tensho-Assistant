using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Alarms;

namespace TenshoAssistant;

// The bell that other tools use to create Tensho alarms (weather periods, fishing and gathering windows, reminders, voyages, Gold Saucer).
// X-881 (0.1892, review of 0.1891, M1): the bell takes no tooltip. Its caller says what will be created right after it, on hover only,
// « if(TipHovered())AlarmTip(…) », so the text of each row (a period, a fishing window, a slot) is not formatted at each frame
// (TipTextParameters1892Tests). Moved here from the Alarms page with its empty space: one component.
internal sealed partial class MainWindow
{
    // The bell button; the alarms are made on click only.
    private void AlarmButton(string id,Func<IReadOnlyList<TenshoAlarm>> create)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        bool clicked;
        try{clicked=ImGui.SmallButton(FontAwesomeIcon.Bell.ToIconString()+"##Alarm"+id);}
        finally{ImGui.PopFont();}
        if(clicked)AddAlarms(create());
    }

    // The bell's tooltip, called behind TipHovered(): what will be created, then the lead the alarms really get.
    // 0.331: daily = alarms every ET day, whose lead is bounded to 60 min (ClampLead): the tooltip gives the lead they really get.
    private void AlarmTip(string text,bool daily=false)
        =>WrappedTooltip(text+"\n"+localization.Format("Alarms.LeadHint",MinutesText(AlarmSchedule.ClampLead(daily ? AlarmRepeat.EorzeaDaily:AlarmRepeat.Once,DefaultAlarmLead))));

    // 0.330: empty space the size of the bell (SmallButton: text plus the frame's side padding) where no alarm can be made, so a list stays aligned.
    private static void AlarmButtonSpace()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.Dummy(ImGui.CalcTextSize(FontAwesomeIcon.Bell.ToIconString())+new Vector2(ImGui.GetStyle().FramePadding.X*2,0));}
        finally{ImGui.PopFont();}
    }
}
