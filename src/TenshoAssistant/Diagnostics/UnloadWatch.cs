using System.Reflection;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-01 C1.3 (0.1187, the root of the leak of unloaded instances): what the log says, ten seconds after a load, about the instances loaded
/// before in the same game process: their load context alive or collected, and which named parts of each (the plugin object, the window,
/// their fields that hold objects of the plugin's own code) are still alive. Weak references only, kept in a process-wide slot made of
/// runtime types only, so the watch itself never keeps an instance or its context alive; nothing is collected or forced.
/// Reading: a context that stays alive while its parts are collected costs little (code and static tables); parts that stay alive name what
/// still holds the instance. Roots found out of the game and readings: docs/evidence/0.1187-unload-roots.md.
/// </summary>
public static class UnloadWatch
{
    /// <summary>Process-wide slot (AppDomain data): the instances loaded in this process, the newest first.</summary>
    public const string Slot="TenshoAssistant.X01.Instances";
    /// <summary>How many instances are kept, this one included.</summary>
    public const int Kept=5;
    /// <summary>The name of the load context among an instance's parts.</summary>
    public const string Context="context";
    /// <summary>0.1189: the mark of an instance whose hubs were emptied by a later load (names starting with « ! » are marks, not parts).</summary>
    public const string Emptied="!emptied2";
    /// <summary>0.1189: the objects everything else of an instance hangs from: the plugin, its window, the constructor's captured locals;
    /// 0.1191: and the closure of the constructor's outer scope (« locals0 », or the closure field of « locals » for an instance noted before).</summary>
    public static readonly IReadOnlyList<string> Hubs=["plugin","window","locals","locals0"];
    /// <summary>0.1191: how the compiler names the field of a closure that holds the closure of the scope around it.</summary>
    public const string ClosureField="CS$<>8__locals";

    /// <summary>0.1191: whether a later load emptied this instance's hubs (any pass).</summary>
    public static bool IsEmptied(IReadOnlyDictionary<string,WeakReference> parts)=>parts.Keys.Any(key=>key.StartsWith("!emptied",StringComparison.Ordinal));

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

    /// <summary>Notes an owner and each of its fields that holds an object of the plugin's own code, weakly, under « name » and « name.field ».</summary>
    public static void Note(Dictionary<string,WeakReference> parts,string name,object? owner,Assembly assembly)
    {
        if(owner is null)return;
        parts[name]=new WeakReference(owner);
        foreach(var field in owner.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
        {
            if(field.FieldType.IsValueType || field.FieldType==typeof(string) || field.FieldType.IsPointer)continue;
            object? value;
            try{value=field.GetValue(owner);}
            catch(Exception error) when(error is NotSupportedException or FieldAccessException or TargetException){continue;}
            if(value is null || !Ours(value.GetType(),assembly))continue;
            parts[name+"."+field.Name]=new WeakReference(value);
        }
    }

    /// <summary>
    /// 0.1192 (after the hubs were emptied, the plugin, the window, the data cache and the outer closure stayed alive, and no captured
    /// service: what holds them is an object that no field of a hub names): notes the plugin's own objects reachable from an owner, to a
    /// given depth, each under its path (« locals.service.field », « window.field→Target » through a delegate), so that an emptied
    /// instance's line names the chain an outside root still holds. Objects of the plugin's classes and delegates' targets only (no
    /// collection is walked), breadth first, each object once, <paramref name="cap"/> at most. Returns the objects noted.
    /// </summary>
    public static int NoteGraph(Dictionary<string,WeakReference> parts,string name,object? owner,Assembly assembly,int depth=3,int cap=3000)
    {
        if(owner is null)return 0;
        var seen=new HashSet<object>(ReferenceEqualityComparer.Instance){owner};
        var queue=new Queue<(string Path,object Value,int Level)>();
        queue.Enqueue((name,owner,0));
        var noted=0;
        while(queue.Count>0 && noted<cap)
        {
            var (path,value,level)=queue.Dequeue();
            if(parts.TryAdd(path,new WeakReference(value)))noted++;
            if(level>=depth)continue;
            for(var type=value.GetType();type is not null && type.Assembly==assembly;type=type.BaseType)
                foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                {
                    if(field.FieldType.IsValueType || field.FieldType.IsPointer || field.FieldType==typeof(string))continue;
                    object? held;
                    try{held=field.GetValue(value);}
                    catch(Exception error) when(error is NotSupportedException or FieldAccessException or TargetException){continue;}
                    if(held is Delegate hook)
                    {
                        foreach(var target in hook.GetInvocationList().Select(one=>one.Target))
                            if(target is not null && target.GetType().Assembly==assembly && !CompilerSingleton(target.GetType()) && seen.Add(target))
                                queue.Enqueue(($"{path}.{field.Name}→{target.GetType().Name}",target,level+1));
                        continue;
                    }
                    if(held is null || held.GetType().Assembly!=assembly || held.GetType().IsEnum || !seen.Add(held))continue;
                    queue.Enqueue(($"{path}.{field.Name}",held,level+1));
                }
        }
        return noted;
    }

    /// <summary>One line for an earlier instance: its context, and its parts still alive out of those noted (the first names, in order).</summary>
    public static string Describe(string label,IReadOnlyDictionary<string,WeakReference> parts,int names=24)
    {
        var context=parts.TryGetValue(Context,out var held) ? (held.IsAlive ? "alive":"collected"):"unknown";
        var noted=parts.Where(part=>part.Key!=Context && !part.Key.StartsWith('!')).ToList();
        if(noted.Count==0)return $"{label}: context {context}, parts not noted";
        var alive=noted.Where(part=>part.Value.IsAlive).Select(part=>part.Key).Order(StringComparer.Ordinal).ToList();
        var head=$"{label}: context {context}{(IsEmptied(parts) ? ", hubs emptied":"")}, {alive.Count} of {noted.Count} parts alive";
        if(alive.Count==0)return head;
        // 0.1188: the shorter list tells more: the parts collected when most are alive (what the root does not reach), the parts alive otherwise.
        var collected=noted.Where(part=>!part.Value.IsAlive).Select(part=>part.Key).Order(StringComparer.Ordinal).ToList();
        var (word,list)=collected.Count>0 && collected.Count<alive.Count ? ("; collected: ",collected):(": ",alive);
        return head+word+string.Join(", ",list.Take(names))+(list.Count>names ? ", …":"");
    }

    /// <summary>
    /// 0.1189 (the second measure of 0.1188: unload finished, no static of the assembly holds an instance, and yet 463 of 471 parts alive:
    /// the root is outside the plugin's code): empties one object of an instance unloaded earlier: each reference field declared by the
    /// plugin's own types is set to null, so that what an outside root still holds no longer reaches the rest. The parts that stay alive
    /// afterwards are what the root holds, and the rest of that instance can be collected. Returns the fields emptied.
    /// Only for an instance whose unload finished at least one whole load ago: nothing of it runs any more.
    /// </summary>
    public static int Empty(object? owner,Assembly assembly)
    {
        if(owner is null)return 0;
        var emptied=0;
        for(var type=owner.GetType();type is not null && type.Assembly==assembly;type=type.BaseType)
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
            {
                if(field.FieldType.IsValueType || field.FieldType.IsPointer || field.FieldType==typeof(string))continue;
                try
                {
                    if(field.GetValue(owner) is null)continue;
                    field.SetValue(owner,null);
                    emptied++;
                }
                catch(Exception error) when(error is FieldAccessException or TargetException or NotSupportedException or ArgumentException){}
            }
        return emptied;
    }

    /// <summary>0.1189: empties the hubs of one earlier instance, once (marked); the fields emptied, 0 when already done or nothing alive.</summary>
    public static int EmptyHubs(Dictionary<string,WeakReference> parts)
    {
        lock(parts)
        {
            if(parts.ContainsKey(Emptied))return 0;
            var emptied=0;
            foreach(var hub in Hubs)
                if(parts.GetValueOrDefault(hub)?.Target is { } target)emptied+=Empty(target,target.GetType().Assembly);
            // 0.1191: the outer closures noted as fields of a hub (an instance noted before « locals0 » existed).
            foreach(var (name,part) in parts.ToArray())
                if(name.Contains("."+ClosureField,StringComparison.Ordinal) && part.Target is { } closure)emptied+=Empty(closure,closure.GetType().Assembly);
            if(emptied>0)parts[Emptied]=new WeakReference(null);
            return emptied;
        }
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
        var holders=type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Count(field=>!field.FieldType.IsValueType && field.FieldType!=typeof(string));
        return holders==0 ? null:$"{type.Name} ({holders} reference fields)";
    }

    // The compiler's « <>c » singletons hold static lambdas only: no instance behind them.
    private static bool CompilerSingleton(Type type)=>type.Name.StartsWith("<>c",StringComparison.Ordinal) && !type.Name.StartsWith("<>c__DisplayClass",StringComparison.Ordinal);
}
