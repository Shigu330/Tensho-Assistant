using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

// 0.915 (X-76 step 2b, harmonisation H5 + H6): one shape for a setting row. Saved settings go through the shared helpers (BoolSetting,
// IntSetting, SliderSetting, ComboSetting, DiscordText, and the map's SettingBox / AreaSetting), each with its tooltip and one « ↺ »
// button (DefaultButton); every tooltip of these helpers ends with a last line naming the default, « Default: … . » in English and
// « Par défaut : … . » in French, and the on/off ones name the value a fresh Configuration really has.
internal static class SettingRows915Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");
    private static string Source(string file)=>File.ReadAllText(Path.Combine(Root,file));
    private static IEnumerable<(string Name,string Text)> Windows()
        =>Directory.EnumerateFiles(Root,"MainWindow*.cs",SearchOption.TopDirectoryOnly).OrderBy(path=>path,StringComparer.Ordinal)
            .Select(path=>(Path.GetFileName(path),File.ReadAllText(path)));

    private static readonly Dictionary<string,JsonDocument> Languages=[];
    private static string? Text(string language,string key)
    {
        if(!Languages.TryGetValue(language,out var document))
            Languages[language]=document=JsonDocument.Parse(Source("Localization/Languages/"+language+".json"));
        var dot=key.IndexOf('.');
        if(dot<0 || !document.RootElement.TryGetProperty(key[..dot],out var section) || section.ValueKind!=JsonValueKind.Object
            || !section.TryGetProperty(key[(dot+1)..],out var value) || value.ValueKind!=JsonValueKind.String)return null;
        return value.GetString();
    }

    // The top-level arguments of the call whose « ( » is at open (string literals and nested brackets skipped), regex only, no Roslyn.
    private static List<string> Arguments(string text,int open)
    {
        var found=new List<string>();var depth=0;var start=open+1;
        for(var index=open;index<text.Length;index++)
        {
            var character=text[index];
            if(character=='"')
            {
                for(index++;index<text.Length && text[index]!='"';index++)if(text[index]=='\\')index++;
                continue;
            }
            if(character is '(' or '[' or '{')depth++;
            else if(character is ')' or ']' or '}')
            {
                if(--depth==0){found.Add(Regex.Replace(text[start..index].Trim(),@"\s+"," "));return found;}
            }
            else if(character==',' && depth==1){found.Add(Regex.Replace(text[start..index].Trim(),@"\s+"," "));start=index+1;}
        }
        return found;
    }

    private static readonly Regex Helper=new(@"(?<![\w.])(BoolSetting|IntSetting|SliderSetting|ComboSetting|DiscordText|SettingBox|AreaSetting)(?:<[^>()]*>)?\(");
    private static readonly Regex Declaration=new(@"(private|static|bool|void)\s+$");

    /// <summary>Every call of a setting helper in the main window: the helper, its tooltip key, its arguments and where it is.</summary>
    private static List<(string Helper,string Tip,List<string> Arguments,string Where)> Rows()
    {
        var rows=new List<(string,string,List<string>,string)>();
        foreach(var (name,text) in Windows())
            foreach(Match match in Helper.Matches(text))
            {
                if(Declaration.IsMatch(text[Math.Max(0,match.Index-30)..match.Index]))continue;
                var arguments=Arguments(text,match.Index+match.Length-1);
                var where=name+":"+(text[..match.Index].Count(character=>character=='\n')+1);
                var helper=match.Groups[1].Value;
                if(helper is "SettingBox" or "AreaSetting")
                {
                    // The map rows: their tooltip is the key + « Help » (SettingBox draws it only when the text exists).
                    Check(arguments.Count>0 && arguments[0].StartsWith('"'),where+": the map row's key is a literal");
                    var help=arguments[0].Trim('"')+"Help";
                    if(helper=="AreaSetting" || Text("en",help) is not null)rows.Add((helper,help,arguments,where));
                    continue;
                }
                Check(arguments.Count>1 && arguments[1].StartsWith('"'),where+": "+helper+" has a literal tooltip key");
                rows.Add((helper,arguments[1].Trim('"'),arguments,where));
            }
        return rows;
    }

    private static readonly Regex EnglishDefault=new(@"\nDefault: [^\n]+\.$");
    private static readonly Regex FrenchDefault=new(@"\nPar défaut : [^\n]+\.$");

    // The value of a default written as « c=>c.A.B » (BoolSetting) read on a fresh Configuration, or null when written otherwise.
    private static bool? Fresh(object root,string path)
    {
        object? node=root;
        foreach(var part in path.Split('.'))
        {
            if(node is null)return null;
            var property=node.GetType().GetProperty(part,BindingFlags.Public|BindingFlags.Instance);
            if(property is null)return null;
            node=property.GetValue(node);
        }
        return node as bool?;
    }

    // Raw checkboxes bound to a saved setting that stay outside the helpers, and why (file and the ref variable).
    private static readonly HashSet<string> RawExceptions=new(StringComparer.Ordinal)
    {
        // The alarm options are edited on a copy and saved together (X-76 H3), several rows greyed by the others.
        "MainWindow.Alarms.cs|play","MainWindow.Alarms.cs|chat","MainWindow.Alarms.cs|onTime","MainWindow.Alarms.cs|mute",
        "MainWindow.Alarms.cs|muteChat","MainWindow.Alarms.cs|useGame","MainWindow.Alarms.cs|gameWeather",
        // Ticks in saved lists (one entry per achievement, tune, home line or tile, reminder row), not on/off settings.
        "MainWindow.AchievementProgress.cs|tracked","MainWindow.OrchestrionPlayer.cs|favorite","MainWindow.HomeToday.cs|shown",
        "MainWindow.Reminders.cs|inBar","MainWindow.Reminders.cs|toDiscord",
        // The gathering help also saves the gathering observation, in one save rolled back together (SaveGatheringChoice).
        "MainWindow.Settings.cs|gatheringPanel",
    };
    private static readonly Regex RawCheckbox=new(@"ImGui\.Checkbox\([^;]*?,\s*ref\s+(\w+)\)");
    private static readonly Regex Member=new(@"^\s{4}(private|internal|public|protected)\b");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Setting rows: every tooltip of a setting helper ends with its « Default » line, in English and in French",() =>
        {
            var rows=Rows();
            Check(rows.Count>=200,"the scan found the setting rows: "+rows.Count);
            var missing=new SortedSet<string>(StringComparer.Ordinal);
            foreach(var (helper,tip,_,where) in rows)
            {
                if(Text("en",tip) is not { } english || !EnglishDefault.IsMatch(english))missing.Add("en "+tip+" ("+helper+", "+where+")");
                if(Text("fr",tip) is not { } french || !FrenchDefault.IsMatch(french))missing.Add("fr "+tip+" ("+helper+", "+where+")");
            }
            Check(missing.Count==0,"tooltips without their last « Default » line: "+string.Join("; ",missing));
        }),
        ("Setting rows: the on/off default named in a tooltip is the value of a fresh Configuration",() =>
        {
            var fresh=new TenshoAssistant.Configuration();var map=fresh.MapView;
            var checkedRows=0;var wrong=new List<string>();
            foreach(var (helper,tip,arguments,where) in Rows())
            {
                bool? value=null;
                if(helper=="BoolSetting" && arguments.Count>4 && Regex.Match(arguments[4],@"^c ?=> ?c\.([\w.]+)$") is { Success:true } path)value=Fresh(fresh,path.Groups[1].Value);
                else if(helper=="SettingBox" && arguments.Count>1 && Regex.Match(arguments[1],@"^settings\.(\w+)$") is { Success:true } setting)value=Fresh(map,setting.Groups[1].Value);
                if(value is not { } on)continue;
                checkedRows++;
                var english=Text("en",tip)!.Split('\n')[^1];var french=Text("fr",tip)!.Split('\n')[^1];
                if(!english.StartsWith(on ? "Default: on":"Default: off",StringComparison.Ordinal)
                    || !french.StartsWith(on ? "Par défaut : activé":"Par défaut : désactivé",StringComparison.Ordinal))
                    wrong.Add(tip+" ("+where+", "+(on ? "on":"off")+")");
            }
            Check(checkedRows>=150,"on/off rows compared: "+checkedRows);
            Check(wrong.Count==0,"tooltips naming another default: "+string.Join("; ",wrong));
        }),
        ("Setting rows: no raw checkbox bound to a saved setting outside the shared helpers (exceptions listed)",() =>
        {
            var found=new SortedSet<string>(StringComparer.Ordinal);
            foreach(var (name,text) in Windows())
            {
                var lines=text.Split('\n');
                for(var index=0;index<lines.Length;index++)
                    foreach(Match match in RawCheckbox.Matches(lines[index]))
                    {
                        var start=index;
                        while(start>0 && !Member.IsMatch(lines[start]))start--;
                        var body=string.Join('\n',lines[start..(index+1)]);
                        // The variable is read from the configuration, directly or through a local taken from it (var options=configuration.…).
                        var sources=new List<string>{"configuration"};
                        foreach(Match local in Regex.Matches(body,@"\bvar\s+(\w+)\s*=\s*configuration\."))sources.Add(local.Groups[1].Value);
                        var variable=match.Groups[1].Value;
                        if(lines[index].Contains("configuration.",StringComparison.Ordinal)
                            || Regex.IsMatch(body,@"\b"+variable+@"\s*=\s*("+string.Join('|',sources)+@")\."))found.Add(name+"|"+variable);
                    }
            }
            var unexpected=found.Except(RawExceptions).ToArray();var stale=RawExceptions.Except(found).ToArray();
            Check(unexpected.Length==0,"use BoolSetting (or add the exception with its reason): "+string.Join(", ",unexpected));
            Check(stale.Length==0,"exceptions no longer found, remove them: "+string.Join(", ",stale));
        }),
        ("Setting rows: one « ↺ » helper; BoolSetting needs its default; the combos bound to a setting use ComboSetting",() =>
        {
            var row=Source("UI/Components/SettingRow.cs");
            Check(row.Contains("private void BoolSetting(string labelKey,string tipKey,Func<bool> read,Action<bool> write,Func<Configuration,bool> defaultOf)",StringComparison.Ordinal),"the default is required");
            Check(row.Contains("private bool DefaultButton(string id,bool differs,string? defaultName=null)",StringComparison.Ordinal)
                && row.Contains("private void ComboSetting<T>(",StringComparison.Ordinal),"one reset button, the combo row");
            Check(Windows().All(file=>!file.Text.Contains("BoolDefaultButton",StringComparison.Ordinal)) && !row.Contains("BoolDefaultButton",StringComparison.Ordinal),"the on/off button is merged into DefaultButton");
            foreach(var (file,call) in new[]
            {
                ("MainWindow.Plots.cs","ComboSetting(\"Plots.Size\",\"Plots.SizeTip\","),("MainWindow.HuntTrain.cs","ComboSetting(\"HuntCallChannel\",\"HuntTrain.ChannelTip\","),
                ("MainWindow.Settings.cs","ComboSetting(\"Settings.MarketScope\",\"Settings.MarketScopeTip\","),("MainWindow.Settings.cs","ComboSetting(\"ModuleSettings.MacroBook\",\"SettingTips.MacroBook\","),
                ("MainWindow.Reminders.cs","BoolSetting(\"Reminders.FromGame\",\"Reminders.FromGameTip\","),
            })
                Check(Source(file).Contains(call,StringComparison.Ordinal),file+": "+call);
            Check(!Source("MainWindow.Plots.cs").Contains("ImGui.Combo(",StringComparison.Ordinal) && !Source("MainWindow.HuntTrain.cs").Contains("ImGui.BeginCombo(",StringComparison.Ordinal),"no hand-made combo left there");
        }),
        ("Setting rows: a help text is shown once; one tooltip for the map replacement; « Show translation keys » in one place",() =>
        {
            var settings=Source("MainWindow.Settings.cs");
            foreach(var key in new[]{"Settings.OpenOnLoadHelp","Settings.OpenOnLoginHelp","BankUi.AutoSettingHelp"})
                Check(!settings.Contains("localization.Get(\""+key+"\")",StringComparison.Ordinal),"tooltip only, not also written under the row: "+key);
            Check(!Source("MainWindow.GatheringGp.cs").Contains("localization.Get(\"GpUi.FirstHelp\")",StringComparison.Ordinal),"GP first bonus: tooltip only");
            Check(Source("MainWindow.Guide.cs").Contains("BoolSetting(\"MapView.ReplaceGameMap\", \"MapView.ReplaceGameMapHelp\",",StringComparison.Ordinal)
                && Text("en","Guide.ReplaceMapTip") is null && Text("fr","Guide.ReplaceMapTip") is null,"the guide and the map settings share one tooltip");
            var keys=Windows().Where(file=>file.Text.Contains("\"Developer.ShowKeys\"",StringComparison.Ordinal)).Select(file=>file.Name).ToArray();
            Check(keys.SequenceEqual(["MainWindow.Advanced.cs"]),"« Show translation keys » drawn in: "+string.Join(", ",keys));
        }),
    ];
}
