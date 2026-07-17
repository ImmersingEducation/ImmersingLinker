using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.IPC.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;
using ImmersingLinker.Core.Services.ThirdParty;
using ImmersingLinker.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImmersingLinker.Test.Controllers;

public class LessonControllerTest
{
    private static readonly Guid MathId = Guid.NewGuid();
    private static readonly Guid ChineseId = Guid.NewGuid();
    private static readonly Guid EnglishId = Guid.NewGuid();

    private static Subject CreateSubject(string name) => new() { Name = name };

    private static Mock<IPublicLessonsService> MockLessonsService(
        ClassPlan? plan = null,
        int selectedIndex = -1,
        Subject? currentSubject = null,
        Subject? nextClassSubject = null,
        TimeState currentState = TimeState.None,
        TimeLayoutItem? currentTimeLayoutItem = null,
        TimeLayoutItem? nextBreakingTimeLayoutItem = null,
        TimeLayoutItem? nextClassTimeLayoutItem = null,
        TimeSpan onClassLeftTime = default,
        TimeSpan onBreakingLeftTime = default,
        bool isClassPlanEnabled = false,
        bool isClassPlanLoaded = false,
        bool isLessonConfirmed = false)
    {
        var mock = new Mock<IPublicLessonsService>();
        mock.Setup(s => s.CurrentClassPlan).Returns(plan!);
        mock.Setup(s => s.CurrentSelectedIndex).Returns(selectedIndex);
        mock.Setup(s => s.CurrentSubject).Returns(currentSubject!);
        mock.Setup(s => s.NextClassSubject).Returns(nextClassSubject!);
        mock.Setup(s => s.CurrentState).Returns(currentState);
        mock.Setup(s => s.CurrentTimeLayoutItem)
            .Returns(currentTimeLayoutItem ?? TimeLayoutItem.Empty);
        mock.Setup(s => s.NextBreakingTimeLayoutItem)
            .Returns(nextBreakingTimeLayoutItem ?? TimeLayoutItem.Empty);
        mock.Setup(s => s.NextClassTimeLayoutItem)
            .Returns(nextClassTimeLayoutItem ?? TimeLayoutItem.Empty);
        mock.Setup(s => s.OnClassLeftTime).Returns(onClassLeftTime);
        mock.Setup(s => s.OnBreakingTimeLeftTime).Returns(onBreakingLeftTime);
        mock.Setup(s => s.IsClassPlanEnabled).Returns(isClassPlanEnabled);
        mock.Setup(s => s.IsClassPlanLoaded).Returns(isClassPlanLoaded);
        mock.Setup(s => s.IsLessonConfirmed).Returns(isLessonConfirmed);
        return mock;
    }

    private static Mock<IPublicProfileService> MockProfileService(
        Profile? profile = null,
        string currentProfilePath = "test.json",
        bool isCurrentProfileTrusted = true)
    {
        profile ??= new Profile();
        var mock = new Mock<IPublicProfileService>();
        mock.Setup(s => s.Profile).Returns(profile);
        mock.Setup(s => s.CurrentProfilePath).Returns(currentProfilePath);
        mock.Setup(s => s.IsCurrentProfileTrusted).Returns(isCurrentProfileTrusted);
        return mock;
    }

    private static ClassIslandService CreateClassIslandService(
        IPublicLessonsService? lessons = null,
        IPublicProfileService? profile = null)
    {
        lessons ??= MockLessonsService().Object;
        profile ??= MockProfileService().Object;
        return new ClassIslandService(lessons, profile);
    }

    private static LessonController CreateController(
        IPublicLessonsService? lessons = null,
        IPublicProfileService? profile = null)
    {
        return new LessonController(CreateClassIslandService(lessons, profile));
    }

    #region Current

    [Fact]
    public void GetCurrentSubject_ReturnsOkWithValue()
    {
        var subject = CreateSubject("数学");
        var controller = CreateController(
            MockLessonsService(currentSubject: subject).Object);

        var result = controller.GetCurrentSubject();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(subject, ok.Value);
    }

    [Fact]
    public void GetCurrentSubject_Null_ReturnsOkWithNull()
    {
        var controller = CreateController(
            MockLessonsService(currentSubject: null).Object);

        var result = controller.GetCurrentSubject();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Null(ok.Value);
    }

    [Fact]
    public void GetNextClassSubject_ReturnsOkWithValue()
    {
        var subject = CreateSubject("语文");
        var controller = CreateController(
            MockLessonsService(nextClassSubject: subject).Object);

        var result = controller.GetNextClassSubject();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(subject, ok.Value);
    }

    [Fact]
    public void GetCurrentState_ReturnsOkWithValue()
    {
        var controller = CreateController(
            MockLessonsService(currentState: TimeState.OnClass).Object);

        var result = controller.GetCurrentState();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(TimeState.OnClass, ok.Value);
    }

    [Fact]
    public void GetCurrentTimeLayoutItem_ReturnsOkWithValue()
    {
        var item = new TimeLayoutItem
        {
            TimeType = 0,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(8, 45, 0)
        };
        var controller = CreateController(
            MockLessonsService(currentTimeLayoutItem: item).Object);

        var result = controller.GetCurrentTimeLayoutItem();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(item, ok.Value);
    }

    [Fact]
    public void GetCurrentClassPlan_ReturnsOkWithValue()
    {
        var plan = new ClassPlan { Name = "TestPlan" };
        var controller = CreateController(
            MockLessonsService(plan: plan).Object);

        var result = controller.GetCurrentClassPlan();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(plan, ok.Value);
    }

    [Fact]
    public void GetCurrentClassPlan_Null_ReturnsOkWithNull()
    {
        var controller = CreateController(
            MockLessonsService(plan: null).Object);

        var result = controller.GetCurrentClassPlan();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Null(ok.Value);
    }

    [Fact]
    public void GetCurrentSelectedIndex_ReturnsOkWithValue()
    {
        var controller = CreateController(
            MockLessonsService(selectedIndex: 3).Object);

        var result = controller.GetCurrentSelectedIndex();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(3, ok.Value);
    }

    [Fact]
    public void GetCurrentSelectedIndex_NegativeOne_ReturnsOkWithValue()
    {
        var controller = CreateController(
            MockLessonsService(selectedIndex: -1).Object);

        var result = controller.GetCurrentSelectedIndex();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(-1, ok.Value);
    }

    [Fact]
    public void GetIsClassPlanEnabled_True_ReturnsOkWithTrue()
    {
        var controller = CreateController(
            MockLessonsService(isClassPlanEnabled: true).Object);

        var result = controller.GetIsClassPlanEnabled();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public void GetIsClassPlanEnabled_False_ReturnsOkWithFalse()
    {
        var controller = CreateController(
            MockLessonsService(isClassPlanEnabled: false).Object);

        var result = controller.GetIsClassPlanEnabled();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(false, ok.Value);
    }

    [Fact]
    public void GetIsClassPlanLoaded_ReturnsOkWithValue()
    {
        var controller = CreateController(
            MockLessonsService(isClassPlanLoaded: true).Object);

        var result = controller.GetIsClassPlanLoaded();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public void GetIsLessonConfirmed_ReturnsOkWithValue()
    {
        var controller = CreateController(
            MockLessonsService(isLessonConfirmed: true).Object);

        var result = controller.GetIsLessonConfirmed();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    #endregion

    #region Next

    [Fact]
    public void GetNextClassTimeLayoutItem_ReturnsOkWithValue()
    {
        var item = new TimeLayoutItem
        {
            TimeType = 0,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 45, 0)
        };
        var controller = CreateController(
            MockLessonsService(nextClassTimeLayoutItem: item).Object);

        var result = controller.GetNextClassTimeLayoutItem();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(item, ok.Value);
    }

    [Fact]
    public void GetNextBreakingTimeLayoutItem_ReturnsOkWithValue()
    {
        var item = new TimeLayoutItem
        {
            TimeType = 1,
            StartTime = new TimeSpan(8, 45, 0),
            EndTime = new TimeSpan(9, 0, 0)
        };
        var controller = CreateController(
            MockLessonsService(nextBreakingTimeLayoutItem: item).Object);

        var result = controller.GetNextBreakingTimeLayoutItem();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(item, ok.Value);
    }

    #endregion

    #region Previous

    [Fact]
    public void GetPreviousClassSubject_ReturnsOkWithNullWhenNoClassPlan()
    {
        var controller = CreateController(
            MockLessonsService(plan: null).Object);

        var result = controller.GetPreviousClassSubject();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Null(ok.Value);
    }

    [Fact]
    public void GetPreviousClassTimeLayoutItem_ReturnsOkWithEmptyWhenNoClassPlan()
    {
        var controller = CreateController(
            MockLessonsService(plan: null).Object);

        var result = controller.GetPreviousClassTimeLayoutItem();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(TimeLayoutItem.Empty, ok.Value);
    }

    [Fact]
    public void GetPreviousBreakingTimeLayoutItem_ReturnsOkWithEmptyWhenNoClassPlan()
    {
        var controller = CreateController(
            MockLessonsService(plan: null).Object);

        var result = controller.GetPreviousBreakingTimeLayoutItem();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(TimeLayoutItem.Empty, ok.Value);
    }

    #endregion

    #region Timer

    [Fact]
    public void GetOnClassLeftTime_ReturnsOkWithValue()
    {
        var expected = TimeSpan.FromMinutes(15);
        var controller = CreateController(
            MockLessonsService(onClassLeftTime: expected).Object);

        var result = controller.GetOnClassLeftTime();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public void GetOnBreakingLeftTime_ReturnsOkWithValue()
    {
        var expected = TimeSpan.FromMinutes(5);
        var controller = CreateController(
            MockLessonsService(onBreakingLeftTime: expected).Object);

        var result = controller.GetOnBreakingLeftTime();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public void GetElapsedSincePreviousClass_ReturnsOkWithTimeSpan()
    {
        var controller = CreateController(MockLessonsService().Object);

        var result = controller.GetElapsedSincePreviousClass();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<TimeSpan>(ok.Value);
    }

    [Fact]
    public void GetElapsedSincePreviousBreaking_ReturnsOkWithTimeSpan()
    {
        var controller = CreateController(MockLessonsService().Object);

        var result = controller.GetElapsedSincePreviousBreaking();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<TimeSpan>(ok.Value);
    }

    [Fact]
    public void GetElapsedSincePreviousAny_ReturnsOkWithTimeSpan()
    {
        var controller = CreateController(MockLessonsService().Object);

        var result = controller.GetElapsedSincePreviousAny();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<TimeSpan>(ok.Value);
    }

    #endregion

    #region Profile

    [Fact]
    public void GetCurrentProfilePath_ReturnsOkWithValue()
    {
        var controller = CreateController(
            profile: MockProfileService(currentProfilePath: "my-profile.json").Object);

        var result = controller.GetCurrentProfilePath();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("my-profile.json", ok.Value);
    }

    [Fact]
    public void GetIsCurrentProfileTrusted_True_ReturnsOkWithTrue()
    {
        var controller = CreateController(
            profile: MockProfileService(isCurrentProfileTrusted: true).Object);

        var result = controller.GetIsCurrentProfileTrusted();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public void GetIsCurrentProfileTrusted_False_ReturnsOkWithFalse()
    {
        var controller = CreateController(
            profile: MockProfileService(isCurrentProfileTrusted: false).Object);

        var result = controller.GetIsCurrentProfileTrusted();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(false, ok.Value);
    }

    [Fact]
    public void GetProfile_ReturnsOkWithValue()
    {
        var profile = new Profile();
        var controller = CreateController(
            profile: MockProfileService(profile: profile).Object);

        var result = controller.GetProfile();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(profile, ok.Value);
    }

    #endregion
}
