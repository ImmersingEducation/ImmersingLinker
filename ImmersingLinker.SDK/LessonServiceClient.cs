using System.Net;
using System.Net.Http.Json;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;

namespace ImmersingLinker.SDK;

public class LessonServiceClient
{
    private readonly HttpClient _http;

    public LessonServiceClient(string port)
    {
        _http = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
    }

    #region Current

    public async Task<Subject?> GetCurrentSubjectAsync()
    {
        var response = await _http.GetAsync("/lesson/current/subject");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Subject>();
    }

    public async Task<Subject> GetNextClassSubjectAsync()
    {
        var response = await _http.GetAsync("/lesson/current/next-class-subject");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Subject>())!;
    }

    public async Task<TimeState> GetCurrentStateAsync()
    {
        var response = await _http.GetAsync("/lesson/current/state");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeState>())!;
    }

    public async Task<TimeLayoutItem> GetCurrentTimeLayoutItemAsync()
    {
        var response = await _http.GetAsync("/lesson/current/time-layout-item");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeLayoutItem>())!;
    }

    public async Task<ClassPlan?> GetCurrentClassPlanAsync()
    {
        var response = await _http.GetAsync("/lesson/current/class-plan");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClassPlan>();
    }

    public async Task<int> GetCurrentSelectedIndexAsync()
    {
        var response = await _http.GetAsync("/lesson/current/selected-index");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<int>());
    }

    public async Task<bool> GetIsClassPlanEnabledAsync()
    {
        var response = await _http.GetAsync("/lesson/current/is-class-plan-enabled");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> GetIsClassPlanLoadedAsync()
    {
        var response = await _http.GetAsync("/lesson/current/is-class-plan-loaded");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> GetIsLessonConfirmedAsync()
    {
        var response = await _http.GetAsync("/lesson/current/is-lesson-confirmed");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    #endregion

    #region Next

    public async Task<TimeLayoutItem> GetNextClassTimeLayoutItemAsync()
    {
        var response = await _http.GetAsync("/lesson/next/class-time-layout-item");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeLayoutItem>())!;
    }

    public async Task<TimeLayoutItem> GetNextBreakingTimeLayoutItemAsync()
    {
        var response = await _http.GetAsync("/lesson/next/breaking-time-layout-item");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeLayoutItem>())!;
    }

    #endregion

    #region Previous

    public async Task<Subject?> GetPreviousClassSubjectAsync()
    {
        var response = await _http.GetAsync("/lesson/previous/class-subject");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Subject>();
    }

    public async Task<TimeLayoutItem> GetPreviousClassTimeLayoutItemAsync()
    {
        var response = await _http.GetAsync("/lesson/previous/class-time-layout-item");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeLayoutItem>())!;
    }

    public async Task<TimeLayoutItem> GetPreviousBreakingTimeLayoutItemAsync()
    {
        var response = await _http.GetAsync("/lesson/previous/breaking-time-layout-item");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeLayoutItem>())!;
    }

    #endregion

    #region Timer

    public async Task<TimeSpan> GetOnClassLeftTimeAsync()
    {
        var response = await _http.GetAsync("/lesson/timer/on-class-left");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeSpan>());
    }

    public async Task<TimeSpan> GetOnBreakingLeftTimeAsync()
    {
        var response = await _http.GetAsync("/lesson/timer/on-breaking-left");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeSpan>());
    }

    public async Task<TimeSpan> GetElapsedSincePreviousClassAsync()
    {
        var response = await _http.GetAsync("/lesson/timer/elapsed-since-previous-class");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeSpan>());
    }

    public async Task<TimeSpan> GetElapsedSincePreviousBreakingAsync()
    {
        var response = await _http.GetAsync("/lesson/timer/elapsed-since-previous-breaking");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeSpan>());
    }

    public async Task<TimeSpan> GetElapsedSincePreviousAnyAsync()
    {
        var response = await _http.GetAsync("/lesson/timer/elapsed-since-previous-any");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeSpan>());
    }

    #endregion

    #region Profile

    public async Task<string> GetCurrentProfilePathAsync()
    {
        var response = await _http.GetAsync("/lesson/profile/current-profile-path");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<string>())!;
    }

    public async Task<bool> GetIsCurrentProfileTrustedAsync()
    {
        var response = await _http.GetAsync("/lesson/profile/is-trusted");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<Profile> GetProfileAsync()
    {
        var response = await _http.GetAsync("/lesson/profile");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Profile>())!;
    }

    #endregion
}
