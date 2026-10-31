using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private string bookmarkName = "";
    private Guid selectedBookmark;
    private string? bookmarkError;
    private bool bookmarkSaved;

    private bool SaveBookmarks(Func<IReadOnlyList<MapBookmark>, IReadOnlyList<MapBookmark>> edit)
    {
        try
        {
            MapBookmarkPersistence.Commit(configuration, save, edit);
            bookmarkError = null; bookmarkSaved = true; return true;
        }
        catch (Exception error)
        {
            bookmarkSaved = false; bookmarkError = "Common.SaveError";
            log.Warning(error, "Could not save map bookmarks."); return false;
        }
    }
    private void DrawMapBookmarks(MapCatalog catalog)
    {
        var points = configuration.MapBookmarks ?? [];
        if (!ImGui.CollapsingHeader(localization.Format("MapCounts.Bookmarks",points.Count)+"###MapBookmarks", ImGuiTreeNodeFlags.DefaultOpen)) return;
        ImGui.TextDisabled(localization.Get("MapsUi.BookmarkHelp"));
        if (points.Count > 0)
        {
            var visible = ImGui.BeginChild("MapBookmarkList", new Vector2(0, 150), true);
            try
            {
                if (visible)
                {
                    var remove = -1;
                    var clipper = ImGui.ImGuiListClipper();
                    try
                    {
                        clipper.Begin(points.Count);
                        while (clipper.Step())
                        for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                        {
                            var point = points[index];
                            var valid = MapBookmarks.TryResolve(point, catalog, out var map);
                            ImGui.PushID(index);
                            try
                            {
                                ImGui.BeginDisabled(!valid);
                                try
                                {
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    bool open;
                                    try { open = ImGui.Button(FontAwesomeIcon.MapMarkedAlt.ToIconString(), new Vector2(28, 24)); }
                                    finally { ImGui.PopFont(); }
                                    if (open && point is not null && map is not null)
                                    { SelectBookmark(point); OpenMapPoint(map, point.X, point.Y); }
                                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(localization.Get("MapsUi.BookmarkOpen"));
                                    ImGui.SameLine();
                                    if (ImGui.SmallButton(localization.Get("MapsUi.Load")) && point is not null) SelectBookmark(point);
                                }
                                finally { ImGui.EndDisabled(); }
                                ImGui.SameLine();
                                if (ImGui.SmallButton(localization.Get("MapsUi.Remove"))) remove = index;
                                ImGui.SameLine();
                                ImGui.TextUnformatted(string.IsNullOrWhiteSpace(point?.Name) ? localization.Get("MapsUi.Unnamed") : point.Name);
                                if (point is not null && ImGui.IsItemHovered())
                                    ImGui.SetTooltip(valid ? $"{map!.Zone} · {map.Subzone}\nX {point.X:F1} · Y {point.Y:F1}"
                                        : localization.Get("MapsUi.MissingBookmark"));
                            }
                            finally { ImGui.PopID(); }
                        }
                    }
                    finally { clipper.End(); clipper.Destroy(); }
                    if (remove >= 0 && SaveBookmarks(items => MapBookmarks.RemoveAt(items, remove))) selectedBookmark = Guid.Empty;
                }
            }
            finally { ImGui.EndChild(); }
        }
        if (bookmarkError is { } errorText)
        { ImGui.PushTextWrapPos(0); try { ImGui.TextUnformatted(localization.Get(errorText)); } finally { ImGui.PopTextWrapPos(); } }
        if (bookmarkSaved) ImGui.TextColored(Success, localization.Get("MapsUi.Saved"));
    }
    private void SelectBookmark(MapBookmark point)
    {
        selectedBookmark = point.Id; selectedMapId = point.MapId;
        mapX = point.X; mapY = point.Y; bookmarkName = point.Name ?? ""; mapOpenError = null;
    }
    private void DrawBookmarkEditor(MapDefinition selected)
    {
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##BookmarkName", localization.Get("MapsUi.Name"), ref bookmarkName, MapBookmarks.NameLimit * 4 + 1)) bookmarkSaved = false;
        string Name() => string.IsNullOrWhiteSpace(bookmarkName) ? $"{selected.Zone} · {mapX:F1}, {mapY:F1}" : bookmarkName;
        ImGui.BeginDisabled(!MapCatalog.CoordinatesValid(mapX, mapY));
        try
        {
            if (ImGui.Button(localization.Get("MapsUi.Add")))
                SaveBookmarks(items => MapBookmarks.Add(items, MapBookmarks.Create(selected, Name(), mapX, mapY)));
            if (selectedBookmark != Guid.Empty)
            {
                ImGui.SameLine();
                if (ImGui.Button(localization.Get("MapsUi.Update")))
                    SaveBookmarks(items => MapBookmarks.Replace(items, MapBookmarks.Create(selected, Name(), mapX, mapY) with { Id = selectedBookmark }));
            }
        }
        finally { ImGui.EndDisabled(); }
        ImGui.TextDisabled(localization.Get("MapsUi.Shared"));
    }
}
