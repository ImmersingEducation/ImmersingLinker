using System.Reflection;
using System.Text.Json;
using ImmersingLinker.Core.Models.Setting;
using ImmersingLinker.Core.Services.Setting;
using Microsoft.AspNetCore.Mvc;

namespace ImmersingLinker.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class SettingsController : ControllerBase
{
    private readonly SettingsService _settingsService;

    public SettingsController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    ///     获取设置项/组。路径为空时返回所有顶级组概要，有路径时按 "/" 分隔逐层查找。
    /// </summary>
    /// <param name="settingPath">点号分隔的路径，如 "groupKey/subKey/subSubKey/itemKey"</param>
    [HttpGet("{**settingPath}")]
    public async Task<IActionResult> Get(string? settingPath)
    {
        if (string.IsNullOrEmpty(settingPath))
            return Ok(GetAllGroupSummaries());

        var keys = settingPath.Split('/');
        SettingItemBase item;
        try
        {
            item = await _settingsService.GetSettingItem(keys);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        if (item is SettingsGroup group)
            return Ok(BuildGroupDetail(group));

        return Ok(BuildItemValueDto(item));
    }

    /// <summary>
    ///     更新指定路径的设置项值
    /// </summary>
    /// <param name="settingPath">点号分隔的路径，必须指向叶子设置项</param>
    /// <param name="request">新值</param>
    [HttpPut("{**settingPath}")]
    public async Task<IActionResult> Update(string settingPath,
        [FromBody] UpdateSettingValueRequest request)
    {
        if (string.IsNullOrEmpty(settingPath))
            return BadRequest("Setting path is required.");

        var keys = settingPath.Split('/');
        SettingItemBase item;
        try
        {
            item = await _settingsService.GetSettingItem(keys);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        if (item is SettingsGroup)
            return BadRequest("Cannot update a settings group. Point to a leaf setting item.");

        var itemType = item.GetType();
        if (!itemType.IsGenericType || itemType.GetGenericTypeDefinition() != typeof(SettingItem<>))
            return BadRequest("Unsupported setting item type.");

        var valueType = itemType.GetGenericArguments()[0];

        object? newValue;
        try
        {
            newValue = JsonSerializer.Deserialize(request.Value.GetRawText(), valueType);
        }
        catch (JsonException ex)
        {
            return BadRequest($"Invalid value for type {valueType.Name}: {ex.Message}");
        }

        var (_, setter) = GetOrCreateValueAccessors(itemType);
        if (setter is null)
            return BadRequest("Cannot set value for this setting item.");

        try
        {
            setter(item, newValue);
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"Validation failed: {ex.Message}");
        }

        return Ok(BuildItemValueDto(item));
    }

    /// <summary>
    ///     强制保存所有设置到文件
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save()
    {
        await _settingsService.SaveSettingsAsync();
        return Ok();
    }

    /// <summary>
    ///     从文件重新加载设置值（不重新 Mount）
    /// </summary>
    [HttpPost("reload")]
    public async Task<IActionResult> Reload()
    {
        await _settingsService.LoadSettingsAsync();
        return Ok();
    }

    #region Helpers

    private List<SettingsGroupSummaryDto> GetAllGroupSummaries()
    {
        return _settingsService.GetAllGroups()
            .Select(g => new SettingsGroupSummaryDto
            {
                Key = g.Key,
                Name = g.Name,
                Description = g.Description,
                ItemCount = g.SettingItems.Count
            })
            .ToList();
    }

    private static SettingsGroupDetailDto BuildGroupDetail(SettingsGroup group)
    {
        return new SettingsGroupDetailDto
        {
            Key = group.Key,
            Name = group.Name,
            Description = group.Description,
            Items = group.SettingItems.Select(BuildItemValueDto).ToList()
        };
    }

    private static SettingItemValueDto BuildItemValueDto(SettingItemBase item)
    {
        var dto = new SettingItemValueDto
        {
            Key = item.Key,
            Name = item.Name,
            Description = item.Description
        };

        if (item is SettingsGroup subGroup)
        {
            dto.Type = "group";
            dto.Items = subGroup.SettingItems.Select(BuildItemValueDto).ToList();
        }
        else
        {
            var itemType = item.GetType();
            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(SettingItem<>))
            {
                var valueType = itemType.GetGenericArguments()[0];
                dto.Type = valueType.Name;

                var (getter, _) = GetOrCreateValueAccessors(itemType);
                var value = getter?.Invoke(item);
                dto.Value = value is null ? null : JsonSerializer.SerializeToElement(value, value.GetType());

                var defaultValueProp = itemType.GetProperty("DefaultValue", BindingFlags.Public | BindingFlags.Instance);
                var defaultValue = defaultValueProp?.GetValue(item);
                dto.DefaultValue = defaultValue is null ? null : JsonSerializer.SerializeToElement(defaultValue, defaultValue.GetType());
            }
        }

        return dto;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type,
        (Func<object, object?>? Getter, Action<object, object?>? Setter)> _valueAccessors = new();

    private static (Func<object, object?>? Getter, Action<object, object?>? Setter) GetOrCreateValueAccessors(Type type)
    {
        return _valueAccessors.GetOrAdd(type, t =>
        {
            var prop = t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) return (null, null);

            var instanceParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "instance");
            var instanceCast = System.Linq.Expressions.Expression.Convert(instanceParam, t);
            var propertyAccess = System.Linq.Expressions.Expression.Property(instanceCast, prop);

            var getter = System.Linq.Expressions.Expression.Lambda<Func<object, object?>>(
                System.Linq.Expressions.Expression.Convert(propertyAccess, typeof(object)), instanceParam).Compile();

            var valueParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "value");
            var setter = System.Linq.Expressions.Expression.Lambda<Action<object, object?>>(
                System.Linq.Expressions.Expression.Assign(propertyAccess,
                    System.Linq.Expressions.Expression.Convert(valueParam, prop.PropertyType)),
                instanceParam, valueParam).Compile();

            return (getter, setter);
        });
    }

    #endregion
}

#region DTOs

public class SettingsGroupSummaryDto
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public int ItemCount { get; init; }
}

public class SettingsGroupDetailDto
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public required List<SettingItemValueDto> Items { get; init; }
}

public class SettingItemValueDto
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string Type { get; set; } = "unknown";
    public JsonElement? Value { get; set; }
    public JsonElement? DefaultValue { get; set; }
    public List<SettingItemValueDto>? Items { get; set; }
}

public class UpdateSettingValueRequest
{
    public required JsonElement Value { get; init; }
}

#endregion