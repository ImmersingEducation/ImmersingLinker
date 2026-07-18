using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ImmersingLinker.Core.Services.Storage;

public sealed class PolymorphicTypeResolver : DefaultJsonTypeInfoResolver
{
    private readonly Type[] _baseTypes;

    public PolymorphicTypeResolver(params Type[] baseTypes)
    {
        _baseTypes = baseTypes;
        Modifiers.Add(ApplyPolymorphism);
    }

    private void ApplyPolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
        if (_baseTypes.All(b => b != typeInfo.Type)) return;

        var derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.GetName().Name?.StartsWith("System") != true)
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeInfo.Type.IsAssignableFrom(t));

        foreach (var derivedType in derivedTypes)
        {
            typeInfo.PolymorphismOptions!.DerivedTypes.Add(new JsonDerivedType(derivedType, derivedType.Name));
        }
    }
}
