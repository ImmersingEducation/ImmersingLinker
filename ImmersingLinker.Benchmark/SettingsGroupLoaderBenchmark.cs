using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using ImmersingLinker.Core.Models.Setting;
using ImmersingLinker.Core.Services.Setting;

namespace ImmersingLinker.Benchmark;

[MemoryDiagnoser]
public class SettingsGroupLoaderBenchmark
{
    private SettingsGroupLoader _loader = null!;
    private JsonNode _basicJson = null!;
    private JsonNode _itemWithValidator = null!;
    private JsonNode _itemWithoutValidator = null!;
    private JsonNode _nestedJson = null!;
    private JsonNode _largeJson = null!;

    [GlobalSetup]
    public void Setup()
    {
        _loader = new SettingsGroupLoader();

        _basicJson = JsonNode.Parse("""
            {
                "name": "基本设置",
                "items": {
                    "launch-on-startup": {
                        "general": "bool",
                        "name": "开机自动启动",
                        "default-value": false,
                        "validator": "x => true"
                    },
                    "server-port": {
                        "general": "int",
                        "name": "服务器端口",
                        "default-value": 5157,
                        "validator": "x => x >= 0 && x <= 65535"
                    }
                }
            }
            """);

        _itemWithValidator = JsonNode.Parse("""
            {"name": "Port", "default-value": 8080, "validator": "x => x >= 0 && x <= 65535"}
            """);

        _itemWithoutValidator = JsonNode.Parse("""
            {"name": "Port", "default-value": 8080, "validator": ""}
            """);

        _nestedJson = JsonNode.Parse("""
            {
                "name": "Root",
                "items": {
                    "network": {
                        "name": "网络设置",
                        "items": {
                            "port": {
                                "general": "int",
                                "name": "端口",
                                "default-value": 8080,
                                "validator": "x => x >= 0 && x <= 65535"
                            },
                            "timeout": {
                                "general": "double",
                                "name": "超时",
                                "default-value": 30.0,
                                "validator": "x => x > 0"
                            }
                        }
                    },
                    "display": {
                        "name": "显示设置",
                        "items": {
                            "fullscreen": {
                                "general": "bool",
                                "name": "全屏",
                                "default-value": true,
                                "validator": ""
                            }
                        }
                    }
                }
            }
            """);

        BuildLargeJson(100);
    }

    private void BuildLargeJson(int itemCount)
    {
        var items = new List<string>();
        for (var i = 0; i < itemCount; i++)
        {
            var type = (i % 3) switch { 0 => "bool", 1 => "int", _ => "string" };
            var defaultVal = type switch { "bool" => "false", "int" => i.ToString(), _ => "\"value-" + i + "\"" };
            var validator = type switch { "bool" => "\"x => true\"", "int" => "\"x => x >= 0\"", _ => "\"x => x != null\"" };
            var key = "item-" + i.ToString("D4");
            items.Add("\"" + key + "\":{\"general\":\"" + type + "\",\"name\":\"Item " + i + "\",\"default-value\":" + defaultVal + ",\"validator\":" + validator + "}");
        }
        var json = "{\"name\":\"Large\",\"items\":{" + string.Join(",", items) + "}}";
        _largeJson = JsonNode.Parse(json)!;
    }

    [Benchmark(Description = "LoadFromJson 2 items")]
    public SettingsGroup LoadFromJson_Basic() => _loader.LoadFromJson("group", _basicJson);

    [Benchmark(Description = "LoadFromJson 100 items")]
    public SettingsGroup LoadFromJson_Large() => _loader.LoadFromJson("large", _largeJson);

    [Benchmark(Description = "LoadFromJson nested (2 groups, 3 items)")]
    public SettingsGroup LoadFromJson_Nested() => _loader.LoadFromJson("root", _nestedJson);

    [Benchmark(Description = "LoadSettingItem<int> with validator")]
    public SettingItem<int> LoadSettingItem_WithValidator() =>
        _loader.LoadSettingItem<int>("port", _itemWithValidator);

    [Benchmark(Description = "LoadSettingItem<int> without validator")]
    public SettingItem<int> LoadSettingItem_WithoutValidator() =>
        _loader.LoadSettingItem<int>("port", _itemWithoutValidator);
}
