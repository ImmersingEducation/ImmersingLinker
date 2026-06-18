using System.Text.Json;
using System.Text.Json.Serialization;
using ImmersingLinker.Core.Models;

namespace ImmersingLinker.SDK;

public class ClassExtraPropertyConverter : JsonConverter<ClassExtraProperty>
{
    public override ClassExtraProperty? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<ClassExtraProperty<object>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, ClassExtraProperty value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

public class StudentExtraPropertyConverter : JsonConverter<StudentExtraProperty>
{
    public override StudentExtraProperty? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<StudentExtraProperty<object>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, StudentExtraProperty value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
