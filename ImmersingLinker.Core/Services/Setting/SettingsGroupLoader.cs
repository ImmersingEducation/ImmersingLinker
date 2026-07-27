using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using ImmersingLinker.Core.Models.Setting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace ImmersingLinker.Core.Services.Setting;

public sealed class SettingsGroupLoader
{
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

                var method = typeof(SettingsGroupLoader)
                    .GetMethod(nameof(LoadSettingItem), BindingFlags.Public | BindingFlags.Instance)!
                    .MakeGenericMethod(type);

                item = (SettingItemBase)method.Invoke(this, [key, value])!;
            }

            items.Add(item);
        }

        return new SettingsGroup(items) { Key = groupKey, Name = groupName, SettingItems = items };
    }

    public SettingItem<T> LoadSettingItem<T>(string key, JsonNode? node)
    {
        var name = node?["name"]?.GetValue<string>() ?? "NULL";
        var validatorScript = node?["validator"]?.GetValue<string>() ?? "";

        return new SettingItem<T>
        {
            Key = key,
            Name = name,
            DefaultValue = node?["default-value"] is JsonValue jv
                ? jv.GetValue<T>()
                : default,
            Validator = string.IsNullOrWhiteSpace(validatorScript)
                ? _ => true
                : CSharpScript.EvaluateAsync<Func<T?, bool>>(validatorScript).GetAwaiter().GetResult()
        };
    }
}