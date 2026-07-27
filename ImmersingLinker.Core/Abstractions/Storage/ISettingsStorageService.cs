using System.Text.Json;

namespace ImmersingLinker.Core.Services.Storage;

public interface ISettingsStorageService
{
    Task<Dictionary<string, Dictionary<string, JsonElement>>?> LoadSettingsAsync();

    Task SaveSettingsAsync(Dictionary<string, Dictionary<string, JsonElement>> data);
}
