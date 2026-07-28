using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ImmersingLinker.SourceGenerator;

[Generator]
public sealed class SettingsValidatorGenerator : IIncrementalGenerator
{
    private const string ValidatorDiagnosticId = "ILNK001";
    private const string GeneralDiagnosticId = "ILNK002";
    private const string Category = "SettingsValidator";

    private static readonly DiagnosticDescriptor ValidatorSyntaxError = new(
        ValidatorDiagnosticId,
        "Validator expression has syntax error",
        "Validator expression '{0}' in '{1}' has syntax error(s): {2}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GeneralTypeError = new(
        GeneralDiagnosticId,
        "General field type reference is invalid",
        "General field value '{0}' in '{1}' is not a valid type reference",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly HashSet<string> KnownAliases = new()
    {
        "int", "string", "bool", "double", "long", "float", "decimal",
        "byte", "sbyte", "short", "ushort", "uint", "ulong", "char", "object",
    };

    private static readonly HashSet<string> KnownGenericTypes = new()
    {
        "List", "Dictionary", "HashSet", "IList", "IDictionary", "IEnumerable",
        "ICollection", "IReadOnlyList", "IReadOnlyDictionary", "Nullable",
        "KeyValuePair", "Stack", "Queue", "LinkedList", "Task",
        "ValueTuple", "Tuple",
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var jsonFiles = context.AdditionalTextsProvider
            .Where(static file => IsSettingsJsonFile(file.Path));

        var combined = jsonFiles.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var (file, compilation) = pair;
            GenerateReport(spc, file, compilation);
        });
    }

    private static bool IsSettingsJsonFile(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.EndsWith(".json")
            && (normalized.Contains("/Assets/Settings/") || normalized.Contains("Assets/Settings/"));
    }

    private static void GenerateReport(
        SourceProductionContext context, AdditionalText file, Compilation compilation)
    {
        var text = file.GetText(context.CancellationToken);
        if (text is null)
            return;

        var jsonContent = text.ToString();

        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        ValidateValidatorExpressions(context, file, root);
        ValidateGeneralFields(context, file, root, compilation);
    }

    private static void ValidateValidatorExpressions(
        SourceProductionContext context, AdditionalText file, JsonElement root)
    {
        var validators = new List<string>();
        CollectStringValues(root, "validator", validators);

        foreach (var expr in validators)
        {
            var tree = CSharpSyntaxTree.ParseText(
                expr,
                options: new CSharpParseOptions(kind: SourceCodeKind.Script),
                cancellationToken: context.CancellationToken);

            var errors = new List<string>();
            foreach (var diagnostic in tree.GetDiagnostics())
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    errors.Add(diagnostic.GetMessage());
            }

            if (errors.Count <= 0)
                continue;

            var errorSummary = string.Join("; ", errors);
            context.ReportDiagnostic(
                Diagnostic.Create(ValidatorSyntaxError, Location.None, expr, file.Path, errorSummary));
        }
    }

    private static void ValidateGeneralFields(
        SourceProductionContext context, AdditionalText file, JsonElement root, Compilation compilation)
    {
        var generalValues = new List<string>();
        CollectStringValues(root, "general", generalValues);

        foreach (var value in generalValues)
        {
            if (!IsValidTypeReference(value, compilation))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(GeneralTypeError, Location.None, value, file.Path));
            }
        }
    }

    private static void CollectStringValues(JsonElement element, string propertyName, List<string> results)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals(propertyName)
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            results.Add(value);
                    }
                    else
                    {
                        CollectStringValues(property.Value, propertyName, results);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectStringValues(item, propertyName, results);
                }
                break;
        }
    }

    private static bool IsValidTypeReference(string typeName, Compilation compilation)
    {
        typeName = typeName.Trim();
        if (typeName.Length == 0)
            return false;

        if (KnownAliases.Contains(typeName))
        {
            var clrName = AliasToClrName(typeName);
            return compilation.GetTypeByMetadataName(clrName) != null;
        }

        if (typeName[typeName.Length - 1] == '?' && typeName.Length > 1)
        {
            var baseType = typeName.Substring(0, typeName.Length - 1);
            return IsValidTypeReference(baseType, compilation)
                && compilation.GetTypeByMetadataName("System.Nullable`1") != null;
        }

        var bracketIndex = FindTopLevelBracket(typeName, '<', '>');
        if (bracketIndex >= 0)
        {
            if (bracketIndex == 0 || typeName[typeName.Length - 1] != '>')
                return false;

            var openName = typeName.Substring(0, bracketIndex).Trim();
            var argsPart = typeName.Substring(bracketIndex + 1, typeName.Length - bracketIndex - 2).Trim();

            var args = SplitTopLevelCommas(argsPart);

            if (!TypeExistsInCompilation(compilation, openName, args.Count))
                return false;

            foreach (var arg in args)
            {
                if (!IsValidTypeReference(arg.Trim(), compilation))
                    return false;
            }

            return true;
        }

        return TypeExistsInCompilation(compilation, typeName, 0)
            || TypeExistsInCompilation(compilation, typeName, -1);
    }

    private static string AliasToClrName(string alias)
    {
        return alias switch
        {
            "int" => "System.Int32",
            "string" => "System.String",
            "bool" => "System.Boolean",
            "double" => "System.Double",
            "long" => "System.Int64",
            "float" => "System.Single",
            "decimal" => "System.Decimal",
            "byte" => "System.Byte",
            "sbyte" => "System.SByte",
            "short" => "System.Int16",
            "ushort" => "System.UInt16",
            "uint" => "System.UInt32",
            "ulong" => "System.UInt64",
            "char" => "System.Char",
            "object" => "System.Object",
            _ => alias,
        };
    }

    private static bool TypeExistsInCompilation(Compilation compilation, string name, int arity)
    {
        if (arity >= 0)
        {
            if (KnownGenericTypes.Contains(name))
            {
                var fullName = GenericNameToClrName(name);
                var withArity = $"{fullName}`{arity}";
                if (compilation.GetTypeByMetadataName(withArity) != null)
                    return true;
            }

            var directArity = $"{name}`{arity}";
            if (compilation.GetTypeByMetadataName(directArity) != null)
                return true;
        }

        if (compilation.GetTypeByMetadataName(name) != null)
            return true;

        if (compilation.GetTypeByMetadataName("System." + name) != null)
            return true;

        if (compilation.GetTypeByMetadataName("System.Collections.Generic." + name) != null)
            return true;

        return TryFindTypeInCompilation(compilation, name);
    }

    private static string GenericNameToClrName(string name)
    {
        return name switch
        {
            "List" => "System.Collections.Generic.List",
            "Dictionary" => "System.Collections.Generic.Dictionary",
            "HashSet" => "System.Collections.Generic.HashSet",
            "IList" => "System.Collections.Generic.IList",
            "IDictionary" => "System.Collections.Generic.IDictionary",
            "IEnumerable" => "System.Collections.Generic.IEnumerable",
            "ICollection" => "System.Collections.Generic.ICollection",
            "IReadOnlyList" => "System.Collections.Generic.IReadOnlyList",
            "IReadOnlyDictionary" => "System.Collections.Generic.IReadOnlyDictionary",
            "Nullable" => "System.Nullable",
            "KeyValuePair" => "System.Collections.Generic.KeyValuePair",
            "Stack" => "System.Collections.Generic.Stack",
            "Queue" => "System.Collections.Generic.Queue",
            "LinkedList" => "System.Collections.Generic.LinkedList",
            "Task" => "System.Threading.Tasks.Task",
            "ValueTuple" => "System.ValueTuple",
            "Tuple" => "System.Tuple",
            _ => name,
        };
    }

    private static bool TryFindTypeInCompilation(Compilation compilation, string name)
    {
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (NamespaceContainsType(assembly.GlobalNamespace, name))
                return true;
        }

        return false;
    }

    private static bool NamespaceContainsType(INamespaceSymbol ns, string name)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (type.Name == name || type.MetadataName == name)
                return true;
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            if (NamespaceContainsType(childNs, name))
                return true;
        }

        return false;
    }

    private static int FindTopLevelBracket(string s, char open, char close)
    {
        var depth = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == open)
            {
                if (depth == 0)
                    return i;
                depth++;
            }
            else if (s[i] == close)
            {
                if (depth == 0)
                    return -2;
                depth--;
            }
        }
        return -1;
    }

    private static List<string> SplitTopLevelCommas(string s)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < s.Length; i++)
        {
            switch (s[i])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(s.Substring(start, i - start).Trim());
                    start = i + 1;
                    break;
            }
        }

        parts.Add(s.Substring(start).Trim());
        return parts;
    }
}
