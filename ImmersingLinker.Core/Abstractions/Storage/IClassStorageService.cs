using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.Core.Abstractions.Storage;

public interface IClassStorageService : ISeveralStorageService<Guid, ClassInfo, Class>;