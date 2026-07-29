using System.Text.Json;
using System.Text.Json.Nodes;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.Setting;
using ImmersingLinker.Core.Services.Setting;
using ImmersingLinker.Core.Services.Storage;
using ImmersingLinker.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImmersingLinker.Test.Controllers;

public class SettingsControllerTest
{
    private static readonly SettingsGroupLoader Loader = new();

    #region Test Data

    private static SettingsGroup CreateBasicGroup()
    {
        return Loader.LoadFromJson("group.basic", JsonNode.Parse("""
            {
                "name": "Basic Settings",
                "description": "Basic group",
                "items": {
                    "group.basic.bool-item": {
                        "general": "bool",
                        "name": "Bool Item",
                        "description": "A bool setting",
                        "default-value": false,
                        "validator": "x => true"
                    },
                    "group.basic.int-item": {
                        "general": "int",
                        "name": "Int Item",
                        "description": "An int setting",
                        "default-value": 5157,
                        "validator": "x => x >= 0 && x <= 65535"
                    }
                }
            }
            """));
    }

    private static SettingsGroup CreateNestedGroup()
    {
        return Loader.LoadFromJson("group.nested", JsonNode.Parse("""
            {
                "name": "Nested Group",
                "description": "A nested structure",
                "items": {
                    "group.nested.sub": {
                        "name": "Sub Group",
                        "description": "A sub group",
                        "items": {
                            "group.nested.sub.string-item": {
                                "general": "string",
                                "name": "String Item",
                                "description": "A string setting",
                                "default-value": "hello",
                                "validator": ""
                            }
                        }
                    },
                    "group.nested.leaf": {
                        "general": "double",
                        "name": "Leaf Double",
                        "description": "A double setting",
                        "default-value": 3.14,
                        "validator": ""
                    }
                }
            }
            """));
    }

    private static async Task<SettingsService> CreateInitializedService(
        Dictionary<string, Dictionary<string, JsonElement>>? savedData = null)
    {
        var mockStorage = new Mock<ISettingsStorageService>();
        mockStorage.Setup(s => s.LoadAsync())
            .ReturnsAsync(savedData);
        mockStorage.Setup(s => s.SaveAsync(It.IsAny<Dictionary<string, Dictionary<string, JsonElement>>>()))
            .Returns(Task.CompletedTask);

        var service = new SettingsService(mockStorage.Object);
        service.MountSettingsGroup(CreateBasicGroup());
        service.MountSettingsGroup(CreateNestedGroup());
        await service.InitializeAsync();
        return service;
    }

    private static SettingsController CreateController(SettingsService? service = null)
    {
        service ??= CreateInitializedService().GetAwaiter().GetResult();
        return new SettingsController(service);
    }

    private static UpdateSettingValueRequest CreateRequest(object value)
    {
        var element = JsonSerializer.SerializeToElement(value);
        return new UpdateSettingValueRequest { Value = element };
    }

    #endregion

    #region GET

    [Fact]
    public void Get_NoPath_ReturnsAllGroupSummaries()
    {
        var controller = CreateController();

        var result = controller.Get(null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var groups = Assert.IsType<List<SettingsGroupSummaryDto>>(okResult.Value);
        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, g => g.Key == "group.basic");
        Assert.Contains(groups, g => g.Key == "group.nested");
    }

    [Fact]
    public void Get_TopLevelGroup_ReturnsGroupDetail()
    {
        var controller = CreateController();

        var result = controller.Get("group.basic");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var group = Assert.IsType<SettingsGroupDetailDto>(okResult.Value);
        Assert.Equal("group.basic", group.Key);
        Assert.Equal("Basic Settings", group.Name);
        Assert.Equal(2, group.Items.Count);
    }

    [Fact]
    public void Get_NestedGroup_ReturnsGroupDetail()
    {
        var controller = CreateController();

        var result = controller.Get("group.nested/group.nested.sub");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var group = Assert.IsType<SettingsGroupDetailDto>(okResult.Value);
        Assert.Equal("group.nested.sub", group.Key);
        Assert.Single(group.Items);
    }

    [Fact]
    public void Get_TopLevelLeafItem_ReturnsItemValue()
    {
        var controller = CreateController();

        var result = controller.Get("group.basic/group.basic.bool-item");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SettingItemValueDto>(okResult.Value);
        Assert.Equal("group.basic.bool-item", dto.Key);
        Assert.Equal("Bool Item", dto.Name);
        Assert.Equal("Boolean", dto.Type);
        Assert.False(dto.DefaultValue?.GetBoolean());
    }

    [Fact]
    public void Get_DeepNestedLeafItem_ReturnsItemValue()
    {
        var controller = CreateController();

        var result = controller.Get("group.nested/group.nested.sub/group.nested.sub.string-item");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SettingItemValueDto>(okResult.Value);
        Assert.Equal("group.nested.sub.string-item", dto.Key);
        Assert.Equal("String", dto.Type);
    }

    [Fact]
    public void Get_NonExistentPath_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = controller.Get("nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Get_NonExistentDeepPath_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = controller.Get("group.basic/nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_LeafItemWithSavedValue_ReturnsValue()
    {
        var service = await CreateInitializedService(new Dictionary<string, Dictionary<string, JsonElement>>
        {
            ["group.basic"] = new()
            {
                ["group.basic.int-item"] = JsonSerializer.SerializeToElement(9999)
            }
        });
        var controller = CreateController(service);

        var result = controller.Get("group.basic/group.basic.int-item");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SettingItemValueDto>(okResult.Value);
        Assert.Equal(9999, dto.Value?.GetInt32());
    }

    #endregion

    #region PUT

    [Fact]
    public void Update_ValidBoolValue_ReturnsOk()
    {
        var controller = CreateController();

        var result = controller.Update("group.basic/group.basic.bool-item", CreateRequest(true));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SettingItemValueDto>(okResult.Value);
        Assert.True(dto.Value?.GetBoolean());
    }

    [Fact]
    public void Update_ValidIntValue_ReturnsOk()
    {
        var controller = CreateController();

        var result = controller.Update("group.basic/group.basic.int-item", CreateRequest(8080));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SettingItemValueDto>(okResult.Value);
        Assert.Equal(8080, dto.Value?.GetInt32());
    }

    [Fact]
    public void Update_NestedPath_ReturnsOk()
    {
        var controller = CreateController();

        var result = controller.Update(
            "group.nested/group.nested.sub/group.nested.sub.string-item",
            CreateRequest("world"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SettingItemValueDto>(okResult.Value);
        Assert.Equal("world", dto.Value?.GetString());
    }

    [Fact]
    public void Update_EmptyPath_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.Update("", CreateRequest(true));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Update_NonExistentPath_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = controller.Update("nonexistent", CreateRequest(true));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Update_GroupPath_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.Update("group.basic", CreateRequest(true));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Update_InvalidValue_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.Update(
            "group.basic/group.basic.int-item",
            new UpdateSettingValueRequest
            {
                Value = JsonSerializer.SerializeToElement("not-a-number")
            });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Update_NullPath_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.Update(null!, CreateRequest(true));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region POST

    [Fact]
    public async Task Save_CallsStorageSave_ReturnsOk()
    {
        var controller = CreateController();

        var result = await controller.Save();

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Reload_CallsStorageLoad_ReturnsOk()
    {
        var controller = CreateController();

        var result = await controller.Reload();

        Assert.IsType<OkResult>(result);
    }

    #endregion
}
