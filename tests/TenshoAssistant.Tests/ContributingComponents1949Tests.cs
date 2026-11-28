// 0.1949 (X-607, user request of 18/11: a short page « add a page to Tensho » for a human). CONTRIBUTING's « Adding a page » names every
// shared component a page is asked to use, in both halves, so that a human who reads it finds the component before writing the form by hand.
internal static class ContributingComponents1949Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Contributing 1949: « Adding a page » names the shared components in both halves",()=>
        {
            var text=File.ReadAllText(Path.GetFullPath("CONTRIBUTING.md")).Replace("\r\n","\n");
            var english=text[text.IndexOf("### Adding a page: where everything is declared",StringComparison.Ordinal)..text.IndexOf("## Help, videos and guides",StringComparison.Ordinal)];
            var french=text[text.IndexOf("### Ajouter une page : où tout se déclare",StringComparison.Ordinal)..text.IndexOf("## Aide, vidéos et guides",StringComparison.Ordinal)];
            foreach(var name in new[]{"DetailTabs","BeginDataTable","HeadersRow","EmptyState","StateLine","DrawDataError","Tip(","TipText","ItemTip","InputWithHint","FilterCombo","CopyButton","BeginFramed","Widths.","BoolSetting","ToolButton","DrawGameIcon"})
            {
                Check(english.Contains(name,StringComparison.Ordinal),"English half: "+name);
                Check(french.Contains(name,StringComparison.Ordinal),"French half: "+name);
            }
        }),
    ];
}
