using ImmersingLinker.Core.Models;
using ImmersingLinker.Server.Controllers;
using ImmersingLinker.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImmersingLinker.Test.Controllers;

public class ClassControllerTest
{
    private static readonly Guid TestGuid = Guid.NewGuid();
    private const string TestGuidString = "test-guid";
    private const string TestClassName = "TestClass";

    private static Class CreateTestClass()
    {
        return new Class
        {
            Guid = TestGuid,
            Name = TestClassName,
            Students =
            [
                new Student
                {
                    Guid = Guid.NewGuid(),
                    Name = "Alice",
                    StudentIdInClass = 1,
                    Gender = Gender.Female,
                    GroupInClass = "GroupA",
                    ExtraProperties =
                    [
                        new StudentExtraProperty<object>
                        {
                            Application = new Application { UniqueId = "app1" },
                            Name = "nickname",
                        }
                    ]
                }
            ],
            ExtraProperties =
            [
                new ClassExtraProperty<object>
                {
                    Application = new Application { UniqueId = "app1" },
                    Name = "room",
                }
            ]
        };
    }

    private static ClassController CreateController(Mock<ClassStorageService>? mock = null)
    {
        mock ??= CreateMockService();
        return new ClassController(mock.Object);
    }

    private static Mock<ClassStorageService> CreateMockService(Class? @class = null)
    {
        var mock = new Mock<ClassStorageService>();
        @class ??= CreateTestClass();

        mock.Setup(s => s.GetClass(It.Is<Guid>(g => g == TestGuid)))
            .ReturnsAsync(@class);

        mock.Setup(s => s.GetClass(It.Is<Guid>(g => g != TestGuid)))
            .ReturnsAsync((Class?)null);

        mock.Setup(s => s.GetClassInfos())
            .ReturnsAsync([new ClassInfo { Guid = TestGuid, Name = TestClassName }]);

        return mock;
    }

    // ===== GET =====

    [Fact]
    public async Task GetAllClasses_ReturnsClasses()
    {
        var controller = CreateController();
        var result = await controller.GetAllClasses();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var classes = Assert.IsType<List<Class>>(okResult.Value);
        Assert.Single(classes);
        Assert.Equal(TestClassName, classes[0].Name);
    }

    [Fact]
    public async Task GetAllClassInfos_ReturnsInfos()
    {
        var controller = CreateController();
        var result = await controller.GetAllClassInfos();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var infos = Assert.IsType<List<ClassInfo>>(okResult.Value);
        Assert.Single(infos);
    }

    [Fact]
    public async Task GetClassByGuid_Existing_ReturnsClass()
    {
        var controller = CreateController();
        var result = await controller.GetClassByGuid(TestGuid.ToString());

        var okResult = Assert.IsType<OkObjectResult>(result);
        var @class = Assert.IsType<Class>(okResult.Value);
        Assert.Equal(TestClassName, @class.Name);
    }

    [Fact]
    public async Task GetClassByGuid_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetClassByGuid(Guid.NewGuid().ToString());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetStudentsByClassGuid_Existing_ReturnsStudents()
    {
        var controller = CreateController();
        var result = await controller.GetStudentsByClassGuid(TestGuid.ToString());

        var okResult = Assert.IsType<OkObjectResult>(result);
        var students = Assert.IsType<List<Student>>(okResult.Value);
        Assert.Single(students);
    }

    [Fact]
    public async Task GetStudentsByClassGuid_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetStudentsByClassGuid(Guid.NewGuid().ToString());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetStudentByStudentIdInClass_Existing_ReturnsStudent()
    {
        var controller = CreateController();
        var result = await controller.GetStudentByStudentIdInClass(TestGuid.ToString(), 1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var student = Assert.IsType<Student>(okResult.Value);
        Assert.Equal("Alice", student.Name);
    }

    [Fact]
    public async Task GetStudentByStudentIdInClass_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetStudentByStudentIdInClass(TestGuid.ToString(), 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetExtraPropertiesByStudentIdInClass_Existing_ReturnsProperties()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertiesByStudentIdInClass(TestGuid.ToString(), 1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var props = Assert.IsType<List<StudentExtraProperty>>(okResult.Value);
        Assert.Single(props);
    }

    [Fact]
    public async Task GetExtraPropertiesByStudentIdInClass_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertiesByStudentIdInClass(TestGuid.ToString(), 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetExtraPropertiesByStudentIdAndAppIdInClass_Existing_ReturnsFiltered()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertiesByStudentIdAndAppIdInClass(TestGuid.ToString(), 1, "app1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var props = Assert.IsType<List<StudentExtraProperty>>(okResult.Value);
        Assert.Single(props);
    }

    [Fact]
    public async Task GetExtraPropertiesByStudentIdAndAppIdInClass_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertiesByStudentIdAndAppIdInClass(TestGuid.ToString(), 999, "app1");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetExtraPropertyByNameAndStudentIdInClass_Existing_ReturnsProperty()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertyByNameAndStudentIdInClass(TestGuid.ToString(), 1, "app1", "nickname");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var prop = Assert.IsType<StudentExtraProperty<object>>(okResult.Value);
        Assert.Equal("nickname", prop.Name);
    }

    [Fact]
    public async Task GetExtraPropertyByNameAndStudentIdInClass_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertyByNameAndStudentIdInClass(TestGuid.ToString(), 1, "app1", "nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetExtraPropertiesByClassGuid_Existing_ReturnsProperties()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertiesByClassGuid(TestGuid.ToString());

        var okResult = Assert.IsType<OkObjectResult>(result);
        var props = Assert.IsType<List<ClassExtraProperty>>(okResult.Value);
        Assert.Single(props);
    }

    [Fact]
    public async Task GetExtraPropertiesByClassGuid_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertiesByClassGuid(Guid.NewGuid().ToString());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetExtraPropertiesByAppIdInClass_Existing_ReturnsFiltered()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertiesByAppIdInClass(TestGuid.ToString(), "app1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var props = Assert.IsType<List<ClassExtraProperty>>(okResult.Value);
        Assert.Single(props);
    }

    [Fact]
    public async Task GetExtraPropertiesByAppIdInClass_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertiesByAppIdInClass(Guid.NewGuid().ToString(), "app1");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetExtraPropertyByAppIdAndNameInClass_Existing_ReturnsProperty()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertyByAppIdAndNameInClass(TestGuid.ToString(), "app1", "room");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var prop = Assert.IsType<ClassExtraProperty<object>>(okResult.Value);
        Assert.Equal("room", prop.Name);
    }

    [Fact]
    public async Task GetExtraPropertyByAppIdAndNameInClass_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetExtraPropertyByAppIdAndNameInClass(TestGuid.ToString(), "app1", "nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    // ===== POST =====

    [Fact]
    public async Task CreateClass_ReturnsCreated()
    {
        var mock = CreateMockService();
        var controller = CreateController(mock);

        var request = new CreateClassRequest("NewClass");
        var result = await controller.CreateClass(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ClassController.GetClassByGuid), createdResult.ActionName);
        var @class = Assert.IsType<Class>(createdResult.Value);
        Assert.Equal("NewClass", @class.Name);
        mock.Verify(s => s.SaveClass(It.IsAny<Class>()), Times.Once);
    }

    [Fact]
    public async Task AddStudent_ExistingClass_ReturnsCreated()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var request = new CreateStudentRequest("Bob", 2, Gender.Male, "GroupB");
        var result = await controller.AddStudent(TestGuid.ToString(), request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ClassController.GetStudentByStudentIdInClass), createdResult.ActionName);
        var student = Assert.IsType<Student>(createdResult.Value);
        Assert.Equal("Bob", student.Name);
        Assert.Equal(2, @class.Students.Count);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task AddStudent_NotFoundClass_ReturnsNotFound()
    {
        var controller = CreateController();
        var request = new CreateStudentRequest("Bob", 2, Gender.Male, "GroupB");
        var result = await controller.AddStudent(Guid.NewGuid().ToString(), request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddStudent_DuplicateStudentId_ReturnsConflict()
    {
        var controller = CreateController();
        var request = new CreateStudentRequest("Bob", 1, Gender.Male, "GroupB");
        var result = await controller.AddStudent(TestGuid.ToString(), request);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task AddClassExtraProperty_Existing_ReturnsCreated()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var request = new CreateExtraPropertyRequest("app2", "building", "A");
        var result = await controller.AddClassExtraProperty(TestGuid.ToString(), request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ClassController.GetExtraPropertyByAppIdAndNameInClass), createdResult.ActionName);
        Assert.Equal(2, @class.ExtraProperties.Count);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task AddClassExtraProperty_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.AddClassExtraProperty(Guid.NewGuid().ToString(),
            new CreateExtraPropertyRequest("app1", "prop", null));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddClassExtraProperty_Duplicate_ReturnsConflict()
    {
        var controller = CreateController();
        var result = await controller.AddClassExtraProperty(TestGuid.ToString(),
            new CreateExtraPropertyRequest("app1", "room", null));

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task AddStudentExtraProperty_Existing_ReturnsCreated()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var request = new CreateExtraPropertyRequest("app2", "grade", "A");
        var result = await controller.AddStudentExtraProperty(TestGuid.ToString(), 1, request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ClassController.GetExtraPropertyByNameAndStudentIdInClass), createdResult.ActionName);
        Assert.Equal(2, @class.Students[0].ExtraProperties.Count);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task AddStudentExtraProperty_NotFoundStudent_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.AddStudentExtraProperty(TestGuid.ToString(), 999,
            new CreateExtraPropertyRequest("app1", "grade", "A"));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddStudentExtraProperty_Duplicate_ReturnsConflict()
    {
        var controller = CreateController();
        var result = await controller.AddStudentExtraProperty(TestGuid.ToString(), 1,
            new CreateExtraPropertyRequest("app1", "nickname", "Ally"));

        Assert.IsType<ConflictObjectResult>(result);
    }

    // ===== PUT =====

    [Fact]
    public async Task UpdateClass_Existing_ReturnsOk()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var result = await controller.UpdateClass(TestGuid.ToString(), new UpdateClassRequest("Renamed"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<Class>(okResult.Value);
        Assert.Equal("Renamed", updated.Name);
        Assert.Equal("Renamed", @class.Name);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task UpdateClass_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.UpdateClass(Guid.NewGuid().ToString(), new UpdateClassRequest("Renamed"));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateStudent_Existing_ReturnsOk()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var result = await controller.UpdateStudent(TestGuid.ToString(), 1,
            new UpdateStudentRequest("AliceUpdated", Gender.Female, "GroupC"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var student = Assert.IsType<Student>(okResult.Value);
        Assert.Equal("AliceUpdated", student.Name);
        Assert.Equal("GroupC", student.GroupInClass);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task UpdateStudent_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.UpdateStudent(TestGuid.ToString(), 999,
            new UpdateStudentRequest("X", Gender.Male, "G"));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateClassExtraProperty_Existing_ReturnsOk()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var result = await controller.UpdateClassExtraProperty(TestGuid.ToString(), "app1", "room",
            new UpdateExtraPropertyRequest("201"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var prop = Assert.IsType<ClassExtraProperty<object>>(okResult.Value);
        Assert.Equal("room", prop.Name);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task UpdateClassExtraProperty_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.UpdateClassExtraProperty(TestGuid.ToString(), "app1", "nonexistent",
            new UpdateExtraPropertyRequest("val"));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateStudentExtraProperty_Existing_ReturnsOk()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var result = await controller.UpdateStudentExtraProperty(TestGuid.ToString(), 1, "app1", "nickname",
            new UpdateExtraPropertyRequest("Ally"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var prop = Assert.IsType<StudentExtraProperty<object>>(okResult.Value);
        Assert.Equal("nickname", prop.Name);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task UpdateStudentExtraProperty_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.UpdateStudentExtraProperty(TestGuid.ToString(), 1, "app1", "nonexistent",
            new UpdateExtraPropertyRequest("val"));

        Assert.IsType<NotFoundResult>(result);
    }

    // ===== DELETE =====

    [Fact]
    public void DeleteClass_ReturnsNoContent()
    {
        var mock = CreateMockService();
        var controller = CreateController(mock);

        var result = controller.DeleteClass(TestGuid.ToString());

        Assert.IsType<NoContentResult>(result);
        mock.Verify(s => s.DeleteClass(TestGuid), Times.Once);
    }

    [Fact]
    public async Task DeleteStudent_Existing_ReturnsNoContent()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var result = await controller.DeleteStudent(TestGuid.ToString(), 1);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(@class.Students);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task DeleteStudent_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.DeleteStudent(TestGuid.ToString(), 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteClassExtraProperty_Existing_ReturnsNoContent()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var result = await controller.DeleteClassExtraProperty(TestGuid.ToString(), "app1", "room");

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(@class.ExtraProperties);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task DeleteClassExtraProperty_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.DeleteClassExtraProperty(TestGuid.ToString(), "app1", "nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteStudentExtraProperty_Existing_ReturnsNoContent()
    {
        var @class = CreateTestClass();
        var mock = CreateMockService(@class);
        var controller = CreateController(mock);

        var result = await controller.DeleteStudentExtraProperty(TestGuid.ToString(), 1, "app1", "nickname");

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(@class.Students[0].ExtraProperties);
        mock.Verify(s => s.SaveClass(@class), Times.Once);
    }

    [Fact]
    public async Task DeleteStudentExtraProperty_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.DeleteStudentExtraProperty(TestGuid.ToString(), 1, "app1", "nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }
}
