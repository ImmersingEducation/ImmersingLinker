namespace ImmersingLinker.Core.Models;

public record CreateClassRequest(string Name);
public record UpdateClassRequest(string Name);
public record CreateStudentRequest(string Name, int StudentIdInClass, Gender Gender);
public record UpdateStudentRequest(string Name, Gender Gender, string GroupInClass);
public record CreateExtraPropertyRequest(string AppId, string Name, object? Value);
public record UpdateExtraPropertyRequest(object? Value);
public record CreateGroupingRuleRequest(string Name);
public record UpdateGroupingRuleRequest(string Name);
public record CreateGroupRequest(string Name);
public record UpdateGroupRequest(string Name);
