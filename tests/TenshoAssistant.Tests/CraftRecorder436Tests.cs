using TenshoAssistant.Craft;

// A-14 (0.436): rotation recorder — a read-only hook on UseAction, only while recording.
internal static class CraftRecorder436Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Recorder: only while recording, model actions in order, others counted, 100 at most",() =>
        {
            var recording=new CraftRecording();
            recording.Observe(CraftActionKind.BasicTouch);
            Check(recording.Snapshot().Actions.Count==0,"not recording");
            recording.Start();recording.Observe(CraftActionKind.MuscleMemory);recording.Observe(null);recording.Observe(CraftActionKind.Veneration);
            var (actions,ignored)=recording.Snapshot();
            Check(actions.SequenceEqual([CraftActionKind.MuscleMemory,CraftActionKind.Veneration]) && ignored==1,"order and ignored");
            recording.Stop();recording.Observe(CraftActionKind.BasicTouch);
            Check(recording.Snapshot().Actions.Count==2,"stopped");
            recording.Start();for(var index=0;index<150;index++)recording.Observe(CraftActionKind.BasicSynthesis);
            Check(recording.Snapshot().Actions.Count==CraftRecording.MaxActions && recording.Snapshot().Ignored==50,"restart clears; bounded");
        }),
        ("Recorder: address checked, game function called first and its result returned, hook only while recording",() =>
        {
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Craft/CraftRecorder.Dalamud.cs"));
            Check(source.Contains("(nint)ActionManager.MemberFunctionPointers.UseAction==0)return false;"),"address checked");
            var detour=source[source.IndexOf("private bool OnUseAction(",StringComparison.Ordinal)..];
            Check(detour.IndexOf("hook!.Original(",StringComparison.Ordinal)<detour.IndexOf("try",StringComparison.Ordinal) && detour.Contains("return result;"),"original first, result unchanged");
            Check(source.Contains("hook.Enable();") && source.Contains("hook?.Disable();") && source.Contains("hook?.Dispose();"),"enabled on Start, disabled on Stop, disposed");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("()=>craftRecorder?.Dispose()"),"disposed with the plugin");
        }),
    ];
}
