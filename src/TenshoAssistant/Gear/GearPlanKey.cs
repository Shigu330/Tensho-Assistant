namespace TenshoAssistant.Gear;

/// <summary>
/// X-01 C5 (0.1242): the Gear page's planned set is rebuilt only when an input changes (MainWindow.GearModelFor). The plan and its
/// materia are changed in place by several controls (a piece picked, materia set, a plan set loaded, settings reset): instead of tracking
/// each of them, their exact content is compared in every frame through this signature (twelve slots at most: cheap).
/// </summary>
public static class GearPlanKey
{
    /// <summary>Each slot's planned item and planned materia, in slot order: equal exactly when the plan and its materia are the same.</summary>
    public static string Signature(IReadOnlyDictionary<GearSlot,uint> plan,IReadOnlyDictionary<GearSlot,List<uint>>? materia)
    {
        var text=new System.Text.StringBuilder();
        foreach(var slot in Enum.GetValues<GearSlot>())
        {
            if(plan.TryGetValue(slot,out var id))text.Append((int)slot).Append('=').Append(id);
            if(materia?.TryGetValue(slot,out var codes)==true){text.Append('[');foreach(var code in codes)text.Append(code).Append(',');text.Append(']');}
            text.Append(';');
        }
        return text.ToString();
    }
}
