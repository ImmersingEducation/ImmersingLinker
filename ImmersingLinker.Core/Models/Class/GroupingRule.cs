namespace ImmersingLinker.Core.Models.Class;

public class GroupingRule
{
    public Guid Guid { get; init; }
    public string Name { get; set; }
    public List<Group> Groups { get; set; } = [];
}

public class Group
{
    public Guid Guid { get; init; }
    public string Name { get; set; }
    public HashSet<Guid> Contains { get; set; } = [];
}

public class GroupingRuleResponse
{
    public Guid Guid { get; init; }
    public string Name { get; set; }
    public List<Group> Groups { get; set; } = [];
    public List<Guid> UnassignedStudentGuids { get; set; } = [];
}