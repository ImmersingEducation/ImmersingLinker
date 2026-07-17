using System.Collections.ObjectModel;
using System.Reflection;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.IPC.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;
using ImmersingLinker.Core.Services.ThirdParty;
using Moq;

namespace ImmersingLinker.Test.ClassIsland;

public class ClassIslandServiceTest
{
    private static readonly Guid MathId = Guid.NewGuid();
    private static readonly Guid ChineseId = Guid.NewGuid();
    private static readonly Guid EnglishId = Guid.NewGuid();

    private static Subject CreateSubject(string name) => new() { Name = name };

    private static ClassPlan CreateClassPlan(
        (int timeType, TimeSpan start, TimeSpan end)[] layout,
        Guid[]? subjectIds = null)
    {
        var plan = new ClassPlan();
        var items = new ObservableCollection<TimeLayoutItem>();
        var classes = new ObservableCollection<ClassInfo>();
        int classIndex = 0;

        foreach (var (timeType, start, end) in layout)
        {
            items.Add(new TimeLayoutItem
            {
                TimeType = timeType,
                StartTime = start,
                EndTime = end
            });

            if (timeType == 0)
            {
                var id = subjectIds != null && classIndex < subjectIds.Length
                    ? subjectIds[classIndex]
                    : Guid.NewGuid();
                classes.Add(new ClassInfo { SubjectId = id });
                classIndex++;
            }
        }

        plan.Classes = classes;

        var validItemsField = typeof(ClassPlan).GetProperty(
            "ValidTimeLayoutItems",
            BindingFlags.Public | BindingFlags.Instance)!;
        ((ObservableCollection<TimeLayoutItem>)validItemsField.GetValue(plan)!).Clear();
        foreach (var item in items)
            ((ObservableCollection<TimeLayoutItem>)validItemsField.GetValue(plan)!).Add(item);

        return plan;
    }

    private static Profile CreateProfile(params (Guid id, Subject subject)[] subjects)
    {
        var profile = new Profile();
        var dict = new ObservableDictionary<Guid, Subject>();
        foreach (var (id, subject) in subjects)
            dict.Add(id, subject);
        profile.Subjects = dict;
        return profile;
    }

    private static Mock<IPublicLessonsService> MockLessonsService(
        ClassPlan? plan,
        int selectedIndex)
    {
        var mock = new Mock<IPublicLessonsService>();
        mock.Setup(s => s.CurrentClassPlan).Returns(plan!);
        mock.Setup(s => s.CurrentSelectedIndex).Returns(selectedIndex);
        return mock;
    }

    private static Mock<IPublicProfileService> MockProfileService(Profile profile)
    {
        var mock = new Mock<IPublicProfileService>();
        mock.Setup(s => s.Profile).Returns(profile);
        return mock;
    }

    private static ClassIslandService CreateService(
        IPublicLessonsService lessons,
        IPublicProfileService profile)
    {
        return new ClassIslandService(lessons, profile);
    }

    #region PreviousClassTimeLayoutItem

    [Fact]
    public void PreviousClassTimeLayoutItem_NoClassPlan_ReturnsEmpty()
    {
        var lessons = MockLessonsService(null, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeLayoutItem.Empty, service.PreviousClassTimeLayoutItem);
    }

    [Fact]
    public void PreviousClassTimeLayoutItem_SelectedIndexZero_ReturnsEmpty()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (1, new TimeSpan(8, 45, 0), new TimeSpan(9, 0, 0)),
            (0, new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeLayoutItem.Empty, service.PreviousClassTimeLayoutItem);
    }

    [Fact]
    public void PreviousClassTimeLayoutItem_SelectedIndexNegative_ReturnsEmpty()
    {
        var lessons = MockLessonsService(null, -1);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeLayoutItem.Empty, service.PreviousClassTimeLayoutItem);
    }

    [Fact]
    public void PreviousClassTimeLayoutItem_NoPreviousClassType_ReturnsEmpty()
    {
        var plan = CreateClassPlan([
            (1, new TimeSpan(8, 0, 0), new TimeSpan(8, 15, 0)),
            (1, new TimeSpan(8, 15, 0), new TimeSpan(8, 30, 0))
        ]);
        var lessons = MockLessonsService(plan, 1);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeLayoutItem.Empty, service.PreviousClassTimeLayoutItem);
    }

    [Fact]
    public void PreviousClassTimeLayoutItem_InBreak_FindsPreviousClass()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (1, new TimeSpan(8, 45, 0), new TimeSpan(9, 0, 0)),
            (0, new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 2);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        var result = service.PreviousClassTimeLayoutItem;
        Assert.Equal(new TimeSpan(8, 0, 0), result.StartTime);
        Assert.Equal(new TimeSpan(8, 45, 0), result.EndTime);
    }

    [Fact]
    public void PreviousClassTimeLayoutItem_InClass_FindsPreviousClassBeforeCurrent()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (1, new TimeSpan(8, 45, 0), new TimeSpan(9, 0, 0)),
            (0, new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 2);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        var result = service.PreviousClassTimeLayoutItem;
        Assert.Equal(new TimeSpan(8, 0, 0), result.StartTime);
    }

    #endregion

    #region PreviousBreakingTimeLayoutItem

    [Fact]
    public void PreviousBreakingTimeLayoutItem_NoClassPlan_ReturnsEmpty()
    {
        var lessons = MockLessonsService(null, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeLayoutItem.Empty, service.PreviousBreakingTimeLayoutItem);
    }

    [Fact]
    public void PreviousBreakingTimeLayoutItem_SelectedIndexZero_ReturnsEmpty()
    {
        var plan = CreateClassPlan([
            (1, new TimeSpan(8, 0, 0), new TimeSpan(8, 15, 0)),
            (0, new TimeSpan(8, 15, 0), new TimeSpan(9, 0, 0))
        ]);
        var lessons = MockLessonsService(plan, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeLayoutItem.Empty, service.PreviousBreakingTimeLayoutItem);
    }

    [Fact]
    public void PreviousBreakingTimeLayoutItem_InClass_FindsPreviousBreak()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (1, new TimeSpan(8, 45, 0), new TimeSpan(9, 0, 0)),
            (0, new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 2);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        var result = service.PreviousBreakingTimeLayoutItem;
        Assert.Equal(new TimeSpan(8, 45, 0), result.StartTime);
        Assert.Equal(new TimeSpan(9, 0, 0), result.EndTime);
    }

    [Fact]
    public void PreviousBreakingTimeLayoutItem_NoPreviousBreak_ReturnsEmpty()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (0, new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 1);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeLayoutItem.Empty, service.PreviousBreakingTimeLayoutItem);
    }

    #endregion

    #region PreviousClassSubject

    [Fact]
    public void PreviousClassSubject_NoClassPlan_ReturnsNull()
    {
        var lessons = MockLessonsService(null, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Null(service.PreviousClassSubject);
    }

    [Fact]
    public void PreviousClassSubject_SelectedIndexZero_ReturnsNull()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Null(service.PreviousClassSubject);
    }

    [Fact]
    public void PreviousClassSubject_NoPreviousClass_ReturnsNull()
    {
        var plan = CreateClassPlan([
            (1, new TimeSpan(8, 0, 0), new TimeSpan(8, 15, 0)),
            (1, new TimeSpan(8, 15, 0), new TimeSpan(8, 30, 0))
        ]);
        var lessons = MockLessonsService(plan, 1);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Null(service.PreviousClassSubject);
    }

    [Fact]
    public void PreviousClassSubject_FindsCorrectSubject()
    {
        var math = CreateSubject("数学");
        var chinese = CreateSubject("语文");
        var plan = CreateClassPlan(
        [
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (1, new TimeSpan(8, 45, 0), new TimeSpan(9, 0, 0)),
            (0, new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0))
        ],
        [MathId, ChineseId]);

        var profile = CreateProfile((MathId, math), (ChineseId, chinese));
        var lessons = MockLessonsService(plan, 2);
        var service = CreateService(lessons.Object, MockProfileService(profile).Object);

        var result = service.PreviousClassSubject;
        Assert.NotNull(result);
        Assert.Equal("数学", result.Name);
    }

    [Fact]
    public void PreviousClassSubject_SubjectNotFound_ReturnsFallback()
    {
        var plan = CreateClassPlan(
        [
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0))
        ],
        [MathId]);

        var profile = CreateProfile();
        var lessons = MockLessonsService(plan, 1);
        var service = CreateService(lessons.Object, MockProfileService(profile).Object);

        var result = service.PreviousClassSubject;
        Assert.NotNull(result);
        Assert.Equal(Subject.Fallback.Name, result.Name);
    }

    #endregion

    #region ElapsedSincePreviousClass

    [Fact]
    public void ElapsedSincePreviousClass_NoPreviousClass_ReturnsZero()
    {
        var lessons = MockLessonsService(null, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeSpan.Zero, service.ElapsedSincePreviousClass);
    }

    [Fact]
    public void ElapsedSincePreviousClass_HasPreviousClass_ReturnsPositiveTime()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (1, new TimeSpan(8, 45, 0), new TimeSpan(9, 0, 0)),
            (0, new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 2);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        var elapsed = service.ElapsedSincePreviousClass;
        Assert.True(elapsed > TimeSpan.Zero);
    }

    #endregion

    #region ElapsedSincePreviousBreaking

    [Fact]
    public void ElapsedSincePreviousBreaking_NoPreviousBreaking_ReturnsZero()
    {
        var lessons = MockLessonsService(null, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeSpan.Zero, service.ElapsedSincePreviousBreaking);
    }

    [Fact]
    public void ElapsedSincePreviousBreaking_HasPreviousBreaking_ReturnsPositiveTime()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (1, new TimeSpan(8, 45, 0), new TimeSpan(9, 0, 0)),
            (0, new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 2);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        var elapsed = service.ElapsedSincePreviousBreaking;
        Assert.True(elapsed > TimeSpan.Zero);
    }

    #endregion

    #region ElapsedSincePreviousAny

    [Fact]
    public void ElapsedSincePreviousAny_NoClassPlan_ReturnsZero()
    {
        var lessons = MockLessonsService(null, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeSpan.Zero, service.ElapsedSincePreviousAny);
    }

    [Fact]
    public void ElapsedSincePreviousAny_SelectedIndexZero_ReturnsZero()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0))
        ]);
        var lessons = MockLessonsService(plan, 0);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeSpan.Zero, service.ElapsedSincePreviousAny);
    }

    [Fact]
    public void ElapsedSincePreviousAny_HasPrevious_ReturnsCorrectTime()
    {
        var plan = CreateClassPlan([
            (0, new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0)),
            (1, new TimeSpan(8, 45, 0), new TimeSpan(9, 0, 0))
        ]);
        var lessons = MockLessonsService(plan, 1);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        var elapsed = service.ElapsedSincePreviousAny;
        Assert.True(elapsed > TimeSpan.Zero);
    }

    #endregion

    #region Passthrough properties

    [Fact]
    public void CurrentSubject_DelegatesToLessonsService()
    {
        var expected = CreateSubject("物理");
        var lessons = new Mock<IPublicLessonsService>();
        lessons.Setup(s => s.CurrentSubject).Returns(expected);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Same(expected, service.CurrentSubject);
    }

    [Fact]
    public void CurrentState_DelegatesToLessonsService()
    {
        var lessons = new Mock<IPublicLessonsService>();
        lessons.Setup(s => s.CurrentState).Returns(TimeState.Breaking);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(TimeState.Breaking, service.CurrentState);
    }

    [Fact]
    public void OnClassLeftTime_DelegatesToLessonsService()
    {
        var expected = TimeSpan.FromMinutes(15);
        var lessons = new Mock<IPublicLessonsService>();
        lessons.Setup(s => s.OnClassLeftTime).Returns(expected);
        var profile = MockProfileService(CreateProfile());
        var service = CreateService(lessons.Object, profile.Object);

        Assert.Equal(expected, service.OnClassLeftTime);
    }

    [Fact]
    public void CurrentProfilePath_DelegatesToProfileService()
    {
        var profile = CreateProfile();
        var profileMock = new Mock<IPublicProfileService>();
        profileMock.Setup(s => s.CurrentProfilePath).Returns("test.json");
        profileMock.Setup(s => s.Profile).Returns(profile);
        var lessons = MockLessonsService(null, -1);
        var service = CreateService(lessons.Object, profileMock.Object);

        Assert.Equal("test.json", service.CurrentProfilePath);
    }

    #endregion
}
