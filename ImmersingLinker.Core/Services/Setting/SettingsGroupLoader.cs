using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DynamicExpresso;
using ImmersingLinker.Core.Models.Setting;

namespace ImmersingLinker.Core.Services.Setting;

public sealed class SettingsGroupLoader
{
    private static readonly ConcurrentDictionary<string, Delegate> _validatorCache = new();
    private readonly Dictionary<Type, Func<string, JsonNode?, SettingItemBase>> _loadItemCache = new();

    private readonly Dictionary<string, string> _aliasMap = new()
    {
        ["int"] = "System.Int32",
        ["string"] = "System.String",
        ["bool"] = "System.Boolean",
        ["double"] = "System.Double",
        ["long"] = "System.Int64",
    };

    public SettingsGroup LoadFromJson(string groupKey, JsonNode? node)
    {
        var groupName = node?["name"]?.GetValue<string>() ?? "NULL";
        var description = node?["description"]?.GetValue<string>() ?? "NULL";
        List<SettingItemBase> items = [];

        foreach (var (key, value) in node?["items"]?.AsObject() ?? [])
        {
            var general = value?["general"]?.GetValue<string>();
            SettingItemBase item;

            if (general is null)
            {
                item = LoadFromJson(key, value);
            }
            else
            {
                var typeName = _aliasMap.TryGetValue(general, out var mapped) ? mapped : general;
                var type = Type.GetType(typeName)
                    ?? throw new InvalidOperationException($"Cannot resolve type '{general}'.");

                if (!_loadItemCache.TryGetValue(type, out var loadFunc))
                {
                    var method = typeof(SettingsGroupLoader)
                        .GetMethod(nameof(LoadSettingItem), BindingFlags.Public | BindingFlags.Instance)!
                        .MakeGenericMethod(type);
                    loadFunc = (Func<string, JsonNode?, SettingItemBase>)method.CreateDelegate(
                        typeof(Func<string, JsonNode?, SettingItemBase>), this);
                    _loadItemCache[type] = loadFunc;
                }

                item = loadFunc(key, value);
            }

            items.Add(item);
        }

        return new SettingsGroup(items) { Key = groupKey, Name = groupName, Description = description, SettingItems = items };
    }

    public SettingItem<T> LoadSettingItem<T>(string key, JsonNode? node)
    {
        var name = node?["name"]?.GetValue<string>() ?? "NULL";
        var description = node?["description"]?.GetValue<string>() ?? "NULL";
        var validatorScript = node?["validator"]?.GetValue<string>() ?? "";

        return new SettingItem<T>
        {
            Key = key,
            Name = name,
            Description = description,
            DefaultValue = node?["default-value"] is JsonValue jv
                ? jv.GetValue<T>()
                : default,
            Validator = string.IsNullOrWhiteSpace(validatorScript)
                ? _ => true
                : ParseValidator<T>(validatorScript)
        };
    }

    private static Func<T?, bool> ParseValidator<T>(string script)
    {
        var cacheKey = $"{typeof(T).FullName}::{script}";
        return (Func<T?, bool>)_validatorCache.GetOrAdd(cacheKey, _ =>
        {
            var body = ExtractExpressionBody(script);
            return new Interpreter().ParseAsDelegate<Func<T?, bool>>(body, "x");
        });
    }

    private static string ExtractExpressionBody(string script)
    {
        var arrowIndex = script.IndexOf("=>", StringComparison.Ordinal);
        return arrowIndex >= 0
            ? script[(arrowIndex + 2)..].Trim()
            : script.Trim();
    }
}