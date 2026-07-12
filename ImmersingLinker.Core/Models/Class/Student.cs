namespace ImmersingLinker.Core.Models.Class;

public class Student
{
    public Guid Guid { get; init; }
    public string Name { get; set; }
    public int StudentIdInClass { get; set; }
    public Gender Gender { get; set; }
    public List<StudentExtraProperty> ExtraProperties { get; set; }
}

public enum Gender
{
    Male,
    Female
}