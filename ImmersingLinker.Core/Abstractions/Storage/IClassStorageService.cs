using ImmersingLinker.Core.Models.Class;
using ImmersingLinker.Core.Services.Storage;

namespace ImmersingLinker.Core.Abstractions.Storage;

public interface IClassStorageService : ISeveralStorageService<Guid, ClassInfo, Class>;
