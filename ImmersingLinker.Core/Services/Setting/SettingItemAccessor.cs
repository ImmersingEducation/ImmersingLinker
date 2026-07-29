using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ImmersingLinker.Core.Services.Setting;

public static class SettingItemAccessor
{
    private static readonly ConcurrentDictionary<Type, (Func<object, object?>? Getter, Action<object, object?>? Setter)> _valueAccessors = new();

    public static (Func<object, object?>? Getter, Action<object, object?>? Setter) GetOrCreateValueAccessors(Type type)
    {
        return _valueAccessors.GetOrAdd(type, t =>
        {
            var prop = t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) return (null, null);

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var instanceCast = Expression.Convert(instanceParam, t);
            var propertyAccess = Expression.Property(instanceCast, prop);

            var getter = Expression.Lambda<Func<object, object?>>(
                Expression.Convert(propertyAccess, typeof(object)), instanceParam).Compile();

            var valueParam = Expression.Parameter(typeof(object), "value");
            var setter = Expression.Lambda<Action<object, object?>>(
                Expression.Assign(propertyAccess, Expression.Convert(valueParam, prop.PropertyType)),
                instanceParam, valueParam).Compile();

            return (getter, setter);
        });
    }
}
