using System.Reflection;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Attributes.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Core.Services.Automation;

public sealed class ActionService : IActionService
{
    private readonly Dictionary<string, Type> _actionTypes = [];

    public void RegisterAction(Type actionType)
    {
        var attr = actionType.GetCustomAttribute<ActionAttribute>(inherit: false);
        if (attr is null) return;

        _actionTypes[attr.Key] = actionType;
    }

    public Type? GetAction(string key)
    {
        return _actionTypes.TryGetValue(key, out var type) ? type : null;
    }

    public bool UnregisterAction(string key)
    {
        return _actionTypes.Remove(key);
    }

    public void ScanAssembly(Assembly assembly)
    {
        var actionTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(Action).IsAssignableFrom(t));

        foreach (var type in actionTypes)
        {
            RegisterAction(type);
        }
    }
}
