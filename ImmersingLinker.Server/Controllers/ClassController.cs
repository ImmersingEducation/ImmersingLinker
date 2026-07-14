using ImmersingLinker.Core.Models;
using ImmersingLinker.Core.Models.Class;
using ImmersingLinker.Services.Services;
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

    public Guid? ParseGuidFromString(string guidString)
    {
        try
        {
            return Guid.Parse(guidString);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public Class? GetClassByGuidLogic(string guidString)
    {
        var guid = ParseGuidFromString(guidString);
        if (guid is null) return null;
        return _classStorageService.GetClass(guid.Value).Result;
    }

    private GroupingRuleResponse BuildGroupingRuleResponse(Class @class, GroupingRule rule)
    {
        var assignedStudentGuids = rule.Groups.SelectMany(g => g.Contains).ToHashSet();
        var unassigned = @class.Students
            .Where(s => !assignedStudentGuids.Contains(s.Guid))
            .Select(s => s.Guid)
            .ToList();
        return new GroupingRuleResponse
        {
            Guid = rule.Guid,
            Name = rule.Name,
            Groups = rule.Groups,
            UnassignedStudentGuids = unassigned
        };
    }

    #endregion

    #region GET

    /// <summary>
    ///     获取所有班级
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
    ///     获取所有班级信息
    /// </summary>
    [HttpGet("infos")]
    public async Task<IActionResult> GetAllClassInfos()
    {
        return Ok(await _classStorageService.GetClassInfos());
    }

    /// <summary>
    ///     获取指定班级
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    [HttpGet("{classGuid}")]
    public async Task<IActionResult> GetClassByGuid(string classGuid)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is not null) return Ok(@class);
        return NotFound();
    }

    /// <summary>
    ///     获取指定班级内所有学生
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    [HttpGet("{classGuid}/student")]
    public async Task<IActionResult> GetStudentsByClassGuid(string classGuid)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is not null) return Ok(@class.Students);
        return NotFound();
    }

    /// <summary>
    ///     获取指定班级内指定学生
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    /// <param name="studentId">学生学号</param>
    [HttpGet("{classGuid}/student/{studentId}")]
    public async Task<IActionResult> GetStudentByStudentIdInClass(string classGuid, int studentId)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is not null)
        {
            var student = @class.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
            if (student is not null) return Ok(student);
        }

        return NotFound();
    }

    /// <summary>
    ///     获取指定班级内指定学生的所有扩展属性
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    /// <param name="studentId">学生学号</param>
    [HttpGet("{classGuid}/student/{studentId}/extraProps")]
    public async Task<IActionResult> GetExtraPropertiesByStudentIdInClass(string classGuid, int studentId)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var student = @class?.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
        if (student != null) return Ok(student.ExtraProperties);
        return NotFound();
    }

    /// <summary>
    ///     获取指定班级内指定学生的指定 App 的所有扩展属性
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    /// <param name="studentId">学生学号</param>
    /// <param name="appId">App ID</param>
    [HttpGet("{classGuid}/student/{studentId}/extraProps/{appId}")]
    public async Task<IActionResult> GetExtraPropertiesByStudentIdAndAppIdInClass(string classGuid, int studentId,
        string appId)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var student = @class?.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
        if (student != null) return Ok(student.ExtraProperties.Where(p => p.Application.UniqueId == appId).ToList());
        return NotFound();
    }

    [HttpGet("{classGuid}/student/{studentId}/extraProps/{appId}/{propName}")]
    public async Task<IActionResult> GetExtraPropertyByNameAndStudentIdInClass(string classGuid, int studentId,
        string appId, string propName)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var student = @class?.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
        var prop = student?.ExtraProperties.FirstOrDefault(p => p.Application.UniqueId == appId && p.Name == propName);
        if (prop != null) return Ok(prop);
        return NotFound();
    }

    /// <summary>
    ///     获取指定班级内所有扩展属性
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    [HttpGet("{classGuid}/extraProps")]
    public async Task<IActionResult> GetExtraPropertiesByClassGuid(string classGuid)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is not null) return Ok(@class.ExtraProperties);
        return NotFound();
    }

    /// <summary>
    ///     获取指定班级内指定 App 的所有扩展属性
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    /// <param name="appId">App ID</param>
    [HttpGet("{classGuid}/extraProps/{appId}")]
    public async Task<IActionResult> GetExtraPropertiesByAppIdInClass(string classGuid, string appId)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is not null) return Ok(@class.ExtraProperties.Where(p => p.Application.UniqueId == appId).ToList());
        return NotFound();
    }

    /// <summary>
    ///     获取指定班级内指定扩展属性
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    /// <param name="appId">App ID</param>
    /// <param name="propName">属性名称</param>
    [HttpGet("{classGuid}/extraProps/{appId}/{propName}")]
    public async Task<IActionResult> GetExtraPropertyByAppIdAndNameInClass(string classGuid, string appId,
        string propName)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var prop = @class?.ExtraProperties.FirstOrDefault(p => p.Application.UniqueId == appId && p.Name == propName);
        if (prop != null) return Ok(prop);
        return NotFound();
    }

    /// <summary>
    ///     获取指定班级内所有分组规则
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    [HttpGet("{classGuid}/groupingRule")]
    public async Task<IActionResult> GetGroupingRules(string classGuid)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        var rules = (@class.GroupingRules ?? [])
            .Select(r => BuildGroupingRuleResponse(@class, r))
            .ToList();
        return Ok(rules);
    }

    /// <summary>
    ///     获取指定班级内指定分组规则
    /// </summary>
    /// <param name="classGuid">班级 GUID</param>
    /// <param name="ruleGuid">分组规则 GUID</param>
    [HttpGet("{classGuid}/groupingRule/{ruleGuid}")]
    public async Task<IActionResult> GetGroupingRule(string classGuid, string ruleGuid)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        var parsed = ParseGuidFromString(ruleGuid);
        if (parsed is null) return BadRequest("Invalid GUID format");
        var rule = (@class.GroupingRules ?? []).FirstOrDefault(r => r.Guid == parsed.Value);
        if (rule is null) return NotFound();
        return Ok(BuildGroupingRuleResponse(@class, rule));
    }

    #endregion

    #region POST

    /// <summary>
    ///     创建班级
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
    {
        var @class = new Class
        {
            Guid = Guid.NewGuid(),
            Name = request.Name,
            Students = [],
            ExtraProperties = [],
            GroupingRules = []
        };
        await _classStorageService.SaveClass(@class);
        return CreatedAtAction(nameof(GetClassByGuid), new { classGuid = @class.Guid }, @class);
    }

    /// <summary>
    ///     添加学生
    /// </summary>
    [HttpPost("{classGuid}/student")]
    public async Task<IActionResult> AddStudent(string classGuid, [FromBody] CreateStudentRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();

        if (@class.Students.Any(s => s.StudentIdInClass == request.StudentIdInClass))
            return Conflict($"Student with StudentIdInClass {request.StudentIdInClass} already exists");

        var student = new Student
        {
            Guid = Guid.NewGuid(),
            Name = request.Name,
            StudentIdInClass = request.StudentIdInClass,
            Gender = request.Gender,
            ExtraProperties = []
        };
        @class.Students.Add(student);
        await _classStorageService.SaveClass(@class);
        return CreatedAtAction(nameof(GetStudentByStudentIdInClass),
            new { classGuid, studentId = student.StudentIdInClass }, student);
    }

    /// <summary>
    ///     添加班级扩展属性
    /// </summary>
    [HttpPost("{classGuid}/extraProps")]
    public async Task<IActionResult> AddClassExtraProperty(string classGuid,
        [FromBody] CreateExtraPropertyRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();

        if (@class.ExtraProperties.Any(p => p.Application.UniqueId == request.AppId && p.Name == request.Name))
            return Conflict($"Extra property '{request.AppId}/{request.Name}' already exists");

        var prop = new ClassExtraProperty<object>
        {
            Application = new Application { UniqueId = request.AppId },
            Name = request.Name
        };
        @class.ExtraProperties.Add(prop);
        await _classStorageService.SaveClass(@class);
        return CreatedAtAction(nameof(GetExtraPropertyByAppIdAndNameInClass),
            new { classGuid, appId = request.AppId, propName = request.Name }, prop);
    }

    /// <summary>
    ///     添加学生扩展属性
    /// </summary>
    [HttpPost("{classGuid}/student/{studentId}/extraProps")]
    public async Task<IActionResult> AddStudentExtraProperty(string classGuid, int studentId,
        [FromBody] CreateExtraPropertyRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var student = @class?.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
        if (student is null) return NotFound();

        if (student.ExtraProperties.Any(p => p.Application.UniqueId == request.AppId && p.Name == request.Name))
            return Conflict($"Extra property '{request.AppId}/{request.Name}' already exists for this student");

        var prop = new StudentExtraProperty<object>
        {
            Application = new Application { UniqueId = request.AppId },
            Name = request.Name
        };
        student.ExtraProperties.Add(prop);
        await _classStorageService.SaveClass(@class);
        return CreatedAtAction(nameof(GetExtraPropertyByNameAndStudentIdInClass),
            new { classGuid, studentId, appId = request.AppId, propName = request.Name }, prop);
    }

    /// <summary>
    ///     添加分组规则
    /// </summary>
    [HttpPost("{classGuid}/groupingRules")]
    public async Task<IActionResult> AddGroupingRule(string classGuid, [FromBody] CreateGroupingRuleRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        @class.GroupingRules ??= [];
        var rule = new GroupingRule
        {
            Guid = Guid.NewGuid(),
            Name = request.Name
        };
        @class.GroupingRules.Add(rule);
        await _classStorageService.SaveClass(@class);
        return CreatedAtAction(nameof(GetGroupingRule), new { classGuid, ruleGuid = rule.Guid.ToString() },
            BuildGroupingRuleResponse(@class, rule));
    }

    /// <summary>
    ///     在指定分组规则中添加小组
    /// </summary>
    [HttpPost("{classGuid}/groupingRules/{ruleGuid}")]
    public async Task<IActionResult> AddGroup(string classGuid, string ruleGuid, [FromBody] CreateGroupRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        var parsed = ParseGuidFromString(ruleGuid);
        if (parsed is null) return BadRequest("Invalid GUID format");
        var rule = (@class.GroupingRules ?? []).FirstOrDefault(r => r.Guid == parsed.Value);
        if (rule is null) return NotFound();
        var group = new Group
        {
            Guid = Guid.NewGuid(),
            Name = request.Name
        };
        rule.Groups.Add(group);
        await _classStorageService.SaveClass(@class);
        return CreatedAtAction(nameof(GetGroupingRule), new { classGuid, ruleGuid = rule.Guid.ToString() },
            BuildGroupingRuleResponse(@class, rule));
    }

    #endregion

    #region PUT

    /// <summary>
    ///     更新班级名称
    /// </summary>
    [HttpPut("{classGuid}")]
    public async Task<IActionResult> UpdateClass(string classGuid, [FromBody] UpdateClassRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        @class.Name = request.Name;
        await _classStorageService.SaveClass(@class);
        return Ok(@class);
    }

    /// <summary>
    ///     更新学生信息
    /// </summary>
    [HttpPut("{classGuid}/student/{studentId}")]
    public async Task<IActionResult> UpdateStudent(string classGuid, int studentId,
        [FromBody] UpdateStudentRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var student = @class?.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
        if (student is null) return NotFound();
        student.Name = request.Name;
        student.Gender = request.Gender;
        await _classStorageService.SaveClass(@class);
        return Ok(student);
    }

    /// <summary>
    ///     更新班级扩展属性值
    /// </summary>
    [HttpPut("{classGuid}/extraProps/{appId}/{propName}")]
    public async Task<IActionResult> UpdateClassExtraProperty(string classGuid, string appId, string propName,
        [FromBody] UpdateExtraPropertyRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var prop = @class?.ExtraProperties.FirstOrDefault(p => p.Application.UniqueId == appId && p.Name == propName);
        if (prop is null) return NotFound();
        prop.Value = request.Value;
        await _classStorageService.SaveClass(@class);
        return Ok(prop);
    }

    /// <summary>
    ///     更新学生扩展属性值
    /// </summary>
    [HttpPut("{classGuid}/student/{studentId}/extraProps/{appId}/{propName}")]
    public async Task<IActionResult> UpdateStudentExtraProperty(string classGuid, int studentId, string appId,
        string propName, [FromBody] UpdateExtraPropertyRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var student = @class?.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
        var prop = student?.ExtraProperties.FirstOrDefault(p => p.Application.UniqueId == appId && p.Name == propName);
        if (prop is null) return NotFound();
        prop.Value = request.Value;
        await _classStorageService.SaveClass(@class);
        return Ok(prop);
    }

    /// <summary>
    ///     修改分组规则名称
    /// </summary>
    [HttpPut("{classGuid}/groupingRules/{ruleGuid}")]
    public async Task<IActionResult> UpdateGroupingRule(string classGuid, string ruleGuid,
        [FromBody] UpdateGroupingRuleRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        var parsed = ParseGuidFromString(ruleGuid);
        if (parsed is null) return BadRequest("Invalid GUID format");
        var rule = (@class.GroupingRules ?? []).FirstOrDefault(r => r.Guid == parsed.Value);
        if (rule is null) return NotFound();
        rule.Name = request.Name;
        await _classStorageService.SaveClass(@class);
        return Ok(BuildGroupingRuleResponse(@class, rule));
    }

    /// <summary>
    ///     修改指定小组的名称
    /// </summary>
    [HttpPut("{classGuid}/groupingRules/{ruleGuid}/{groupGuid}")]
    public async Task<IActionResult> UpdateGroup(string classGuid, string ruleGuid, string groupGuid,
        [FromBody] UpdateGroupRequest request)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        var ruleParsed = ParseGuidFromString(ruleGuid);
        if (ruleParsed is null) return BadRequest("Invalid GUID format");
        var groupParsed = ParseGuidFromString(groupGuid);
        if (groupParsed is null) return BadRequest("Invalid GUID format");
        var rule = (@class.GroupingRules ?? []).FirstOrDefault(r => r.Guid == ruleParsed.Value);
        if (rule is null) return NotFound();
        var group = rule.Groups.FirstOrDefault(g => g.Guid == groupParsed.Value);
        if (group is null) return NotFound();
        group.Name = request.Name;
        await _classStorageService.SaveClass(@class);
        return Ok(BuildGroupingRuleResponse(@class, rule));
    }

    #endregion

    #region DELETE

    /// <summary>
    ///     删除班级
    /// </summary>
    [HttpDelete("{classGuid}")]
    public IActionResult DeleteClass(string classGuid)
    {
        var guid = ParseGuidFromString(classGuid);
        if (guid is null) return BadRequest("Invalid GUID format");
        _classStorageService.DeleteClass(guid.Value);
        return NoContent();
    }

    /// <summary>
    ///     删除学生
    /// </summary>
    [HttpDelete("{classGuid}/student/{studentId}")]
    public async Task<IActionResult> DeleteStudent(string classGuid, int studentId)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var student = @class?.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
        if (student is null) return NotFound();
        @class.Students.Remove(student);
        await _classStorageService.SaveClass(@class);
        return NoContent();
    }

    /// <summary>
    ///     删除班级扩展属性
    /// </summary>
    [HttpDelete("{classGuid}/extraProps/{appId}/{propName}")]
    public async Task<IActionResult> DeleteClassExtraProperty(string classGuid, string appId, string propName)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var prop = @class?.ExtraProperties.FirstOrDefault(p => p.Application.UniqueId == appId && p.Name == propName);
        if (prop is null) return NotFound();
        @class.ExtraProperties.Remove(prop);
        await _classStorageService.SaveClass(@class);
        return NoContent();
    }

    /// <summary>
    ///     删除学生扩展属性
    /// </summary>
    [HttpDelete("{classGuid}/student/{studentId}/extraProps/{appId}/{propName}")]
    public async Task<IActionResult> DeleteStudentExtraProperty(string classGuid, int studentId, string appId,
        string propName)
    {
        var @class = GetClassByGuidLogic(classGuid);
        var student = @class?.Students.FirstOrDefault(s => s.StudentIdInClass == studentId);
        var prop = student?.ExtraProperties.FirstOrDefault(p => p.Application.UniqueId == appId && p.Name == propName);
        if (prop is null) return NotFound();
        student.ExtraProperties.Remove(prop);
        await _classStorageService.SaveClass(@class);
        return NoContent();
    }

    /// <summary>
    ///     删除分组规则
    /// </summary>
    [HttpDelete("{classGuid}/groupingRules/{ruleGuid}")]
    public async Task<IActionResult> DeleteGroupingRule(string classGuid, string ruleGuid)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        var parsed = ParseGuidFromString(ruleGuid);
        if (parsed is null) return BadRequest("Invalid GUID format");
        var rule = (@class.GroupingRules ?? []).FirstOrDefault(r => r.Guid == parsed.Value);
        if (rule is null) return NotFound();
        @class.GroupingRules.Remove(rule);
        await _classStorageService.SaveClass(@class);
        return NoContent();
    }

    /// <summary>
    ///     删除指定分组规则中的小组
    /// </summary>
    [HttpDelete("{classGuid}/groupingRules/{ruleGuid}/{groupGuid}")]
    public async Task<IActionResult> DeleteGroup(string classGuid, string ruleGuid, string groupGuid)
    {
        var @class = GetClassByGuidLogic(classGuid);
        if (@class is null) return NotFound();
        var ruleParsed = ParseGuidFromString(ruleGuid);
        if (ruleParsed is null) return BadRequest("Invalid GUID format");
        var groupParsed = ParseGuidFromString(groupGuid);
        if (groupParsed is null) return BadRequest("Invalid GUID format");
        var rule = (@class.GroupingRules ?? []).FirstOrDefault(r => r.Guid == ruleParsed.Value);
        if (rule is null) return NotFound();
        var group = rule.Groups.FirstOrDefault(g => g.Guid == groupParsed.Value);
        if (group is null) return NotFound();
        rule.Groups.Remove(group);
        await _classStorageService.SaveClass(@class);
        return NoContent();
    }

    #endregion
}