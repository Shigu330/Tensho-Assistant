using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// Map bookmarks of the Maps page: named map points (zone + X/Y) kept in the configuration and shared by all characters. A row
// loads its coordinates into the editor or opens the game map there (flag); add / update / remove save at once. Zone access is
// not checked.
internal sealed partial class MainWindow
{
    private string bookmarkName = "";
    // 0.378 (g, M-08): note and group of the bookmark being edited, and the share code typed to import.
    private string bookmarkNote = "", bookmarkGroup = "", bookmarkImport = "";
    private string? bookmarkShareMessage;
    private Guid selectedBookmark;
    private string? bookmarkError;
    private bool bookmarkSaved;

    private bool SaveBookmarks(Func<IReadOnlyList<MapBookmark>, IReadOnlyList<MapBookmark>> edit, bool adding = false)
    {
        // 0.328: a refusal of the edit itself (name, duplicate, full list, bookmark gone; MapBookmarks throws before anything is saved)
        // says why, since trying again cannot help; Common.SaveError stays for a failed save.
        var editing = false;
        try
        {
            MapBookmarkPersistence.Commit(configuration, save, items => { editing = true; var edited = edit(items); editing = false; return edited; });
            bookmarkError = null; bookmarkSaved = true; return true;
        }
        catch (Exception error) when (editing && error is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            bookmarkSaved = false;
            bookmarkError = error switch
            {
                ArgumentOutOfRangeException or KeyNotFoundException => "MapsUi.BookmarkMissing",
                ArgumentException => "MapsUi.BookmarkInvalidName",
                _ => adding && (configuration.MapBookmarks?.Count ?? 0) >= MapBookmarks.Capacity ? "MapsUi.BookmarkFull" : "MapsUi.BookmarkDuplicate",
            };
            log.Debug(error, "Map bookmark refused."); return false;
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
        if (!TenshoAssistant.UI.UiComponents.Header(localization.Format("MapCounts.Bookmarks",points.Count)+"###MapBookmarks", ImGuiTreeNodeFlags.DefaultOpen)) return;
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
                                    { SelectBookmark(point); OpenMapPointFromPage(map, point.X, point.Y); }
                                    if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("MapsUi.BookmarkOpen"));
                                    ImGui.SameLine();
                                    if (ImGui.SmallButton(localization.Get("MapsUi.Load")) && point is not null) SelectBookmark(point);
                                }
                                finally { ImGui.EndDisabled(); }
                                ImGui.SameLine();
                                if (ImGui.SmallButton(localization.Get("MapsUi.Remove"))) remove = index;
                                Tip("MapsUi.RemoveTip");
                                ImGui.SameLine();
                                ImGui.TextUnformatted((point?.Group is { } group ? $"[{group}] " : "") + (string.IsNullOrWhiteSpace(point?.Name) ? localization.Get("MapsUi.Unnamed") : point.Name));
                                if (point is not null && ImGui.IsItemHovered())
                                {
                                    WrappedTooltip(valid ? $"{map!.Zone} · {map.Subzone}\nX {point.X:F1} · Y {point.Y:F1}" + (point.Note is { } note ? "\n" + note : "")
                                        : localization.Get("MapsUi.MissingBookmark"));
                                }
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
        DrawBookmarkShare();
    }
    // 0.378 (g, M-08): share the bookmarks as a TENSHO-MAP1 text code (clipboard), or add those of a code typed or pasted here.
    private void DrawBookmarkShare()
    {
        var points = configuration.MapBookmarks ?? [];
        ImGui.BeginDisabled(points.Count == 0);
        try
        {
            if (ImGui.SmallButton(localization.Get("MapsUi.ShareCopy")))
            { ImGui.SetClipboardText(MapBookmarks.ShareCode(points)); bookmarkShareMessage = localization.Format("MapsUi.ShareCopied", Math.Min(points.Count, MapBookmarks.ShareLimit)); }
        }
        finally { ImGui.EndDisabled(); }
        Tip("MapsUi.ShareCopyTip");
        ImGui.SameLine(); ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("##BookmarkImport", localization.Get("MapsUi.ShareHint"), ref bookmarkImport, 200_000);
        Tip("MapsUi.ShareHintTip");
        ImGui.SameLine();
        ImGui.BeginDisabled(bookmarkImport.Trim().Length == 0);
        try
        {
            if (ImGui.SmallButton(localization.Get("MapsUi.ShareImport")))
            {
                try
                {
                    var imported = MapBookmarks.ParseShareCode(bookmarkImport);
                    var added = 0; var skipped = 0;
                    if (SaveBookmarks(items => { var merged = MapBookmarks.Merge(items, imported); added = merged.Added; skipped = merged.Skipped; return merged.Bookmarks; }))
                    { bookmarkImport = ""; bookmarkShareMessage = localization.Format("MapsUi.ShareImported", added, skipped); }
                }
                catch (FormatException)
                {
                    // N-11 (0.437): not a share code: « Place ( x , y ) » lines (hunt sites, chat), matched to the zones by name.
                    var list = maps.State.Catalog is { } catalog
                        ? MapBookmarks.ParseCoordinateList(bookmarkImport, name => catalog.Entries.FirstOrDefault(map => map.Zone.Equals(name, StringComparison.CurrentCultureIgnoreCase)),
                            localization.Get("MapsUi.PastedGroup"))
                        : (Bookmarks: (IReadOnlyList<MapBookmark>)[], Unknown: 0);
                    if (list.Bookmarks.Count == 0) bookmarkShareMessage = localization.Get("MapsUi.ShareInvalid");
                    else
                    {
                        var added = 0; var skipped = 0;
                        if (SaveBookmarks(items => { var merged = MapBookmarks.Merge(items, list.Bookmarks); added = merged.Added; skipped = merged.Skipped; return merged.Bookmarks; }))
                        { bookmarkImport = ""; bookmarkShareMessage = localization.Format("MapsUi.PastedImported", added, skipped, list.Unknown); }
                    }
                }
            }
        }
        finally { ImGui.EndDisabled(); }
        Tip("MapsUi.ShareImportTip");
        if (bookmarkShareMessage is { } message) ImGui.TextDisabled(message);
    }
    private void SelectBookmark(MapBookmark point)
    {
        selectedBookmark = point.Id; selectedMapId = point.MapId; mapsTabRequest = 0;
        mapX = point.X; mapY = point.Y; bookmarkName = point.Name ?? ""; mapOpenError = null;
        bookmarkNote = point.Note ?? ""; bookmarkGroup = point.Group ?? "";
    }
    private void DrawBookmarkEditor(MapDefinition selected)
    {
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##BookmarkName", localization.Get("MapsUi.Name"), ref bookmarkName, MapBookmarks.NameLimit * 4 + 1)) bookmarkSaved = false;
        Tip("MapsUi.BookmarkNameTip");
        string Name() => string.IsNullOrWhiteSpace(bookmarkName) ? $"{selected.Zone} · {mapX:F1}, {mapY:F1}" : bookmarkName;
        // 0.378 (g, M-08): optional group and note.
        ImGui.SetNextItemWidth(160);
        if (ImGui.InputTextWithHint("##BookmarkGroup", localization.Get("MapsUi.Group"), ref bookmarkGroup, MapBookmarks.GroupLimit * 4 + 1)) bookmarkSaved = false;
        Tip("MapsUi.GroupTip");
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextMultiline("##BookmarkNote", ref bookmarkNote, MapBookmarks.NoteLimit * 4 + 1, new System.Numerics.Vector2(-1, 48))) bookmarkSaved = false;
        Tip("MapsUi.NoteTip");
        MapBookmark Edited(MapBookmark created) => created with { Note = MapBookmarks.CleanNote(bookmarkNote), Group = MapBookmarks.CleanGroup(bookmarkGroup) };
        ImGui.BeginDisabled(!MapCatalog.CoordinatesValid(mapX, mapY));
        try
        {
            if (ImGui.Button(localization.Get("MapsUi.Add")))
                SaveBookmarks(items => MapBookmarks.Add(items, Edited(MapBookmarks.Create(selected, Name(), mapX, mapY))), adding: true);
            if (selectedBookmark != Guid.Empty)
            {
                ImGui.SameLine();
                if (ImGui.Button(localization.Get("MapsUi.Update")))
                    SaveBookmarks(items => MapBookmarks.Replace(items, Edited(MapBookmarks.Create(selected, Name(), mapX, mapY)) with { Id = selectedBookmark }));
            }
        }
        finally { ImGui.EndDisabled(); }
        ImGui.TextDisabled(localization.Get("MapsUi.Shared"));
    }
}
