using TenshoAssistant.Inventory;

// 0.1319 (X-320, user report of 2026-11-14, test T-0.351-1): gil changes left in « Other ». The cases of dalamud.log
// (docs/evidence/0.1319-gil-other.md): a FATE's reward (window FateReward), a challenge log entry done (LogMessage 3780), a defeated
// monster's gil (LogMessage 557, only when nothing else tells); spendings next to them stay « Other »; the watcher hears the messages.
internal static class GilOther1319Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Now=new(2026,11,13,13,52,48,TimeSpan.Zero);
    private static readonly Dictionary<string,DateTimeOffset> NoWindow=[];

    private static GilCategory Gain(Dictionary<string,DateTimeOffset> windows,Dictionary<uint,DateTimeOffset>? messages,long delta=84)
        =>GilJournal.ForDelta(GilJournal.Classify(windows,Now,messages),delta);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil other 1319: the cases of the log: FATE reward, challenge log entry, defeated monster, and what stays « Other »",() =>
        {
            // 11/13 13:52:48: « +84, windows seen in the last 15 s: XBMMonsterBookDetail, …, FateReward, ContextMenu, ActionDetail ».
            Check(Gain(new(){["FateReward"]=Now.AddSeconds(-3),["ActionDetail"]=Now},new(){[557]=Now.AddSeconds(-1)})==GilCategory.Fate,"FATE reward, among defeats");
            // 11/12 15:31:42: « +1,000, windows: Synthesis, RecipeNote » with the challenge log message at the same second.
            Check(Gain(new(){["Synthesis"]=Now.AddSeconds(-2),["RecipeNote"]=Now.AddSeconds(-2)},new(){[3780]=Now},1000)==GilCategory.ChallengeLog,"challenge log, synthesis entry");
            // 11/13 14:17:28: « Compagnon : ennemis vaincus II » and « Carnet d'objectifs II » done: +12,000, with defeats around.
            Check(Gain(NoWindow,new(){[3780]=Now,[557]=Now},12000)==GilCategory.ChallengeLog,"challenge log wins over a defeat");
            // 11/11 22:39:37: « Vous avez vaincu le chasse-nuages. » then « Vous obtenez 63 gils. », windows Tooltip, ActionDetail.
            Check(Gain(new(){["Tooltip"]=Now,["ActionDetail"]=Now},new(){[557]=Now.AddSeconds(-1)},63)==GilCategory.Defeat,"a defeated monster's gil");
            Check(Gain(new(){["Shop"]=Now.AddSeconds(-10)},new(){[557]=Now.AddSeconds(-1)},63)==GilCategory.NpcShop,"a known window wins over a defeat");
            Check(Gain(NoWindow,new(){[557]=Now.AddSeconds(-6)},63)==GilCategory.Other,"a defeat counts 5 seconds");
            Check(Gain(NoWindow,new(){[3780]=Now.AddSeconds(-6)},1000)==GilCategory.Other,"so does the challenge log");
            Check(Gain(NoWindow,null,12000)==GilCategory.Other && Gain(new(){["NowLoading"]=Now},null,12000)==GilCategory.Other,"unknown cases stay « Other » (+12,000 after a loading screen)");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>{["FateReward"]=Now.AddSeconds(-16)},Now)==GilCategory.Other,"the FATE window counts 15 seconds");
        }),
        ("Gil other 1319: spendings next to a FATE, a defeat or the challenge log stay « Other »; teleports and shops unchanged",() =>
        {
            Check(Gain(new(){["FateReward"]=Now},null,-500)==GilCategory.Other,"FATE spending");
            Check(Gain(NoWindow,new(){[557]=Now},-500)==GilCategory.Other && Gain(NoWindow,new(){[3780]=Now},-500)==GilCategory.Other,"defeat, challenge log spending");
            Check(Gain(new(){["NowLoading"]=Now},null,-1240)==GilCategory.Teleport && Gain(new(){["NowLoading"]=Now},null,1240)==GilCategory.Other,"teleport as before");
            Check(Gain(new(){["Shop"]=Now},null,-300)==GilCategory.NpcShop && Gain(new(){["Shop"]=Now},null,300)==GilCategory.NpcShop,"buying and selling to a vendor");
            Check(Gain(new(){["Repair"]=Now},null,-1000)==GilCategory.Repair,"repair");
            Check(GilJournal.Messages.SequenceEqual([(3780u,GilCategory.ChallengeLog),(557u,GilCategory.Defeat)]),"the two messages");
        }),
        ("Gil other 1319: added last (saved numbers kept), named in both languages, an icon each, the watcher hears the messages",() =>
        {
            Check((int)GilCategory.OceanFishing==8 && (int)GilCategory.Fate==9 && (int)GilCategory.Defeat==10 && (int)GilCategory.ChallengeLog==11,"numbers");
            Check(!GilJournal.IsTransfer(GilCategory.Fate) && !GilJournal.IsTransfer(GilCategory.Defeat) && !GilJournal.IsTransfer(GilCategory.ChallengeLog),"not transfers");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var category in Enum.GetValues<GilCategory>())
                Check(en.Contains($"    \"Category{category}\": ") && fr.Contains($"    \"Category{category}\": "),"named: "+category);
            var page=Read("src/TenshoAssistant/MainWindow.GilJournal.cs");
            foreach(var category in new[]{"Fate","Defeat","ChallengeLog"})Check(page.Contains($"GilCategory.{category}=>Dalamud.Interface.FontAwesomeIcon."),"icon: "+category);
            var watcher=Read("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs");
            Check(watcher.Contains("if(chat is not null)chat.LogMessage+=OnLogMessage;") && watcher.Contains("if(chat is not null)chat.LogMessage-=OnLogMessage;\n        TenshoAssistant.Competitive.CompetitiveGate.Remove"),"subscribed, and removed on dispose");
            Check(watcher.Contains("GilJournal.ForDelta(GilJournal.Classify(seen,at,heard),delta)"),"the messages heard classify");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("()=>mainWindow.DeveloperMode,chatGui),()=>gilWatcher?.Dispose());"),"the plugin gives the chat");
            Check(Read("docs/evidence/0.1319-gil-other.md").Contains("| 3780 | 57 |"),"evidence");
        }),
    ];
}
