using System.Text.Json.Nodes;
using ImmersingLinker.Core.Models.Setting;
using ImmersingLinker.Core.Services.Setting;

namespace ImmersingLinker.Test.Setting;

public class SettingsGroupLoaderTest
{
    private static SettingsGroupLoader CreateLoader()
    {
        return new SettingsGroupLoader();
    }

    #region LoadSettingItem

    [Fact]
    public void LoadSettingItem_BoolTypeWithDefaultFalse_ReturnsCorrectItem()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {"name": "Test", "default-value": false, "validator": ""}
                                  """);

        var item = loader.LoadSettingItem<bool>("test.key", node);

        Assert.Equal("test.key", item.Key);
        Assert.Equal("Test", item.Name);
        Assert.False(item.DefaultValue);
    }

    [Fact]
    public void LoadSettingItem_IntTypeWithDefaultValue_ReturnsCorrectItem()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {"name": "Count", "default-value": 42, "validator": ""}
                                  """);

        var item = loader.LoadSettingItem<int>("test.key", node);

        Assert.Equal(42, item.DefaultValue);
    }

    [Fact]
    public void LoadSettingItem_StringTypeWithDefaultValue_ReturnsCorrectItem()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {"name": "Label", "default-value": "hello", "validator": ""}
                                  """);

        var item = loader.LoadSettingItem<string>("test.key", node);

        Assert.Equal("hello", item.DefaultValue);
    }

    [Fact]
    public void LoadSettingItem_WithValidatorScript_ValidatorEvaluatesCorrectly()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {"name": "Even", "default-value": 0, "validator": "x => x % 2 == 0"}
                                  """);

        var item = loader.LoadSettingItem<int>("test.key", node);

        Assert.True(item.Validator(4));
        Assert.False(item.Validator(3));
    }

    [Fact]
    public void LoadSettingItem_EmptyValidator_AlwaysReturnsTrue()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {"name": "NoValidator", "default-value": 0, "validator": ""}
                                  """);

        var item = loader.LoadSettingItem<int>("test.key", node);

        Assert.True(item.Validator(0));
        Assert.True(item.Validator(42));
    }

    [Fact]
    public void LoadSettingItem_NullNode_UsesFallbackDefaults()
    {
        var loader = CreateLoader();

        var item = loader.LoadSettingItem<int>("test.key", null);

        Assert.Equal("test.key", item.Key);
        Assert.Equal("NULL", item.Name);
        Assert.Equal(0, item.DefaultValue);
        Assert.True(item.Validator(99));
    }

    [Fact]
    public void LoadSettingItem_MissingDefaultValue_UsesTypeDefault()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {"name": "NoDefault", "validator": ""}
                                  """);

        var item = loader.LoadSettingItem<int>("test.key", node);

        Assert.Equal(0, item.DefaultValue);
    }

    #endregion

    #region LoadFromJson

    [Fact]
    public void LoadFromJson_SingleBoolItem_ReturnsGroupWithOneItem()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {
                                      "name": "TestGroup",
                                      "items": {
                                          "setting1": {
                                              "general": "bool",
                                              "name": "Bool Setting",
                                              "default-value": true,
                                              "validator": ""
                                          }
                                      }
                                  }
                                  """);

        var group = loader.LoadFromJson("group.key", node);

        Assert.Equal("group.key", group.Key);
        Assert.Equal("TestGroup", group.Name);
        var item = Assert.Single(group.SettingItems);
        var typedItem = Assert.IsType<SettingItem<bool>>(item);
        Assert.Equal("setting1", typedItem.Key);
        Assert.True(typedItem.DefaultValue);
    }

    [Fact]
    public void LoadFromJson_MultipleItemsDifferentTypes_ReturnsAllItems()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {
                                      "name": "MixedGroup",
                                      "items": {
                                          "boolVal": {
                                              "general": "bool",
                                              "name": "Bool",
                                              "default-value": true,
                                              "validator": ""
                                          },
                                          "intVal": {
                                              "general": "int",
                                              "name": "Int",
                                              "default-value": 42,
                                              "validator": ""
                                          },
                                          "strVal": {
                                              "general": "string",
                                              "name": "Str",
                                              "default-value": "hello",
                                              "validator": ""
                                          }
                                      }
                                  }
                                  """);

        var group = loader.LoadFromJson("group.key", node);

        Assert.Equal(3, group.SettingItems.Count);
        Assert.IsType<SettingItem<bool>>(group.SettingItems[0]);
        Assert.IsType<SettingItem<int>>(group.SettingItems[1]);
        Assert.IsType<SettingItem<string>>(group.SettingItems[2]);
        Assert.Equal("hello", ((SettingItem<string>)group.SettingItems[2]).DefaultValue);
    }

    [Fact]
    public void LoadFromJson_NestedGroup_ReturnsHierarchicalGroup()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {
                                      "name": "Root",
                                      "items": {
                                          "sub": {
                                              "name": "SubGroup",
                                              "items": {
                                                  "leaf": {
                                                      "general": "bool",
                                                      "name": "Leaf",
                                                      "default-value": false,
                                                      "validator": ""
                                                  }
                                              }
                                          }
                                      }
                                  }
                                  """);

        var group = loader.LoadFromJson("root", node);

        Assert.Equal("Root", group.Name);
        var subGroup = Assert.Single(group.SettingItems);
        var typedSub = Assert.IsType<SettingsGroup>(subGroup);
        Assert.Equal("SubGroup", typedSub.Name);
        var leaf = Assert.Single(typedSub.SettingItems);
        Assert.IsType<SettingItem<bool>>(leaf);
    }

    [Fact]
    public void LoadFromJson_ComplexStructure_LoadsAllItems()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {
                                      "name": "ExampleSettingsGroup",
                                      "items": {
                                          "iedu.ilinker.basic": {
                                              "name": "Basic Settings",
                                              "items": {
                                                  "iedu.ilinker.basic.launch-on-startup": {
                                                      "general": "bool",
                                                      "name": "Launch on startup",
                                                      "default-value": false,
                                                      "validator": "x => x == true"
                                                  },
                                                  "iedu.ilinker.basic.volume": {
                                                      "general": "int",
                                                      "name": "Volume",
                                                      "default-value": 50,
                                                      "validator": "x => x >= 0 && x <= 100"
                                                  }
                                              }
                                          },
                                          "iedu.ilinker.advanced": {
                                              "name": "Advanced",
                                              "items": {
                                                  "iedu.ilinker.advanced.timeout": {
                                                      "general": "double",
                                                      "name": "Timeout",
                                                      "default-value": 30.0,
                                                      "validator": ""
                                                  }
                                              }
                                          }
                                      }
                                  }
                                  """);

        var root = loader.LoadFromJson("root", node);

        Assert.Equal("ExampleSettingsGroup", root.Name);
        Assert.Equal(2, root.SettingItems.Count);

        var basic = Assert.IsType<SettingsGroup>(root.SettingItems[0]);
        Assert.Equal("Basic Settings", basic.Name);
        Assert.Equal(2, basic.SettingItems.Count);

        var launchSetting = Assert.IsType<SettingItem<bool>>(basic.SettingItems[0]);
        Assert.False(launchSetting.DefaultValue);
        Assert.True(launchSetting.Validator(true));

        var volumeSetting = Assert.IsType<SettingItem<int>>(basic.SettingItems[1]);
        Assert.Equal(50, volumeSetting.DefaultValue);

        var advanced = Assert.IsType<SettingsGroup>(root.SettingItems[1]);
        var timeout = Assert.IsType<SettingItem<double>>(Assert.Single(advanced.SettingItems));
        Assert.Equal(30.0, timeout.DefaultValue);
    }

    [Fact]
    public void LoadFromJson_NullNode_ReturnsGroupWithFallbackValues()
    {
        var loader = CreateLoader();

        var group = loader.LoadFromJson("fallback", null);

        Assert.Equal("fallback", group.Key);
        Assert.Equal("NULL", group.Name);
        Assert.Empty(group.SettingItems);
    }

    [Fact]
    public void LoadFromJson_EmptyItems_ReturnsEmptyGroup()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {"name": "Empty", "items": {}}
                                  """);

        var group = loader.LoadFromJson("empty", node);

        Assert.Empty(group.SettingItems);
    }

    [Fact]
    public void LoadFromJson_UnknownType_ThrowsInvalidOperationException()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {
                                      "name": "Bad",
                                      "items": {
                                          "badItem": {
                                              "general": "NonExistentType",
                                              "name": "Bad",
                                              "default-value": 0,
                                              "validator": ""
                                          }
                                      }
                                  }
                                  """);

        var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadFromJson("bad", node));
        Assert.Contains("NonExistentType", ex.Message);
    }

    [Fact]
    public void LoadFromJson_DuplicateKeys_ThrowsArgumentException()
    {
        var loader = CreateLoader();
        var node = JsonNode.Parse("""
                                  {
                                      "name": "Dup",
                                      "items": {
                                          "duplicate": {
                                              "general": "bool",
                                              "name": "First",
                                              "default-value": true,
                                              "validator": ""
                                          },
                                          "duplicate": {
                                              "general": "bool",
                                              "name": "Second",
                                              "default-value": false,
                                              "validator": ""
                                          }
                                      }
                                  }
                                  """);

        Assert.Throws<ArgumentException>(() => loader.LoadFromJson("dup", node));
    }

    #endregion
}