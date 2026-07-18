using System.Text.Json;
using System.Text.Json.Serialization;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Services.Storage;

public sealed class AutomationStorageService : IAutomationStorageService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new PolymorphicTypeResolver(
            typeof(Trigger),
            typeof(Abstractions.Automation.Action),
            typeof(RuleBase)),
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string _dataDirectory =
        Path.Combine(AppContext.BaseDirectory, "Data", "Automation");

    public AutomationStorageService()
    {
        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<List<AutomationPlanInfo>> GetPlanInfos()
    {
        List<AutomationPlanInfo> infos = [];
        var dataDir = new DirectoryInfo(_dataDirectory);
        if (!dataDir.Exists) return infos;

        foreach (var file in dataDir.GetFiles("*.json"))
        {
            var guid = Guid.Parse(Path.GetFileNameWithoutExtension(file.Name));
            var plan = await GetPlan(guid);
            if (plan is not null)
            {
                infos.Add(new AutomationPlanInfo
                {
                    Guid = guid,
                    Name = plan.Name
                });
            }
        }

        return infos;
    }

    public async Task<AutomationPlan?> GetPlan(Guid guid)
    {
        var path = Path.Combine(_dataDirectory, $"{guid}.json");
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<AutomationPlan>(json, _options);
    }

    public async Task SavePlan(AutomationPlan plan)
    {
        var path = Path.Combine(_dataDirectory, $"{plan.Guid}.json");
        var json = JsonSerializer.Serialize(plan, _options);
        await File.WriteAllTextAsync(path, json);
    }

    public void DeletePlan(Guid guid)
    {
        var path = Path.Combine(_dataDirectory, $"{guid}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}

public class AutomationPlanInfo
{
    public Guid Guid { get; init; }
    public string Name { get; init; } = string.Empty;
}
