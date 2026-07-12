using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImmersingLinker.Core.Models;
using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.SDK;

public class ClassServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new ClassExtraPropertyConverter(),
            new StudentExtraPropertyConverter()
        }
    };

    private readonly HttpClient _http;

    public ClassServiceClient(string port)
    {
        _http = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
    }

    #region GET

    public async Task<List<Class>> GetAllClassesAsync()
    {
        var response = await _http.GetAsync("/class");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Class>>(JsonOptions) ?? [];
    }

    public async Task<List<ClassInfo>> GetAllClassInfosAsync()
    {
        var response = await _http.GetAsync("/class/infos");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ClassInfo>>(JsonOptions) ?? [];
    }

    public async Task<Class?> GetClassByGuidAsync(string classGuid)
    {
        var response = await _http.GetAsync($"/class/{classGuid}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Class>(JsonOptions);
    }

    public async Task<List<Student>> GetStudentsByClassGuidAsync(string classGuid)
    {
        var response = await _http.GetAsync($"/class/{classGuid}/student");
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Student>>(JsonOptions) ?? [];
    }

    public async Task<Student?> GetStudentByStudentIdInClassAsync(string classGuid, int studentId)
    {
        var response = await _http.GetAsync($"/class/{classGuid}/student/{studentId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Student>(JsonOptions);
    }

    public async Task<List<StudentExtraProperty>> GetExtraPropertiesByStudentIdInClassAsync(string classGuid,
        int studentId)
    {
        var response = await _http.GetAsync($"/class/{classGuid}/student/{studentId}/extraProps");
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<StudentExtraProperty>>(JsonOptions) ?? [];
    }

    public async Task<List<StudentExtraProperty>> GetExtraPropertiesByStudentIdAndAppIdInClassAsync(
        string classGuid, int studentId, string appId)
    {
        var response = await _http.GetAsync($"/class/{classGuid}/student/{studentId}/extraProps/{appId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<StudentExtraProperty>>(JsonOptions) ?? [];
    }

    public async Task<StudentExtraProperty?> GetExtraPropertyByNameAndStudentIdInClassAsync(
        string classGuid, int studentId, string appId, string propName)
    {
        var response = await _http.GetAsync($"/class/{classGuid}/student/{studentId}/extraProps/{appId}/{propName}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudentExtraProperty>(JsonOptions);
    }

    public async Task<List<ClassExtraProperty>> GetExtraPropertiesByClassGuidAsync(string classGuid)
    {
        var response = await _http.GetAsync($"/class/{classGuid}/extraProps");
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ClassExtraProperty>>(JsonOptions) ?? [];
    }

    public async Task<List<ClassExtraProperty>> GetExtraPropertiesByAppIdInClassAsync(string classGuid, string appId)
    {
        var response = await _http.GetAsync($"/class/{classGuid}/extraProps/{appId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ClassExtraProperty>>(JsonOptions) ?? [];
    }

    public async Task<ClassExtraProperty?> GetExtraPropertyByAppIdAndNameInClassAsync(
        string classGuid, string appId, string propName)
    {
        var response = await _http.GetAsync($"/class/{classGuid}/extraProps/{appId}/{propName}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClassExtraProperty>(JsonOptions);
    }

    #endregion

    #region POST

    public async Task<Class> CreateClassAsync(CreateClassRequest request)
    {
        var response = await _http.PostAsJsonAsync("/class", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Class>(JsonOptions);
        return result!;
    }

    public async Task<Student> AddStudentAsync(string classGuid, CreateStudentRequest request)
    {
        var response = await _http.PostAsJsonAsync($"/class/{classGuid}/student", request, JsonOptions);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Class {classGuid} not found");
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException(
                $"Student with ID {request.StudentIdInClass} already exists in class {classGuid}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Student>(JsonOptions);
        return result!;
    }

    public async Task<ClassExtraProperty> AddClassExtraPropertyAsync(string classGuid,
        CreateExtraPropertyRequest request)
    {
        var response = await _http.PostAsJsonAsync($"/class/{classGuid}/extraProps", request, JsonOptions);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Class {classGuid} not found");
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException(
                $"Extra property '{request.AppId}/{request.Name}' already exists in class {classGuid}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClassExtraProperty>(JsonOptions);
        return result!;
    }

    public async Task<StudentExtraProperty> AddStudentExtraPropertyAsync(string classGuid, int studentId,
        CreateExtraPropertyRequest request)
    {
        var response =
            await _http.PostAsJsonAsync($"/class/{classGuid}/student/{studentId}/extraProps", request, JsonOptions);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Class {classGuid} or student {studentId} not found");
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException(
                $"Extra property '{request.AppId}/{request.Name}' already exists for student {studentId}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StudentExtraProperty>(JsonOptions);
        return result!;
    }

    #endregion

    #region PUT

    public async Task<Class> UpdateClassAsync(string classGuid, UpdateClassRequest request)
    {
        var response = await _http.PutAsJsonAsync($"/class/{classGuid}", request, JsonOptions);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Class {classGuid} not found");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Class>(JsonOptions);
        return result!;
    }

    public async Task<Student> UpdateStudentAsync(string classGuid, int studentId, UpdateStudentRequest request)
    {
        var response = await _http.PutAsJsonAsync($"/class/{classGuid}/student/{studentId}", request, JsonOptions);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Class {classGuid} or student {studentId} not found");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Student>(JsonOptions);
        return result!;
    }

    public async Task<ClassExtraProperty> UpdateClassExtraPropertyAsync(string classGuid, string appId, string propName,
        UpdateExtraPropertyRequest request)
    {
        var response =
            await _http.PutAsJsonAsync($"/class/{classGuid}/extraProps/{appId}/{propName}", request, JsonOptions);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Extra property '{appId}/{propName}' not found in class {classGuid}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClassExtraProperty>(JsonOptions);
        return result!;
    }

    public async Task<StudentExtraProperty> UpdateStudentExtraPropertyAsync(string classGuid, int studentId,
        string appId, string propName, UpdateExtraPropertyRequest request)
    {
        var response = await _http.PutAsJsonAsync(
            $"/class/{classGuid}/student/{studentId}/extraProps/{appId}/{propName}",
            request, JsonOptions);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException(
                $"Extra property '{appId}/{propName}' not found for student {studentId}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StudentExtraProperty>(JsonOptions);
        return result!;
    }

    #endregion

    #region DELETE

    public async Task DeleteClassAsync(string classGuid)
    {
        var response = await _http.DeleteAsync($"/class/{classGuid}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteStudentAsync(string classGuid, int studentId)
    {
        var response = await _http.DeleteAsync($"/class/{classGuid}/student/{studentId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Class {classGuid} or student {studentId} not found");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteClassExtraPropertyAsync(string classGuid, string appId, string propName)
    {
        var response = await _http.DeleteAsync($"/class/{classGuid}/extraProps/{appId}/{propName}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Extra property '{appId}/{propName}' not found in class {classGuid}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteStudentExtraPropertyAsync(string classGuid, int studentId, string appId, string propName)
    {
        var response = await _http.DeleteAsync($"/class/{classGuid}/student/{studentId}/extraProps/{appId}/{propName}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException(
                $"Extra property '{appId}/{propName}' not found for student {studentId}");
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region OffLine

    public static Class CreateClassOffline(string name)
    {
        return new Class
        {
            Guid = Guid.NewGuid(),
            Name = name,
            Students = [],
            ExtraProperties = []
        };
    }

    public static Student CreateStudentOffline(string name, int studentIdInClass, Gender gender)
    {
        return new Student
        {
            Guid = Guid.NewGuid(),
            Name = name,
            StudentIdInClass = studentIdInClass,
            Gender = gender,
            ExtraProperties = []
        };
    }

    public static ClassExtraProperty CreateClassExtraPropertyOffline(string appId, string name, object? value)
    {
        return new ClassExtraProperty<object>
        {
            Application = new Application { UniqueId = appId },
            Name = name,
            Value = value
        };
    }

    public static StudentExtraProperty CreateStudentExtraPropertyOffline(string appId, string name, object? value)
    {
        return new StudentExtraProperty<object>
        {
            Application = new Application { UniqueId = appId },
            Name = name,
            Value = value
        };
    }

    #endregion
}