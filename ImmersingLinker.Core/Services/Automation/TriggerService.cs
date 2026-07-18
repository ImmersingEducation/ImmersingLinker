using System.Reflection;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Attributes.Automation;

namespace ImmersingLinker.Core.Services.Automation;

public sealed class TriggerService : ITriggerService
{
    private readonly Dictionary<string, Type> _triggerTypes = [];

    public void RegisterTrigger(Type triggerType)
    {
        var attr = triggerType.GetCustomAttribute<TriggerAttribute>(inherit: false);
        if (attr is null) return;

        _triggerTypes[attr.Key] = triggerType;
    }

    public Type? GetTrigger(string key)
    {
        return _triggerTypes.TryGetValue(key, out var type) ? type : null;
    }

    public bool UnregisterTrigger(string key)
    {
        return _triggerTypes.Remove(key);
    }

    public void ScanAssembly(Assembly assembly)
    {
        var triggerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Trigger).IsAssignableFrom(t));

        foreach (var type in triggerTypes)
        {
            RegisterTrigger(type);
        }
    }
}
