using System.Collections.Concurrent;
using System.Reflection;

namespace ImmersingLinker.Core.Services.Setting;

public static class TypeNameResolver
{
    private static readonly ConcurrentDictionary<string, Type> Cache = new();

    private static readonly Dictionary<string, string> AliasMap = new()
    {
        ["int"] = "System.Int32",
        ["string"] = "System.String",
        ["bool"] = "System.Boolean",
        ["double"] = "System.Double",
        ["long"] = "System.Int64",
        ["float"] = "System.Single",
        ["decimal"] = "System.Decimal",
        ["byte"] = "System.Byte",
        ["sbyte"] = "System.SByte",
        ["short"] = "System.Int16",
        ["ushort"] = "System.UInt16",
        ["uint"] = "System.UInt32",
        ["ulong"] = "System.UInt64",
        ["char"] = "System.Char",
        ["object"] = "System.Object",
    };

    private static readonly Dictionary<string, string> GenericNameMap = new()
    {
        ["List"] = "System.Collections.Generic.List",
        ["Dictionary"] = "System.Collections.Generic.Dictionary",
        ["HashSet"] = "System.Collections.Generic.HashSet",
        ["IList"] = "System.Collections.Generic.IList",
        ["IDictionary"] = "System.Collections.Generic.IDictionary",
        ["IEnumerable"] = "System.Collections.Generic.IEnumerable",
        ["ICollection"] = "System.Collections.Generic.ICollection",
        ["IReadOnlyList"] = "System.Collections.Generic.IReadOnlyList",
        ["IReadOnlyDictionary"] = "System.Collections.Generic.IReadOnlyDictionary",
        ["Nullable"] = "System.Nullable",
        ["KeyValuePair"] = "System.Collections.Generic.KeyValuePair",
        ["Stack"] = "System.Collections.Generic.Stack",
        ["Queue"] = "System.Collections.Generic.Queue",
        ["LinkedList"] = "System.Collections.Generic.LinkedList",
        ["Task"] = "System.Threading.Tasks.Task",
        ["ValueTuple"] = "System.ValueTuple",
        ["Tuple"] = "System.Tuple",
    };

    private static Assembly[]? _assemblies;

    public static Type Resolve(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        return Cache.GetOrAdd(typeName.Trim(), ResolveCore);
    }

    private static Type ResolveCore(string typeName)
    {
        if (typeName.EndsWith("?") && typeName.Length > 1)
        {
            var wrapped = ResolveCore(typeName[..^1]);
            return typeof(Nullable<>).MakeGenericType(wrapped);
        }

        if (AliasMap.TryGetValue(typeName, out var fullName))
            return LoadType(fullName);

        var genericBracket = FindTopLevelBracket(typeName, '<', '>');
        if (genericBracket >= 0)
            return ResolveGenericType(typeName, genericBracket);

        return LoadType(typeName);
    }

    private static Type LoadType(string typeName)
    {
        var type = Type.GetType(typeName, throwOnError: false);
        if (type != null)
            return type;

        _assemblies ??= AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in _assemblies)
        {
            type = assembly.GetType(typeName, throwOnError: false);
            if (type != null)
                return type;
        }

        var candidates = new List<Type>();
        foreach (var assembly in _assemblies)
        {
            try
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.FullName == typeName || t.Name == typeName)
                        candidates.Add(t);
                }
            }
            catch (ReflectionTypeLoadException)
            {
            }
        }

        if (candidates.Count == 1)
            return candidates[0];

        if (candidates.Count > 1)
            throw new InvalidOperationException(
                $"Ambiguous type '{typeName}': found in multiple assemblies.");

        throw new InvalidOperationException($"Cannot resolve type '{typeName}'.");
    }

    private static Type ResolveGenericType(string typeName, int bracketIndex)
    {
        var openName = typeName[..bracketIndex].Trim();
        var argsPart = typeName[(bracketIndex + 1)..^1].Trim();
        var argStrings = SplitTopLevelCommas(argsPart);
        var typeArgs = argStrings.Select(a => ResolveCore(a.Trim())).ToArray();
        var openType = ResolveOpenGenericType(openName, typeArgs.Length);
        return openType.MakeGenericType(typeArgs);
    }

    private static Type ResolveOpenGenericType(string name, int arity)
    {
        if (GenericNameMap.TryGetValue(name, out var mapped))
        {
            var withArity = $"{mapped}`{arity}";
            var type = Type.GetType(withArity, throwOnError: false);
            if (type != null)
                return type;

            type = SearchAssembliesForGenericDefinition(mapped, arity);
            if (type != null)
                return type;
        }

        var arityName = $"{name}`{arity}";
        var directType = Type.GetType(arityName, throwOnError: false);
        if (directType != null)
            return directType;

        var candidates = SearchAssembliesForGenericDefinition(name, arity);
        if (candidates != null)
            return candidates;

        throw new InvalidOperationException(
            $"Cannot resolve generic type '{name}`{arity}'.");
    }

    private static Type? SearchAssembliesForGenericDefinition(string name, int arity)
    {
        _assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
        var candidates = new List<Type>();

        var targetName = $"{name}`{arity}";

        foreach (var assembly in _assemblies)
        {
            try
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.IsGenericTypeDefinition && t.Name == targetName)
                        candidates.Add(t);
                }
            }
            catch (ReflectionTypeLoadException)
            {
            }
        }

        if (candidates.Count == 1)
            return candidates[0];

        if (candidates.Count > 1)
        {
            var fullNames = string.Join(", ", candidates.Select(c => c.AssemblyQualifiedName));
            throw new InvalidOperationException(
                $"Ambiguous generic type '{targetName}': found in multiple assemblies ({fullNames}).");
        }

        return null;
    }

    private static int FindTopLevelBracket(string s, char open, char close)
    {
        var depth = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == open)
            {
                if (depth == 0) return i;
                depth++;
            }
            else if (s[i] == close)
            {
                if (depth == 0)
                    throw new InvalidOperationException($"Unexpected closing bracket '{close}' at position {i} in '{s}'.");
                depth--;
            }
        }

        if (depth > 0)
            throw new InvalidOperationException($"Unclosed opening bracket '{open}' in '{s}'.");

        return -1;
    }

    private static string[] SplitTopLevelCommas(string s)
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
                    parts.Add(s[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        parts.Add(s[start..].Trim());
        return [.. parts];
    }
}
