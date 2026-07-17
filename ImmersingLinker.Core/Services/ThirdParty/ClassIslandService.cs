using ClassIsland.Shared.Enums;
using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;

namespace ImmersingLinker.Core.Services.ThirdParty;

public sealed class ClassIslandService
{
    private IpcClient _client;
    private IPublicLessonsService _lessonsService;
    private IPublicProfileService _profileService;
    private IPublicUriNavigationService _uriNavigationService;

    public Subject? CurrentSubject => _lessonsService.CurrentSubject;
    public Subject NextClassSubject => _lessonsService.NextClassSubject;
    public TimeState CurrentState => _lessonsService.CurrentState;
    public TimeLayoutItem CurrentTimeLayoutItem => _lessonsService.CurrentTimeLayoutItem;
    public ClassPlan? CurrentClassPlan => _lessonsService.CurrentClassPlan;
    public TimeLayoutItem NextBreakingTimeLayoutItem => _lessonsService.NextBreakingTimeLayoutItem;
    public TimeLayoutItem NextClassTimeLayoutItem => _lessonsService.NextClassTimeLayoutItem;
    public int CurrentSelectedIndex =>  _lessonsService.CurrentSelectedIndex;
    public TimeSpan OnClassLeftTime => _lessonsService.OnClassLeftTime;
    public TimeSpan OnBreakingLeftTime => _lessonsService.OnBreakingTimeLeftTime;
    public bool IsClassPlanEnabled => _lessonsService.IsClassPlanEnabled;
    public bool IsClassPlanLoaded =>  _lessonsService.IsClassPlanLoaded;
    public bool IsLessonConfirmed => _lessonsService.IsLessonConfirmed;
    
    public ClassIslandService()
    {
        _client = new IpcClient();
        _client.Connect();
        _lessonsService = _client.Provider.CreateIpcProxy<IPublicLessonsService>(_client.PeerProxy);
        _profileService = _client.Provider.CreateIpcProxy<IPublicProfileService>(_client.PeerProxy);
        _uriNavigationService = _client.Provider.CreateIpcProxy<IPublicUriNavigationService>(_client.PeerProxy);
    }
}