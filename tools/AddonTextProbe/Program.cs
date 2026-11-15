using System.Reflection;
using System.Runtime.InteropServices;
using Iced.Intel;

// X-434 (0.1420): which game values feed an Addon sheet text. Reads the installed client (ffxiv_dx11.exe) as a file only: nothing is run,
// no process is touched. Lists the code that loads one of the given Addon row ids as an immediate value, with the instructions around it,
// so the fields passed to the text's parameters can be read; then the field offsets of a FFXIVClientStructs type (metadata only).
// Usage: AddonTextProbe <row>[,<row>…] [--before=N] [--after=N] [--struct=Full.Type.Name] [--exe=path]
var rows = new HashSet<ulong>();
int before = 40, after = 60;
string? structName = null;
var exe = @"C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY XIV Online\game\ffxiv_dx11.exe";
foreach (var argument in args)
{
    if (argument.StartsWith("--before=", StringComparison.Ordinal)) before = int.Parse(argument[9..]);
    else if (argument.StartsWith("--after=", StringComparison.Ordinal)) after = int.Parse(argument[8..]);
    else if (argument.StartsWith("--struct=", StringComparison.Ordinal)) structName = argument[9..];
    else if (argument.StartsWith("--exe=", StringComparison.Ordinal)) exe = argument[6..];
    else foreach (var row in argument.Split(',', StringSplitOptions.RemoveEmptyEntries)) rows.Add(ulong.Parse(row));
}

if (structName != null)
{
    var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher/addon/Hooks/dev");
    var paths = Directory.GetFiles(directory, "*.dll").Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
    using var context = new MetadataLoadContext(new PathAssemblyResolver(paths));
    var type = context.LoadFromAssemblyPath(Path.Combine(directory, "FFXIVClientStructs.dll")).GetType(structName, true)!;
    Console.WriteLine($"STRUCT {type.FullName}");
    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    {
        var offset = field.GetCustomAttributesData().FirstOrDefault(data => data.AttributeType.Name == "FieldOffsetAttribute")?.ConstructorArguments.FirstOrDefault().Value;
        Console.WriteLine($"  0x{offset:X2} {field.FieldType.Name} {field.Name}");
    }
}
if (rows.Count == 0) return;

var image = File.ReadAllBytes(exe);
var peOffset = BitConverter.ToInt32(image, 0x3C);
var sectionCount = BitConverter.ToUInt16(image, peOffset + 6);
var optionalSize = BitConverter.ToUInt16(image, peOffset + 20);
var imageBase = BitConverter.ToUInt64(image, peOffset + 24 + 24);
var sectionTable = peOffset + 24 + optionalSize;
for (var index = 0; index < sectionCount; index++)
{
    var header = sectionTable + index * 40;
    var name = System.Text.Encoding.ASCII.GetString(image, header, 8).TrimEnd('\0');
    if (name != ".text") continue;
    var virtualSize = BitConverter.ToInt32(image, header + 8);
    var virtualAddress = BitConverter.ToUInt32(image, header + 12);
    var rawSize = BitConverter.ToInt32(image, header + 16);
    var rawPointer = BitConverter.ToInt32(image, header + 20);
    var length = Math.Min(virtualSize, rawSize);
    var code = new ByteArrayCodeReader(image, rawPointer, length);
    var decoder = Decoder.Create(64, code, imageBase + virtualAddress);
    var all = new List<Instruction>(length / 4);
    while (code.CanReadByte)
    {
        decoder.Decode(out var instruction);
        all.Add(instruction);
    }
    Console.WriteLine($"TEXT {all.Count} instructions, base 0x{imageBase:X}");
    var formatter = new NasmFormatter();
    var output = new StringOutput();
    var hits = 0;
    for (var at = 0; at < all.Count; at++)
    {
        var instruction = all[at];
        if (instruction.Mnemonic != Mnemonic.Mov || instruction.OpCount != 2 || instruction.Op0Kind != OpKind.Register) continue;
        var immediate = instruction.Op1Kind switch
        {
            OpKind.Immediate32 => instruction.Immediate32,
            OpKind.Immediate32to64 => (ulong)instruction.Immediate32to64,
            _ => ulong.MaxValue,
        };
        if (!rows.Contains(immediate)) continue;
        hits++;
        Console.WriteLine($"=== row {immediate} at 0x{instruction.IP:X} (rva 0x{instruction.IP - imageBase:X})");
        for (var near = Math.Max(0, at - before); near < Math.Min(all.Count, at + after); near++)
        {
            formatter.Format(all[near], output);
            Console.WriteLine($"{(near == at ? ">" : " ")} {all[near].IP - imageBase:X8} {output.ToStringAndReset()}");
        }
    }
    Console.WriteLine($"HITS {hits}");
}
