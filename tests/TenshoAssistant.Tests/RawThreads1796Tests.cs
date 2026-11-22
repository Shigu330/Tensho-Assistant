// 0.1796 (X-783): the tests' process fell once (« tests exit 127 », a trace ending in Thread.StartCallback, 22/11 during relay 177): an
// exception on a raw thread (new Thread) ends the whole process, every test with it, where the runner would have kept it for its test.
// Every raw thread a test starts keeps its exception for the test (a catch (Exception …) in its body, checked after the join), as
// FairLock1309's busy store did since 0.1794; the runner's own threads (TestRunner.cs) catch per test. A ratchet over the test files.
// 0.1800 (X-793, review of 0.1799, M3): the ratchet took any « catch (Exception » in the body, a catch around part of the work too, and never
// looked whether the kept exception was read: now the whole body is one try with an unfiltered catch (Exception name) keeping it (KeptIn), and
// a Check reads it after the thread's Join (Unchecked; RawThreads1800Tests).
using System.Text.RegularExpressions;

internal static class RawThreads1796Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly string Needle = "new " + "Thread(";

    /// <summary>The raw threads of a source: (line, body, position) for each « new Thread( » in code (not in a string or a comment), its body
    /// the text up to the matching parenthesis.</summary>
    internal static List<(int Line,string Body,int At)> Threads(string source)
    {
        var found = new List<(int,string,int)>();
        var code = Code(source);
        for (var at = code.IndexOf(Needle,StringComparison.Ordinal); at >= 0; at = code.IndexOf(Needle,at + 1,StringComparison.Ordinal))
        {
            var open = at + Needle.Length - 1;
            var depth = 0;
            var end = open;
            for (; end < code.Length; end++)
            {
                if (code[end] == '(') depth++;
                else if (code[end] == ')' && --depth == 0) break;
            }
            var line = 1 + source.AsSpan(0,at).Count('\n');
            found.Add((line,source.Substring(open,Math.Min(end,source.Length - 1) - open + 1),at));
        }
        return found;
    }

    /// <summary>The source with its strings, characters and comments blanked out (same length, same lines), so that only code is searched.</summary>
    internal static string Code(string source)
    {
        var text = source.ToCharArray();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n') text[i++] = ' ';
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var close = source.IndexOf("*/",i + 2,StringComparison.Ordinal);
                var stop = close < 0 ? text.Length : close + 2;
                for (; i < stop; i++) if (text[i] != '\n') text[i] = ' ';
            }
            else if (c == '"' || c == '\'')
            {
                var verbatim = c == '"' && i > 0 && (source[i - 1] == '@' || (i > 1 && source[i - 1] == '$' && source[i - 2] == '@'));
                i++;
                while (i < text.Length)
                {
                    if (!verbatim && text[i] == '\\') { text[i] = ' '; if (i + 1 < text.Length && text[i + 1] != '\n') text[i + 1] = ' '; i += 2; continue; }
                    if (text[i] == c)
                    {
                        if (verbatim && i + 1 < text.Length && text[i + 1] == '"') { text[i] = ' '; text[i + 1] = ' '; i += 2; continue; }
                        break;
                    }
                    if (text[i] == '\n' && !verbatim) break;
                    text[i] = ' ';
                    i++;
                }
                i++;
            }
            else i++;
        }
        return new string(text);
    }

    internal static bool KeepsItsException(string body) => KeptIn(body) is not null;

    /// <summary>X-793 (0.1800, M3): where a raw thread's body keeps its exception, else null. Kept only when the lambda's whole block is one try
    /// statement (nothing before or after it) with a catch (Exception name), not filtered (no when), whose block stores that exception: the
    /// target's name (kept = error, box.Value = error, ref unexpected, errors.Add(error)).</summary>
    internal static string? KeptIn(string body)
    {
        var code = Code(body);
        var arrow = code.IndexOf("=>",StringComparison.Ordinal);
        if (arrow < 0) return null;
        var open = Skip(code,arrow + 2);
        var close = Closing(code,open,'{','}');
        if (close < 0) return null;
        var after = Skip(code,close + 1);
        if (after >= code.Length || (code[after] != ')' && code[after] != ',')) return null;
        var at = Skip(code,open + 1);
        if (!Word(code,at,"try")) return null;
        var tried = Skip(code,at + 3);
        var end = Closing(code,tried,'{','}');
        if (end < 0 || end > close) return null;
        at = end + 1;
        string? kept = null;
        while (true)
        {
            at = Skip(code,at);
            if (Word(code,at,"catch"))
            {
                var next = Skip(code,at + 5);
                string? name = null;
                if (next < close && code[next] == '(')
                {
                    var shut = Closing(code,next,'(',')');
                    if (shut < 0) return null;
                    var declared = code[(next + 1)..shut].Split((char[])[' ','\t','\r','\n'],StringSplitOptions.RemoveEmptyEntries);
                    if (declared.Length == 2 && declared[0] is "Exception" or "System.Exception") name = declared[1];
                    next = Skip(code,shut + 1);
                }
                var filtered = Word(code,next,"when");
                if (filtered)
                {
                    var shut = Closing(code,Skip(code,next + 4),'(',')');
                    if (shut < 0) return null;
                    next = Skip(code,shut + 1);
                }
                var handler = Closing(code,next,'{','}');
                if (handler < 0 || handler > close) return null;
                if (name is not null && !filtered) kept ??= Target(code[(next + 1)..handler],name);
                at = handler + 1;
            }
            else if (Word(code,at,"finally"))
            {
                var shut = Closing(code,Skip(code,at + 7),'{','}');
                if (shut < 0 || shut > close) return null;
                at = shut + 1;
            }
            else break;
        }
        return at == close ? kept : null;
    }

    // The variable a catch block stores its exception in: the first « ref x », « x = », « x.Value = » or « x.Add( » of the block, when the
    // exception's name is used in it at all.
    private static string? Target(string block,string name)
    {
        if (!Regex.IsMatch(block,@"\b" + Regex.Escape(name) + @"\b")) return null;
        var use = Regex.Match(block,@"\bref\s+(?<t>[A-Za-z_]\w*)|\b(?<t>[A-Za-z_]\w*)(?:\.Value)?\s*=(?!=)|\b(?<t>[A-Za-z_]\w*)\.Add\(");
        return use.Success ? use.Groups["t"].Value : null;
    }

    private static readonly HashSet<string> Statements = new(StringComparer.Ordinal) { "if","for","foreach","while","using","lock","catch","when","switch","fixed" };

    /// <summary>X-793 (0.1800, M3): why the exception a raw thread keeps (<paramref name="target"/>) is not read after its Join, else null.
    /// In the method or lambda around the thread (at), after it: a Join( of a thread (not string.Join), then a Check( whose arguments name the
    /// target. A helper that starts the thread and stores into one of its parameters (FairLock1309's Greedy) is followed to each of its calls
    /// in the file, with the argument given there.</summary>
    internal static string? Unchecked(string source,int at,string target)
    {
        var code = Code(source);
        var (_,end,helper,parameters,declared) = Scope(code,at);
        var index = parameters.IndexOf(target);
        if (helper is null || index < 0)
            return CheckedAfter(code,at,end,target) ? null : target + " is not read by a Check after the thread's Join";
        var calls = Regex.Matches(code,@"(?<![\w.])" + Regex.Escape(helper) + @"\s*\(").Where(call => call.Index + call.Length - 1 != declared).ToList();
        if (calls.Count == 0) return "its helper " + helper + " is never called";
        foreach (var call in calls)
        {
            var paren = call.Index + call.Length - 1;
            var arguments = Arguments(code,paren);
            var given = index < arguments.Count ? Regex.Match(arguments[index],@"^\s*(?:ref\s+|out\s+)?(?<t>[A-Za-z_]\w*)") : Match.Empty;
            var line = 1 + source.AsSpan(0,call.Index).Count('\n');
            if (!given.Success) return "the call of " + helper + " at line " + line + " gives no variable for " + target;
            if (!CheckedAfter(code,call.Index,Scope(code,call.Index).End,given.Groups["t"].Value))
                return "the call of " + helper + " at line " + line + " does not read " + given.Groups["t"].Value + " by a Check after a Join";
        }
        return null;
    }

    private static bool CheckedAfter(string code,int from,int end,string name)
    {
        var join = Regex.Match(code[from..end],@"(?<![Ss]tring)\.Join\(");
        if (!join.Success) return false;
        var word = new Regex(@"\b" + Regex.Escape(name) + @"\b");
        var after = from + join.Index;
        foreach (Match check in Regex.Matches(code[after..end],@"\bCheck\("))
        {
            var paren = after + check.Index + check.Length - 1;
            var shut = Closing(code,paren,'(',')');
            if (shut > paren && word.IsMatch(code[paren..shut])) return true;
        }
        return false;
    }

    // The method or lambda block around a position: its braces, and for a method (or local function), its name, its parameters' names and the
    // position of its parenthesis; statement blocks (if, using, try, else…) are passed through outward. The whole source when none.
    private static (int Start,int End,string? Name,List<string> Parameters,int Declared) Scope(string code,int at)
    {
        var position = at;
        while (true)
        {
            var start = OpenBefore(code,position);
            if (start < 0) return (0,code.Length,null,[],-1);
            var end = Closing(code,start,'{','}');
            if (end < 0) end = code.Length;
            var before = Back(code,start - 1);
            if (before >= 1 && code[before] == '>' && code[before - 1] == '=') return (start,end,null,[],-1);
            if (before >= 0 && code[before] == ')')
            {
                var paren = OpeningBack(code,before);
                var nameEnd = Back(code,paren - 1);
                var nameStart = nameEnd;
                while (nameStart >= 0 && (char.IsLetterOrDigit(code[nameStart]) || code[nameStart] == '_')) nameStart--;
                var name = paren > 0 && nameEnd >= 0 ? code[(nameStart + 1)..(nameEnd + 1)] : "";
                if (name.Length > 0 && !Statements.Contains(name))
                {
                    var names = Split(code[(paren + 1)..before],true)
                        .Select(parameter => Regex.Match(parameter.Split('=')[0],@"(?<n>[A-Za-z_]\w*)\s*$"))
                        .Where(match => match.Success).Select(match => match.Groups["n"].Value).ToList();
                    return (start,end,name,names,paren);
                }
            }
            position = start;
        }
    }

    private static List<string> Arguments(string code,int paren)
    {
        var shut = Closing(code,paren,'(',')');
        return shut < 0 ? [] : Split(code[(paren + 1)..shut],false);
    }

    // Top-level pieces between commas (parentheses, brackets and braces counted; angle brackets too for a parameter list's generic types).
    private static List<string> Split(string text,bool angles)
    {
        var pieces = new List<string>();
        var depth = 0;
        var from = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '(' or '[' or '{' || (angles && c == '<')) depth++;
            else if (c is ')' or ']' or '}' || (angles && c == '>')) depth--;
            else if (c == ',' && depth == 0) { pieces.Add(text[from..i]); from = i + 1; }
        }
        if (text[from..].Trim().Length > 0) pieces.Add(text[from..]);
        return pieces;
    }

    private static int Skip(string code,int at)
    {
        while (at < code.Length && char.IsWhiteSpace(code[at])) at++;
        return at;
    }

    private static int Back(string code,int at)
    {
        while (at >= 0 && char.IsWhiteSpace(code[at])) at--;
        return at;
    }

    private static bool Word(string code,int at,string word) =>
        at >= 0 && at + word.Length <= code.Length && string.CompareOrdinal(code,at,word,0,word.Length) == 0
        && (at + word.Length == code.Length || !(char.IsLetterOrDigit(code[at + word.Length]) || code[at + word.Length] == '_'))
        && (at == 0 || !(char.IsLetterOrDigit(code[at - 1]) || code[at - 1] == '_'));

    // The matching closing bracket of the one at « at » (-1 when « at » is not that opening bracket or it is never closed).
    private static int Closing(string code,int at,char open,char close)
    {
        if (at < 0 || at >= code.Length || code[at] != open) return -1;
        var depth = 0;
        for (var i = at; i < code.Length; i++)
        {
            if (code[i] == open) depth++;
            else if (code[i] == close && --depth == 0) return i;
        }
        return -1;
    }

    private static int OpeningBack(string code,int at)
    {
        var depth = 0;
        for (var i = at; i >= 0; i--)
        {
            if (code[i] == ')') depth++;
            else if (code[i] == '(' && --depth == 0) return i;
        }
        return -1;
    }

    // The innermost « { » whose block holds the position (-1: none).
    private static int OpenBefore(string code,int at)
    {
        var depth = 0;
        for (var i = at - 1; i >= 0; i--)
        {
            if (code[i] == '}') depth++;
            else if (code[i] == '{') { if (depth == 0) return i; depth--; }
        }
        return -1;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raw threads 1796: the scan finds a raw thread in code only, with its whole body, and tells one that keeps its exception",() =>
        {
            var source = "var a = new Thread(() => { Work(\"(\"); });\n"
                + "// var b = new Thread(() => Crash());\n"
                + "Check(text.Contains(\"var worker=new Thread(\"),\"wiring\");\n"
                + "var c = new Thread(() =>\n{\n    try { Work(')'); }\n    catch (Exception error) { kept = error; }\n}) { IsBackground = true };\n";
            var threads = Threads(source);
            Check(threads.Count == 2,"two threads in code (not the comment nor the string): " + threads.Count);
            Check(threads[0].Line == 1 && threads[0].Body == "(() => { Work(\"(\"); })" && !KeepsItsException(threads[0].Body),"the first, unguarded: " + threads[0].Body);
            Check(threads[1].Line == 4 && threads[1].Body.EndsWith("catch (Exception error) { kept = error; }\n})",StringComparison.Ordinal)
                && KeepsItsException(threads[1].Body),"the second, guarded: " + threads[1].Body);
        }),
        ("Raw threads 1796: behaviour: a raw thread's exception kept for the test reaches the test, the process goes on",() =>
        {
            Exception? kept = null;
            var thread = new Thread(() =>
            {
                try { throw new IOException("the lock was not given in time"); }
                catch (Exception error) { kept = error; }
            }) { IsBackground = true };
            thread.Start();
            Check(thread.Join(TimeSpan.FromSeconds(10)),"ended");
            Check(kept is IOException { Message: "the lock was not given in time" },"kept for the test: " + kept);
        }),
        ("Raw threads 1796: every raw thread a test starts keeps its exception for the test (a crash on it ends every test: X-783)",() =>
        {
            var folder = Path.GetFullPath("tests/TenshoAssistant.Tests");
            var bare = new List<string>();
            var unread = new List<string>();
            var count = 0;
            foreach (var file in Directory.GetFiles(folder,"*.cs").Order(StringComparer.Ordinal))
            {
                if (Path.GetFileName(file) == "TestRunner.cs") continue;   // its threads run the tests, each one caught (RunGroup)
                var text = File.ReadAllText(file);
                foreach (var (line,body,at) in Threads(text))
                {
                    count++;
                    // 0.1800 (X-793, M3): the whole body guarded, and the kept exception read after the Join.
                    if (KeptIn(body) is not { } kept) bare.Add(Path.GetFileName(file) + ":" + line);
                    else if (Unchecked(text,at,kept) is { } why) unread.Add(Path.GetFileName(file) + ":" + line + " (" + why + ")");
                }
            }
            Check(count >= 7,"the test threads found (" + count + "): the scan reads the files");
            Check(bare.Count == 0,"raw threads whose exception would end the test process: " + string.Join(", ",bare));
            Check(unread.Count == 0,"raw threads whose kept exception no test reads after the Join: " + string.Join(", ",unread));
            var runner = File.ReadAllText(Path.Combine(folder,"TestRunner.cs")).ReplaceLineEndings("\n");
            Check(runner.Contains("try { run(); }\n            catch (SkipTestException skip) { skipped = skip.Message; }\n            catch (Exception failure) { error = failure.ToString(); }",StringComparison.Ordinal),
                "the runner's threads catch each test's exception");
        }),
    ];
}
