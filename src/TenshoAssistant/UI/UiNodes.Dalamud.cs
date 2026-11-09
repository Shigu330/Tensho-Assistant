using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.UI;

/// <summary>X-76 (step 2): the guard of every walk through a game window's nodes (R-14), written once; each walker keeps its own depth and count caps.</summary>
internal static unsafe class UiNodes
{
    /// <summary>A node list that can be walked: present, its count within its allocated size, and its component fully loaded.</summary>
    public static bool Walkable(AtkUldManager* manager)
        =>manager!=null && manager->NodeList!=null && manager->NodeListCount<=manager->NodeListSize && manager->LoadedState==AtkLoadState.Loaded;
}
