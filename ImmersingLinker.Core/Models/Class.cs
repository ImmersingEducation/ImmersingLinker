namespace ImmersingLinker.Core.Models;

public class Class
{
    public Guid Guid { get; init; }
    public string Name { get; set; }
    public List<Student> Students { get; set; }
    public List<GroupingRule> GroupingRules { get; set; }
    public List<ClassExtraProperty> ExtraProperties { get; set; }
}