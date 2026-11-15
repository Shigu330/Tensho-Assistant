using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-01 and X-177 (0.1279). What a method calls, read from its IL, so that the code a frame runs is compiled ahead with what it calls.
/// Measured in dalamud.log on 13/11 (38 loads): the free panels' first frame took 152 to 338 ms, and the readers whose class was already
/// compiled still compiled 27 to 43 methods on their first pass (50 to 101 ms): the methods they call in other classes of the plugin and the
/// generic code made for the plugin's own types (LINQ over its records). The warm-up compiled each class alone.
/// Followed: the plugin's own methods (their IL read in turn) and generic code closed over one of the plugin's types (its IL read too:
/// its iterator and helpers are made for this load). Not followed: the libraries' plain methods (compiled once for the process, not per
/// load of the plugin), abstract and interface methods (the method run is chosen at run time), delegates (their target is not in the IL).
/// Bounded by a count, an explicit queue (no recursion over the plugin's own code), and a stop asked before each method.
/// </summary>
public static class CallGraph
{
    private static readonly OpCode[] OneByte=new OpCode[256];
    private static readonly OpCode[] TwoByte=new OpCode[256];
    private static readonly bool[] KnownOne=new bool[256];
    private static readonly bool[] KnownTwo=new bool[256];

    static CallGraph()
    {
        foreach(var field in typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static))
        {
            if(field.GetValue(null) is not OpCode code)continue;
            var value=(ushort)code.Value;
            if(code.Size==1){OneByte[value]=code;KnownOne[value]=true;}
            else{TwoByte[value&0xFF]=code;KnownTwo[value&0xFF]=true;}
        }
    }

    /// <summary>The methods and constructors this method's IL names (call, callvirt, newobj, ldftn, ldvirtftn, jmp), in order, each once.
    /// Nothing when the body cannot be read or a token cannot be resolved (that one is skipped).</summary>
    public static IReadOnlyList<MethodBase> Callees(MethodBase method)
    {
        byte[]? il;
        try{il=method.GetMethodBody()?.GetILAsByteArray();}
        catch(Exception){return [];}
        if(il is null || il.Length==0)return [];
        Module module;
        Type[]? typeArguments=null,methodArguments=null;
        try
        {
            module=method.Module;
            if(method.DeclaringType is { IsGenericType:true } declaring)typeArguments=declaring.GetGenericArguments();
            if(method.IsGenericMethod)methodArguments=method.GetGenericArguments();
        }
        catch(Exception){return [];}
        var found=new List<MethodBase>();
        var seen=new HashSet<MethodBase>();
        var at=0;
        while(at<il.Length)
        {
            OpCode code;
            var first=il[at++];
            if(first==0xFE)
            {
                if(at>=il.Length || !KnownTwo[il[at]])break;
                code=TwoByte[il[at++]];
            }
            else
            {
                if(!KnownOne[first])break;
                code=OneByte[first];
            }
            var size=OperandSize(code.OperandType,il,at);
            if(size<0 || at+size>il.Length)break;
            if(code.OperandType==OperandType.InlineMethod)
            {
                var token=BitConverter.ToInt32(il,at);
                try{if(module.ResolveMethod(token,typeArguments,methodArguments) is { } callee && seen.Add(callee))found.Add(callee);}
                catch(Exception){}
            }
            at+=size;
        }
        return found;
    }

    private static int OperandSize(OperandType type,byte[] il,int at)=>type switch
    {
        OperandType.InlineNone=>0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,
        OperandType.InlineVar=>2,
        OperandType.InlineI8 or OperandType.InlineR=>8,
        OperandType.InlineSwitch=>at+4<=il.Length && BitConverter.ToInt32(il,at) is var count && count>=0 && count<=(il.Length-at-4)/4 ? 4+4*count:-1,
        _=>4,
    };

    /// <summary>
    /// The roots, then what they call, breadth first, each once, at most <paramref name="cap"/> methods: see the class. <paramref name="home"/>
    /// is the plugin's assembly. Every method returned can be given to <see cref="Prepare"/>; the roots are kept even when they are not.
    /// </summary>
    public static IReadOnlyList<MethodBase> Closure(IEnumerable<MethodBase> roots,Assembly home,int cap,Func<bool>? stop=null)
    {
        var order=new List<MethodBase>();
        var seen=new HashSet<MethodBase>();
        var queue=new Queue<MethodBase>();
        foreach(var root in roots)
            if(seen.Add(root))queue.Enqueue(root);
        while(queue.Count>0 && order.Count<cap)
        {
            if(stop?.Invoke()==true)break;
            var method=queue.Dequeue();
            order.Add(method);
            if(!Followed(method,home))continue;
            // A type's static constructor runs on its first use without being named in the IL (the lambdas' cache class has one).
            if(TypeInitializer(method) is { } initializer && Wanted(initializer,home) && seen.Add(initializer))queue.Enqueue(initializer);
            foreach(var callee in Callees(method))
                if(Wanted(callee,home) && seen.Add(callee))queue.Enqueue(callee);
        }
        return order;
    }

    private static ConstructorInfo? TypeInitializer(MethodBase method)
    {
        try{return method.DeclaringType is { ContainsGenericParameters:false } type ? type.TypeInitializer:null;}
        catch(Exception){return null;}
    }

    /// <summary>Whether a method's own IL is read: the plugin's own, or generic code closed over one of its types.</summary>
    private static bool Followed(MethodBase method,Assembly home)
    {
        try{return method.Module.Assembly==home || ClosedOver(method,home);}
        catch(Exception){return false;}
    }

    /// <summary>A callee worth compiling ahead: a body to compile (not abstract, no open generic parameter, not native), and either the
    /// plugin's own or generic code closed over one of the plugin's types.</summary>
    public static bool Wanted(MethodBase method,Assembly home)
    {
        try
        {
            if(method.DeclaringType is { IsInterface:true } || !JitWarmup.Compilable(method))return false;
            return method.Module.Assembly==home || ClosedOver(method,home);
        }
        catch(Exception){return false;}
    }

    private static bool ClosedOver(MethodBase method,Assembly home)
    {
        if(method.IsGenericMethod)
            foreach(var argument in method.GetGenericArguments())
                if(Mentions(argument,home))return true;
        if(method.DeclaringType is { IsGenericType:true } declaring)
            foreach(var argument in declaring.GetGenericArguments())
                if(Mentions(argument,home))return true;
        return false;
    }

    /// <summary>Whether a type is one of the plugin's, or is built from one (array, pointer, generic argument), explicit stack, bounded.</summary>
    private static bool Mentions(Type type,Assembly home)
    {
        var stack=new Stack<Type>();
        stack.Push(type);
        var steps=0;
        while(stack.Count>0 && steps++<64)
        {
            var next=stack.Pop();
            if(next.IsGenericParameter)continue;
            if(next.Assembly==home)return true;
            if(next.HasElementType && next.GetElementType() is { } element)stack.Push(element);
            if(next.IsGenericType)foreach(var argument in next.GetGenericArguments())stack.Push(argument);
        }
        return false;
    }

    /// <summary>Compiles one method now, on the calling thread: generic code with its instantiation (the declaring type's arguments, then the
    /// method's), as the runtime asks. False when it cannot be compiled ahead (it is then compiled on its first call, as before).</summary>
    public static bool Prepare(MethodBase method)
    {
        try
        {
            var typeArguments=method.DeclaringType is { IsGenericType:true } declaring ? declaring.GetGenericArguments():Type.EmptyTypes;
            var methodArguments=method.IsGenericMethod ? method.GetGenericArguments():Type.EmptyTypes;
            if(typeArguments.Length+methodArguments.Length==0)RuntimeHelpers.PrepareMethod(method.MethodHandle);
            else RuntimeHelpers.PrepareMethod(method.MethodHandle,[..typeArguments.Select(type=>type.TypeHandle),..methodArguments.Select(type=>type.TypeHandle)]);
            return true;
        }
        catch(Exception){return false;}
    }
}
