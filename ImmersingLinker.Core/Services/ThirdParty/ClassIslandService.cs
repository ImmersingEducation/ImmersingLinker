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
    
    public string CurrentProfilePath => _profileService.CurrentProfilePath;
    public bool IsCurrentProfileTrusted => _profileService.IsCurrentProfileTrusted;
    public Profile Profile => _profileService.Profile;

    public TimeLayoutItem PreviousClassTimeLayoutItem => FindPreviousTimeLayoutItem(0);
    public TimeLayoutItem PreviousBreakingTimeLayoutItem => FindPreviousTimeLayoutItem(1);

    public Subject? PreviousClassSubject
    {
        get
        {
            var plan = _lessonsService.CurrentClassPlan;
            if (plan == null) return null;

            var items = plan.ValidTimeLayoutItems;
            var idx = _lessonsService.CurrentSelectedIndex;
            if (idx <= 0) return null;

            int classCountBefore = 0;
            for (int i = 0; i < idx && i < items.Count; i++)
            {
                if (items[i].TimeType == 0)
                    classCountBefore++;
            }

            if (classCountBefore == 0 || classCountBefore > plan.Classes.Count)
                return null;

            var subjectId = plan.Classes[classCountBefore - 1].SubjectId;
            return _profileService.Profile.Subjects.TryGetValue(subjectId, out var subject)
                ? subject
                : Subject.Fallback;
        }
    }

    public TimeSpan ElapsedSincePreviousClass =>
        PreviousClassTimeLayoutItem == TimeLayoutItem.Empty
            ? TimeSpan.Zero
            : DateTime.Now.TimeOfDay - PreviousClassTimeLayoutItem.EndTime;

    public TimeSpan ElapsedSincePreviousBreaking =>
        PreviousBreakingTimeLayoutItem == TimeLayoutItem.Empty
            ? TimeSpan.Zero
            : DateTime.Now.TimeOfDay - PreviousBreakingTimeLayoutItem.EndTime;

    public TimeSpan ElapsedSincePreviousAny
    {
        get
        {
            var plan = _lessonsService.CurrentClassPlan;
            if (plan == null) return TimeSpan.Zero;

            var items = plan.ValidTimeLayoutItems;
            var idx = _lessonsService.CurrentSelectedIndex;
            if (idx <= 0) return TimeSpan.Zero;

            return DateTime.Now.TimeOfDay - items[idx - 1].EndTime;
        }
    }

    private TimeLayoutItem FindPreviousTimeLayoutItem(int timeType)
    {
        var plan = _lessonsService.CurrentClassPlan;
        if (plan == null) return TimeLayoutItem.Empty;

        var items = plan.ValidTimeLayoutItems;
        var idx = _lessonsService.CurrentSelectedIndex;
        if (idx <= 0) return TimeLayoutItem.Empty;

        for (int i = idx - 1; i >= 0; i--)
        {
            if (items[i].TimeType == timeType)
                return items[i];
        }
        return TimeLayoutItem.Empty;
    }

    internal ClassIslandService(
        IPublicLessonsService lessonsService,
        IPublicProfileService profileService)
    {
        _lessonsService = lessonsService;
        _profileService = profileService;
    }

    public ClassIslandService()
    {
        _client = new IpcClient();
        _client.Connect();
        _lessonsService = _client.Provider.CreateIpcProxy<IPublicLessonsService>(_client.PeerProxy);
        _profileService = _client.Provider.CreateIpcProxy<IPublicProfileService>(_client.PeerProxy);
        _uriNavigationService = _client.Provider.CreateIpcProxy<IPublicUriNavigationService>(_client.PeerProxy);
    }
}