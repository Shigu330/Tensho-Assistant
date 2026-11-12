using System.Reflection;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-01 C1.3 (0.1187, the root of the leak of unloaded instances): what the log says, ten seconds after a load, about the instances loaded
/// before in the same game process: their load context alive or collected, and which named parts of each (the plugin object, the window,
/// the constructor's closures, and their fields that hold objects of the plugin's own code) are still alive. Weak references only, kept
/// in a process-wide slot made of runtime types only, so the watch itself never keeps an instance or its context alive; nothing of an
/// earlier instance is run, written, collected or forced.
/// Reading: a context that stays alive while its parts are collected costs little (code and static tables); parts that stay alive name what
/// still holds the instance. Roots found out of the game and readings: docs/evidence/0.1187-unload-roots.md.
/// 0.1196: the root was found and fixed (0.1194) and the fix read in the game (0.1195: « 0 held »), so the tools of the search are gone:
/// the emptying of old instances (0.1189) and the notes three levels deep (0.1192). One short line per earlier instance stays.
/// </summary>
public static class UnloadWatch
{
    /// <summary>Process-wide slot (AppDomain data): the instances loaded in this process, the newest first.</summary>
    public const string Slot="TenshoAssistant.X01.Instances";
    /// <summary>How many instances are kept, this one included.</summary>
    public const int Kept=5;
    /// <summary>The name of the load context among an instance's parts.</summary>
    public const string Context="context";
    /// <summary>0.1191: how the compiler names the field of a closure that holds the closure of the scope around it.</summary>
    public const string ClosureField="CS$<>8__locals";

    /// <summary>0.1191: the closure of the scope around a closure (the first field the compiler named so); null when there is none.</summary>
    public static object? OuterClosure(object? closure)
    {
        if(closure is null)return null;
        try
        {
            return closure.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                .FirstOrDefault(field=>field.Name.StartsWith(ClosureField,StringComparison.Ordinal))?.GetValue(closure);
        }
        catch(Exception error) when(error is FieldAccessException or TargetException or NotSupportedException){return null;}
    }

    /// <summary>This instance first (its context noted weakly), then the earlier ones read from the slot, <see cref="Kept"/> at most.</summary>
    public static List<KeyValuePair<string,Dictionary<string,WeakReference>>> Push(object? slot,string label,object? context)
    {
        var parts=new Dictionary<string,WeakReference>(StringComparer.Ordinal);
        if(context is not null)parts[Context]=new WeakReference(context);
        var all=new List<KeyValuePair<string,Dictionary<string,WeakReference>>>{new(label,parts)};
        if(slot is List<KeyValuePair<string,Dictionary<string,WeakReference>>> earlier)all.AddRange(earlier.Take(Kept-1));
        return all;
    }

    /// <summary>Whether a type comes from the given assembly, by itself, as an array's element or as a generic argument.</summary>
    public static bool Ours(Type type,Assembly assembly)=>
        type.Assembly==assembly || (type.IsArray && type.GetElementType() is { } element && Ours(element,assembly)) ||
        (type.IsGenericType && type.GetGenericArguments().Any(argument=>Ours(argument,assembly)));

    /// <summary>
    /// 0.1195 (the first reload after 0.1194: the plugin, the window and the closures of the unloaded instance were collected, and 225 of
    /// its 1689 parts stayed alive, every one read a field initialised with « [] » or with a static value): whether a value is shared by
    /// construction, so alive as long as the load context whatever became of the instance: an empty array (the one « [] » of its type, which
    /// holds nothing), or the value a static field of its own type or of its owner's type holds (« None », « Empty », a static default).
    /// Such a value says nothing about a leak: it is not noted at unload, and a note made before says « shared ».
    /// </summary>
    public static bool Shared(object value,Type? owner=null,Dictionary<Type,HashSet<object>>? cache=null)=>
        value is Array { Length:0 } || StaticValues(value.GetType(),cache).Contains(value) || (owner is not null && StaticValues(owner,cache).Contains(value));

    /// <summary>0.1195: the objects the static fields of a type and of its base types hold now (by reference); the runtime's own types have none to tell.</summary>
    private static HashSet<object> StaticValues(Type type,Dictionary<Type,HashSet<object>>? cache)
    {
        if(cache is not null && cache.TryGetValue(type,out var known))return known;
        var values=new HashSet<object>(ReferenceEqualityComparer.Instance);
        for(var current=type;current is not null && current.Assembly!=typeof(object).Assembly;current=current.BaseType)
            foreach(var field in current.GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
            {
                if(field.IsLiteral || field.FieldType.IsValueType || field.FieldType==typeof(string) || field.FieldType.IsPointer)continue;
                try{if(field.GetValue(null) is { } value)values.Add(value);}
                catch(Exception error) when(error is TargetInvocationException or TypeInitializationException or NotSupportedException or FieldAccessException or InvalidOperationException or ArgumentException){}
            }
        if(cache is not null)cache[type]=values;
        return values;
    }

    /// <summary>Notes an owner and each of its fields that holds an object of the plugin's own code, weakly, under « name » and « name.field »
    /// (0.1195: but a value shared by construction, see <see cref="Shared"/>). One level only: the hubs' own fields (0.1196).</summary>
    public static void Note(Dictionary<string,WeakReference> parts,string name,object? owner,Assembly assembly)
    {
        if(owner is null)return;
        parts[name]=new WeakReference(owner);
        var statics=new Dictionary<Type,HashSet<object>>();
        foreach(var field in owner.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
        {
            if(field.FieldType.IsValueType || field.FieldType==typeof(string) || field.FieldType.IsPointer)continue;
            object? value;
            try{value=field.GetValue(owner);}
            catch(Exception error) when(error is NotSupportedException or FieldAccessException or TargetException){continue;}
            if(value is null || !Ours(value.GetType(),assembly) || Shared(value,owner.GetType(),statics))continue;
            parts[name+"."+field.Name]=new WeakReference(value);
        }
    }

    /// <summary>One line for an earlier instance: its context, and its parts still alive out of those noted (the first names, in order).</summary>
    public static string Describe(string label,IReadOnlyDictionary<string,WeakReference> parts,int names=24)
    {
        var context=parts.TryGetValue(Context,out var held) ? (held.IsAlive ? "alive":"collected"):"unknown";
        var noted=parts.Where(part=>part.Key!=Context).ToList();
        if(noted.Count==0)return $"{label}: context {context}, parts not noted";
        // 0.1195: each part read once (a target taken is alive for the line); a part shared by construction is counted apart from those a
        // root holds (an instance noted before 0.1195 has its empty arrays and static values among its parts; none is noted since).
        var alive=new List<string>();
        var collected=new List<string>();
        int empties=0,statics=0;
        var cache=new Dictionary<Type,HashSet<object>>();
        foreach(var part in noted.OrderBy(part=>part.Key,StringComparer.Ordinal))
        {
            if(part.Value.Target is not { } target){collected.Add(part.Key);continue;}
            if(target is Array { Length:0 })empties++;
            else if(Shared(target,null,cache))statics++;
            else alive.Add(part.Key);
        }
        var shared=empties+statics;
        var head=$"{label}: context {context}, {alive.Count+shared} of {noted.Count} parts alive";
        if(shared>0)head+=$", {shared} shared (empty arrays: {empties}, static values: {statics})"+(alive.Count>0 ? $", {alive.Count} held":"");
        if(alive.Count==0)return head;
        // 0.1188: the shorter list tells more: the parts collected when most are alive (what the root does not reach), the parts alive otherwise.
        var (word,list)=collected.Count>0 && collected.Count<alive.Count ? ("; collected: ",collected):(": ",alive);
        return head+word+string.Join(", ",list.Take(names))+(list.Count>names ? ", …":"");
    }

    /// <summary>
    /// 0.1188: whether an earlier instance's plugin object finished its unload: its registration scope disposed and how many releases were
    /// left in it (read by name, without running anything of that instance); « unknown » when the fields are not there.
    /// </summary>
    public static string Released(object plugin)
    {
        const BindingFlags any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        try
        {
            if(plugin.GetType().GetField("registrations",any)?.GetValue(plugin) is not { } scope)return "registrations unknown";
            var disposed=scope.GetType().GetField("disposed",any)?.GetValue(scope);
            var left=scope.GetType().GetField("cleanup",any)?.GetValue(scope) is System.Collections.ICollection cleanup ? cleanup.Count.ToString(System.Globalization.CultureInfo.InvariantCulture):"?";
            return $"registrations disposed: {disposed ?? "?"}, releases left: {left}";
        }
        catch(Exception error) when(error is FieldAccessException or TargetException or NotSupportedException){return "registrations unknown";}
    }

    /// <summary>
    /// 0.1188: the static fields of an assembly that hold something an instance could hang from: a delegate with a target, an object of the
    /// assembly's own classes that has reference fields, a non-empty collection of such objects or of delegates. One text per field
    /// (« Type.field: what »), the first <paramref name="max"/>. Read only; a field that cannot be read is skipped.
    /// </summary>
    public static List<string> Statics(Assembly assembly,int max=40)
    {
        var found=new List<string>();
        Type[] types;
        try{types=assembly.GetTypes();}
        catch(ReflectionTypeLoadException error){types=error.Types.Where(type=>type is not null).ToArray()!;}
        foreach(var type in types.OrderBy(type=>type.FullName,StringComparer.Ordinal))
        {
            if(type.ContainsGenericParameters)continue;
            foreach(var field in type.GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
            {
                if(field.IsLiteral || field.FieldType.IsValueType || field.FieldType==typeof(string) || field.FieldType.IsPointer)continue;
                object? value;
                try{value=field.GetValue(null);}
                catch(Exception error) when(error is TargetInvocationException or TypeInitializationException or NotSupportedException or FieldAccessException or InvalidOperationException){continue;}
                if(Holds(value,assembly,type) is not { } what)continue;
                found.Add($"{type.FullName}.{field.Name}: {what}");
                if(found.Count>=max)return found;
            }
        }
        return found;
    }

    private static string? Holds(object? value,Assembly assembly,Type declaring)
    {
        switch(value)
        {
            case null:return null;
            case Delegate hook:
                var targets=hook.GetInvocationList().Select(one=>one.Target).Where(target=>target is not null && !CompilerSingleton(target!.GetType())).Select(target=>target!.GetType().Name).Distinct().ToList();
                return targets.Count==0 ? null:"delegate on "+string.Join(", ",targets);
            case System.Collections.ICollection { Count:>0 } many when value.GetType().IsGenericType && value.GetType().GetGenericArguments().Any(argument=>typeof(Delegate).IsAssignableFrom(argument) || (argument.IsClass && argument.Assembly==assembly)):
                return $"{many.Count} items of {value.GetType().Name}<{string.Join(",",value.GetType().GetGenericArguments().Select(argument=>argument.Name))}>";
        }
        var type=value.GetType();
        // An object of the assembly's own classes counts when it has fields that can hold other objects (a record of numbers and texts cannot hold an instance).
        if(type.Assembly!=assembly || type==declaring || CompilerSingleton(type) || type.IsEnum)return null;
        var fields=type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(field=>!field.FieldType.IsValueType && field.FieldType!=typeof(string) && !field.FieldType.IsPointer).ToArray();
        if(fields.Length==0)return null;
        // 0.1194 (« RaphaelSequenceCache (4 reference fields) » was the root and read like a table of data): one level inside, what the
        // object's fields hold now: a delegate with a target, or another object of the assembly that has reference fields.
        var inside=new List<string>();
        foreach(var field in fields)
        {
            object? held;
            try{held=field.GetValue(value);}
            catch(Exception error) when(error is NotSupportedException or FieldAccessException or TargetException){continue;}
            if(held is Delegate inner)
            {
                var on=inner.GetInvocationList().Select(one=>one.Target).Where(target=>target is not null && !CompilerSingleton(target!.GetType())).Select(target=>target!.GetType().Name).Distinct().ToList();
                if(on.Count>0)inside.Add($"{field.Name}: delegate on {string.Join(", ",on)}");
            }
            else if(held is not null && held.GetType() is var heldType && heldType.Assembly==assembly && !heldType.IsEnum && !CompilerSingleton(heldType)
                && heldType.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Any(one=>!one.FieldType.IsValueType && one.FieldType!=typeof(string)))
                inside.Add($"{field.Name}: {heldType.Name}");
        }
        return inside.Count>0 ? $"{type.Name} holding {string.Join("; ",inside)}":$"{type.Name} ({fields.Length} reference fields)";
    }

    // The compiler's « <>c » singletons hold static lambdas only: no instance behind them.
    private static bool CompilerSingleton(Type type)=>type.Name.StartsWith("<>c",StringComparison.Ordinal) && !type.Name.StartsWith("<>c__DisplayClass",StringComparison.Ordinal);
}
