namespace ImmersingLinker.Core.Models;

public abstract class StudentExtraProperty
{
    public Application Application { get; init; }
    public string Name { get; init; }
    public object? Value { get; set; }
}

public class StudentExtraProperty<T> : StudentExtraProperty
{
    public T? Value { get; set; }
}