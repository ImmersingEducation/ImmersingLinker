using System.Text.Json;
using ImmersingLinker.Core.Services.Storage;

namespace ImmersingLinker.Core.Abstractions.Storage;

public interface ISettingsStorageService : IStorageService<Dictionary<string, Dictionary<string, JsonElement>>>;
