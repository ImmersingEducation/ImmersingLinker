namespace ImmersingLinker.Core.Models;

public abstract class ClassExtraProperty
{
    public Application Application { get; init; }
    public string Name { get; init; }
    public object? Value { get; set; }
}

public class ClassExtraProperty<T> : ClassExtraProperty
{
    public T? Value { get; init; }
}