using System.Text.Json;
using ImmersingLinker.Core.Services.Storage;

namespace ImmersingLinker.Test.Setting;

public class SettingsStorageServiceTest
{
    private static string GetTempFilePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ImmersingLinkerTest", Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "Settings.json");
    }

    #region LoadAsync

    [Fact]
    public async Task LoadSettingsAsync_FileNotExists_ReturnsNull()
    {
        var path = GetTempFilePath();
        var service = new SettingsStorageService(path);

        var result = await service.LoadAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task LoadSettingsAsync_ValidFile_ReturnsParsedData()
    {
        var path = GetTempFilePath();
        var json = """
            {
                "iedu.ilinker.basic": {
                    "iedu.ilinker.basic.launch-on-startup": false,
                    "iedu.ilinker.basic.volume": 50
                }
            }
            """;
        await File.WriteAllTextAsync(path, json);
        var service = new SettingsStorageService(path);

        var result = await service.LoadAsync();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey("iedu.ilinker.basic"));
        var basic = result["iedu.ilinker.basic"];
        Assert.Equal(2, basic.Count);
        Assert.False(basic["iedu.ilinker.basic.launch-on-startup"].GetBoolean());
        Assert.Equal(50, basic["iedu.ilinker.basic.volume"].GetInt32());
    }

    [Fact]
    public async Task LoadSettingsAsync_MultipleGroups_ReturnsAllGroups()
    {
        var path = GetTempFilePath();
        var json = """
            {
                "group.alpha": {
                    "alpha.key1": "hello"
                },
                "group.beta": {
                    "beta.key1": 42,
                    "beta.key2": true
                }
            }
            """;
        await File.WriteAllTextAsync(path, json);
        var service = new SettingsStorageService(path);

        var result = await service.LoadAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("hello", result["group.alpha"]["alpha.key1"].GetString());
        Assert.Equal(42, result["group.beta"]["beta.key1"].GetInt32());
        Assert.True(result["group.beta"]["beta.key2"].GetBoolean());
    }

    [Fact]
    public async Task LoadSettingsAsync_EmptyJsonObject_ReturnsEmptyDictionary()
    {
        var path = GetTempFilePath();
        await File.WriteAllTextAsync(path, "{}");
        var service = new SettingsStorageService(path);

        var result = await service.LoadAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadSettingsAsync_EmptyGroup_ReturnsGroupWithNoItems()
    {
        var path = GetTempFilePath();
        var json = """{"group.empty": {}}""";
        await File.WriteAllTextAsync(path, json);
        var service = new SettingsStorageService(path);

        var result = await service.LoadAsync();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Empty(result["group.empty"]);
    }

    #endregion

    #region SaveAsync

    [Fact]
    public async Task SaveSettingsAsync_WritesJsonFile()
    {
        var path = GetTempFilePath();
        var service = new SettingsStorageService(path);
        var data = new Dictionary<string, Dictionary<string, JsonElement>>
        {
            ["test.group"] = new()
            {
                ["test.item"] = JsonSerializer.SerializeToElement(123)
            }
        };

        await service.SaveAsync(data);

        Assert.True(File.Exists(path));
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("test.group", content);
        Assert.Contains("test.item", content);
        Assert.Contains("123", content);
    }

    [Fact]
    public async Task SaveSettingsAsync_FileIsValidJson()
    {
        var path = GetTempFilePath();
        var service = new SettingsStorageService(path);
        var data = new Dictionary<string, Dictionary<string, JsonElement>>
        {
            ["group"] = new()
            {
                ["str"] = JsonSerializer.SerializeToElement("value"),
                ["num"] = JsonSerializer.SerializeToElement(3.14),
                ["flag"] = JsonSerializer.SerializeToElement(false)
            }
        };

        await service.SaveAsync(data);

        var content = await File.ReadAllTextAsync(path);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(content);
        Assert.NotNull(parsed);
        Assert.Equal("value", parsed["group"]["str"].GetString());
        Assert.Equal(3.14, parsed["group"]["num"].GetDouble());
        Assert.False(parsed["group"]["flag"].GetBoolean());
    }

    [Fact]
    public async Task SaveSettingsAsync_EmptyDictionary_WritesEmptyJsonObject()
    {
        var path = GetTempFilePath();
        var service = new SettingsStorageService(path);
        var data = new Dictionary<string, Dictionary<string, JsonElement>>();

        await service.SaveAsync(data);

        var content = await File.ReadAllTextAsync(path);
        Assert.Equal("{}", content.Trim());
    }

    #endregion

    #region RoundTrip

    [Fact]
    public async Task SaveThenLoad_RoundTrip_ReturnsSameData()
    {
        var path = GetTempFilePath();
        var service = new SettingsStorageService(path);
        var original = new Dictionary<string, Dictionary<string, JsonElement>>
        {
            ["alpha"] = new()
            {
                ["x"] = JsonSerializer.SerializeToElement(1),
                ["y"] = JsonSerializer.SerializeToElement("test"),
                ["z"] = JsonSerializer.SerializeToElement(true)
            },
            ["beta"] = new()
            {
                ["w"] = JsonSerializer.SerializeToElement(3.14)
            }
        };

        await service.SaveAsync(original);
        var loaded = await service.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Count);
        Assert.Equal(1, loaded["alpha"]["x"].GetInt32());
        Assert.Equal("test", loaded["alpha"]["y"].GetString());
        Assert.True(loaded["alpha"]["z"].GetBoolean());
        Assert.Equal(3.14, loaded["beta"]["w"].GetDouble());
    }

    [Fact]
    public async Task SaveThenLoad_NullValues_ArePreserved()
    {
        var path = GetTempFilePath();
        var service = new SettingsStorageService(path);
        var original = new Dictionary<string, Dictionary<string, JsonElement>>
        {
            ["g"] = new()
            {
                ["nullable"] = JsonSerializer.SerializeToElement<object?>(null)
            }
        };

        await service.SaveAsync(original);
        var loaded = await service.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(JsonValueKind.Null, loaded["g"]["nullable"].ValueKind);
    }

    #endregion

    [Fact]
    public void Constructor_CreatesDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ImmersingLinkerTest", Guid.NewGuid().ToString());
        var path = Path.Combine(dir, "SubDir", "Settings.json");

        _ = new SettingsStorageService(path);

        Assert.True(Directory.Exists(dir));
    }
}
