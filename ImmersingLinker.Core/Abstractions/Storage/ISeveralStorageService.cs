namespace ImmersingLinker.Core.Abstractions.Storage;

public interface ISeveralStorageService<in TId, TInfo, TData>
{
    Task<List<TInfo>> GetInfos();
    Task<TData?> GetData(TId id);
    Task SaveData(TData data);
    void DeleteData(TId id);
}