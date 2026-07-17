using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;

namespace ImmersingLinker.Core.Services.ThirdParty;

public sealed class ClassIslandService
{
    private IpcClient _client;
    private IPublicLessonsService _lessonsService;
    private IPublicProfileService _profileService;
    private IPublicUriNavigationService _uriNavigationService;

    public ClassIslandService()
    {
        _client = new IpcClient();
        _client.Connect();
        _lessonsService = _client.Provider.CreateIpcProxy<IPublicLessonsService>(_client.PeerProxy);
        _profileService = _client.Provider.CreateIpcProxy<IPublicProfileService>(_client.PeerProxy);
        _uriNavigationService = _client.Provider.CreateIpcProxy<IPublicUriNavigationService>(_client.PeerProxy);
    }
}