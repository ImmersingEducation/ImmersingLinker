namespace ImmersingLinker.Core.Abstractions.Storage;

public interface IStorageService<T>
{
    Task<T?> LoadAsync();
    Task SaveAsync(T data);
}