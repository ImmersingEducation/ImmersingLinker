using System.Text.Json;

namespace ImmersingLinker.Core.Abstractions.Storage;

public interface ISettingsStorageService : IStorageService<Dictionary<string, Dictionary<string, JsonElement>>>;