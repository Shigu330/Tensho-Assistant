using TenshoAssistant.UI;

// 0.1264 (X-156, first pages: Market and Stocks): where a value comes from (measured, estimate, old) is said the same way everywhere, and
// « old » is the « old data » state of a page itself. Market and Stocks say waiting, old and failed through StateLine; a cached read past
// its duration (the cache stamp of every page) is the « old data » state.
internal static class Origins1264Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Origins 1264: measured, estimate and old each have a word in both languages, a glyph and a colour; old is the stale state",() =>
        {
            var origins=Enum.GetValues<UiOrigin>();
            Check(origins.Length==3,"three origins");
            Check(origins.Select(UiOrigins.Key).Distinct().Count()==3 && origins.Select(UiOrigins.Icon).Distinct().Count()==3,"one word and one glyph each");
            Check(UiOrigins.Key(UiOrigin.Old)==UiStates.Key(UiState.Stale) && UiOrigins.Icon(UiOrigin.Old)==UiStates.Icon(UiState.Stale)
                && UiOrigins.Colour(UiOrigin.Old)==UiStates.Colour(UiState.Stale),"an old value is said like the old data state: one way to say old");
            Check(UiOrigins.Colour(UiOrigin.Measured)==UiColour.Muted && UiOrigins.Colour(UiOrigin.Estimated)==UiColour.Muted,"measured and estimate are not alarms: the glyph and the word tell them");
            foreach(var origin in origins)Check(UiOrigins.Icon(origin).All(char.IsAsciiLetter),$"{origin}: glyph named, not a character");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                var common=text[text.IndexOf("\n  \"Common\": {",StringComparison.Ordinal)..];
                common=common[..common.IndexOf("\n  },",StringComparison.Ordinal)];
                foreach(var origin in origins)Check(common.Contains("\n    \""+UiOrigins.Key(origin)["Common.".Length..]+"\": \""),$"{file}: {UiOrigins.Key(origin)}");
            }
        }),
        ("Origins 1264: the line and the mark draw the glyphs of the table; a measured value carries no mark",() =>
        {
            var lines=Read("src/TenshoAssistant/UI/Components/StatusLines.cs");
            var line=lines[lines.IndexOf("private void OriginLine(UiOrigin origin,string? detail=null)",StringComparison.Ordinal)..lines.IndexOf("private void OriginMark(",StringComparison.Ordinal)];
            Check(line.Contains("if(origin==UiOrigin.Old){StateLine(UiState.Stale,detail);return;}"),"an old origin is the stale line itself");
            Check(line.Contains("UiOrigin.Measured=>FontAwesomeIcon."+UiOrigins.Icon(UiOrigin.Measured)) && line.Contains("_=>FontAwesomeIcon."+UiOrigins.Icon(UiOrigin.Estimated)),"the glyphs of the table");
            Check(line.Contains("localization.Get(UiOrigins.Key(origin))"),"the word of the table");
            var mark=lines[lines.IndexOf("private void OriginMark(",StringComparison.Ordinal)..lines.IndexOf("private void GlyphLine(",StringComparison.Ordinal)];
            Check(mark.Contains("if(origin==UiOrigin.Measured)return;"),"no mark on a measured value");
            Check(mark.Contains("UiOrigin.Estimated=>FontAwesomeIcon."+UiOrigins.Icon(UiOrigin.Estimated)) && mark.Contains("_=>FontAwesomeIcon."+UiOrigins.Icon(UiOrigin.Old)),"the glyphs of the table");
            Check(mark.Contains("TipText("),"the word and the reason on hover: never the glyph alone");
            var state=lines[lines.IndexOf("private void StateLine(UiState state,string? detail=null)",StringComparison.Ordinal)..lines.IndexOf("private void OriginLine(",StringComparison.Ordinal)];
            Check(state.Contains("GlyphLine(icon,colour,localization.Get(UiStates.Key(state)),detail);"),"states and origins share one drawing");
        }),
        ("Origins 1264: Market says waiting and old as everywhere",() =>
        {
            var market=Read("src/TenshoAssistant/MainWindow.Market.cs");
            Check(market.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"MarketUi.Loading\"))"),"summary: waiting");
            Check(market.Contains("StateLine(UI.UiState.Stale,localization.Get(\"MarketUi.Expired\"))") && market.Contains("StateLine(UI.UiState.Stale,localization.Get(\"MarketUi.Old\"))"),"summary: cache expired and old upload are old data");
            Check(market.Contains("UI.UiComponents.BeginDataTable(\"MarketPrices\", 4)") && market.Contains("UI.UiComponents.NumberCell("),"summary: the common table, numbers to the right");
            Check(!market.Contains("ImGui.TextColored(Accent"),"no accent line written by hand");
            var board=Read("src/TenshoAssistant/MainWindow.MarketBoard.cs");
            Check(board.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"MarketUi.Loading\"))") && board.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"MarketBoard.Cooldown\"))"),"board: waiting");
            Check(board.Contains("StateLine(UI.UiState.Stale,localization.Format(state.Error is null ? \"MarketBoard.Waiting\":\"MarketBoard.StaleCache\""),"board: a cached board is old data, dated");
            Check(!board.Contains("ImGui.BeginTable("),"board: the common tables");
            var hover=Read("src/TenshoAssistant/MainWindow.MarketHover.cs");
            Check(hover.Contains("StateLine(UI.UiState.Stale,localization.Get(\"HoverUi.Old\"))") && hover.Contains("StateLine(UI.UiState.Stale,localization.Get(\"HoverUi.Expired\"))"),"hover prices: old data");
        }),
        ("Origins 1264: Stocks say waiting, old and failed as everywhere; an expired cache stamp is old data",() =>
        {
            var bank=Read("src/TenshoAssistant/MainWindow.Bank.cs");
            Check(bank.Split("StateLine(UI.UiState.Waiting,localization.Get(\"BankUi.Loading\"))").Length==3,"loading: waiting, on both views");
            Check(bank.Contains("StateLine(UI.UiState.Failed,localization.Get(\"ServiceStatus.StorageReadUnavailable\"))"),"a storage that could not be read: failed");
            Check(bank.Contains("StateLine(UI.UiState.Stale,localization.Get(\"BankUi.RetainerGone\"))"),"a retainer gone from the bell: its read is old data");
            foreach(var key in new[]{"OpenSaddlebag","OpenDresser","OpenArmoire","OpenBell","Waiting","Unavailable"})
                Check(bank.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"BankUi."+key+"\"))"),key+": waiting for a first read");
            Check(bank.Contains("StateLine(UI.UiState.Waiting,localization.Format(\"BankUi.OpenRetainer\",name))"),"a retainer never opened: waiting");
            Check(bank.Contains("UI.UiComponents.SubTitle(localization.Get(\"BankUi.Title\"))"),"the browser's title is a sub-title");
            var report=Read("src/TenshoAssistant/MainWindow.StorageReport.cs");
            Check(report.Contains("StateLine(UI.UiState.Failed,localization.Get(\"StorageReport.SetsUnavailable\"))") && report.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"StorageReport.SetsLoading\"))"),"report: failed or waiting");
            foreach(var file in new[]{"MainWindow.BankList.cs","MainWindow.RetainerSales.cs"})
                Check(!Read("src/TenshoAssistant/"+file).Contains("ImGui.BeginTable("),file+": the common table");
            var caching=Read("src/TenshoAssistant/MainWindow.Caching.cs");
            Check(caching.Contains("if(fresh)ImGui.TextColored(Muted,stamp);") && caching.Contains("else StateLine(UI.UiState.Stale,stamp);"),"cache stamp: valid is a muted line, expired is old data");
        }),
    ];
}
