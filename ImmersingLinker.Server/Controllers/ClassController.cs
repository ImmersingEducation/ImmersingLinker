using ImmersingLinker.Core.Models;
using ImmersingLinker.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImmersingLinker.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ClassController : ControllerBase
{
    private readonly ClassStorageService _classStorageService;

    public ClassController(ClassStorageService classStorageService)
    {
        _classStorageService = classStorageService;
    }
    
    #region Logic

    public Class? GetClassByGuidLogic(string guidString)
    {
        Guid guid = Guid.Parse(guidString);
        return _classStorageService.GetClass(guid).Result;
    }
    
    #endregion
    
    #region GET
    
    /// <summary>
    /// 获取所有班级
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllClasses()
    {
        List<Class> classes = [];
        foreach (var classInfo in _classStorageService.GetClassInfos().Result)
        {
            var @class = await _classStorageService.GetClass(classInfo.Guid);
            if (@class is not null) classes.Add(@class);
        }

        return Ok(classes);
    }
    
    /// <summary>
    /// 获取所有班级信息
    /// </summary>
    [HttpGet("infos")]
    public async Task<IActionResult> GetAllClassInfos()
    {
        return Ok(await _classStorageService.GetClassInfos());
    }

    /// <summary>
    /// 获取指定班级
    /// </summary>
    /// <param name="guidString">班级 GUID</param>
    [HttpGet("{classGuid}")]
    public async Task<IActionResult> GetClassByGuid(string guidString)
    {
        var @class = GetClassByGuidLogic(guidString);
        if (@class is not null) return Ok(@class);
        return NotFound();
    }

    /// <summary>
    /// 获取指定班级内所有学生
    /// </summary>
    /// <param name="classGuidString">班级 GUID</param>
    [HttpGet("{classGuid}/students")]
    public async Task<IActionResult> GetStudentsByClassGuid(string classGuidString)
    {
        var @class = GetClassByGuidLogic(classGuidString);
        if (@class is not null) return Ok(@class.Students);
        return NotFound();
    }
    
    /// <summary>
    /// 获取指定班级内所有扩展属性
    /// </summary>
    /// <param name="classGuidString">班级 GUID</param>
    [HttpGet("{classGuid}/extraProps")]
    public async Task<IActionResult> GetExtraPropertiesByClassGuid(string classGuidString)
    {
        var @class = GetClassByGuidLogic(classGuidString);
        if (@class is not null) return Ok(@class.ExtraProperties);
        return NotFound();
    }
    
    #endregion
    
    #region POST

    /// <summary>
    /// 创建班级
    /// </summary>
    /// <param name="class">班级</param>
    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] Class @class)
    {
        if (_classStorageService.GetClassInfos().Result.Select(i => i.Guid).FirstOrDefault(i => i == @class.Guid) == Guid.Empty)
        {
            await _classStorageService.SaveClass(@class);
            return Ok(@class);
        }
        else
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// 创建学生
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    /// <param name="students">学生</param>
    [HttpPost("{classGuid}/students")]
    public async Task<IActionResult> UploadStudents(string classGuid, [FromBody] List<Student> students)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();

        @class.Students = students;
        await _classStorageService.SaveClass(@class);
        return Ok(@class.Students);
    }

    /// <summary>
    /// 创建班级扩展属性
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    /// <param name="extraProps">班级扩展属性</param>
    [HttpPost("{classGuid}/extraProps")]
    public async Task<IActionResult> UploadExtraProperties(string classGuid, [FromBody] List<ClassExtraProperty> extraProps)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();

        @class.ExtraProperties = extraProps;
        await _classStorageService.SaveClass(@class);
        return Ok(@class.ExtraProperties);
    }
    
    #endregion
    
    #region PUT
    
    #endregion
}