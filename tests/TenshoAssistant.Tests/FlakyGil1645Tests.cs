// 0.1645 (X-672): the A14 tests of the gil journal (AfterError1551Tests) failed once in CI under load on a 5 s bound; they wait on their
// signal now, with a guard of minutes that only stops a real hang. This keeps the file from getting a short bound back.
internal static class FlakyGil1645Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Flaky gil 1645: the « after error » tests wait for the writer and the exchange on their signal, with a guard of 2 min, never a bound of seconds",()=>
        {
            var text=File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/AfterError1551Tests.cs"));
            Check(text.Contains("private static readonly TimeSpan Guard=TimeSpan.FromMinutes(2);",StringComparison.Ordinal),"the guard");
            Check(!text.Contains("TimeSpan.FromSeconds(",StringComparison.Ordinal) && !text.Contains("Thread.Sleep(",StringComparison.Ordinal)
                && !System.Text.RegularExpressions.Regex.IsMatch(text,@"\.(Wait|Flush)\(\d"),"no bound of seconds, no sleep");
            Check(System.Text.RegularExpressions.Regex.Matches(text,@"writer\.Flush\(Guard\)").Count>=7 && text.Contains("SpinWait.SpinUntil(()=>!exchange.Busy,Guard);",StringComparison.Ordinal),"every wait on its signal");
        }),
    ];
}
