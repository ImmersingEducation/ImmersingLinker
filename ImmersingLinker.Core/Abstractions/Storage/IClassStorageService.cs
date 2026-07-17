using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.Core.Services.Storage;

public interface IClassStorageService
{
    Task<List<ClassInfo>> GetClassInfos();
    Task<Class?> GetClass(Guid guid);
    Task SaveClass(Class @class);
    void DeleteClass(Guid guid);
}
